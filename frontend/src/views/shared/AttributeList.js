import React, { useState, useEffect, useRef } from 'react'
import Form from 'react-bootstrap/Form'
import InputGroup from 'react-bootstrap/InputGroup'
import FormControl from 'react-bootstrap/FormControl'
import Button from 'react-bootstrap/Button'

import axiosInstance from "../../services/AxiosService";

import { SVGIcon } from '../../components/SVGIcon'
import color from '../../components/Constants'

import { generateLogMessageString, pageDataRows, convertToNumeric, toInt, onChangeNumericKeysOnly } from '../../utils/UtilityService'
import {
    getAttributesPreferences, setAttributesPageSize, attributeNew, validate_All, validate_nameDuplicate,
    validate_name, onChangeDataTypeShared, onChangeAttributeTypeShared, validate_attributeType, validate_enumValueDuplicate, validate_enumValueNumeric, onChangeInterfaceShared, onChangeCompositionShared, renderDataTypeUIShared
} from '../../services/AttributesService';
import AttributeItemRow from './AttributeItemRow';
import AttributeSlideOut from './AttributeSlideOut';
import GridPager from '../../components/GridPager'
import { AppSettings } from '../../utils/appsettings';
import { useLoadingContext } from '../../components/contexts/LoadingContext'

const CLASS_NAME = "AttributeList";

function AttributeList(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    //merge together two attributes collections
    const _preferences = getAttributesPreferences();
    const _dataAttrTypeDdlRef = useRef(null);
    var _allAttributes = mergeAttributesCollections(props.profileAttributes, props.extendedProfileAttributes);
    const [_filterVal, setFilterVal] = useState(''); //props.searchValue
    const [_dataRows, setDataRows] = useState({
        all: _allAttributes.all, filtered: _allAttributes.filtered, paged: _allAttributes.paged,
        pager: { currentPage: 1, pageSize: _preferences.pageSize, itemCount: _allAttributes.filtered.length }
    });
    const [_addItem, setAdd] = useState(JSON.parse(JSON.stringify(attributeNew)));
    const [_lookupCompositions, setLookupCompositions] = useState([]);
    const [_lookupStructures, setLookupStructures] = useState([]);
    const [_lookupEngUnits, setLookupEngUnits] = useState([]);
    //all interfaces is our list from the server, current list is the ones not yet chosen
    const [_lookupInterfaces, setLookupInterfaces] = useState({ all: [], current: [] });
    const [_lookupDataTypes, setLookupDataTypes] = useState([]);
    const [_lookupAttributeTypes, setLookupAttributeTypes] = useState([]);
    const [_addSettings, setAddSettings] = useState({
        useMinMax: true, useEngUnit: true, showComposition: false, /*showStructure: false,*/ showInterface: false, showEnumeration: false,
        isCustomDataType: false, showDescription: true
    });
    const [_isValid, setIsValid] = useState({
        name: true,
        nameDuplicate: true,
        dataType: true,
        attributeType: true,
        composition: true,
        structure: true,
        interface: true,
        minMax: true,
        minIsNumeric: true,
        maxIsNumeric: true,
        engUnit: true,
        enumValue: true,
        enumValueDuplicate: true
    });
    const [_slideOut, SetSlideOut] = useState({ isOpen: false, item: null, showDetail: false, readOnly: true });
    const { loadingProps, setLoadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Region: init methods
    //-------------------------------------------------------------------
    function mergeAttributesCollections(profileAttributes, extendedProfileAttributes) {
        //add a field indicator so we can distinguish between attribute types in display and code
        if (profileAttributes != null) {
            profileAttributes.forEach((a) => {
                if (a._itemType == null)
                    a._itemType = 'profile';
            });
        }
        if (extendedProfileAttributes != null) {
            extendedProfileAttributes.forEach((a) => {
                if (a._itemType == null)
                    a._itemType = 'extended';
            });
        }

        //merge together two attributes collections, sort enum val (if present) then name
        var result = (profileAttributes == null ? [] : profileAttributes).concat(extendedProfileAttributes == null ? [] : extendedProfileAttributes)
        result.sort((a, b) => {
            var enumValA = a.enumValue == null ? 999999 : a.enumValue;
            var enumValB = b.enumValue == null ? 999999 : b.enumValue;
            if (enumValA < enumValB) {
                return -1;
            }
            if (enumValA > enumValB) {
                return 1;
            }
            //if we get here, they enum a and b are equal so we sort by name
            if (a.name.toLowerCase() < b.name.toLowerCase()) {
                return -1;
            }
            if (a.name.toLowerCase() > b.name.toLowerCase()) {
                return 1;
            }
            return 0;
        }); //sort by name

        //page data
        var pagedData = pageDataRows(result, 1, _preferences.pageSize); 

        return { all: result, filtered: result, paged: pagedData };
    }

    //-------------------------------------------------------------------
    // Region: Hooks - When composition data type is chosen, go get a list of profiles
    //      where the profile is neither a descendant or a parent/grandparent, etc. of the profile we 
    //      are working with
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchLookupProfileTypeDefs() {

            //Filter out anything
            //where the profile is neither a descendant or a parent/grandparent, etc. of the profile we 
            //are working with, can't be a dependency of this profile
            // If we are working with a profile, then composition can't be an interface type
            // If we are working with an interface, then composition can't be a profile type
            var data = { id: props.typeDefinition.id };
            var url = `profiletypedefinition/lookup/profilerelated`; //profiles only
            //this is an extend scenario
            if ((props.typeDefinition.id == null || props.typeDefinition.id === 0) && props.typeDefinition.parent != null) {
                data = { id: props.typeDefinition.parent.id };
                url = `profiletypedefinition/lookup/profilerelated/extend`; //profiles only
            }
            console.log(generateLogMessageString(`useEffect||fetchLookupProfileTypeDefs||${url}`, CLASS_NAME));
            //const result = await axiosInstance.post(url, data);

            await axiosInstance.post(url, data).then(result => {
                if (result.status === 200) {
                    //profile id - 3 scenarios - 1. typical - use profile id, 2. extend profile where parent profile should be used, 
                    //      3. new profile - no parent, no inheritance, use 0 
                    //var pId = props.typeDefinition.id;
                    //if (props.typeDefinition.id === 0 && props.typeDefinition.parent != null) pId = props.typeDefinition.parent.id;

                    //TBD - handle paged data scenario, do a predictive search look up
                    setLookupCompositions(result.data.compositions); //also updates state
                    //Pull interfaces from back end
                    setLookupInterfaces({ all: result.data.interfaces, current: result.data.interfaces });
                } else {
                    console.warn(generateLogMessageString(`useEffect||fetchLookupProfileTypeDefs||error||status:${result.status}`, CLASS_NAME));
                }
            }).catch(e => {
                if (e.response && e.response.status === 401) {
                    console.error(generateLogMessageString(`useEffect||fetchLookupProfileTypeDefs||error||status:${e.response.status}`, CLASS_NAME));
                }
                else {
                    console.error(generateLogMessageString(`useEffect||fetchLookupProfileTypeDefs||error||status:${e.response && e.response.data ? e.response.data : `A system error has occurred during the profile api call.`}`, CLASS_NAME));
                    console.log(e);
                }
            });
        }

        fetchLookupProfileTypeDefs();

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [props.typeDefinition?.id]);

    //-------------------------------------------------------------------
    // Region: Hooks - load lookup data static from context. if not present, trigger a fetch of this data. 
    //-------------------------------------------------------------------
    useEffect(() => {
        async function initLookupData() {

            //if data not there, but loading in progress.  
            if (loadingProps.lookupDataStatic == null && loadingProps.refreshLookupData) {
                //do nothing, the loading effect will inform when complete
                return;
            }
            //if data not there, but loading NOT in progress.  
            else if (loadingProps.lookupDataStatic == null) {
                //trigger get of data
                setLoadingProps({ refreshLookupData: true });
                return;
            }

            //get from local storage and keep local to the component lifecycle
            setLookupDataTypes(loadingProps.lookupDataStatic.dataTypes);
            setLookupStructures(loadingProps.lookupDataStatic.structures); //also updates state
            setLookupEngUnits(loadingProps.lookupDataStatic.engUnits); //also updates state

        }

        initLookupData();

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [loadingProps.lookupDataStatic, loadingProps.lookupDataRefreshed]);

    //-------------------------------------------------------------------
    // Populate attribute type based on profile type
    //-------------------------------------------------------------------
    useEffect(() => {

        //if data not there, but loading in progress.  
        if (loadingProps.lookupDataStatic == null && loadingProps.refreshLookupData) {
            //do nothing, the loading effect will inform when complete
            return;
        }
        //if data not there, but loading NOT in progress.  
        else if (loadingProps.lookupDataStatic == null) {
            //trigger get of data
            setLoadingProps({ refreshLookupData: true });
            return;
        }

        //if no profile type is chosen, we can't know how to add attribute.
        if (props.typeDefinition?.typeId == null || props.typeDefinition?.typeId.toString() === "-1") {
            setLookupAttributeTypes([]);
        }
        //only show enumeration for the enumeration type
        else if (props.typeDefinition?.typeId === AppSettings.ProfileTypeDefaults.EnumerationId) {
            var matches = loadingProps.lookupDataStatic.attributeTypes.filter(x => x.id === AppSettings.AttributeTypeDefaults.EnumerationId);
            setLookupAttributeTypes(matches);
        }
        //don't show enumeration or interface if we are editing an interface type def
        else if (props.typeDefinition?.typeId === AppSettings.ProfileTypeDefaults.InterfaceId) {
            var matches1 = loadingProps.lookupDataStatic.attributeTypes.filter(x =>
                x.id !== AppSettings.AttributeTypeDefaults.EnumerationId
                && x.id !== AppSettings.AttributeTypeDefaults.InterfaceId);
            setLookupAttributeTypes(matches1);
        }
        //only show structure field for structure profile
        else if (props.typeDefinition?.typeId === AppSettings.ProfileTypeDefaults.StructureId) {
            var matches2 = loadingProps.lookupDataStatic.attributeTypes.filter(x => x.id === AppSettings.AttributeTypeDefaults.StructureId);
            setLookupAttributeTypes(matches2);
        }
        //don't show enumeration, structure if we are editing a non-enum type def
        else {
            var matches3 = loadingProps.lookupDataStatic.attributeTypes.filter(x =>
                x.id !== AppSettings.AttributeTypeDefaults.EnumerationId &&
                x.id !== AppSettings.AttributeTypeDefaults.StructureId);
            setLookupAttributeTypes(matches3);
        }

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [props.typeDefinition?.typeId, loadingProps.lookupDataRefreshed]);


    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------
    const validateForm_name = (e) => {
        var isValid = validate_name(e.target.value, _addItem);
        //dup check
        var isValidDup = validate_nameDuplicate(e.target.value, _addItem, _allAttributes.all);
        setIsValid({ ..._isValid, name: isValid, nameDuplicate: isValidDup });
    };

    //const validateForm_dataType = (e) => {
    //    var dataType = _lookupDataTypes.find(dt => { return dt.id === parseInt(e.target.value); });
    //    setIsValid({ ..._isValid, dataType: validate_dataType(dataType) });
    //};

    const validateForm_attributeType = (e) => {
        setIsValid({ ..._isValid, attributeType: validate_attributeType(_addItem.dataType, e.target.value) });
    };

    const validateForm_composition = (e) => {
        var isValid = e.target.value.toString() !== "-1" || parseInt(_addItem.attributeType.id) !== AppSettings.AttributeTypeDefaults.CompositionId;
        setIsValid({ ..._isValid, composition: isValid });
    };

    //const validateForm_structure = (e) => {
    //    var isValid = e.target.value.toString() !== "-1" || parseInt(_addItem.attributeType.id) !== AppSettings.AttributeTypeDefaults.StructureId;
    //    setIsValid({ ..._isValid, structure: isValid });
    //};

    const validateForm_interface = (e) => {
        var isValid = e.target.value.toString() !== "-1" || parseInt(_addItem.attributeType.id) !== AppSettings.AttributeTypeDefaults.InterfaceId;
        setIsValid({ ..._isValid, interface: isValid });
    };

    const validateForm_enumValue = (e) => {
        //dup check
        var isValidDup = validate_enumValueDuplicate(e.target.value, _addItem, _allAttributes.all);
        //check for valid integer - is numeric and is positive
        var isValidValue = validate_enumValueNumeric(e.target.value, _addItem);
        setIsValid({ ..._isValid, enumValue: isValidValue, enumValueDuplicate: isValidDup });
    }

    //validate all - call from button click
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        var isValid = validate_All(_addItem, _addSettings, _allAttributes.all);
        isValid.composition = (_addItem.composition != null && _addItem.compositionId > 0) || _addItem.attributeType.id !== AppSettings.AttributeTypeDefaults.CompositionId;
        isValid.interface = (_addItem.interface != null && _addItem.interfaceId > 0) || _addItem.attributeType.id !== AppSettings.AttributeTypeDefaults.InterfaceId;

        setIsValid(JSON.parse(JSON.stringify(isValid)));
        return (isValid.name && isValid.nameDuplicate && isValid.dataType && isValid.attributeType
            && isValid.composition && isValid.structure && isValid.interface && _isValid.enumValue && _isValid.enumValueDuplicate);
    }

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    ////update state for when search click happens
    const onSearchBlur = (e) => {
        //console.log(generateLogMessageString(`onSearchBlur||Search value: ${e.target.value}`, CLASS_NAME));
        setFilterVal(e.target.value);
    }

    const onSearchClick = () => {
        console.log(generateLogMessageString('onSearchClick||Search value: ' + _filterVal, CLASS_NAME));
        //if (filterVal === val) return;

        //filter the data, update the state
        var filteredData = filterDataRows(_dataRows.all, _filterVal);
        //setFilterVal(val); //update state
        //setDataRowsFiltered(filteredData); //filter rows state updated to matches

        //page the filtered data and update the paged data state
        var pagedData = pageDataRows(filteredData, 1, _dataRows.pager.pageSize);
        //updatePagerState(pagedData, 1, _pager.pageSize, filteredData == null ? 0 : filteredData.length);
        //update state - several items w/in keep their existing vals
        setDataRows({
            all: _dataRows.all, filtered: filteredData, paged: pagedData,
            pager: { currentPage: 1, pageSize: _dataRows.pager.pageSize, itemCount: filteredData == null ? 0 : filteredData.length }
        });
    };

    const onChangePage = (currentPage, pageSize) => {
        console.log(generateLogMessageString(`onChangePage||Current Page: ${currentPage}, Page Size: ${pageSize}`, CLASS_NAME));
        var pagedData = pageDataRows(_dataRows.filtered, currentPage, pageSize);
        //update state - several items w/in keep their existing vals
        setDataRows({
            all: _dataRows.all, filtered: _dataRows.filtered, paged: pagedData,
            pager: { currentPage: currentPage, pageSize: pageSize, itemCount: _dataRows.filtered == null ? 0 : _dataRows.filtered.length }
        });

        //preserve choice in local storage
        setAttributesPageSize(pageSize);
    };

    const onAdd = () => {
        //raised from add button click
        console.log(generateLogMessageString(`onAdd`, CLASS_NAME));

        //validate form
        if (!validateForm()) {
            //alert("validation failed");
            return;
        }

        //we need to be aware of newly added rows and those will be signified by a negative -id. 
        //Once saved server side, these will be issued ids from db.
        //Depending on how we are adding (single row or multiple rows), the id generation will be different. Both need 
        //a starting point negative id
        var attributeIdInit = (-1) * (_allAttributes == null || _allAttributes.all == null ? 1 : _allAttributes.all.length + 1);

        //adding non-interface attribute
        if (_addItem.interfaceId === -1) {
            _addItem.id = attributeIdInit;
            _addItem.typeDefinitionId = props.typeDefinition.id; //set parent val

            //for certain types, clear out engunit, min max, etc. 
            if (_addSettings.showComposition || _addSettings.showStructure || _addSettings.showInterface || _addSettings.isCustomDataType
                || !_addSettings.useMinMax || !_addSettings.useEngUnit) {
                _addItem.minValue = null;
                _addItem.maxValue = null;
                _addItem.engUnit = null;
            }
            //convert min, max to number
            else if (_addSettings.useMinMax) {
                _addItem.minValue = convertToNumeric(_addItem.dataType, _addItem.minValue);
                _addItem.maxValue = convertToNumeric(_addItem.dataType, _addItem.maxValue);
            }

            //call parent to add to items collection, update state
            var attributes = props.onAttributeAdd(_addItem);

            //after parent adds, update this component's state
            onAddUpdateState(attributes);
        }
        //adding interface attribute(s)
        else {
            onAttributeAddInterface(_addItem.interfaceId);
        }

    };


    const onAttributeAddInterface = (id) => {
        //raised from add button click
        console.log(generateLogMessageString(`onAttributeAddInterface||${id}`, CLASS_NAME));

        if (props.typeDefinition.extendedProfileAttributes == null) props.typeDefinition.extendedProfileAttributes = [];

        //go get the interface profile to retrieve its attributes.
        var data = { id: _addItem.interface.id };
        axiosInstance.post(`profiletypedefinition/getbyid`, data).then(result => {
            if (result.status === 200) {
                var interfaceGroupId = Math.floor(Math.random() * 30);
                var iface = result.data;
                //create a new combined collection of the attributes to be added, update some vals and then bubble up
                //add both the interface's attributes and its extended attributes into one collection and add em all
                var interfaceAttrItems = iface.profileAttributes.concat(iface.extendedProfileAttributes);
                interfaceAttrItems.forEach((attrib, counter) => {
                    //assign the interface obj and id for downstream usage
                    attrib.interface = { id: iface.id, name: iface.name };
                    attrib.innterfaceGroupId = interfaceGroupId;

                    //TBD - how do we avoid name collision for 2 diff interfaces which have same attribute names.

                    //if attribute already exists in current profile, then rename it so we avoid a name duplication
                    var match = props.typeDefinition.profileAttributes.find((a) => { return a.id === attrib.id && a.name.toLowerCase() === attrib.name.toLowerCase() });
                    if (match != null) {
                        //TBD - account for scenario where there is already a duplicate(1)
                        match.name = `${match.name}(1)`;
                    }

                    var matchEx = props.typeDefinition.extendedProfileAttributes.find((a) => { return a.id === attrib.id && a.name.toLowerCase() === attrib.name.toLowerCase() });
                    if (matchEx != null) {
                        //TBD - what to do here?
                    }

                    attrib.interfaceGroupId = interfaceGroupId;

                    //add attr as an extended attr so we can't edit
                    props.typeDefinition.extendedProfileAttributes.push(JSON.parse(JSON.stringify(attrib)));
                });

                //call parent to add to items collection, update state
                var attributes = props.onAttributeInterfaceAdd(iface, props.typeDefinition.profileAttributes, props.typeDefinition.extendedProfileAttributes);

                //if adding interface, remove the selected item from the list
                setLookupInterfaces({
                    ..._lookupInterfaces,
                    current: _lookupInterfaces.current.filter(p => { return p.id !== _addItem.interface.id })
                });

                //after parent adds, update this component's state
                onAddUpdateState(attributes, true);
            }
            else if (result.status === 404) {
                var msg = 'An error occurred retrieving the interface attributes. The interface was not found.';
                console.log(generateLogMessageString(`onAttributeAdd||Interface||error||${msg}`, CLASS_NAME, 'error'));
                setLoadingProps({ ...loadingProps, isLoading: false, message: msg });
            } else {
                var msg2 = 'An error occurred retrieving the interface attributes. ';
                console.log(generateLogMessageString(`onAttributeAdd||Interface||error||${msg2}`, CLASS_NAME, 'error'));
                setLoadingProps({ ...loadingProps, isLoading: false, message: msg2 });
            }
        }).catch(e => {
            var msg = JSON.stringify(e);
            console.log(generateLogMessageString(`onAttributeAdd||Interface||error||${msg}`, CLASS_NAME, 'error'));
            setLoadingProps({ ...loadingProps, isLoading: false, message: 'An error occurred retrieving the interface attributes.' });
        });
    };

    const onAddUpdateState = (attributes) => {
        //re-merge collections after add
        _allAttributes = mergeAttributesCollections(attributes.profileAttributes, attributes.extendedProfileAttributes);

        //parent will return updated version if user adds from here.
        setDataRows({
            all: _allAttributes.all, filtered: _allAttributes.filtered, paged: _allAttributes.paged,
            pager: { currentPage: 1, pageSize: _preferences.pageSize, itemCount: _allAttributes.filtered.length }
        });

        //reset item add.
        setAdd(JSON.parse(JSON.stringify(attributeNew)));

        //Reset add settings to init the add ui back to starting point. 
        setAddSettings({
            ..._addSettings,
            useMinMax: true,
            useEngUnit: true,
            showComposition: false,
            showStructure: false,
            showInterface: false,
            isCustomDataType: false,
            showDescription: true,
            showEnumeration: false
        });

        //set focus back to attr type ddl
        //_dataAttrTypeDdlRef.current.focus();
    };

    //attribute add ui - change composition ddl
    const onChangeComposition = (e) => {
        //_addItem changed by ref in shared method
        onChangeCompositionShared(e, _addItem);

        //call commonn change method
        onChange(e);
    }

    const onChangeInterface = (e) => {
        //_addItem changed by ref in shared method
        onChangeInterfaceShared(e, _addItem);

        //call commonn change method
        onChange(e);
    }

    //attribute add ui - change structure 
    //const onChangeStructure = (e) => {
    //    //Note - still sets data type val
    //    var data = onChangeDataTypeShared(e, _addItem, _addSettings, _lookupStructures);

    //    //replace add settings (updated in shared method)
    //    setAddSettings(JSON.parse(JSON.stringify(data.settings)));
    //    //update state - after changes made in shared method
    //    setAdd(JSON.parse(JSON.stringify(data.item)));
    //    return;
    //}

    //attribute add ui - change data type
    const onChangeDataType = (e) => {
        //var data = onChangeDataTypeShared(e.target.value, _addItem, _addSettings, _lookupDataTypes);
        var data = onChangeDataTypeShared(e.value, _addItem, _addSettings, _lookupDataTypes);

        //replace add settings (updated in shared method)
        setAddSettings(JSON.parse(JSON.stringify(data.settings)));
        //update state - after changes made in shared method
        setAdd(JSON.parse(JSON.stringify(data.item)));
        return;

    }

    //attribute add ui - change data type
    const onChangeAttributeType = (e) => {
        var data = onChangeAttributeTypeShared(e, _addItem, _addSettings, _lookupAttributeTypes, _lookupDataTypes);

        //replace settings (updated in shared method)
        setAddSettings(JSON.parse(JSON.stringify(data.settings)));
        //update state - after changes made in shared method
        setAdd(JSON.parse(JSON.stringify(data.item)));
        return;
    }

    //onchange numeric field
    const onChangeEnumValue = (e) => {
        // Perform a numeric only check for numeric fields.
        if (!onChangeNumericKeysOnly(e)) {
            e.preventDefault();
            return;
        }

        //convert to int - this will convert '10.' to '10' to int
        var val = toInt(e.target.value);

        _addItem[e.target.id] = val;
        setAdd(JSON.parse(JSON.stringify(_addItem)));
    }

    //attribute add ui - update state on change (used by multiple controls except onChangeDataType)
    const onChange = (e) => {
        //TBD - remove this check for now because the model is evolving during dev
        ////check existence of field
        //if (e.target.id in _addItem === false) {
        //    console.warn(generateLogMessageString(`onAttributeChange||Unknown column:${e.target.id}. Id value should match a valid property value.`, CLASS_NAME));
        //    return;
        //}
        _addItem[e.target.id] = e.target.value;
        setAdd(JSON.parse(JSON.stringify(_addItem)));
    }

    const onDelete = (id) => {
        //raised from del button click in child component
        console.log(generateLogMessageString(`onDelete||item id:${id}`, CLASS_NAME));
        
        //call parent to delete item from collection, update state
        var attributes = props.onAttributeDelete(id);

        //re-merge collections after add
        _allAttributes = mergeAttributesCollections(attributes.profileAttributes, attributes.extendedProfileAttributes);

        //parent will return updated version if user adds from here.
        setDataRows({
            all: _allAttributes.all, filtered: _allAttributes.filtered, paged: _allAttributes.paged,
            pager: { currentPage: 1, pageSize: _preferences.pageSize, itemCount: _allAttributes.filtered.length }
        });
    };

    //delete an interface - delete all attribs for the interface
    const onDeleteInterface = (id) => {
        //raised from del button click in child component
        console.log(generateLogMessageString(`onDeleteInterface||interface id:${id}`, CLASS_NAME));

        //call parent to remove to items in collection, update state
        var attributes = props.onAttributeInterfaceDelete(id);

        //re-merge collections after add
        _allAttributes = mergeAttributesCollections(attributes.profileAttributes, attributes.extendedProfileAttributes);

        //parent will return updated version if user adds from here.
        setDataRows({
            all: _allAttributes.all, filtered: _allAttributes.filtered, paged: _allAttributes.paged,
            pager: { currentPage: 1, pageSize: _preferences.pageSize, itemCount: _allAttributes.filtered.length }
        });

        //re-add the deleted interface drop down for potential re-selection.
        var deletedInterface = _lookupInterfaces.all.find(i => { return i.id === id });
        if (deletedInterface != null) {
            _lookupInterfaces.current.push(deletedInterface);
            setLookupInterfaces({
                ..._lookupInterfaces,
                current: JSON.parse(JSON.stringify(_lookupInterfaces.current))
            });
        }
    };

    const onAttributeUpdate = (item) => {
        //raised from update button click in child component
        console.log(generateLogMessageString(`onAttributeUpdate||item id:${item.id}`, CLASS_NAME));

        //call parent to update item collection, update state
        var attributes = props.onAttributeUpdate(item);

        //re-merge collections after add
        _allAttributes = mergeAttributesCollections(attributes.profileAttributes, attributes.extendedProfileAttributes);

        //parent will return updated version if user adds from here.
        setDataRows({
            all: _allAttributes.all, filtered: _allAttributes.filtered, paged: _allAttributes.paged,
            pager: { currentPage: 1, pageSize: _preferences.pageSize, itemCount: _allAttributes.filtered.length }
        });
    };

    //call from item row click or from panel itself - slides out profile attribute list from a custom data type profile
    const toggleSlideOutCustomType = (isOpen, customTypeDefId, attrId, typeDefId) => {
        console.log(generateLogMessageString(`toggleSlideOutCustomType||Id:${customTypeDefId}`, CLASS_NAME));

        if (isOpen) {
            //show a spinner
            //setLoadingProps({ isLoading: true, message: null });

            //TBD - for now, treat everything as normal slide out so that a user
            //can edit advanced properties of their attribute. 
            //To go back to sliding out the profile of the child, we uncomment the following /**/
            toggleSlideOutDetail(isOpen, typeDefId, attrId, props.readOnly);

            /* See comment above. This contains support for sliding out for certain items like structures or types w/ attrs. 
            var data = { id: customTypeDefId };
            axiosInstance.post(`profiletypedefinition/getbyid`, data).then(result => {
                if (result.status === 200) {
                    //if there are no child attribs, treat this like a normal slide out
                    if (result.data.profileAttributes?.length === 0) {
                        toggleSlideOutDetail(isOpen, typeDefId, attrId, props.readOnly);
                    }
                    else {
                        SetSlideOut({ isOpen: isOpen, item: result.data, showDetail: false, readOnly: true });
                    }
                }
                else if (result.status === 404) {
                    var msg = 'An error occurred retrieving this attribute. This attribute was not found.';
                    console.log(generateLogMessageString(`toggleSlideOutCustomType||error||${msg}`, CLASS_NAME, 'error'));
                    setLoadingProps({ ...loadingProps, isLoading: false, message: msg });
                } else {
                    var msg2 = 'An error occurred retrieving this attribute.';
                    console.log(generateLogMessageString(`toggleSlideOutCustomType||error||${msg2}`, CLASS_NAME, 'error'));
                    setLoadingProps({ ...loadingProps, isLoading: false, message: msg2 });
                }
            }).catch(e => {
                var msg = JSON.stringify(e);
                console.log(generateLogMessageString(`toggleSlideOutCustomType||error||${msg}`, CLASS_NAME, 'error'));
                setLoadingProps({ ...loadingProps, isLoading: false, message: 'An error occurred retrieving this attribute.'  });
            });
            */
        }
        else {
            //not open
            SetSlideOut({ isOpen: isOpen, item: null, showDetail: false, readOnly: true });
        }
    };

    //call from item row click or from panel itself - slides out attribute item detail
    const toggleSlideOutDetail = (isOpen, typeDefId, id, readOnly) => {
        console.log(generateLogMessageString(`toggleSlideOut||TypeDefinitionId:${typeDefId}||Id:${id}`, CLASS_NAME));

        if (isOpen) {
            //add profile type def id to filter to account for inherited attr
            var attr = _allAttributes.all.find(a => { return a.id === id && a.typeDefinitionId === typeDefId });
            if (attr != null) {
                SetSlideOut({ isOpen: isOpen, item: attr, showDetail: true, readOnly: readOnly });
            }
        }
        else {
            //not open
            SetSlideOut({ isOpen: isOpen, item: null, showDetail: false, readOnly: readOnly });
        }
    };

    //-------------------------------------------------------------------
    // Region: Methods
    //-------------------------------------------------------------------
    // Apply filter on data starting with all rows
    const filterDataRows = (dataRows, val) => {
        const delimiter = ":::";

        if (dataRows == null) return null;

        var filteredCopy = JSON.parse(JSON.stringify(dataRows));

        if (val == null || val === '') {
            return filteredCopy;
        }

        // Filter data - match up against a number of fields
        return filteredCopy.filter((item, i) => {
            var concatenatedSearch = delimiter + item.name.toLowerCase() + delimiter
                + item.dataType.name.toLowerCase() + delimiter
                + (item.minValue != null ? item.minValue.toString().toLowerCase() + delimiter : "")
                + (item.maxValue != null ? item.maxValue.toString().toLowerCase() + delimiter : "")
                + (item.engUnit != null ? item.engUnit.toString().toLowerCase() + delimiter : "")
                + (item.composition != null ? item.composition.name.toString().toLowerCase() + delimiter : "")
                + (item.interface != null ? item.interface.name.toString().toLowerCase() + delimiter : "")
            return (concatenatedSearch.indexOf(val.toLowerCase()) !== -1);
        });
    }

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    //render the attribute header row on the grid - used by both attributes grids
    const renderHeaderRow = () => {
        return (<AttributeItemRow key="header" item={null} isHeader={true} readOnly={true} isPopout={props.isPopout} cssClass="attribute-list-header pb-2" />)
    }

    //render the attributes grid
    const renderGrid = () => {
        if (_dataRows == null || _dataRows.paged == null || _dataRows.paged.length === 0) {
            return (
                <div className="alert alert-info-custom mt-2 mb-2">
                    <div className="text-center" >There are no attribute records.</div>
                </div>
            )
        }

        var isTypeDefReadOnly = props.typeDefinition.isReadOnly || props.typeDefinition.authorId == null ||
            props.typeDefinition.authorId !== props.currentUserId;

        const mainBody = _dataRows.paged.map((row) => {
            if (row.isDeleted) return null;  //if user deletes row client side, hide that row from view.
            var key = `${row.id}|${row.compositionId == null ? '' : row.compositionId}|` +
                `${row.dataType.customTypeId == null ? '' : row.dataType.customTypeId}|${row.interfaceId == null ? '' : row.interfaceId}`;

            //FIXED
            //if (props.typeDefinition == null) {
            //    console.error("TBD - props.typeDefinition is null. Fix this. This happens when slideout shows attr list and not all properties of attr list are set.");
            //}
            //the presence of the callback will indicate to attrib item row whether to display delete button
            //only allow onDeleteInterface callback if attrib is assoc w/ interface and interface is implemented by this profile.
            var x = props.typeDefinition.interfaces == null ? -1 :
                props.typeDefinition.interfaces.findIndex(i => { return i.id === row.interfaceId; });
            //item is an interface attr or not associated with this profile
            //must be owner of this profile type def
            var deleteInterfaceCallback = isTypeDefReadOnly || row.interfaceId == null || x < 0 ? null : onDeleteInterface;

            //only allow onDelete callback if attrib is assoc w/ this profile.
            //must be owner of this profile type def
            var deleteCallback = isTypeDefReadOnly || row.typeDefinitionId !== props.typeDefinition.id ? null : onDelete;

            //set readonly if the item is not a part of this profile.
            //console.log(generateLogMessageString(`renderGrid||key||${key}`, CLASS_NAME));
            return (<AttributeItemRow key={key} item={row} isHeader={false}
                readOnly={row.typeDefinitionId !== props.typeDefinition.id || props.readOnly || props.mode === "extended"} cssClass="attribute-list-item" isPopout={props.isPopout}
                onDelete={deleteCallback} onDeleteInterface={deleteInterfaceCallback} onUpdate={onAttributeUpdate} allAttributes={_allAttributes.all}
                toggleSlideOutCustomType={toggleSlideOutCustomType} toggleSlideOutDetail={toggleSlideOutDetail}
                lookupDataTypes={_lookupDataTypes} lookupAttributeTypes={_lookupAttributeTypes} lookupCompositions={_lookupCompositions}
                lookupStructures={_lookupStructures} />)
        });

        return (
            <div className="flex-grid attribute-list">
                <table className="mt-3 w-100">
                    <thead>{renderHeaderRow()}</thead>
                    <tbody>{mainBody}</tbody>
                </table>
            </div>
        );
    }

    const renderAttributeCount = () => {
        if (_dataRows.all == null) return;

        var captionAll = _dataRows.all.length === 1 ? `1 Attribute` : `${_dataRows.all.length} Attributes`;
        var captionFiltered = _dataRows.filtered.length !== _dataRows.all.length ? ` (${_dataRows.filtered.length} filtered)` : '';

        return (
            <span className="small-size mt-2 mr-3">{captionAll}{captionFiltered}</span>
        );
    }

    //render pagination ui
    const renderPagination = () => {
        if (_dataRows == null || _dataRows.all.length === 0) return;
        return <GridPager currentPage={_dataRows.pager.currentPage} pageSize={_dataRows.pager.pageSize} itemCount={_dataRows.pager.itemCount} onChangePage={onChangePage} />
    }

    /*
    const renderDataTypeUI = () => {
        if (_lookupDataTypes == null || _lookupDataTypes.length === 0) return;

        const options = renderDataTypeSelectOptions(_lookupDataTypes, props.typeDefinition.type);

        return (
            <Form.Group>
                <Form.Label>Data Type</Form.Label>
                {!_isValid.dataType &&
                    <span className="invalid-field-message inline">
                        Required
                    </span>
                }
                <Form.Control id="dataType" as="select" value={_addItem.dataType.id} 
                    onChange={onChangeDataType} className={(!_isValid.dataType ? 'invalid-field minimal pr-5' : 'minimal pr-5')} >
                    <option key="-1|Select One" value="-1" >Select</option>
                    {options}
                </Form.Control>
            </Form.Group>
        )
    };
    */

    const renderDataTypeUI = () => {
        return renderDataTypeUIShared(_addItem, _lookupDataTypes, props.typeDefinition.type, _isValid.dataType, true, onChangeDataType);
    };

    const renderAttributeTypeUI = () => {
        if (_lookupAttributeTypes == null || _lookupAttributeTypes.length === 0) return;
        const options = _lookupAttributeTypes.map((item) => {
            return (<option key={item.id} value={item.id} >{item.name}</option>)
        });

        return (
            <Form.Group>
                <Form.Label>Attribute Type</Form.Label>
                {!_isValid.attributeType &&
                    <span className="invalid-field-message inline">
                        Required
                    </span>
                }
                <Form.Control ref={_dataAttrTypeDdlRef} id="attributeType" as="select" value={_addItem.attributeType.id} onBlur={validateForm_attributeType}
                    onChange={onChangeAttributeType} className={(!_isValid.attributeType ? 'invalid-field minimal pr-5' : 'minimal pr-5')} >
                    <option key="-1|Select One" value="-1" >Select</option>
                    {options}
                </Form.Control>
            </Form.Group>
        )
    };

    //render for enumeration attr type
    const renderEnumValueUI = () => {

        if (!_addSettings.showEnumeration) return;
        //if (_addItem.attributeType.id !== AppSettings.AttributeTypeDefaults.EnumerationId) return;

        var tip = !_isValid.enumValue ? 'Integer > 0 required.' : '';
        tip = !_isValid.enumValueIsNumeric ? tip + ' Integer required.' : tip;
        return (
            <Form.Group>
                <Form.Label>Enum Value</Form.Label>
                {!_isValid.enumValue &&
                    <span className="invalid-field-message inline">
                        Integer &gt; 0 required
                    </span>
                }
                {!_isValid.enumValueDuplicate &&
                    <span className="invalid-field-message inline">
                        Duplicate value
                    </span>
                }
                <Form.Control id="enumValue" type="" value={_addItem.enumValue == null ? '' : _addItem.enumValue} 
                    onChange={onChangeEnumValue} onBlur={validateForm_enumValue} title={tip}
                    className={(!_isValid.enumValue || !_isValid.enumValueDuplicate ? 'invalid-field' : '')} />
            </Form.Group>
        );
    };

    //only show this for one attr type
    const renderCompositionUI = () => {
        if (!_addSettings.showComposition) return;

        const options = _lookupCompositions.map((item) => {
            return (<option key={item.id} value={item.id} >{item.name}</option>)
        });

        return (
            <Form.Group>
                <Form.Label>Composition [{props.typeDefinition.type == null || props.typeDefinition.type.name.toLowerCase() === "class" ? "Profile" : "Interface"}]</Form.Label>
                {!_isValid.composition &&
                    <span className="invalid-field-message inline">
                        Required
                    </span>
                }
                <Form.Control id="compositionId" as="select" value={_addItem.compositionId} onBlur={validateForm_composition}
                    onChange={onChangeComposition} className={(!_isValid.composition ? 'invalid-field minimal pr-5' : 'minimal pr-5')} >
                    <option key="-1|Select One" value="-1" >Select</option>
                    {options}
                </Form.Control>
            </Form.Group>
        )
    };

    //only show this for one attr type
    //const renderStructureUI = () => {
    //    if (!_addSettings.showStructure) return;

    //    const options = _lookupStructures.map((item) => {
    //        return (<option key={item.id} value={item.id} >{item.name}</option>)
    //    });

    //    return (
    //        <Form.Group>
    //            <Form.Label>Structure</Form.Label>
    //            {!_isValid.structure &&
    //                <span className="invalid-field-message inline">
    //                    Required
    //                </span>
    //            }
    //            <Form.Control id="structureId" as="select" value={_addItem.dataType.id} onBlur={validateForm_structure}
    //                onChange={onChangeStructure} className={(!_isValid.structure ? 'invalid-field minimal pr-5' : 'minimal pr-5')} >
    //                <option key="-1|Select One" value="-1" >Select</option>
    //                {options}
    //            </Form.Control>
    //        </Form.Group>
    //    )
    //};

    //only show this for one data type
    const renderInterfaceUI = () => {
        if (!_addSettings.showInterface) return;

        const options = _lookupInterfaces.current.map((item) => {
            return (<option key={item.id} value={item.id} >{item.name}</option>)
        });

        return (
            <Form.Group>
                <Form.Label>Interface</Form.Label>
                {!_isValid.interface &&
                    <span className="invalid-field-message inline">
                        Required
                    </span>
                }
                <Form.Control id="interfaceId" as="select" value={_addItem.interfaceId} onBlur={validateForm_interface}
                    onChange={onChangeInterface} className={(!_isValid.interface ? 'invalid-field minimal pr-5' : 'minimal pr-5')} >
                    <option key="-1|Select One" value="-1" >Select</option>
                    {options}
                </Form.Control>
            </Form.Group>
        )
    };

    //only show this for certain data types
    const renderDescription = () => {
        if (!_addSettings.showDescription) return;
        return (
            <>
                <Form.Group>
                <Form.Label>Description</Form.Label>
                <Form.Control id="description" type="" placeholder="Attribute description" value={_addItem.description} onChange={onChange} />
            </Form.Group>
            </>
        )
    };

    //render an add form that sits on top of the grid.
    const renderSearchUI = () => {
        return (
                <Form.Row className="m-0" >
                    <InputGroup className="quick-search">
                        {renderAttributeCount()}
                        <FormControl

                            type="text"
                            placeholder="Quick filter"
                            aria-label="Enter text to filter by"
                            val={_filterVal}
                            onChange={onSearchBlur}
                            onKeyPress={(e) => e.key === 'Enter' && onSearchClick()}
                            className="border-right-0"
                        />
                    <InputGroup.Append>
                        <Button variant="search" className="p-0 pl-3 pr-2 border-left-0" onClick={onSearchClick} title="Filter attribute list" >
                                <SVGIcon name="search" size="24" fill={color.shark} />
                            </Button>
                        </InputGroup.Append>
                    </InputGroup>
                </Form.Row>
        );
    }

    const renderAddButtonUI = () => {
        return (
            <Form.Group>
            <Button variant="inline-add" aria-label="Add attribute" onClick={onAdd} className="d-flex align-items-center justify-content-center">
                <span>
                    <SVGIcon name="playlist-add" size="24" fill={color.shark} />
                </span>
            </Button>
            </Form.Group>
        );
    }

    //render an add form that sits on top of the grid.
    const renderAttributeHeaderBlock = () => {
        //return search only
        if (props.readOnly) {
            return (
                <div className="d-flex align-items-end mb-3">
                    <div className="ml-auto">
                        {renderSearchUI()}
                    </div>
                </div>
            );
        }
        //else return the search and the add ui
        return (
            <>
                <div className="d-flex align-items-end mb-3">
                    <div className="ml-auto">
                        {renderSearchUI()}
                    </div>
                </div>
                <div className="p-4 hl-blue">
                    <div className="row" >
                        <div className="col-sm-5" >{renderAttributeTypeUI()}</div>
                        {(!_addSettings.showInterface && !_addSettings.showComposition && !_addSettings.showEnumeration) &&
                            <div className="col-sm-6" >{renderDataTypeUI()}</div>
                        }
                        {_addSettings.showEnumeration &&
                            <div className="col-sm-3" >{renderEnumValueUI()}</div>
                        }
                        {_addSettings.showComposition &&
                            <div className="col-sm-6" >{renderCompositionUI()}</div>
                        }
                        {_addSettings.showInterface &&
                            <>
                            <div className="col-md-6 col-sm-5" >
                                {renderInterfaceUI()}
                            </div>
                            <div className="col-sm-1 align-items-end align-self-end" >
                                {renderAddButtonUI()}
                            </div>
                            </>
                        }
                    </div>
                    {!_addSettings.showInterface &&
                        <div className="row" >
                        <div className="col-sm-5" >
                                <Form.Group className="">
                                    <Form.Label>Name</Form.Label>
                                    {!_isValid.name &&
                                        <span className="invalid-field-message inline">
                                            Required
                                </span>
                                    }
                                    {!_isValid.nameDuplicate &&
                                        <span className="invalid-field-message inline">
                                            Duplicate
                                </span>
                                    }
                                    <Form.Control id="name" type="" placeholder="Attribute name" value={_addItem.name}
                                        onBlur={validateForm_name}
                                        onChange={onChange} className={(!_isValid.name || !_isValid.nameDuplicate ? 'invalid-field' : '')} />
                                </Form.Group>
                            </div>
                        <div className="col-md-6 col-sm-5" >
                                {renderDescription()}
                            </div>
                        <div className="col-sm-1 align-items-end align-self-end" >
                                {renderAddButtonUI()}
                            </div>
                        </div>
                    }
                </div>
            </>
        );
    }

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    return (
        <>
            {renderAttributeHeaderBlock()}
            {renderGrid()}
            {renderPagination()}
            <AttributeSlideOut isOpen={_slideOut.isOpen} item={_slideOut.item} onClosePanel={toggleSlideOutCustomType} readOnly={_slideOut.readOnly}
                showDetail={_slideOut.showDetail} lookupDataTypes={_lookupDataTypes} lookupAttributeTypes={_lookupAttributeTypes}
                allAttributes={_allAttributes.all} lookupCompositions={_lookupCompositions} lookupInterfaces={_lookupInterfaces}
                lookupEngUnits={_lookupEngUnits} onUpdate={onAttributeUpdate} currentUserId={props.currentUserId}
            />
        </>
    )
}

export default AttributeList;