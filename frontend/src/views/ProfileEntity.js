import React, { useState, useEffect, Fragment } from 'react'
import { useParams, useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import axios from 'axios'

import Form from 'react-bootstrap/Form'
import InputGroup from 'react-bootstrap/InputGroup'
import Card from 'react-bootstrap/Card'
import Button from 'react-bootstrap/Button'
import Dropdown from 'react-bootstrap/Dropdown'
import Tab from 'react-bootstrap/Tab'
import Nav from 'react-bootstrap/Nav'
import Row from 'react-bootstrap/Row'
import Col from 'react-bootstrap/Col'

import { LookupData, AppSettings } from '../utils/appsettings';
import { generateLogMessageString, concatenateField, getProfileIconName, getProfileCaption, downloadFileJSON } from '../utils/UtilityService'
import AttributeList from './shared/AttributeList';
import DependencyList from './shared/DependencyList';
import ProfileBreadcrumbs from './shared/ProfileBreadcrumbs';
import { useLoadingContext, UpdateRecentFileList, toggleFavoritesList } from "../components/contexts/LoadingContext";
import { useAuthContext } from "../components/authentication/AuthContext";

import { SVGIcon } from "../components/SVGIcon";
import color from "../components/Constants";

const CLASS_NAME = "ProfileEntity";
const entityInfo = {
    name: "Profile",
    namePlural: "Profiles",
    entityUrl: "/profile/:id",
    listUrl: "/profile/all"
}


function ProfileEntity() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();

    const { id, parentId } = useParams();
    //var pageMode = //state is not always present. If user types a url or we use an href link, state is null. history.location.state.viewMode;
    //see logic below for how we calculate.
    const [mode, setMode] = useState(initPageMode());
    const [item, setItem] = useState({});
    const [isLoading, setIsLoading] = useState(true);
    const [isReadOnly, setIsReadOnly] = useState(true);
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const { authTicket } = useAuthContext();
    const [_isValid, setIsValid] = useState({ name: true, namespace: true , description: true, type: true });
    //is favorite calc
    const [isFavorite, setIsFavorite] = useState((loadingProps.favoritesList != null && loadingProps.favoritesList.findIndex(x => x.url === history.location.pathname) > -1));

    var content = "";

    useEffect(() => {
        async function fetchData() {
            console.log(generateLogMessageString('useEffect||fetchData||async', CLASS_NAME));
            //initialize spinner during loading
            setLoadingProps({ isLoading: true, message: null });

            //mode not set right if we were on this page, save an extend and navigate into edit same profile. Rely on
            // parentId, id. Then determine mode. for extend, we use parentId, for edit/view, we use id.
            var result = null;
            try {
                result = await axios(
                    `${AppSettings.BASE_API_URL}/profile/${(parentId != null ? parentId : id)}`);
            }
            catch (err) {
                var msg = 'An error occurred retrieving this profile.';
                console.log(generateLogMessageString('useEffect||fetchData||error', CLASS_NAME, 'error'));
                //console.log(err.response.status);
                if (err != null && err.response != null && err.response.status === 404) {
                    msg += ' This profile was not found.';
                    history.push('/404');
                }
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: msg }]
                });
            }

            if (result == null) return;

            // set view/edit mode now that we can compare user against author of item
            // just a check to make sure that if the mode is edit, but user isn't the author,
            // we force them back to view mode
            var thisMode = 'view';
            if (id != null) {
                thisMode = (result.data.author == null || result.data.author.id !== authTicket.user.id) ? "view" : "edit";
            }
            // if we're extending a class, result will have the class we're inheriting from
            // and we'll need to move some info around to support this
            else if (parentId != null) {
                //clear the attributes array, update the parent object with the object we are extending
                //clear out the old id, set author
                result.data.parentProfile = JSON.parse(JSON.stringify(result.data));
                result.data.extendedProfileAttributes = result.data.extendedProfileAttributes.concat(result.data.profileAttributes);
                result.data.profileAttributes = [];
                result.data.id = 0;
                result.data.author = authTicket.user;
                result.data.name = "New profile extending " + result.data.name;
                thisMode = "extend";
            }

            //convert collection to comma separated list
            //special handling of meta tags which shows as a concatenated list in an input box
            result.data.metaTagsConcatenated = result.data == null ? "" : concatenateField(result.data.metaTags, 'name', ', ');
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
                    url: history.location.pathname, caption: result.data.name, iconName: getProfileIconName(result.data),
                    authorId: result.data.author != null ? result.data.author.id : null });
                setLoadingProps({ recentFileList: revisedList });
            }

        }

        //fetch our data 
        // for view/edit modes
        if ((id != null && id.toString() !== 'new') || parentId != null) {
            fetchData();
        }
        else {
            // build blank object and set that
            // set to current user & init arrays and stuff for page binding
            var newObj = {
                id: 0,
                type: { id: -1, name: '' },
                author: authTicket.user, metaTags: [], metaTagsConcatenated: '',
                name: "New profile name", namespace: "New_namespace"
            };
            setItem(newObj);
            setIsLoading(false);
            setLoadingProps({ isLoading: false, message: null });
            setIsReadOnly(false);
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [id, parentId, authTicket.user]);

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
        var caption = getProfileCaption(item);
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

    const validateForm_namespace = (e) => {
        var isValid = (e.target.value != null && e.target.value.trim().length > 0);
        setIsValid({ ..._isValid, namespace: isValid });
    };

    const validateForm_description = (e) => {
        var isValid = (e.target.value != null && e.target.value.trim().length > 0);
        setIsValid({ ..._isValid, description: isValid });
    };

    const validateForm_type = (e) => {
        var isValid = e.target.value.toString() !== "-1";
        setIsValid({ ..._isValid, type: isValid });
    };

     ////update state for when search click happens
     const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        _isValid.name = item.name != null && item.name.trim().length > 0;
        _isValid.namespace = item.namespace != null && item.namespace.trim().length > 0;
        _isValid.description = item.description != null && item.description.trim().length > 0;
        _isValid.type = item.type != null && item.type.id !== -1 && item.type.id !== "-1";

        setIsValid(JSON.parse(JSON.stringify(_isValid)));
        return (_isValid.name && _isValid.namespace && _isValid.description && _isValid.type);
    }

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onCancel = () => {
        //raised from header nav
        console.log(generateLogMessageString('onCancel', CLASS_NAME));
        history.goBack();
    };

    const onSave = () => {
        //raised from header nav
        console.log(generateLogMessageString('onSave', CLASS_NAME));

        //do validation
        if (!validateForm()){
            //alert("validation failed");
            return;
        } 

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //convert my metatags string back into array representation for saving
        item.metaTags = [];
        //split the string into array and then build out array of tags
        var tags = item.metaTagsConcatenated.split(",");
        tags.forEach((tag) => {
            item.metaTags.push({ name: tag.trim() });
        });

        //perform insert call
        console.log(generateLogMessageString(`handleOnSave||${mode}`, CLASS_NAME));
        if (mode.toLowerCase() === "extend" || mode.toLowerCase() === "new") {
            item.id = new Date().getTime(); //TBD - in final app, the server side and likely the db will issue the new id
            axios.post(
                `${AppSettings.BASE_API_URL}/profile`, item)
                .then(resp => {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "success", body: `Profile was ${mode.toLowerCase() === "extend" ? "extended" : "created"}` }
                        ],
                        profileCount: { all: loadingProps.profileCount.all + 1, mine: loadingProps.profileCount.mine + 1 }
                    });
                    //TBD - redirect to the edit mode once the item is created
                    console.log(resp.data);
                    history.push(entityInfo.entityUrl.replace(':id', resp.data.id));
                })
                .catch(error => {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: `An error occurred ${mode.toLowerCase() === "extend" ? "extending" : "creating"} this profile.` }
                        ]
                    });
                    console.log(generateLogMessageString('handleOnSave||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
                    console.log(error);
                });
        }
        else if (mode.toLowerCase() === "edit") {
            axios.put(
                `${AppSettings.BASE_API_URL}/profile/${id}`, item)
                .then(resp => {
                    //update the item that was saved
                    setItem(resp.data);
                    //console.log(resp.data);
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "success", body: "Profile was saved" }
                        ]
                    });
                    //go back to top
                    window.scroll({
                        top: 0,
                        left: 0,
                        behavior: 'smooth',
                    });
                })
                .catch(error => {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: "An error occurred updating this profile." }
                        ]
                    });
                    console.log(generateLogMessageString('handleOnSave||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
                    console.log(error);
                });
        }
        
    };

    const onToggleFavorite = () => {
        console.log(generateLogMessageString('onToggleFavorite', CLASS_NAME));

        //add to the favorite list to keep track of where we have been
        if (mode.toLowerCase() === "view" || mode.toLowerCase() === "edit") {
            var revisedList = toggleFavoritesList(loadingProps.favoritesList, { url: history.location.pathname, caption: item.name, iconName: getProfileIconName(item), authorId: item.author.id });
            setLoadingProps({ favoritesList: revisedList });
            setIsFavorite(revisedList != null && revisedList.findIndex(x => x.url === history.location.pathname) > -1);
        }
    };

    //on change handler to update state
    const onChange = (e) => {
        //console.log(generateLogMessageString(`onEntityChange||e:${e.target}`, CLASS_NAME));

        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        switch (e.target.id) {
            case "name":
            case "namespace":
            case "description":
            case "metaTagsConcatenated":
                item[e.target.id] = e.target.value;
                break;
            case "type":
                if (e.target.value.toString() === "-1") item.type = null;
                else {
                    item.type = { id: e.target.value, name: e.target.options[e.target.selectedIndex].text };
                }
                break;
            default:
                return;
        }
        //update the state
        setItem(JSON.parse(JSON.stringify(item)));
    }

    //raised from add button click in child component
    const onAttributeAdd = (row) => {
        console.log(generateLogMessageString(`onAttributeAdd||item:${JSON.stringify(row)}`, CLASS_NAME));
        
        if (item.profileAttributes == null) item.profileAttributes = [];
        // doing the JSON stuff here to break up the object reference
        item.profileAttributes.push(JSON.parse(JSON.stringify(row))); 
        
        setItem(JSON.parse(JSON.stringify(item)));
        return {
            profileAttributes: item.profileAttributes, extendedProfileAttributes: item.extendedProfileAttributes
        };
    };

    //add an interface which adds a collection of attributes from an interface object
    const onInterfaceAttributeAdd = (iface, profileAttributes, extendedProfileAttributes) => {
        //raised from add button click in child component
        console.log(generateLogMessageString(`onInterfaceAttributeAdd||interface:${JSON.stringify(iface)}`, CLASS_NAME));

        //replace the current attributes with the revised list.
        item.profileAttributes = JSON.parse(JSON.stringify(profileAttributes));
        item.extendedProfileAttributes = JSON.parse(JSON.stringify(extendedProfileAttributes));

        // add the interface to the interfaces collection
        if (item.interfaces == null) item.interfaces = [];
        item.interfaces.push(JSON.parse(JSON.stringify(iface)));

        setItem(JSON.parse(JSON.stringify(item)));
        return {
            profileAttributes: item.profileAttributes, extendedProfileAttributes: item.extendedProfileAttributes
        };
    };

    const onAttributeDelete = (id) => {
        //raised from del button click in child component
        console.log(generateLogMessageString(`onAttributeDelete||item id:${id}`, CLASS_NAME));

        var x = item.profileAttributes.findIndex(attr => { return attr.id === id; });
        //no item found
        if (x < 0) {
            console.warn(generateLogMessageString(`onAttributeDelete||no item found to delete with this id`, CLASS_NAME));
            return {
                profileAttributes: item.profileAttributes, extendedProfileAttributes: item.extendedProfileAttributes
            };
        }
        //if the id < 0, then that item should be removed entirely - it was created client side
        if (item.profileAttributes[x].id < 0) {
            item.profileAttributes.splice(x, 1);
        }
        else { //if the id > 0, just update the isDeleted flag
            item.profileAttributes[x].isDeleted = false;
        }

        //update the state, return collection to child components
        var itemLocal = JSON.parse(JSON.stringify(item));
        setItem(itemLocal);
        return {
            profileAttributes: item.profileAttributes, extendedProfileAttributes: item.extendedProfileAttributes
        };
    };

    //delete all attributes for passed in interface id. Can only delete interface in my attributes, not in extended attrib.
    const onAttributeInterfaceDelete = (id) => {
        //raised from del button click in child component
        console.log(generateLogMessageString(`onAttributeInterfaceDelete||interface id:${id}`, CLASS_NAME));

        var matches = item.profileAttributes.filter(attr => { return attr.interface == null || attr.interface.id !== id; });
        var matchesExtended = item.extendedProfileAttributes.filter(attr => { return attr.interface == null || attr.interface.id !== id; });

        //no items found
        if (matches.length === item.profileAttributes.length && matchesExtended.length === item.extendedProfileAttributes.length) {
            console.warn(generateLogMessageString(`onAttributeInterfaceDelete||no items found to delete with this interface id`, CLASS_NAME));
            return {
                profileAttributes: item.profileAttributes, extendedProfileAttributes: item.extendedProfileAttributes
            };
        }

        //update the state, return collection to child components
        var itemLocal = JSON.parse(JSON.stringify(item));
        setItem(itemLocal);
        return {
            profileAttributes: matches, extendedProfileAttributes: matchesExtended
        };
    };

    const onAttributeUpdate = (attr) => {
        //raised from del button click in child component
        console.log(generateLogMessageString(`onAttributeUpdate||item id:${attr.id}`, CLASS_NAME));

        var aIndex = item.profileAttributes.findIndex(a => { return a.id === attr.id; });
        //no item found
        if (aIndex === -1) {
            console.warn(generateLogMessageString(`onAttributeUpdate||no item found with this id`, CLASS_NAME));
            return {
                profileAttributes: item.profileAttributes, extendedProfileAttributes: item.extendedProfileAttributes
            };
        }
        //replace attr
        item.profileAttributes[aIndex] = JSON.parse(JSON.stringify(attr));

        //update the state, return collection to child components
        var itemLocal = JSON.parse(JSON.stringify(item));
        setItem(itemLocal);
        //return item local b/c item is not yet updated in state
        return {
            profileAttributes: itemLocal.profileAttributes, extendedProfileAttributes: itemLocal.extendedProfileAttributes
        };
    };

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderProfileBreadcrumbs = () => {
        if (item == null || item.parentProfile == null) return;

        return (
            <>
                <ProfileBreadcrumbs item={item} currentUserId={authTicket.currentUserId} />
            </>
        );
    };

    const renderProfileType = () => {
        //show readonly input for view mode
        if (isReadOnly) {
            return (
                <Form.Group className="flex-grow-1">
                    <Form.Label>Type</Form.Label>
                    <Form.Control id="type" type="" value={item.type.name} readOnly={isReadOnly} />
                </Form.Group>
            )
        }
        //show readonly input for extend mode. You have to extend the same type
        //show readonly input for edit mode. There are downstream implications to the type that should prevent changing here. 
        if (item.type != null && (mode.toLowerCase() === "extend" || mode.toLowerCase() === "edit" )) {
            return (
                <Form.Group className="flex-grow-1">
                    <Form.Label>Type</Form.Label>
                    <Form.Control id="type" type="" value={item.type.name} readOnly={true} />
                </Form.Group>
            )
        }
        //if extending, then user can only choose certain types. 
        //show drop down list for edit, extend mode
        const options = LookupData.profileTypes.map((item) => {
            return (<option key={item.id} value={item.id} >{item.name}</option>)
        });

        return (
            <Form.Group className="flex-grow-1">
                <Form.Label>Type</Form.Label>
                {!_isValid.type &&
                    <span className="invalid-field-message inline">
                        Required
                    </span>
                }
                <Form.Control id="type" as="select" className={(!_isValid.type ? 'invalid-field minimal pr-5' : 'minimal pr-5')} value={item.type.id}
                    onBlur={validateForm_type} onChange={onChange} >
                    <option key="-1|Select One" value="-1" >Select</option>
                    {options}
                </Form.Control>
            </Form.Group>
        )
    };

    const downloadMe = async () => {
        downloadFileJSON(item, `${item.name.trim().replace(" ", "_")}`);
    }

    const renderMoreDropDown = () => {
        if (item == null || (mode.toLowerCase() === "extend" || mode.toLowerCase() === "new") ) return;

        //React-bootstrap bug if you launch modal, then the dropdowns don't work. Add onclick code to the drop down as a workaround - https://github.com/react-bootstrap/react-bootstrap/issues/5561
        return (
            <Dropdown className="action-menu icon-dropdown" onClick={(e) => e.stopPropagation()} > 
                <Dropdown.Toggle drop="left">
                    <SVGIcon name="more-vert" size="24" fill={color.shark} />
                </Dropdown.Toggle>
                <Dropdown.Menu>
                    <Dropdown.Item href={`/profile/extend/${item.id}`}>Extend '{item.name}'</Dropdown.Item>
                    <Dropdown.Item onClick={downloadMe} >Download '{item.name}'</Dropdown.Item>
                </Dropdown.Menu>
            </Dropdown>
        );
    }

    const renderButtons = () => {
        if (mode.toLowerCase() !== "view") {
            return (
                <>
                    <Button variant="text-solo" className="mx-1" onClick={onCancel} >Cancel</Button>
                    <Button variant="secondary" type="button" className="mx-3" onClick={onSave} >Save</Button>
                </>
            );
        }
    }
    
    const renderHeaderBlock = () => {

        var iconName = mode.toLowerCase() === "extend" ? "extend" : getProfileIconName(item);
        var inputCssClass = (mode.toLowerCase() === "edit" || mode.toLowerCase() === "view") ?
            "input-with-append input-with-prepend" : "input-with-prepend";
        
        var groupCssClass = (mode.toLowerCase() === "edit" || mode.toLowerCase() === "view") ?
        "mr-3" : "mr-3";

        return (
            <div className="d-flex align-items-center my-4">
                <InputGroup className={(!_isValid.name ? `mr-3 invalid-group ${groupCssClass}` : `mr-3 ${groupCssClass}`)}>
                    <InputGroup.Prepend>
                        <InputGroup.Text className={isReadOnly ? `input-prepend readonly` : `input-prepend`}>
                            <SVGIcon name={iconName} size="24" fill={color.shark} />
                        </InputGroup.Text>
                    </InputGroup.Prepend>
                    <Form.Group className="flex-grow-1 m-0">
                        <Form.Control size="lg" className={(!_isValid.name ? `invalid-field ${inputCssClass}` : inputCssClass)} id="name" type="" placeholder={`Enter your ${caption.toLowerCase()} name here`}
                            value={item.name} onBlur={validateForm_name} onChange={onChange} readOnly={isReadOnly} />
                        {!_isValid.name &&
                            <span className="invalid-field-message">Required</span>
                        }
                    </Form.Group>
                    {/* <Form.Control aria-label="New profile name" size="lg" placeholder="Enter your new profile name here" className="input-with-append input-with-prepend" /> */}
                    {(mode.toLowerCase() === "edit" || mode.toLowerCase() === "view") &&
                        <InputGroup.Append>
                            <InputGroup.Text className="input-append fav" onClick={onToggleFavorite} >
                                <SVGIcon name={isFavorite ? "favorite" : "favorite-border"} size="24" fill={color.citron} />
                            </InputGroup.Text>
                        </InputGroup.Append>
                    }
                </InputGroup>

                <Form className="d-flex align-items-center">
                    {renderButtons()}
                    {renderMoreDropDown()}
                </Form>
            </div>
        )
    }

    // TBD: need loop to remove and add styles for "nav-item" CSS animations
    const tabListener = (eventKey) => {
        // alert(eventKey);
      }

    
    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    var titleCaption = buildTitleCaption(mode);
    var caption = getProfileCaption(item);

    if (!loadingProps.isLoading && !isLoading) {
        content = <div id="--cesmii-left-content" className="profile-entity">

            {renderHeaderBlock()}

            <div id="--attributes-inputs">
                {/* TABS */}
                <Tab.Container id="profile-definition" defaultActiveKey="details" onSelect={tabListener}>
                    <Col className="pl-0 pr-0">
                        <Row noGutters>
                            <Nav variant="pills" className="flex-grow-1">
                                <Nav.Item className="col rounded p-0 mr-4">
                                    <Nav.Link eventKey="details" className="text-left pt-4 pb-4"><span className="attr-tab-header">1. Profile details</span><br/><span>Basic information about this profile</span></Nav.Link>
                                </Nav.Item>
                                <Nav.Item className="col rounded p-0 mr-4">
                                    <Nav.Link eventKey="attributes" className="text-left pt-4 pb-4"><span className="attr-tab-header">2. Attributes</span><br/><span>The parameters that define this profile</span></Nav.Link>
                                </Nav.Item>
                                <Nav.Item className="col rounded p-0">
                                    <Nav.Link eventKey="dependencies" className="text-left pt-4 pb-4"><span className="attr-tab-header">3. Dependencies</span><br /><span>Profiles that depend on 'me'</span></Nav.Link>
                                </Nav.Item>
                            </Nav>
                        </Row>
                        
                        <Tab.Content>
                            <Tab.Pane eventKey="details">
                            {/* DETAILS CONTENT */}
                                <Card className="mt-4 pb-4 pl-5 pr-5">
                                    <Card.Body>
                                    <Form noValidate>
                                        <div className="d-flex">
                                            <Form.Group className="flex-grow-1 mr-4">
                                                <Form.Label>Namespace</Form.Label>                                
                                                {!_isValid.namespace &&
                                                    <span className="invalid-field-message inline">
                                                        Required
                                                </span>
                                                }
                                                <Form.Control className={(!_isValid.namespace ? 'invalid-field' : '')} id="namespace" type="" placeholder="Enter namespace"
                                                        value={item.namespace} onBlur={validateForm_namespace} onChange={onChange} readOnly={isReadOnly} />
                                            </Form.Group>
                                            {renderProfileType()}
                                        </div>
                                        <div className="d-flex mt-4">
                                            <Form.Group className="flex-grow-1">
                                                <Form.Label>Description</Form.Label>                                
                                                {!_isValid.description &&
                                                    <span className="invalid-field-message inline">
                                                        Required
                                                    </span>
                                                }                            
                                                <Form.Control className={(!_isValid.description ? 'invalid-field' : '')} id="description" type="" placeholder={`Describe your ${caption.toLowerCase()} here`}
                                                    value={item.description} onBlur={validateForm_description} onChange={onChange} readOnly={mode === "view"} />
                                            </Form.Group>
                                        </div>
                                        <div className="d-flex mt-4">
                                            <Form.Group className="flex-grow-1 mr-4">
                                                <Form.Label>Author name</Form.Label>
                                                <Form.Control  id="entity_author" type="" placeholder="Enter your name here" value={item.author.fullName} onChange={onChange} readOnly={isReadOnly} disabled='disabled' />
                                            </Form.Group>

                                            <Form.Group className="flex-grow-1 mr-4">
                                                <Form.Label>Organization</Form.Label>
                                                    <Form.Control id="entity_organization" type="" placeholder="Enter your orgainization's name" value={item.author.organization.name} onChange={onChange} readOnly={isReadOnly} disabled='disabled' />
                                            </Form.Group>

                                            <Form.Group className="flex-grow-1">
                                                <Form.Label>License type</Form.Label>
                                                    <Form.Control id="entity_licenseType" as="select" className="minimal pr-5" onChange={onChange} readOnly={isReadOnly} disabled='disabled' >
                                                    <option value="-1" >Select</option>
                                                    <option value="1" >1</option>
                                                    <option value="2" >2</option>
                                                    <option value="3" >3</option>
                                                    <option value="4" >4</option>
                                                    <option value="5" >5</option>
                                                </Form.Control>
                                            </Form.Group>
                                        </div>
                                        <div className="d-flex mt-4">
                                            <Form.Group className="flex-grow-1">
                                                <Form.Label>Meta tags (optional)</Form.Label>
                                                <Form.Control  id="metaTagsConcatenated" type="" placeholder="Enter tags seperated by a comma" value={item.metaTagsConcatenated} onChange={onChange} readOnly={isReadOnly} />
                                            </Form.Group>
                                        </div>
                                    </Form>
                                    </Card.Body>
                                </Card>
                            </Tab.Pane>
                            <Tab.Pane eventKey="attributes">
                            {/* ATTRIBUTES CONTENT */}
                                <Card className="mt-4 pb-4">
                                    <Card.Body>
                                        <AttributeList profile={item} profileAttributes={item.profileAttributes} extendedProfileAttributes={item.extendedProfileAttributes} readOnly={mode === "view"}
                                            onAttributeAdd={onAttributeAdd} onAttributeInterfaceAdd={onInterfaceAttributeAdd}
                                            onAttributeDelete={onAttributeDelete} onAttributeInterfaceDelete={onAttributeInterfaceDelete} onAttributeUpdate={onAttributeUpdate} />
                                    </Card.Body>
                                </Card>
                            </Tab.Pane>
                            <Tab.Pane eventKey="dependencies">
                            {/* DEPENDENCIES CONTENT */}
                                <Card className="mt-4 pb-4">
                                    <Card.Body>
                                        <DependencyList profile={item} />
                                    </Card.Body>
                                </Card>
                            </Tab.Pane>  
                        </Tab.Content>
                    </Col>
                </Tab.Container>
            </div>

            <div className="d-flex align-items-center justify-content-end">
                <Form className="d-flex align-items-center">
                    {renderButtons()}
                </Form>
            </div>

        </div>
    }

    //return final ui
    return (
        <Fragment>
            <Helmet>
                <title>{AppSettings.Titles.Main + " | " + titleCaption}</title>
            </Helmet>
            {renderProfileBreadcrumbs()}
            {/* My Profile specific view here */}
            {content}
            {/* RIGHT PANEL USED FOR VIEWING ATTRIBUTE DETAILS 
                <RightPanel />*/}

        </Fragment>
    )
}

export default ProfileEntity;
