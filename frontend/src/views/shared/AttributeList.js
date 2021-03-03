import React, { useState, useEffect } from 'react'
import axios from 'axios'
import Form from 'react-bootstrap/Form'
import InputGroup from 'react-bootstrap/InputGroup'
import FormControl from 'react-bootstrap/FormControl'
import Button from 'react-bootstrap/Button'

import { SVGIcon } from '../../components/SVGIcon'
import color from '../../components/Constants'

import { generateLogMessageString, pageDataRows, convertToNumeric } from '../../utils/UtilityService'
import { getAttributesPreferences, setAttributesPageSize, attributeNew, validate_All, validate_nameDuplicate, validate_dataType, validate_name } from '../../services/AttributesService';
import AttributeItemRow from './AttributeItemRow';
import AttributeSlideOut from './AttributeSlideOut';
import GridPager from '../../components/GridPager'
import { AppSettings } from '../../utils/appsettings';
import { getProfileCompositionsLookup, getProfileInterfacesLookup, getProfileDataTypesLookup } from '../../services/ProfileService';

const CLASS_NAME = "AttributeList";

function AttributeList(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    //merge together two attributes collections
    const _preferences = getAttributesPreferences();
    var _allAttributes = mergeAttributesCollections(props.profileAttributes, props.extendedProfileAttributes);
    const [_filterVal, setFilterVal] = useState(''); //props.searchValue
    const [_dataRows, setDataRows] = useState({
        all: _allAttributes.all, filtered: _allAttributes.filtered, paged: _allAttributes.paged,
        pager: { currentPage: 1, pageSize: _preferences.pageSize, itemCount: _allAttributes.filtered.length }
    });
    const [_addItem, setAdd] = useState(JSON.parse(JSON.stringify(attributeNew)));
    const [_lookupProfiles, setLookupProfiles] = useState([]);
    const [_lookupInterfaces, setLookupInterfaces] = useState([]);
    const [_lookupDataTypes, setLookupDataTypes] = useState([]);
    const [_addSettings, setAddSettings] = useState({ useMinMax: true, useEngUnit: true, showComposition: false, showInterface: false, isVariableType: false });
    const [_isValid, setIsValid] = useState({
        name: true,
        nameDuplicate: true,
        dataType: true,
        composition: true,
        interface: true,
        minMax: true,
        minIsNumeric: true,
        maxIsNumeric: true,
        engUnit: true
    });
    const [_slideOut, SetSlideOut] = useState({ isOpen: false, item: null, showDetail: false, readOnly: true });

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

        //merge together two attributes collections, sort alpha
        var result = (profileAttributes == null ? [] : profileAttributes).concat(extendedProfileAttributes == null ? [] : extendedProfileAttributes)
        result.sort((a, b) => {
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
    // Region: Event Handling - When composition data type is chosen, go get a list of profiles
    //      where the profile is neither a descendant or a parent/grandparent, etc. of the profile we 
    //      are working with
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchLookupProfiles() {
            //initialize spinner during loading
            //setLoadingProps({ isLoading: true, message: null });

            //TBD - in the phase II system, the endpoint would handle this server side. Filter out anything 
            //where the profile is neither a descendant or a parent/grandparent, etc. of the profile we 
            //are working with, can't be a dependency of this profile
            // If we are working with a profile, then composition can't be an interface type
            // If we are working with an interface, then composition can't be a profile type
            //var url = `${AppSettings.BASE_API_URL}/profile?id!=${props.profile.id}`;
            var url = `${AppSettings.BASE_API_URL}/profile`; //profiles only
            console.log(generateLogMessageString(`useEffect||fetchLookupProfiles||${url}`, CLASS_NAME));
            const result = await axios(url);

            //profile id - 3 scenarios - 1. typical - use profile id, 2. extend profile where parent profile should be used, 
            //      3. new profile - no parent, no inheritance, use 0 
            var pId = props.profile.id;
            if (props.profile.id === 0 && props.profile.parentProfile != null) pId = props.profile.parentProfile.id;
            var trimmedItems = getProfileCompositionsLookup(pId, result.data);

            //TBD - handle paged data scenario, do a predictive search look up
            setLookupProfiles(trimmedItems); //also updates state
            setLookupInterfaces(getProfileInterfacesLookup(props.profile.id, result.data));
            setLookupDataTypes(getProfileDataTypesLookup(props.profile.id, result.data)); 

            //hide a spinner
            //setLoadingProps({ isLoading: false, message: null });
        }

        fetchLookupProfiles();

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [props.profile.id]);


    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------
    const validateForm_name = (e) => {
        var isValid = validate_name(e.target.value, _addItem);
        //dup check
        var isValidDup = validate_nameDuplicate(e.target.value, _addItem, _allAttributes.all);
        setIsValid({ ..._isValid, name: isValid, nameDuplicate: isValidDup });
    };

    const validateForm_dataType = (e) => {
        setIsValid({ ..._isValid, dataType: validate_dataType(e.target.value) });
    };

    const validateForm_composition = (e) => {
        var isValid = e.target.value.toString() !== "-1" || _addItem.dataType !== "composition";
        setIsValid({ ..._isValid, composition: isValid });
    };

    const validateForm_interface = (e) => {
        var isValid = e.target.value.toString() !== "-1" || _addItem.dataType !== "interface";
        setIsValid({ ..._isValid, interface: isValid });
    };

    //validate all - call from button click
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        var isValid = validate_All(_addItem, _addSettings, _allAttributes.all);
        isValid.composition = (_addItem.composition != null && _addItem.compositionId > 0) || _addItem.dataType !== "composition";
        isValid.interface = (_addItem.interface != null && _addItem.interfaceId > 0) || _addItem.dataType !== "interface";

        setIsValid(JSON.parse(JSON.stringify(isValid)));
        return (isValid.name && isValid.nameDuplicate && isValid.dataType && isValid.composition && isValid.interface );
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

    const onAttributeAdd = () => {
        //raised from add button click
        console.log(generateLogMessageString(`onAttributeAdd`, CLASS_NAME));

        //validate form
        if (!validateForm()) {
            //alert("validation failed");
            return;
        }

        var attributes = [];

        //adding non-interface attribute
        if (_addItem.interfaceId === -1) {
            //dynamically generate an id w/ a negative value. We need a way to add/remove items before doing a save to API 
            // and therefore need to be aware of an id for newly added rows. 
            _addItem.id = (-1) * (new Date()).getTime();

            //for certain types, clear out engunit, min max, etc. 
            if (_addSettings.showComposition || _addSettings.showInterface || _addSettings.isVariableType 
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
            attributes = props.onAttributeAdd(_addItem);
        }
        //adding interface
        else {
            // get the interface we're adding (interface is a reserved word)
            var iface = _lookupInterfaces.find(p => { return p.id === _addItem.interface.id });
            
            var interfaceGroupId = Math.floor(Math.random() * 30);

            //create a new combined collection of the attributes to be added, update some vals and then bubble up
            //add both the interface's attributes and its extended attributes into one collection and add em all
            iface.profileAttributes = iface.profileAttributes == null ? [] : iface.profileAttributes;
            iface.extendedProfileAttributes = iface.extendedProfileAttributes == null ? [] : iface.extendedProfileAttributes;
            var interfaceAttrItems = iface.profileAttributes.concat(iface.extendedProfileAttributes);
            interfaceAttrItems.forEach((attrib) => {
                //if attribute already exists in current profile, then update it so it becomes an interface attribute
                var match = props.profile.profileAttributes.find((a) => { return a.id === attrib.id && a.name.toLowerCase() === attrib.name.toLowerCase() });
                if (match != null) {
                    match.interface = { id: iface.id, name: iface.name, type: 'Interface' };
                    // specify a way to keep these items visually connected
                    match.interfaceGroupId = interfaceGroupId;
                }
                var matchEx = props.profile.extendedProfileAttributes.find((a) => { return a.id === attrib.id && a.name.toLowerCase() === attrib.name.toLowerCase() });
                if (matchEx != null) {
                    matchEx.interface = { id: iface.id, name: iface.name, type: 'Interface' };
                    // specify an itemtype since interfaces are special
                    matchEx.interfaceGroupId = interfaceGroupId;
                }
                //new attr
                if (match == null && matchEx == null) {
                    // add a random seed to the id to prevent collisions from iterating too fast
                    attrib.id = ((-1) * (new Date()).getTime()) + Math.floor(Math.random() * 10000);
                    attrib.interface = { id: iface.id, name: iface.name, type: 'Interface' };
                    // specify an itemtype since interfaces are special
                    attrib.interfaceGroupId = interfaceGroupId;
                    attrib._itemType = "profile";
                    props.profile.profileAttributes.push(attrib);
                }
            });

            //call parent to add to items collection, update state
            attributes = props.onAttributeInterfaceAdd(iface, props.profile.profileAttributes, props.profile.extendedProfileAttributes);
        }
      
        //re-merge collections after add
        _allAttributes = mergeAttributesCollections(attributes.profileAttributes, attributes.extendedProfileAttributes);

        //parent will return updated version if user adds from here.
        setDataRows({
            all: _allAttributes.all, filtered: _allAttributes.filtered, paged: _allAttributes.paged,
            pager: { currentPage: 1, pageSize: _preferences.pageSize, itemCount: _allAttributes.filtered.length }
        });

        //reset item add.
        setAdd(JSON.parse(JSON.stringify(attributeNew)));
    };

    //attribute add ui - change composition ddl
    const onChangeComposition = (e) => {
        //console.log(generateLogMessageString(`onChangeComposition||e:${e.target}`, CLASS_NAME));
        //if data type is composition, set the profile id and the name field
        _addItem.compositionId = parseInt(e.target.value); 
        if (e.target.value.toString() === "-1") {
            _addItem.composition = null;
            _addItem.compositionId = null;
        }
        else {
            _addItem.composition = {};
            _addItem.composition.id = parseInt(e.target.value);
            _addItem.composition.name = e.target.options[e.target.selectedIndex].text;
        }

        //call commonn change method
        onChange(e);
    }

    const onChangeInterface = (e) => {
        //console.log(generateLogMessageString(`onChangeComposition||e:${e.target}`, CLASS_NAME));
        //if data type is composition, set the profile id and the name field
        _addItem.interfaceId = parseInt(e.target.value); 
        if (e.target.value.toString() === "-1") {
            _addItem.interface = null;
            _addItem.interfaceId = null;
        }
        else {
            _addItem.interface = {};
            _addItem.interface.id = parseInt(e.target.value);
            _addItem.interface.name = e.target.options[e.target.selectedIndex].text;
        }

        //call commonn change method
        onChange(e);
    }

    //attribute add ui - change data type
    const onChangeDataType = (e) => {
        //console.log(generateLogMessageString(`onChangeDataType||e:${e.target}`, CLASS_NAME));
        var isVariableType = false;
        var useMinMax = false;
        var useEngUnit = false;
        if (e.target.value == null || e.target.value.toString() === "-1") {
            useMinMax = true;
            useEngUnit = true;
        }
        else {
            var lookupItem = _lookupDataTypes.find(dt => { return dt.val === e.target.value; });
            isVariableType = lookupItem != null && lookupItem.isVariableType;
            useMinMax = !isVariableType && lookupItem != null && lookupItem.useMinMax;
            useEngUnit = !isVariableType && lookupItem != null && lookupItem.useEngUnit;
        }

        //reset composition, interface object anytime this changes
        _addItem.composition = null;
        _addItem.compositionId = -1;
        _addItem.interface = null;
        _addItem.interfaceId = -1;
        _addItem.variableType = null;
        _addItem.variableTypeId = -1;

        //clear out vals based on data type
        _addItem.minValue = useMinMax ? _addItem.minValue : null;
        _addItem.maxValue = useMinMax ? _addItem.maxValue : null;
        _addItem.engUnit = useEngUnit ? _addItem.engUnit : null;

        //if is variable type, handle this specially
        if (isVariableType) {
            _addItem.variableType = { id: parseInt(e.target.value), name: e.target.options[e.target.selectedIndex].text};
            _addItem.variableTypeId = parseInt(e.target.value);
        }

        setAddSettings({
            useMinMax: useMinMax,
            useEngUnit: useEngUnit,
            showComposition: e.target.value === "composition",
            showInterface: e.target.value === "interface",
            isVariableType: isVariableType
        });

        //call commonn change method
        onChange(e);
    }


    //attribute add ui - update state on change
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

    const onAttributeDelete = (id) => {
        //raised from del button click in child component
        console.log(generateLogMessageString(`onAttributeDelete||item id:${id}`, CLASS_NAME));
        
        //call parent to add to items collection, update state
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

        //call parent to add to items collection, update state
        var attributes = props.onAttributeInterfaceDelete(id);

        //re-merge collections after add
        _allAttributes = mergeAttributesCollections(attributes.profileAttributes, attributes.extendedProfileAttributes);

        //parent will return updated version if user adds from here.
        setDataRows({
            all: _allAttributes.all, filtered: _allAttributes.filtered, paged: _allAttributes.paged,
            pager: { currentPage: 1, pageSize: _preferences.pageSize, itemCount: _allAttributes.filtered.length }
        });
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

    //call from item row click or from panel itself - slides out profile attribute list from a variable type profile
    const toggleSlideOutVariableType = (isOpen, id) => {
        console.log(generateLogMessageString(`toggleSlideOut||${id}`, CLASS_NAME));

        if (isOpen) {
            //show a spinner
            //setLoadingProps({ isLoading: true, message: null });

            axios(`${AppSettings.BASE_API_URL}/profile/${id}`).then(result => {
                if (result.status === 200) {
                    //set item state value
                    SetSlideOut({ isOpen: isOpen, item: result.data, showDetail: false, readOnly: true });
                } else {
                    console.log(generateLogMessageString(`toggleSlideOut||Error retrieving attribute item`, CLASS_NAME, 'error'));
                }
                //hide a spinner
                //setLoadingProps({ isLoading: false, message: null });
            }).catch(e => {
                console.log(generateLogMessageString(`toggleSlideOut||Error retrieving attribute item`, CLASS_NAME, 'error'));
                //hide a spinner
                //setLoadingProps({ isLoading: false, message: null });
            });
        }
        else {
            //not open
            SetSlideOut({ isOpen: isOpen, item: null, showDetail: false, readOnly: true });
        }
    };

    //call from item row click or from panel itself - slides out attribute item detail
    const toggleSlideOutDetail = (isOpen, id, readOnly) => {
        console.log(generateLogMessageString(`toggleSlideOut||${id}`, CLASS_NAME));

        if (isOpen) {
            var attr = _allAttributes.all.find(a => { return a.id === id });
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
                + item.dataType.toLowerCase() + delimiter
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
        return (<AttributeItemRow key="header" item={null} isHeader={true} readOnly={true} cssClass="attribute-list-header pb-2" />)
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
        const mainBody = _dataRows.paged.map((row) => {
            if (row.isDeleted === false) return null;  //if user deletes row client side, hide that row from view.
            return (<AttributeItemRow key={row.id} item={row} isHeader={false} readOnly={props.readOnly || props.mode === "extended"} cssClass="attribute-list-item"
                onDelete={onAttributeDelete} onDeleteInterface={onDeleteInterface} onUpdate={onAttributeUpdate} allAttributes={_allAttributes.all}
                toggleSlideOutVariableType={toggleSlideOutVariableType} toggleSlideOutDetail={toggleSlideOutDetail} lookupDataTypes={_lookupDataTypes}  />)
        });

        return (
            <div className="flex-grid attribute-list">
                {renderHeaderRow()}
                {mainBody}
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

    const renderDataTypeDDL = () => {
        if (_lookupDataTypes == null || _lookupDataTypes.length === 0) return;
        const options = _lookupDataTypes.map((item) => {
            if (props.profile.type != null && props.profile.type.name.toLowerCase() === 'interface' && item.val === 'interface') {
                //skip this one if we are on a profile of type interface. Interface profile can't add interfaces. It extends interfaces.
                return null;
            }
            else {
                return (<option key={item.val} value={item.val} >{item.caption}</option>)
            }
        });

        return (
            <Form.Group className="flex-grow-1" style={{maxWidth: "512px"}}>
                <Form.Label>Data type</Form.Label>
                {!_isValid.dataType &&
                    <span className="invalid-field-message inline">
                        Required
                    </span>
                }
                <Form.Control id="dataType" as="select" value={_addItem.dataType} onBlur={validateForm_dataType}
                    onChange={onChangeDataType} className={(!_isValid.dataType ? 'invalid-field minimal pr-5' : 'minimal pr-5')} >
                    <option key="-1|Select One" value="-1" >Select</option>
                    {options}
                </Form.Control>
            </Form.Group>
        )
    };

    //only show this for one data type
    const renderCompositionDDL = () => {
        if (!_addSettings.showComposition) return;

        const options = _lookupProfiles.map((item) => {
            return (<option key={item.id} value={item.id} >{item.name}</option>)
        });

        return (
            <Form.Group className="flex-grow-1 mr-4">
                <Form.Label>Composition [{props.profile.type == null || props.profile.type.name.toLowerCase() === "class" ? "Profile" : "Interface"}]</Form.Label>
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

    //only show this for one data type
    const renderInterfaceDDL = () => {
        if (!_addSettings.showInterface) return;

        const options = _lookupInterfaces.map((item) => {
            return (<option key={item.id} value={item.id} >{item.name}</option>)
        });

        return (
            <Form.Group className="flex-grow-1 mr-4">
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
        return (
            <>
                <Form.Group className="flex-grow-1 mr-4">
                <Form.Label>Description</Form.Label>
                <Form.Control id="description" type="" placeholder="Enter description" value={_addItem.description} onChange={onChange} />
            </Form.Group>
            </>
        )
    };

    //render an add form that sits on top of the grid.
    const renderSearchUI = () => {
        return (
            <Form>
                <Form.Row className="m-0" >
                    <InputGroup className="quick-search">
                        {renderAttributeCount()}
                        <FormControl

                            type="text"
                            placeholder="Quick filter"
                            aria-label="Quick filter"
                            aria-describedby="basic-addon2"
                            val={_filterVal}
                            onBlur={onSearchBlur}
                            className="border-right-0"
                        />
                        <InputGroup.Append>
                            <Button variant="search" className="p-0 pl-3 pr-2 border-left-0" onClick={onSearchClick} >
                                <SVGIcon name="search" size="24" fill={color.shark} />
                            </Button>
                        </InputGroup.Append>
                    </InputGroup>
                </Form.Row>
            </Form>
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
                    {renderDataTypeDDL()}
                    <div className="ml-auto">
                        {renderSearchUI()}
                    </div>
                </div>
                <div className="d-flex align-items-end p-4 pb-5 hl-blue">
                    {!_addSettings.showInterface &&
                        <Form.Group className="flex-grow-1 mr-4">
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
                            <Form.Control id="name" type="" placeholder="Enter a name" value={_addItem.name}
                                onBlur={validateForm_name}
                                onChange={onChange} className={(!_isValid.name || !_isValid.nameDuplicate ? 'invalid-field' : '')} />
                        </Form.Group>
                    }
                    {renderCompositionDDL()}

                    {renderInterfaceDDL()}

                    {renderDescription()}

                    <Button variant="inline-add" aria-label="Add attribute" onClick={onAttributeAdd} className="d-flex align-items-center justify-content-center">
                        <span>
                            <SVGIcon name="playlist-add" size="24" fill={color.shark} />
                        </span>
                    </Button>
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
            <AttributeSlideOut isOpen={_slideOut.isOpen} item={_slideOut.item} onClosePanel={toggleSlideOutVariableType} readOnly={_slideOut.readOnly}
                showDetail={_slideOut.showDetail} lookupDataTypes={_lookupDataTypes} allAttributes={_allAttributes.all} onUpdate={onAttributeUpdate} />
        </>
    )
}

export default AttributeList;