import React, { useState, useEffect } from 'react'
import { useParams, useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import axiosInstance from "../services/AxiosService";

import Form from 'react-bootstrap/Form'
import Card from 'react-bootstrap/Card'
import Button from 'react-bootstrap/Button'
import Dropdown from 'react-bootstrap/Dropdown'
import Tab from 'react-bootstrap/Tab'
import Nav from 'react-bootstrap/Nav'

import { useLoadingContext, UpdateRecentFileList } from "../components/contexts/LoadingContext";
import { useAuthState } from "../components/authentication/AuthContext";
import { useWizardContext } from '../components/contexts/WizardContext';
import { AppSettings } from '../utils/appsettings';
import { generateLogMessageString, getTypeDefIconName, getProfileTypeCaption, cleanFileName, validate_NoSpecialCharacters } from '../utils/UtilityService'
import AttributeList from './shared/AttributeList';
import DependencyList from './shared/DependencyList';
import ProfileBreadcrumbs from './shared/ProfileBreadcrumbs';
import ProfileEntityModal from './modals/ProfileEntityModal';
import ProfileItemRow from './shared/ProfileItemRow';

import { SVGIcon } from "../components/SVGIcon";
import color from "../components/Constants";
import { getProfileCaption } from '../services/ProfileService';
import { getWizardNavInfo, renderWizardBreadcrumbs, WizardSettings } from '../services/WizardUtil';

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

    const { id, parentId, profileId } = useParams();
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const authTicket = useAuthState();

    //var pageMode = //state is not always present. If user types a url or we use an href link, state is null. history.location.state.viewMode;
    //see logic below for how we calculate.
    const [mode, setMode] = useState(initPageMode());
    const [_item, setItem] = useState({});
    const [isLoading, setIsLoading] = useState(true);
    const [isReadOnly, setIsReadOnly] = useState(true);
    const [_isValid, setIsValid] = useState({ name: true, profile: true, description: true, type: true, symbolicName: true });
    //const [_lookupProfiles, setLookupProfiles] = useState([]);
    //used in popup profile add/edit ui. Default to new version
    const [_profileEntityModal, setProfileEntityModal] = useState({ show: false, item: null, autoSave: false  });
    const _profileNew = { id: 0, namespace: '', version: null, publishDate: null, authorId: null };

    const { wizardProps, setWizardProps } = useWizardContext();
    var _navInfo = history.location.pathname.indexOf('/wizard/') === - 1 ? null : getWizardNavInfo(wizardProps.mode, 'ExtendBaseType');
    const _currentPage = history.location.pathname.indexOf('/wizard/') === - 1 ? null : WizardSettings.panels.find(p => { return p.id === 'ExtendBaseType'; });
    
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
            var result = null;
            try {
                //add logic for wizard scenario
                var data = { id: (parentId != null ? parentId : id) };
                var url = `profiletypedefinition/getbyid`;
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
                var msg = 'An error occurred retrieving this profile.';
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
            var thisMode = 'view';
            if (id != null) {
                thisMode = (result.data.isReadOnly || result.data.author == null || result.data.author.id !== authTicket.user.id) ? "view" : "edit";
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
                var revisedList = UpdateRecentFileList(loadingProps.recentFileList, {
                    url: history.location.pathname, caption: result.data.name, iconName: getTypeDefIconName(result.data),
                    authorId: result.data.author != null ? result.data.author.id : null });
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
                var url = `profiletypedefinition/init`
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
    }, [id, parentId, profileId, authTicket.user]);

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

    function buildTitleCaption(mode) {
        var caption = getProfileTypeCaption(_item);
        if (mode != null) {
            switch (mode.toLowerCase()) {
                case "new":
                    caption = `New ${caption}`
                    break;
                case "edit":
                    caption = `Edit ${caption}`;
                    break;
                case "extend":
                    caption = `Extend ${caption}`;
                    break;
                case "view":
                default:
                    caption = `View ${caption}`;
                    break;
            }
        }
        return caption;
    }

    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------
    const validateForm_name = (e) => {
        var isValid = (e.target.value != null && e.target.value.trim().length > 0);
        setIsValid({ ..._isValid, name: isValid });
    };

    //const validateForm_profile = (e) => {
    //    var isValid = e.target.value.toString() !== "-1";
    //    setIsValid({ ..._isValid, profile: isValid });
    //};

    const validateForm_description = (e) => {
        //var isValid = (e.target.value != null && e.target.value.trim().length > 0);
        //setIsValid({ ..._isValid, description: isValid });
    };

    const validateForm_type = (e) => {
        var isValid = e.target.value.toString() !== "-1";
        setIsValid({ ..._isValid, type: isValid });
    };

    const validateForm_symbolicName = (e) => {
        var isValid = validate_NoSpecialCharacters(e.target.value);
        setIsValid({ ..._isValid, symbolicName: isValid });
    };

     ////update state for when search click happens
     const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        _isValid.name = _item.name != null && _item.name.trim().length > 0;
        _isValid.profile = _item.profile != null && _item.profile.id !== -1 && _item.profile.id !== "-1";
        _isValid.description = true; //item.description != null && item.description.trim().length > 0;
        _isValid.type = _item.type != null && _item.type.id !== -1 && _item.type.id !== "-1";
         _isValid.symbolicName = validate_NoSpecialCharacters(_item.symbolicName);

         setIsValid(JSON.parse(JSON.stringify(_isValid)));
         return (_isValid.name && _isValid.profile && _isValid.description && _isValid.type && _isValid.symbolicName);
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
                        { id: new Date().getTime(), severity: "danger", body: `An error occurred ${mode.toLowerCase() === "extend" ? "extending" : "saving"} this profile.`, isTimed: false }
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
        setLoadingProps({ isLoading: true, message: "" });

        //perform update call
        var url = `profiletypedefinition/togglefavorite`;
        var data = { id: _item.id };
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
        //console.log(generateLogMessageString(`onEntityChange||e:${e.target}`, CLASS_NAME));

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
    }

    const onAddProfile = () => {
        console.log(generateLogMessageString(`onAddProfile`, CLASS_NAME));
        setProfileEntityModal({ show: true, item: JSON.parse(JSON.stringify(_profileNew)) });
    };

    //add profile on the fly then go get a refreshed list of items.
    const onSaveProfile = (p) => {
        console.log(generateLogMessageString(`onSaveProfile`, CLASS_NAME));
        var autoSave = _profileEntityModal.autoSave;
        setProfileEntityModal({ show: false, item: null, autoSave: false });
        ////add the new profile to the list
        //_lookupProfiles.push(p);
        //_lookupProfiles.sort((a, b) => {
        //    var versionA = a.version == null ? '' : a.version;
        //    var versionB = b.version == null ? '' : b.version;
        //    var sortA = `${a.namespace}+${versionA.toString()}`.toLowerCase();
        //    var sortB = `${b.namespace}+${versionB.toString()}`.toLowerCase();
        //    if (sortA < sortB) return -1;
        //    if (sortA > sortB) return 1;
        //    return 0;
        //}); //sort by name+version
        //setLookupProfiles(JSON.parse(JSON.stringify(_lookupProfiles)));
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
        //item.profileId = null;
        //item.profile = null;
        //setItem(JSON.parse(JSON.stringify(item)));
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
        //console.log(generateLogMessageString(`onInterfaceAttributeAdd||interface:${JSON.stringify(iface)}`, CLASS_NAME));

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

    const onAttributeDelete = (id) => {
        //raised from del button click in child component
        console.log(generateLogMessageString(`onAttributeDelete||item id:${id}`, CLASS_NAME));

        var x = _item.profileAttributes.findIndex(attr => { return attr.id === id; });
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
        var itemCopy = JSON.parse(JSON.stringify(_item));
        setItem(itemCopy);
        return {
            profileAttributes: itemCopy.profileAttributes, extendedProfileAttributes: itemCopy.extendedProfileAttributes
        };
    };

    //delete all attributes for passed in interface id. 
    const onAttributeInterfaceDelete = (id) => {
        //raised from del button click in child component
        console.log(generateLogMessageString(`onAttributeInterfaceDelete||interface id:${id}`, CLASS_NAME));

        //delete the interface reference
        var x = _item.interfaces.findIndex(i => { return i.id === id; });
        //no item found
        if (x < 0) {
            console.warn(generateLogMessageString(`onAttributeInterfaceDelete||no interface found to delete with id: ${id}`, CLASS_NAME));
            return {
                profileAttributes: _item.profileAttributes, extendedProfileAttributes: _item.extendedProfileAttributes
            };
        }
        //remove the interface with the matching id. 
        _item.interfaces.splice(x, 1);

        //Remove associated attributed. Can only delete interface in my attributes, not in extended attrib.
        //note all interface attributes are in the extended collection
        var matches = _item.extendedProfileAttributes.filter(attr => { return attr.interface == null || attr.interface.id !== id; });

        //no items found
        if (matches.length === _item.extendedProfileAttributes.length) {
            console.warn(generateLogMessageString(`onAttributeInterfaceDelete||no attributes found to delete with this interface id`, CLASS_NAME));
            return {
                profileAttributes: _item.profileAttributes, extendedProfileAttributes: _item.extendedProfileAttributes
            };
        }

        //update the state, return attrib collection to child components
        _item.extendedProfileAttributes = matches;
        var itemCopy = JSON.parse(JSON.stringify(_item));
        setItem(itemCopy);
        return {
            profileAttributes: itemCopy.profileAttributes, extendedProfileAttributes: itemCopy.extendedProfileAttributes
        };
    };

    const onAttributeUpdate = (attr) => {
        //raised from del button click in child component
        console.log(generateLogMessageString(`onAttributeUpdate||item id:${attr.id}`, CLASS_NAME));

        var aIndex = _item.profileAttributes.findIndex(a => { return a.id === attr.id; });
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
        var itemCopy = JSON.parse(JSON.stringify(_item));
        setItem(itemCopy);
        //return item local b/c item is not yet updated in state
        return {
            profileAttributes: itemCopy.profileAttributes, extendedProfileAttributes: itemCopy.extendedProfileAttributes
        };
    };

    //const downloadMe = async () => {
    //    downloadFileXML(item.id, cleanFileName(item.name), false);
    //}
    const downloadProfile = async () => {
        console.log(generateLogMessageString(`downloadProfile||start`, CLASS_NAME));
        //add a row to download messages and this will kick off download
        var msgs = loadingProps.downloadItems || [];
        msgs.push({ profileId: _item.profile?.id, fileName: cleanFileName(_item.profile?.namespace), immediateDownload: true });
        setLoadingProps({ downloadItems: JSON.parse(JSON.stringify(msgs)) });
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
                    <ProfileBreadcrumbs item={_item} currentUserId={authTicket.user.id} />
                </>
            );
        }
        return (
            <ProfileBreadcrumbs item={_item} currentUserId={authTicket.user.id} />
        );
    };

    const renderProfileDefType = () => {
        //show readonly input for view mode
        if (isReadOnly) {
            return (
                <Form.Group>
                    <Form.Label htmlFor="type">Type</Form.Label>
                    <Form.Control id="type" type="" value={_item.type != null ? _item.type.name : ""} readOnly={isReadOnly} />
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

    //render Profile Row as a read-only reminder of the parent
    const renderProfile = () => {

        var actionUI = (_item.id != null && _item.id > 0 && _item.profile != null) ? null :
            (
                <button type="button" className="btn btn-secondary auto-width" onClick={onSelectProfile} title="Select Profile" >Select</button>
            );

        return (
            <div className="row pt-2 mb-2">
                <div className="col-md-12">
                    <ProfileItemRow key="p-1" mode="simple" item={_item.profile} actionUI={actionUI}
                        currentUserId={authTicket.user.id} cssClass="shaded profile-list-item rounded no-gutters px-2" />
                </div>
            </div>
        );
    };

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
            <Dropdown className="action-menu icon-dropdown ml-1 ml-md-2" onClick={(e) => e.stopPropagation()} >
                <Dropdown.Toggle drop="left" title="Actions" >
                    <SVGIcon name="more-vert" size="24" fill={color.shark} />
                </Dropdown.Toggle>
                <Dropdown.Menu>
                    <Dropdown.Item href={`/type/extend/${_item.id}`}>Extend '{_item.name}'</Dropdown.Item>
                    {/*<Dropdown.Item onClick={downloadMe} >Download '{item.name}'</Dropdown.Item>*/}
                    <Dropdown.Item onClick={downloadProfile} >Download Profile '{getProfileCaption(_item.profile)}'</Dropdown.Item>
                </Dropdown.Menu>
            </Dropdown>
        );
    }

    const renderButtons = () => {
        var urlCancel = history.location.pathname.indexOf('/wizard/') > - 1 ? _navInfo.prev.href : '/types/library';
        var captionCancel = history.location.pathname.indexOf('/wizard/') > - 1 ? 'Back' : 'Cancel';
        if (mode.toLowerCase() !== "view") {
            return (
                <>
                    <Button variant="text-solo" className="mx-1 btn-auto auto-width" href={urlCancel} >{captionCancel}</Button>
                    <Button variant="secondary" type="button" className="mx-3 d-none d-lg-block" onClick={onSave} >Save</Button>
                    <Button variant="icon-outline" type="button" className="mx-1 d-lg-none" onClick={onSave} title="Save" ><i className="material-icons">save</i></Button>
                </>
            );
        }
    }
    
    const renderCommonSection = () => {

        var iconName = mode.toLowerCase() === "extend" ? "extend" : getTypeDefIconName(_item);

        return (
            <>
                <div className="row my-2">
                    <div className="col-sm-9 col-md-8 mb-2" >
                        <h1 className="mb-0">
                            <SVGIcon name={iconName} size="24" fill={color.shark} className="mr-1 mr-md-2" />
                            Type Definition
                            {(_item.name != null && _item.name.trim() !== '') &&
                                <>
                                    <span className="d-none d-lg-inline mx-1" >-</span>
                                    <br className="d-block d-lg-none" />{_item.name}
                                </>
                            }
                            {(_item.id != null && _item.id > 0) &&
                                <button className="btn btn-icon btn-nofocusborder favorite" onClick={onToggleFavorite} title={_item.isFavorite ? "Unfavorite item" : "Favorite item"} >
                                    <i className="material-icons" >{_item.isFavorite ? "favorite" : "favorite_border"}</i>
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
                        <div className="col-md-8">
                        <Form.Label htmlFor="name" >Name</Form.Label>
                            {!_isValid.name &&
                                <span className="ml-2 d-inline invalid-field-message">Required</span>
                            }
                            <div className={`input-group ${(!_isValid.name ? "invalid-group" : "")}`} >
                                {/*<InputGroup.Prepend>*/}
                                {/*    <InputGroup.Text className={isReadOnly ? `input-prepend readonly` : `input-prepend`}>*/}
                                {/*        <SVGIcon name={iconName} size="24" fill={color.shark} />*/}
                                {/*    </InputGroup.Text>*/}
                                {/*</InputGroup.Prepend>*/}
                                <Form.Group className="flex-grow-1 m-0">
                                    <Form.Control className={(!_isValid.name ? `invalid-field` : ``)} id="name" type="" placeholder={`Enter name`}
                                        value={_item.name} onBlur={validateForm_name} onChange={onChange} readOnly={isReadOnly} />
                                </Form.Group>
                                {/*    {(mode.toLowerCase() === "edit" || mode.toLowerCase() === "view") &&*/}
                                {/*        <InputGroup.Append>*/}
                                {/*            <InputGroup.Text className="input-append fav" onClick={onToggleFavorite} >*/}
                                {/*                <SVGIcon name={isFavorite ? "favorite" : "favorite-border"} size="24" fill={color.citron} />*/}
                                {/*            </InputGroup.Text>*/}
                                {/*        </InputGroup.Append>*/}
                                {/*    }*/}
                            </div>
                        </div>
                    }
                    <div className="col-md-4">
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
                <div className="row mt-2">
                    <div className="col-md-6">
                        <Form.Group>
                            <Form.Label htmlFor="entity_author" >Author name</Form.Label>
                            <Form.Control id="entity_author" type="" placeholder="Enter your name here" value={_item.author?.fullName} onChange={onChange} readOnly={isReadOnly} disabled='disabled' />
                        </Form.Group>
                    </div>
                    <div className="col-md-6">
                        <Form.Group>
                            <Form.Label htmlFor="entity_organization" >Organization</Form.Label>
                            <Form.Control id="entity_organization" type="" placeholder="Enter your organization's name" value={_item.author?.organization.name} onChange={onChange} readOnly={isReadOnly} disabled='disabled' />
                        </Form.Group>
                    </div>
                </div>
                <div className="row mt-2">
                    <div className="col-sm-6">
                        <Form.Group>
                            <Form.Label htmlFor="browseName" >OPC Browse Name</Form.Label>
                            <Form.Control id="browseName" type="" placeholder="" readOnly={isReadOnly}
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
                            <Form.Control id="symbolicName" className={(!_isValid.symbolicName ? `invalid-field` : ``)} type="" placeholder="" readOnly={isReadOnly}
                                value={_item.symbolicName != null ? _item.symbolicName : ""} onChange={onChange} onBlur={validateForm_symbolicName} />
                        </Form.Group>
                    </div>
                </div>
                <div className="row mt-2">
                    <div className="col-sm-4">
                        <Form.Group>
                            <Form.Label htmlFor="opcNodeId" >OPC Node Id</Form.Label>
                            <Form.Control id="opcNodeId" type="" placeholder=""
                                value={_item.opcNodeId == null ? '' : _item.opcNodeId} onChange={onChange} readOnly="readOnly" />
                        </Form.Group>
                    </div>
                </div>
                <div className="row mt-2">
                    <div className="col-sm-6 col-lg-2">
                        <Form.Group className="d-flex h-100">
                            <Form.Check className="align-self-end" type="checkbox" id="isAbstract" label="Is Abstract" checked={_item.isAbstract}
                                onChange={onChange} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                </div>
                <div className="row mt-2">
                    <div className="col-sm-12">
                        <Form.Group>
                            <Form.Label htmlFor="metaTagsConcatenated" >Meta tags (optional)</Form.Label>
                            <Form.Control id="metaTagsConcatenated" type="" placeholder="Enter tags seperated by a comma" value={_item.metaTagsConcatenated} onChange={onChange} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                </div>
                <div className="row mt-2">
                    <div className="col-sm-12">
                        <Form.Group>
                            <Form.Label htmlFor="documentUrl">Document Url</Form.Label>
                            <Form.Control id="documentUrl" type="" placeholder="Enter Url to the reference documentation (if applicable)."
                                value={_item.documentUrl != null ? _item.documentUrl : ""} onChange={onChange} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                </div>
            </>
        );
    }

    // TBD: need loop to remove and add styles for "nav-item" CSS animations
    const tabListener = (eventKey) => {
        // alert(eventKey);
      }


    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    var titleCaption = buildTitleCaption(mode);
    var caption = getProfileTypeCaption(_item);

    const renderMainContent = () => {
        if (loadingProps.isLoading || isLoading) return;
        return (
            <div className="profile-entity">
                <Form noValidate>
                    {renderCommonSection()}

                    <div className="entity-details">
                        {/* TABS */}
                        <Tab.Container id="profile-definition" defaultActiveKey="attributes" onSelect={tabListener}>
                            <Nav variant="pills" className="row mt-2 px-2 pr-md-3">
                                <Nav.Item className="col-sm-4 rounded p-0 pl-2" >
                                    <Nav.Link eventKey="attributes" className="text-center text-md-left p-1 px-2 h-100" >
                                        <span className="headline-3">Attributes</span>
                                        {/*<span className="d-none d-md-inline"><br />The properties that define this type definition</span>*/}
                                    </Nav.Link>
                                </Nav.Item>
                                <Nav.Item className="col-sm-4 rounded p-0 px-md-0" >
                                    <Nav.Link eventKey="dependencies" className="text-center text-md-left p-1 px-2 h-100" >
                                        <span className="headline-3">Dependencies</span>
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
                                    <Card className="pb-2 pb-md-4">
                                        <Card.Body>
                                            <AttributeList typeDefinition={_item} profileAttributes={_item.profileAttributes} extendedProfileAttributes={_item.extendedProfileAttributes} readOnly={mode === "view"}
                                                onAttributeAdd={onAttributeAdd} onAttributeInterfaceAdd={onAttributeInterfaceAdd} currentUserId={authTicket.user.id}
                                                onAttributeDelete={onAttributeDelete} onAttributeInterfaceDelete={onAttributeInterfaceDelete} onAttributeUpdate={onAttributeUpdate} />
                                        </Card.Body>
                                    </Card>
                                </Tab.Pane>
                                <Tab.Pane eventKey="dependencies">
                                    {/* DEPENDENCIES CONTENT */}
                                    <Card className="p-2 pb-md-4">
                                        <Card.Body>
                                            <DependencyList typeDefinition={_item} />
                                        </Card.Body>
                                    </Card>
                                </Tab.Pane>
                                <Tab.Pane eventKey="advanced">
                                    {/* advanced CONTENT */}
                                    <Card className="p-2 pb-md-4">
                                        <Card.Body>
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