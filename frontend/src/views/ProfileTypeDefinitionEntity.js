import React, { useState, useEffect } from 'react'
import { useParams, useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import { useMsal } from "@azure/msal-react";
import axiosInstance from "../services/AxiosService";

import Form from 'react-bootstrap/Form'
import Card from 'react-bootstrap/Card'
import Button from 'react-bootstrap/Button'
import Tab from 'react-bootstrap/Tab'
import Nav from 'react-bootstrap/Nav'

import { useLoadingContext, UpdateRecentFileList } from "../components/contexts/LoadingContext";
import { useWizardContext } from '../components/contexts/WizardContext';
import { AppSettings } from '../utils/appsettings';
import { generateLogMessageString, getTypeDefIconName, getProfileTypeCaption, getIconColorByProfileState, isDerivedFromDataType, getPermittedDataTypesForVariableTypeById } from '../utils/UtilityService'
import { renderDataTypeUIShared } from '../services/AttributesService';
import AttributeList from './shared/AttributeList';
import DependencyList from './shared/DependencyList';
import ProfileBreadcrumbs from './shared/ProfileBreadcrumbs';
import ProfileEntityModal from './modals/ProfileEntityModal';
import ProfileItemRow from './shared/ProfileItemRow';
import TypeDefinitionActions from './shared/TypeDefinitionActions';

import { SVGIcon } from "../components/SVGIcon";
import { getWizardNavInfo, renderWizardBreadcrumbs, WizardSettings } from '../services/WizardUtil';
import { isOwner } from './shared/ProfileRenderHelpers';
import './styles/ProfileTypeDefinitionEntity.scss';
import { Prompt } from 'react-router'

const CLASS_NAME = "ProfileTypeDefinitionEntity";
const entityInfo = {
    name: "Type",
    namePlural: "Types",
    entityUrl: "/type/:id",
    listUrl: "/types/all"
}


function ProfileTypeDefinitionEntity() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();
    const { instance } = useMsal();
    const _activeAccount = instance.getActiveAccount();

    const { id, parentId, profileId } = useParams();
    const { loadingProps, setLoadingProps } = useLoadingContext();

    //var pageMode = //state is not always present. If user types a url or we use an href link, state is null. history.location.state.viewMode;
    //see logic below for how we calculate.
    const [mode, setMode] = useState(initPageMode());
    const [_item, setItem] = useState({});
    const [isLoading, setIsLoading] = useState(true);
    const [_lookupDataTypes, setLookupDataTypes] = useState([]);
    const [_permittedDataTypes, setPermittedDataTypes] = useState(null);
    const [_isReadOnly, setIsReadOnly] = useState(true);
    const [_isValid, setIsValid] = useState({ name: true, profile: true, description: true, type: true, symbolicName: true, variableDataType: true });
    //const [_lookupProfiles, setLookupProfiles] = useState([]);
    //used in popup profile add/edit ui. Default to new version
    const [_profileEntityModal, setProfileEntityModal] = useState({ show: false, item: null, autoSave: false  });
    const _profileNew = { id: 0, namespace: '', version: null, publishDate: null, authorId: null };
    const [_lookupRelated, setLookupRelated] = useState({ compositions: [], interfaces: [], variableTypes: [] });

    const { wizardProps, setWizardProps } = useWizardContext();
    const _navInfo = history.location.pathname.indexOf('/wizard/') === - 1 ? null : getWizardNavInfo(wizardProps.mode, 'ExtendBaseType');
    const _currentPage = history.location.pathname.indexOf('/wizard/') === - 1 ? null : WizardSettings.panels.find(p => { return p.id === 'ExtendBaseType'; });
    
    //-------------------------------------------------------------------
    // Region: hooks - Execute once after component loads
    //-------------------------------------------------------------------
    useEffect(() => {
        // Init flags to detect unsaved changes and warn a user when they try to leave the page
        setLoadingProps({ bIsTypeEditUnsaved: false });
    }, []);

    //-------------------------------------------------------------------
    // Region: hooks
    //-------------------------------------------------------------------
    useEffect(() => {

        //only run this in wizard mode
        if (history.location.pathname.indexOf('/wizard/') === -1) return;

        //only want this to run once on load
        if (wizardProps != null && wizardProps.currentPage === _currentPage.id) {
            return;
        }

        //update state on load 
        setWizardProps({ currentPage: _currentPage.id });

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||wizardProps||Cleanup', CLASS_NAME));
            //setFilterValOnChild('');
        };
    }, [wizardProps.currentPage]);

    //-------------------------------------------------------------------
    // Region: hooks
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchData() {
            console.log(generateLogMessageString('useEffect||fetchData||async', CLASS_NAME));
            //initialize spinner during loading
            setLoadingProps({ isLoading: true, message: null });

            //mode not set right if we were on this page, save an extend and navigate into edit same profile. Rely on
            // parentId, id. Then determine mode. for extend, we use parentId, for edit/view, we use id.
            let result = null;
            try {
                //add logic for wizard scenario
                let data = { id: (parentId != null ? parentId : id) };
                let url = `profiletypedefinition/getbyid`;
                if (parentId != null && history.location.pathname.indexOf('/wizard/') > -1) {
                    url = `profiletypedefinition/wizard/extend`;
                    //add profile id in
                    data = { id: parentId, profileId: profileId }
                }
                else if (parentId != null) {
                    url = `profiletypedefinition/extend`;
                }

                result = await axiosInstance.post(url, data);
            }
            catch (err) {
                var msg = 'An error occurred retrieving this type definition.';
                console.log(generateLogMessageString('useEffect||fetchData||error', CLASS_NAME, 'error'));
                //console.log(err.response.status);
                if (err != null && err.response != null && err.response.status === 404) {
                    msg += ' This type was not found.';
                    history.push('/404');
                }
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: msg, isTimed: false }]
                });
            }

            if (result == null) return;

            // set view/edit mode now that we can compare user against author of item
            // just a check to make sure that if the mode is edit, but user isn't the author,
            // we force them back to view mode
            let thisMode = 'view';
            if (id != null) {
                thisMode = (result.data.isReadOnly || !isOwner(result.data, _activeAccount)) ? "view" : "edit";
            }
            // if we're extending a class, result will have the class we're inheriting from
            // and we'll need to move some info around to support this
            else if (parentId != null) {
                thisMode = "extend";
            }

            //convert collection to comma separated list
            //special handling of meta tags which shows as a concatenated list in an input box
            result.data.metaTagsConcatenated = result.data == null || result.data.metaTags == null ? "" : result.data.metaTags.join(', ');
            //set item state value
            setItem(result.data);
            setIsLoading(false);
            setLoadingProps({ isLoading: false, message: null });
            setMode(thisMode);
    
            // set form to readonly if we're in viewmode            
            setIsReadOnly(thisMode.toLowerCase() === "view");
            //setIsReadOnly(true);

            //add to the recent file list to keep track of where we have been
            if (thisMode.toLowerCase() === "view" || thisMode.toLowerCase() === "edit") {
                const revisedList = UpdateRecentFileList(loadingProps.recentFileList, {
                    url: history.location.pathname, caption: result.data.name, iconName: getTypeDefIconName(result.data),
                    authorId: result.data.author != null && result.data.isReadOnly === false ? result.data.author.objectIdAAD : null
                });
                setLoadingProps({ recentFileList: revisedList });
            }

        }

        //get a blank type object from server
        async function fetchDataAdd() {
            console.log(generateLogMessageString('useEffect||fetchDataAdd||async', CLASS_NAME));
            //initialize spinner during loading
            setLoadingProps({ isLoading: true, message: null });

            var result = null;
            try {
                const url = `profiletypedefinition/init`
                result = await axiosInstance.get(url);
            }
            catch (err) {
                var msg = 'An error occurred retrieving the blank type definition.';
                console.log(generateLogMessageString('useEffect||fetchDataAdd||error', CLASS_NAME, 'error'));
                //console.log(err.response.status);
                if (err != null && err.response != null && err.response.status === 404) {
                    msg += ' A problem occurred with the Add type screen.';
                    history.push('/404');
                }
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: msg, isTimed: false  }]
                });
            }

            if (result == null) return;

            //if we add from the profile id row, assign the profile id that triggered the add
            if (profileId != null) {
                result.data.profileId = parseInt(profileId);
                result.data.profile = { id: parseInt(profileId) };
            }
            //set item state value
            setItem(result.data);
            setIsLoading(false);
            setLoadingProps({ isLoading: false, message: null });
            //setMode(thisMode);
            setIsReadOnly(false);
        }

        //fetch our data
        // for view/edit modes
        if ((id != null && id.toString() !== 'new') || parentId != null) {
            fetchData();
        }
        else {
            fetchDataAdd();
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [id, parentId, profileId ]);

    useEffect(() => {
        setPermittedDataTypes(_lookupDataTypes);
    }, [_lookupDataTypes]);

    useEffect(() => {
        const newPermittedDataTypes = getPermittedDataTypesForVariableTypeById(_item.parent?.variableDataTypeId, _lookupDataTypes);
        if (newPermittedDataTypes != null) {
            setPermittedDataTypes(newPermittedDataTypes)
        }
        else {
            setPermittedDataTypes(_lookupDataTypes);
        }
    }, [_item]);

    //-------------------------------------------------------------------
    // Region: Hooks - When composition data type is chosen, go get a list of profiles
    //      where the profile is neither a descendant or a parent/grandparent, etc. of the profile we 
    //      are working with
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchLookupProfileTypeDefs(lookupId, isExtend) {

            //placeholder vals while we load
            setLookupRelated({
                ..._lookupRelated,
                compositions: [{ id: -1, name: 'Loading...', profile: { id: -1, title: '', namespace: '', version: '', publishDate: '' } },],
                variableTypes: [{ id: -1, name: 'Loading...', profile: { id: -1, title: '', namespace: '', version: '', publishDate: '' } },]
            });

            //Filter out anything
            //where the profile is neither a descendant or a parent/grandparent, etc. of the profile we 
            //are working with, can't be a dependency of this type definition
            // If we are working with a profile, then composition can't be an interface type
            // If we are working with an interface, then composition can't be a profile type
            const data = { id: lookupId };
            const url = `profiletypedefinition/lookup/profilerelated${isExtend ? '/extend' : ''}`;
            console.log(generateLogMessageString(`useEffect||fetchLookupProfileTypeDefs||${url}`, CLASS_NAME));
            //const result = await axiosInstance.post(url, data);

            await axiosInstance.post(url, data).then(result => {
                if (result.status === 200) {
                    //profile id - 3 scenarios - 1. typical - use profile id, 2. extend profile where parent profile should be used, 
                    //      3. new profile - no parent, no inheritance, use 0 
                    //var pId = props.typeDefinition.id;
                    //if (props.typeDefinition.id === 0 && props.typeDefinition.parent != null) pId = props.typeDefinition.parent.id;

                    //TBD - handle paged data scenario, do a predictive search look up
                    setLookupRelated({
                        compositions: result.data.compositions,
                        interfaces: result.data.interfaces,
                        variableTypes: result.data.variableTypes,
                    });
                } else {
                    console.warn(generateLogMessageString(`useEffect||fetchLookupProfileTypeDefs||error||status:${result.status}`, CLASS_NAME));
                    setLookupRelated({
                        ..._lookupRelated,
                        compositions: [{ id: -1, name: 'Error loading composition data...', profile: { id: -1, title: '', namespace: '', version: '', publishDate: '' } }],
                        variableTypes: [{ id: -1, name: 'Error loading variable type data...', profile: { id: -1, title: '', namespace: '', version: '', publishDate: '' } }]
                    });
                }
            }).catch(e => {
                if (e.response && e.response.status === 401) {
                    console.error(generateLogMessageString(`useEffect||fetchLookupProfileTypeDefs||error||status:${e.response.status}`, CLASS_NAME));
                }
                else {
                    console.error(generateLogMessageString(`useEffect||fetchLookupProfileTypeDefs||error||status:${e.response && e.response.data ? e.response.data : `A system error has occurred during the profile api call.`}`, CLASS_NAME));
                    console.log(e);
                }
                setLookupRelated({
                    ..._lookupRelated,
                    compositions: [{ id: -1, name: 'Error loading composition data...', profile: { id: -1, title: '', namespace: '', version: '', publishDate: '' } }],
                    variableTypes: [{ id: -1, name: 'Error loading variable type data...', profile: { id: -1, title: '', namespace: '', version: '', publishDate: '' } }]
                });
            });
        }

        //we load even if we might be in readonly mode because we don't know if readonly until 
        //we get the full data back and evaluate ownership. We want to run this in parallel
        //and thus can't wait to know if readonly. 
        fetchLookupProfileTypeDefs(
            parentId != null ? parentId : (id != null && id.toString() !== 'new' ? id : null),
            parentId != null
        );

    }, [id, parentId]);

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
        }

        initLookupData();

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [loadingProps.lookupDataStatic, loadingProps.lookupDataRefreshed]);

    //-------------------------------------------------------------------
    //
    //-------------------------------------------------------------------
    function initPageMode() {
        //if path contains extend and parent id is set, mode is extend
        //else - we won't know the author ownership till we fetch data, default view
        if (parentId != null && history.location.pathname.indexOf('/extend/') > -1) return 'extend';

        //if path contains new, then go into a new mode
        if (id === 'new') {
            return 'new';
        }

        //if path contains id, then default to view mode and determine in fetch whether user is owner or not.
        return 'view';
    }

    function buildTitleCaption(formMode) {
        let result = getProfileTypeCaption(_item);
        if (formMode != null) {
            switch (formMode.toLowerCase()) {
                case "new":
                    result = `New ${result}`
                    break;
                case "edit":
                    result = `Edit ${result}`;
                    break;
                case "extend":
                    result = `Extend ${result}`;
                    break;
                case "view":
                default:
                    result = `View ${result}`;
                    break;
            }
        }
        return result;
    }

    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------
    const validateForm_name = (e) => {
        const isValid = (e.target.value != null && e.target.value.trim().length > 0);
        setIsValid({ ..._isValid, name: isValid });
    };

    const validateForm_description = () => {
    };

    const validateForm_type = (e) => {
        const isValid = e.target.value.toString() !== "-1";
        setIsValid({ ..._isValid, type: isValid });
    };

    const validateForm_symbolicName = (e) => {
        setIsValid({ ..._isValid, symbolicName: validate_symbolicName(e.target.value) });
    };

    const validate_symbolicName = (val) => {
        const format = /[ `!@#$%^&*()+\-=[\]{};':"\\|,.<>/?~(0-9)]/;  //includes a space, underscore allowed
        return val == null || val.length === 0 || !format.test(val);
    };

    const validateForm_variableDataType = (e) => {
        const isValid = validate_variableDataType(_item.variableDataType);
        setIsValid({ ..._isValid, variableDataType: isValid });
    };

    const validate_variableDataType = (vdt) => {
        var lookupDataType = _lookupDataTypes.find(dt => { return dt.customTypeId.toString() === vdt?.id?.toString(); });
        var baseVTDataType = _lookupDataTypes.find(dt => { return dt.customTypeId.toString() === _item.parent?.variableDataTypeId?.toString(); })
        if (lookupDataType != null && baseVTDataType != null) {
            return isDerivedFromDataType(lookupDataType, baseVTDataType, _lookupDataTypes);
        }
        return lookupDataType == baseVTDataType; // ok if both are null or if they are identical
    }

    ////update state for when search click happens
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        _isValid.name = _item.name != null && _item.name.trim().length > 0;
        _isValid.profile = _item.profile != null && _item.profile.id !== -1 && _item.profile.id !== "-1";
        _isValid.description = true; //item.description != null && item.description.trim().length > 0;
        _isValid.type = _item.type != null && _item.type.id !== -1 && _item.type.id !== "-1";
        _isValid.symbolicName = validate_symbolicName(_item.symbolicName);
        _isValid.variableDataType = validate_variableDataType(_item.variableDataType);

        setIsValid(JSON.parse(JSON.stringify(_isValid)));
        return (_isValid.name && _isValid.profile && _isValid.description && _isValid.type && _isValid.symbolicName && _isValid.variableDataType);
    }

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onSave = () => {
        //raised from header nav
        console.log(generateLogMessageString('onSave', CLASS_NAME));

        //do validation
        if (!validateForm()) {
            //if profile the only one not valid, then show the user the profile selection/add ui.
            if (_isValid.name && _isValid.type && _isValid.description && !_isValid.profile) {
                setProfileEntityModal({ show: true, item: null, autoSave: true });
                return;
            }
            //otherwise, return and have user correct the other required items. 
            //alert("validation failed");
            return;
        }

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //convert my metatags string back into array representation for saving
        //split the string into array and then build out array of tags
        _item.metaTags = _item.metaTagsConcatenated == null || _item.metaTagsConcatenated.trim().length === 0 ?
            null : _item.metaTagsConcatenated.split(",").map(x => x.trim(' '));

        // Everything saved - no need to warn the user about unsaved changes.
        setLoadingProps({ bIsTypeEditUnsaved: false });

        //perform insert call
        console.log(generateLogMessageString(`handleOnSave||${mode}`, CLASS_NAME));
        var url = mode.toLowerCase() === "extend" || mode.toLowerCase() === "new" ?
            `profiletypedefinition/add` : `profiletypedefinition/update`;
        axiosInstance.post(url, _item)
            .then(resp => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "success", body: `Type was ${mode.toLowerCase() === "extend" ? "extended" : "saved"}`, isTimed: true  }
                    ],
                    //for new/extend - get type count from server...this will trigger that call on the side menu
                    refreshTypeCount: mode.toLowerCase() === "extend" || mode.toLowerCase() === "new" ? true : loadingProps.refreshTypeCount
                });

                //On Add, redirect to edit type page in edit mode
                if (mode.toLowerCase() === "extend" || mode.toLowerCase() === "new") {
                    history.push(entityInfo.entityUrl.replace(':id', resp.data.data));
                }
                else {
                    //TBD - on edit, for now, re-get the item fresh from server. Having trouble server side returning the
                    //fully updated item
                    history.push(entityInfo.entityUrl.replace(':id', resp.data.data));

                }
                //do this as final step so we don't cause issues for something looking for wizardProps before navigation
                //if we were using wizard, then clear out wizard props
                if (history.location.pathname.indexOf('/wizard/') > - 1) {
                    setWizardProps({
                        currentPage: null, mode: null, profile: null, parentId: null
                    });
                }
            })
            .catch(error => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: `An error occurred ${mode.toLowerCase() === "extend" ? "extending" : "saving"} this type definition.`, isTimed: false }
                    ]
                });
                console.log(generateLogMessageString('handleOnSave||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
                //scroll back to top
                window.scroll({
                    top: 0,
                    left: 0,
                    behavior: 'smooth',
                });
            });
    };

    const onToggleFavorite = (e) => {
        //raised from header nav
        console.log(generateLogMessageString('onToggleFavorite', CLASS_NAME));

        e.preventDefault();

        //show a spinner
        //setLoadingProps({ isLoading: true, message: "" });

        //perform update call
        const url = `profiletypedefinition/togglefavorite`;
        const data = { id: _item.id };
        axiosInstance.post(url, data)
            .then(resp => {
                //hide a spinner, trigger refresh of favorites list
                setLoadingProps({ isLoading: false, refreshFavoritesList: true });
                _item.isFavorite = !_item.isFavorite;
                setItem(JSON.parse(JSON.stringify(_item)));
            })
            .catch(error => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: `An error occurred toggling favorite. Please try again.`, isTimed: true }
                    ]
                });
                console.log(generateLogMessageString('toggleFavorite||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
            });
    };

    //on change handler to update state
    const onChange = (e) => {

        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        switch (e.target.id) {
            case "name":
            case "description":
            case "metaTagsConcatenated":
            case "browseName":
            case "symbolicName":
                _item[e.target.id] = e.target.value;
                break;
            case "isAbstract":
                _item[e.target.id] = e.target.checked;
                break;
            case "profile":
                if (e.target.value.toString() === "-1") {
                    _item.profileId = null;
                    _item.profile = null;
                }
                if (e.target.value.toString() === "-99") {
                    _item.profileId = null;
                    _item.profile = null;
                    onAddProfile();
                    return;
                }
                else {
                    _item.profileId = e.target.value;
                    _item.profile = { id: e.target.value, namespace: e.target.options[e.target.selectedIndex].text };
                }
                break;
            case "type":
                if (e.target.value.toString() === "-1") _item.type = null;
                else {
                    _item.type = { id: e.target.value, name: e.target.options[e.target.selectedIndex].text };
                }
                break;
            default:
                return;
        }
        //update the state
        setItem(JSON.parse(JSON.stringify(_item)));

        // Note unsaved changes
        setLoadingProps({ bIsTypeEditUnsaved: true });
    }

    const onAddProfile = () => {
        console.log(generateLogMessageString(`onAddProfile`, CLASS_NAME));
        setProfileEntityModal({ show: true, item: JSON.parse(JSON.stringify(_profileNew)) });
    };

    //add profile on the fly then go get a refreshed list of items.
    const onSaveProfile = (p) => {
        console.log(generateLogMessageString(`onSaveProfile`, CLASS_NAME));
        const autoSave = _profileEntityModal.autoSave;
        setProfileEntityModal({ show: false, item: null, autoSave: false });

        //and then assign it to the new item
        _item.profileId = p.id;
        _item.profile = p;
        setItem(JSON.parse(JSON.stringify(_item)));
        setIsValid({ ..._isValid, profile: true });

        //now that we have a profile assigned/attempt to save profile type definition again.
        if (autoSave) onSave();
    };

    const onSaveProfileCancel = () => {
        console.log(generateLogMessageString(`onSaveProfileCancel`, CLASS_NAME));
        setProfileEntityModal({ show: false, item: null });
    };

    //change profile selection if in add mode only
    const onSelectProfile = (e) => {
        setProfileEntityModal({ show: true, item: _item.profile, autoSave: false });
        e.preventDefault();
    };


    //raised from add button click in child component
    const onAttributeAdd = (row) => {
        console.log(generateLogMessageString(`onAttributeAdd||item:${JSON.stringify(row.name)}`, CLASS_NAME));

        //assign parent relationship for server side. 
        row.typeDefinitionId = _item.id;

        //make copy of item
        var itemCopy = JSON.parse(JSON.stringify(_item));

        if (itemCopy.profileAttributes == null) itemCopy.profileAttributes = [];
        // doing the JSON copy stuff here to break up the object reference
        itemCopy.profileAttributes.push(JSON.parse(JSON.stringify(row)));

        setItem(JSON.parse(JSON.stringify(itemCopy)));
        return {
            profileAttributes: itemCopy.profileAttributes, extendedProfileAttributes: itemCopy.extendedProfileAttributes
        };
    };

    //add an interface which adds a collection of attributes from an interface object
    const onAttributeInterfaceAdd = (iface, profileAttributes, extendedProfileAttributes) => {
        //raised from add button click in child component
        console.log(generateLogMessageString(`onAttributeInterfaceAdd||interface:${iface.name}`, CLASS_NAME));

        //make copy of item
        var itemCopy = JSON.parse(JSON.stringify(_item));

        //replace the current attributes with the revised list.
        itemCopy.profileAttributes = JSON.parse(JSON.stringify(profileAttributes));
        itemCopy.extendedProfileAttributes = JSON.parse(JSON.stringify(extendedProfileAttributes));

        // add the interface to the interfaces collection
        if (itemCopy.interfaces == null) itemCopy.interfaces = [];
        itemCopy.interfaces.push(JSON.parse(JSON.stringify(iface)));

        setItem(JSON.parse(JSON.stringify(itemCopy)));
        return {
            profileAttributes: itemCopy.profileAttributes, extendedProfileAttributes: itemCopy.extendedProfileAttributes
        };
    };

    const onAttributeDelete = (key) => {
        //raised from del button click in child component
        console.log(generateLogMessageString(`onAttributeDelete||item id:${key}`, CLASS_NAME));

        const x = _item.profileAttributes.findIndex(attr => { return attr.id === key; });
        //no item found
        if (x < 0) {
            console.warn(generateLogMessageString(`onAttributeDelete||no item found to delete with this id`, CLASS_NAME));
            return {
                profileAttributes: _item.profileAttributes, extendedProfileAttributes: _item.extendedProfileAttributes
            };
        }
        //remove the item with the matching id. If the id < 0, it was created client side and removing it will not have impact
        //server side. For items removed here, the server side code will remove from the original collection
        _item.profileAttributes.splice(x, 1);

        //update the state, return collection to child components
        const itemCopy = JSON.parse(JSON.stringify(_item));
        setItem(itemCopy);
        return {
            profileAttributes: itemCopy.profileAttributes, extendedProfileAttributes: itemCopy.extendedProfileAttributes
        };
    };

    //delete all attributes for passed in interface id. 
    const onAttributeInterfaceDelete = (key) => {
        //raised from del button click in child component
        console.log(generateLogMessageString(`onAttributeInterfaceDelete||interface id:${key}`, CLASS_NAME));

        //delete the interface reference
        const x = _item.interfaces.findIndex(i => { return i.id === key; });
        //no item found
        if (x < 0) {
            console.warn(generateLogMessageString(`onAttributeInterfaceDelete||no interface found to delete with id: ${key}`, CLASS_NAME));
            return {
                profileAttributes: _item.profileAttributes, extendedProfileAttributes: _item.extendedProfileAttributes
            };
        }
        //remove the interface with the matching id. 
        _item.interfaces.splice(x, 1);

        //Remove associated attributed. Can only delete interface in my attributes, not in extended attrib.
        //note all interface attributes are in the extended collection
        var matches = _item.extendedProfileAttributes.filter(attr => { return attr.interface == null || attr.interface.id !== key; });

        //no items found
        if (matches.length === _item.extendedProfileAttributes.length) {
            console.warn(generateLogMessageString(`onAttributeInterfaceDelete||no attributes found to delete with this interface id`, CLASS_NAME));
            return {
                profileAttributes: _item.profileAttributes, extendedProfileAttributes: _item.extendedProfileAttributes
            };
        }

        //update the state, return attrib collection to child components
        _item.extendedProfileAttributes = matches;
        const itemCopy = JSON.parse(JSON.stringify(_item));
        setItem(itemCopy);
        return {
            profileAttributes: itemCopy.profileAttributes, extendedProfileAttributes: itemCopy.extendedProfileAttributes
        };
    };

    const onAttributeUpdate = (attr) => {
        //raised from del button click in child component
        console.log(generateLogMessageString(`onAttributeUpdate||item id:${attr.id}`, CLASS_NAME));

        const aIndex = _item.profileAttributes.findIndex(a => { return a.id === attr.id; });
        //no item found
        if (aIndex === -1) {
            console.warn(generateLogMessageString(`onAttributeUpdate||no item found with this id`, CLASS_NAME));
            return {
                profileAttributes: _item.profileAttributes, extendedProfileAttributes: _item.extendedProfileAttributes
            };
        }
        //replace attr
        _item.profileAttributes[aIndex] = JSON.parse(JSON.stringify(attr));

        //update the state, return collection to child components
        const itemCopy = JSON.parse(JSON.stringify(_item));
        setItem(itemCopy);
        //return item local b/c item is not yet updated in state
        return {
            profileAttributes: itemCopy.profileAttributes, extendedProfileAttributes: itemCopy.extendedProfileAttributes
        };
    };

    //if delete goes through, navigate to profiles library page
    const onDelete = (success) => {
        console.log(generateLogMessageString(`onDelete || ${success}`, CLASS_NAME));
        if (success) {
            history.push(`/types/library`);
        }
    };

    //onchange data type
    const onChangeVariableDataType = (e) => {
        var lookupItem = _lookupDataTypes.find(dt => { return dt.id.toString() === e.value.toString(); });

        _item.variableDataType = 
            lookupItem != null ?
            { id: lookupItem.customTypeId, name: lookupItem.name }
            : { id: -1, name: '' };
        setItem(JSON.parse(JSON.stringify(_item)));
    }


    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderProfileBreadcrumbs = () => {
        if (_item == null || _item.parent == null) return;

        if (history.location.pathname.indexOf('/wizard/') > - 1) {
            return (
                <>
                    {renderWizardBreadcrumbs(wizardProps.mode, _navInfo.stepNum)}
                    <ProfileBreadcrumbs item={_item} />
                </>
            );
        }
        return (
            <ProfileBreadcrumbs item={_item} />
        );
    };

    const renderProfileDefType = () => {
        //show readonly input for view mode
        if (_isReadOnly) {
            return (
                <Form.Group>
                    <Form.Label htmlFor="type">Type</Form.Label>
                    <Form.Control id="type" type="" value={_item.type != null ? _item.type.name : ""} readOnly={_isReadOnly} />
                </Form.Group>
            )
        }
        //show readonly input for extend mode. You have to extend the same type
        //show readonly input for edit mode. There are downstream implications to the type that should prevent changing here. 
        if (_item.type != null && (mode.toLowerCase() === "extend" || mode.toLowerCase() === "edit" )) {
            return (
                <Form.Group>
                    <Form.Label htmlFor="type">Type</Form.Label>
                    <Form.Control id="type" type="" value={_item.type.name} readOnly={true} />
                </Form.Group>
            )
        }
        //if extending, then user can only choose certain types. 
        //show drop down list for edit, extend mode
        const options = loadingProps.lookupDataStatic.profileTypes.map((item) => {
            return (<option key={item.id} value={item.id} >{item.name}</option>)
        });

        return (
            <Form.Group>
                <Form.Label htmlFor="type">Type</Form.Label>
                {!_isValid.type &&
                    <span className="invalid-field-message inline">
                        Required
                    </span>
                }
                <Form.Control id="type" as="select" className={(!_isValid.type ? 'invalid-field minimal pr-5' : 'minimal pr-5')} value={_item.type == null ? "-1" : _item.type.id}
                    onBlur={validateForm_type} onChange={onChange} >
                    <option key="-1|Select One" value="-1" >Select</option>
                    {options}
                </Form.Control>
            </Form.Group>
        )
    };

    //if anything is invalid, show a warning...
    const renderValidationMessage = () => {

        //name the field(s) with the issue
        let fieldError = [];
        if (!_isValid.name) fieldError.push(`Name is required.`);
        if (!_isValid.profile) fieldError.push(`Profile is required.`);
        if (!_isValid.description) fieldError.push(`Description is required.`);
        if (!_isValid.type) fieldError.push(`Type is required.`);
        if (!_isValid.symbolicName) fieldError.push(`Symbolic Name is invalid (Advanced tab).`);
        if (!_isValid.variableDataType) fieldError.push(`Variable Data Tyype is required.`);

        if (fieldError.length === 0) return null;

        //add extra row and col-md-12 to get align to match with surroundings
        return (
            <div className="row pt-2 mb-2 no-gutters">
                <div className="col-md-12 px-2 alert alert-danger">
                    {fieldError.length === 1 ?
                        fieldError[0] :
                        `Validation Errors: ` + fieldError.join(' ')
                    }
                </div>
            </div>
        );
    };

    //render Profile Row as a read-only reminder of the parent
    const renderProfile = () => {

        const actionUI = (_item.id != null && _item.id > 0 && _item.profile != null) ? null :
            (
                <button type="button" className="btn btn-secondary auto-width" onClick={onSelectProfile} title="Select Profile" >Select</button>
            );

        return (
            <div className="row pt-2 mb-2">
                <div className="col-md-12">
                    <ProfileItemRow key="p-1" mode="simple" item={_item.profile} actionUI={actionUI}
                        activeAccount={_activeAccount} cssClass="shaded profile-list-item rounded no-gutters px-2" />
                </div>
            </div>
        );
    };

    const renderVariableDataType = () => {
        if (_item.typeId !== AppSettings.ProfileTypeDefaults.VariableTypeId) {
            return null;
        }
        if (_isReadOnly) {

            return (
                <div className="col-lg-4 col-md-6">
                    <Form.Group>
                        <Form.Label htmlFor="type">Data Type of the Variable</Form.Label>
                        <Form.Control id="type" type="" value={_item.variableDataType != null ? _item.variableDataType.name : ""} readOnly={_isReadOnly} />
                    </Form.Group>
                </div>
            );
        }
        else {
            return (
                <div className="col-lg-4 col-md-6">
                    {renderVariableDataTypeUI()}
                </div>
            );
        }
    };

    const renderVariableDataTypeUI = () => {
        var lookupItem = _lookupDataTypes.find(dt => { return dt.customTypeId.toString() === _item.variableDataType?.id?.toString(); });
            //set isValid === true always - not required here...
        return renderDataTypeUIShared(lookupItem, _permittedDataTypes, null, _isValid.variableDataType, true, "Data Type of the Variable", onChangeVariableDataType, validateForm_variableDataType)
    }

    //renderProfileEntity as a modal to force user to say ok.
    const renderProfileModal = () => {

        if (!_profileEntityModal.show) return;

        return (
            <ProfileEntityModal item={_profileEntityModal.item} showModal={_profileEntityModal.show} onSave={onSaveProfile} onSelect={onSaveProfile} onCancel={onSaveProfileCancel} showSavedMessage={false}
                showSelectUI={true}
            />
        );
    };

    const renderMoreDropDown = () => {
        if (_item == null || (mode.toLowerCase() === "extend" || mode.toLowerCase() === "new") ) return;

        //React-bootstrap bug if you launch modal, then the dropdowns don't work. Add onclick code to the drop down as a workaround - https://github.com/react-bootstrap/react-bootstrap/issues/5561
        return (
            <TypeDefinitionActions item={_item} activeAccount={_activeAccount} onDeleteCallback={onDelete} showExtend={true} className='ml-2' isReadOnly={_isReadOnly} />
        );
    }

    const renderButtons = () => {
        const urlCancel = history.location.pathname.indexOf('/wizard/') > - 1 ? _navInfo.prev.href : '/types/library';
        const captionCancel = history.location.pathname.indexOf('/wizard/') > - 1 ? 'Back' : 'Cancel';
        if (mode.toLowerCase() !== "view") {
            return (
                <>
                    <Button variant="text-solo" className="mx-1 d-none d-lg-block btn-auto auto-width" href={urlCancel} >{captionCancel}</Button>
                    <Button variant="secondary" type="button" className="mx-3 d-none d-lg-block" onClick={onSave} >Save</Button>
                    <Button variant="icon-solo" type="button" className="mx-1 d-lg-none" href={urlCancel} title={captionCancel} ><i className="material-icons">close</i></Button>
                    <Button variant="icon-solo" type="button" className="mx-1 d-lg-none" onClick={onSave} title="Save" ><i className="material-icons">save</i></Button>
                </>
            );
        }
    }

    const renderCommonSection = () => {

        const iconName = mode.toLowerCase() === "extend" ? "extend" : getTypeDefIconName(_item);
        const iconColor = getIconColorByProfileState(_isReadOnly ? AppSettings.ProfileStateEnum.Core : AppSettings.ProfileStateEnum.Local);

        return (
            <>
                <Prompt
                    when={loadingProps.bIsTypeEditUnsaved}
                    message="Unsaved changes will be lost. Ok to exit the page? To save, click Cancel then click Save."
                />
                {renderValidationMessage()}
                <div className="row my-1">
                    <div className="col-sm-9 col-md-8 align-self-center" >
                        <h1 className="mb-0 pl-3">
                            <SVGIcon name={iconName} fill={iconColor} alt={iconName} size="24" className="mr-1 mr-md-2" />
                            Type Definition
                            {(_item.name != null && _item.name.trim() !== '') &&
                                <>
                                    <span className="d-none d-lg-inline mx-1" >-</span>
                                    <br className="d-block d-lg-none" />{_item.name}
                                </>
                            }
                            {(_item.id != null && _item.id > 0) &&
                                <button className="btn btn-icon btn-nofocusborder favorite px-2 py-0" onClick={onToggleFavorite} title={_item.isFavorite ? "Unfavorite item" : "Favorite item"} >
                                    <i className="material-icons size24" >{_item.isFavorite ? "favorite" : "favorite_border"}</i>
                                </button>
                            }
                        </h1>
                    </div>
                    <div className="col-sm-3 col-md-4 d-flex align-items-center justify-content-end" >
                        {renderButtons()}
                        {renderMoreDropDown()}
                    </div>
                </div>
                {renderProfile()}
                <div className="row">
                    {(mode.toLowerCase() !== "view") &&
                        <div className="col-lg-5 col-md-6">
                            <Form.Label htmlFor="name" >Name</Form.Label>
                            {!_isValid.name &&
                                <span className="ml-2 d-inline invalid-field-message">Required</span>
                            }
                            <div className={`input-group ${(!_isValid.name ? "invalid-group" : "")}`} >
                                <Form.Group className="flex-grow-1 m-0">
                                    <Form.Control className={(!_isValid.name ? `invalid-field` : ``)} id="name" type="" placeholder={`Enter name`}
                                        value={_item.name} onBlur={validateForm_name} onChange={onChange} readOnly={_isReadOnly} />
                                </Form.Group>
                            </div>
                        </div>
                    }
                    {renderVariableDataType()}
                    <div className="col-lg-3 col-md-6">
                        {renderProfileDefType()}
                    </div>
                </div>
                <div className="row mb-3">
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label htmlFor="description" >Description</Form.Label>
                            <Form.Control id="description" type="" placeholder={`Describe your ${caption.toLowerCase()}`}
                                value={_item.description == null ? '' : _item.description} onBlur={validateForm_description} onChange={onChange} readOnly={mode === "view"} />
                        </Form.Group>
                    </div>
                </div>
            </>

        )
    }

    const renderAdvancedPane = () => {
        return (
            <>
                <div className="row mt-1">
                    <div className="col-md-6">
                        <Form.Group>
                            <Form.Label htmlFor="entity_author" >Author name</Form.Label>
                            <Form.Control id="entity_author" type="" placeholder="Enter your name here" value={_item.author?.fullName} onChange={onChange} readOnly={_isReadOnly} disabled='disabled' />
                        </Form.Group>
                    </div>
                    <div className="col-md-6">
                        <Form.Group>
                            <Form.Label htmlFor="entity_organization" >Organization</Form.Label>
                            <Form.Control id="entity_organization" type="" placeholder="Enter your organization's name" value={_item.author?.organization?.name} onChange={onChange} readOnly={_isReadOnly} disabled='disabled' />
                        </Form.Group>
                    </div>
                </div>
                <div className="row mt-1">
                    <div className="col-sm-6">
                        <Form.Group>
                            <Form.Label htmlFor="browseName" >OPC Browse Name</Form.Label>
                            <Form.Control id="browseName" type="" placeholder="" readOnly={_isReadOnly}
                                value={_item.browseName != null ? _item.browseName : ""} onChange={onChange}  />
                        </Form.Group>
                    </div>
                    <div className="col-sm-6">
                        <Form.Group>
                            <Form.Label htmlFor="symbolicName" >Symbolic Name</Form.Label>
                            {!_isValid.symbolicName &&
                                <span className="invalid-field-message inline">
                                    No numbers, spaces or special characters permitted
                                </span>
                            }
                            <Form.Control id="symbolicName" className={(!_isValid.symbolicName ? `invalid-field` : ``)} type="" placeholder="" readOnly={_isReadOnly}
                                value={_item.symbolicName != null ? _item.symbolicName : ""} onChange={onChange} onBlur={validateForm_symbolicName} />
                        </Form.Group>
                    </div>
                </div>
                <div className="row mt-1">
                    <div className="col-sm-4">
                        <Form.Group>
                            <Form.Label htmlFor="opcNodeId" >OPC Node Id</Form.Label>
                            <Form.Control id="opcNodeId" type="" placeholder=""
                                value={_item.opcNodeId == null ? '' : _item.opcNodeId} onChange={onChange} readOnly="readOnly" />
                        </Form.Group>
                    </div>
                </div>
                <div className="row mt-1">
                    <div className="col-sm-6 col-lg-2">
                        {!_isReadOnly ?
                            <Form.Group className="d-flex h-100">
                                <Form.Check className="align-self-end" type="checkbox" id="isAbstract" label="Is Abstract" checked={_item.isAbstract}
                                    onChange={onChange} />
                            </Form.Group>
                            :
                            <Form.Group>
                                <Form.Label>Is Abstract</Form.Label>
                                <Form.Control id="isAbstract" type="" placeholder=""
                                    value={_item.isAbstract} readOnly="readOnly" />
                            </Form.Group>
                        }
                    </div>
                </div>
                <div className="row mt-1">
                    <div className="col-sm-12">
                        <Form.Group>
                            <Form.Label htmlFor="metaTagsConcatenated" >Meta tags (optional)</Form.Label>
                            <Form.Control id="metaTagsConcatenated" type="" placeholder="Enter tags seperated by a comma" value={_item.metaTagsConcatenated} onChange={onChange} readOnly={_isReadOnly} />
                        </Form.Group>
                    </div>
                </div>
                <div className="row mt-1">
                    <div className="col-sm-12">
                        <Form.Group>
                            <Form.Label htmlFor="documentUrl">Document Url</Form.Label>
                            <Form.Control id="documentUrl" type="" placeholder="Enter Url to the reference documentation (if applicable)."
                                value={_item.documentUrl != null ? _item.documentUrl : ""} onChange={onChange} readOnly={_isReadOnly} />
                        </Form.Group>
                    </div>
                </div>
            </>
        );
    }

    // TBD: need loop to remove and add styles for "nav-item" CSS animations
    const tabListener = (eventKey) => {
    }


    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    const titleCaption = buildTitleCaption(mode);
    const caption = getProfileTypeCaption(_item);

    const renderMainContent = () => {
        if (loadingProps.isLoading || isLoading) return;
        return (
            <div className="profile-entity">
                <Form noValidate>
                    {renderCommonSection()}

                    <div className="entity-details">
                        {/* TABS */}
                        <Tab.Container id="profile-definition" defaultActiveKey="attributes" onSelect={tabListener}>
                            <Nav variant="pills" className="row mt-1 px-2 pr-md-3">
                                <Nav.Item className="col-sm-4 rounded p-0 pl-2" >
                                    <Nav.Link eventKey="attributes" className="text-center text-md-left p-1 px-2 h-100" >
                                        <span className="headline-3">Attributes</span>
                                        {/*<span className="d-none d-md-inline"><br />The properties that define this type definition</span>*/}
                                    </Nav.Link>
                                </Nav.Item>
                                <Nav.Item className="col-sm-4 rounded p-0 px-md-0" >
                                    <Nav.Link eventKey="dependencies" className="text-center text-md-left p-1 px-2 h-100" >
                                        <span className="headline-3">Extended By</span>
                                        {/*<span className="d-none d-md-inline"><br />Type Definitions that depend on 'me'</span>*/}
                                    </Nav.Link>
                                </Nav.Item>
                                <Nav.Item className="col-sm-4 rounded p-0 pr-2">
                                    <Nav.Link eventKey="advanced" className="text-center text-md-left p-1 px-2 h-100" >
                                        <span className="headline-3">Advanced</span>
                                        {/*<span className="d-none d-md-inline"><br />Optional and advanced settings</span>*/}
                                    </Nav.Link>
                                </Nav.Item>
                            </Nav>

                            <Tab.Content>
                                <Tab.Pane eventKey="attributes">
                                    {/* ATTRIBUTES CONTENT */}
                                    <Card className="">
                                        <Card.Body className="pt-3">
                                            <AttributeList typeDefinition={_item} profileAttributes={_item.profileAttributes} extendedProfileAttributes={_item.extendedProfileAttributes} readOnly={mode === "view"}
                                                onAttributeAdd={onAttributeAdd} onAttributeInterfaceAdd={onAttributeInterfaceAdd} activeAccount={_activeAccount}
                                                onAttributeDelete={onAttributeDelete} onAttributeInterfaceDelete={onAttributeInterfaceDelete} onAttributeUpdate={onAttributeUpdate}
                                                lookupRelated={_lookupRelated}
                                            />
                                        </Card.Body>
                                    </Card>
                                </Tab.Pane>
                                <Tab.Pane eventKey="dependencies">
                                    {/* DEPENDENCIES CONTENT */}
                                    <Card className="">
                                        <Card.Body className="pt-3">
                                            <DependencyList typeDefinition={_item} activeAccount={_activeAccount} />
                                        </Card.Body>
                                    </Card>
                                </Tab.Pane>
                                <Tab.Pane eventKey="advanced">
                                    {/* advanced CONTENT */}
                                    <Card className="">
                                        <Card.Body className="pt-3">
                                            {renderAdvancedPane()}
                                        </Card.Body>
                                    </Card>
                                </Tab.Pane>
                            </Tab.Content>
                        </Tab.Container>
                    </div>
                    <div className="d-flex align-items-center justify-content-end">
                        <div className="d-flex align-items-center">
                            {renderButtons()}
                        </div>
                    </div>
                </Form>
            </div>
        );
    };



    //return final ui
    return (
        <>
            <Helmet>
                <title>{AppSettings.Titles.Main + " | " + titleCaption}</title>
            </Helmet>
            {renderProfileBreadcrumbs()}
            {renderMainContent()}
            {renderProfileModal()}

        </>
    )
}

export default ProfileTypeDefinitionEntity;
