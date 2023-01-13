import React, { useState, useEffect } from 'react'
import { useParams, useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import Card from 'react-bootstrap/Card'
import Tab from 'react-bootstrap/Tab'
import Nav from 'react-bootstrap/Nav'
import { Button } from 'react-bootstrap';
import { useMsal } from "@azure/msal-react";
import axiosInstance from "../services/AxiosService";

import { generateLogMessageString, renderTitleBlock, useQueryString } from '../utils/UtilityService'
import { AppSettings } from '../utils/appsettings'
import { useLoadingContext, UpdateRecentFileList } from '../components/contexts/LoadingContext';
import { isOwner } from './shared/ProfileRenderHelpers';
import { clearSearchCriteria, isProfileValid, profileNew, saveProfile, toggleSearchFilterSelected, validate_All } from '../services/ProfileService';
import ProfileEntityForm from './shared/ProfileEntity';
import ProfileTypeDefinitionListGrid from './shared/ProfileTypeDefinitionListGrid';
import ProfileActions from './shared/ProfileActions';

import color from '../components/Constants';
import './styles/ProfileEntity.scss';

const CLASS_NAME = "ProfileEntity";

function ProfileEntity() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();
    const { id } = useParams();
    const _tab = useQueryString("tab");
    const { instance } = useMsal();
    const _activeAccount = instance.getActiveAccount();
    const [_mode, setMode] = useState(initPageMode());
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_item, setItem] = useState(null);
    const [_isValid, setIsValid] = useState({ namespace: true, namespaceFormat: true, description: true, type: true, symbolicName: true });
    const _iconName = 'folder-profile';
    const _iconColor = color.shark;

    const [_initSearchCriteria, setInitSearchCriteria] = useState(true);
    const [_searchCriteria, setSearchCriteria] = useState(null);
    const [_searchCriteriaChanged, setSearchCriteriaChanged] = useState(0);
    const [_defaultTab, setDefaultTab] = useState('general');

    function initPageMode() {
        //if path contains new, then go into a new mode
        if (id === 'new') {
            return 'new';
        }

        //if path contains id, then default to view mode and determine in fetch whether user is owner or not.
        return 'view';
    }

    //-------------------------------------------------------------------
    // Region: hooks - get item by id
    //-------------------------------------------------------------------
    useEffect(() => {

        async function fetchData() {
            console.log(generateLogMessageString('useEffect||fetchData||async', CLASS_NAME));
            //initialize spinner during loading
            setLoadingProps({ isLoading: true, message: null });

            var result = null;
            try {
                const data = { id: id };
                const url = `profile/getbyid`;
                result = await axiosInstance.post(url, data);
            }
            catch (err) {
                let msg = 'An error occurred retrieving this profile.';
                console.log(generateLogMessageString('useEffect||fetchData||error', CLASS_NAME, 'error'));
                //console.log(err.response.status);
                if (err != null && err.response != null && err.response.status === 404) {
                    msg += ' This item was not found.';
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

            //set item state value
            setItem(result.data);
            setLoadingProps({ isLoading: false, message: null });
            setMode(thisMode);

            //add to the recent file list to keep track of where we have been
            if (thisMode.toLowerCase() === "view" || thisMode.toLowerCase() === "edit") {
                let friendlyCaption = result.data?.title != null && result.data?.title != '' ? result.data?.title :
                    result.data?.namespace.replace('https://', '').replace('http://', '');
                if (friendlyCaption.length > 25) {
                    friendlyCaption = `...${friendlyCaption.substring(friendlyCaption.length - 25)}`;
                }
                var revisedList = UpdateRecentFileList(loadingProps.recentFileList, {
                    url: history.location.pathname,
                    caption: friendlyCaption,
                    iconName: _iconName,
                    authorId: result.data.author != null ? result.data.author.objectIdAAD : null
                });
                setLoadingProps({ recentFileList: revisedList });
            }
        }

        //get a blank object from server
        async function fetchDataAdd() {
            console.log(generateLogMessageString('useEffect||fetchDataAdd||async', CLASS_NAME));
            //initialize spinner during loading
            setLoadingProps({ isLoading: true, message: null });

            //set item state value
            setItem(JSON.parse(JSON.stringify(profileNew)));
            setLoadingProps({ isLoading: false, message: null });
        }

        //fetch our data
        // for view/edit modes
        if ((id != null && id.toString() !== 'new')) {
            fetchData();
        }
        else {
            fetchDataAdd();
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [id]);

    //-------------------------------------------------------------------
    // Region: hook - trigger search criteria change to get the type definitions
    //-------------------------------------------------------------------
    useEffect(() => {

        if (!_initSearchCriteria) return;

        //check for searchcriteria - trigger fetch of search criteria data - if not already triggered
        if ((loadingProps.searchCriteria == null || loadingProps.searchCriteria.filters == null) && !loadingProps.refreshSearchCriteria) {
            setLoadingProps({ refreshSearchCriteria: true });
        }
        //start with a blank criteria slate. Handle possible null scenario if criteria hasn't loaded yet. 
        var criteria = loadingProps.searchCriteria == null ? null : JSON.parse(JSON.stringify(loadingProps.searchCriteria));

        if (criteria != null) {
            criteria = clearSearchCriteria(criteria);
            //add in any profile filter passed in url
            if (id != null) {
                toggleSearchFilterSelected(criteria, AppSettings.SearchCriteriaCategory.Profile, parseInt(id));
            }
        }

        //update state
        setInitSearchCriteria(false);
        if (criteria != null) {
            setSearchCriteria(criteria);
            setSearchCriteriaChanged(_searchCriteriaChanged + 1);
        }
        setLoadingProps({ ...loadingProps, searchCriteria: criteria });

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [id, _initSearchCriteria, loadingProps.searchCriteriaRefreshed]);

    //-------------------------------------------------------------------
    // Region: hook - set the default tab on load if passed in
    //-------------------------------------------------------------------
    useEffect(() => {
        if (_tab != null) setDefaultTab(_tab);
    }, [_tab]);

    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------
    //on validate handler from child form
    const onValidate = (isValid) => {
        setIsValid({
            namespace: isValid.namespace,
            namespaceFormat: isValid.namespaceFormat
        });
    }

    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));
        var isValid = validate_All(_item);
        setIsValid(isValid);
        return isProfileValid(isValid);
    }

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onChange = (item) => {
        console.log(generateLogMessageString(`onChange`, CLASS_NAME));

        //update state - child component already makes a copy
        setItem(item);
    }

    const onSave = () => {
        console.log(generateLogMessageString('onSave', CLASS_NAME));

        //do validation
        if (!validateForm()) {
            //alert("validation failed");
            return;
        }

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform insert/update call
        console.log(generateLogMessageString(`handleOnSave||${_mode}`, CLASS_NAME));
        saveProfile(_item, onSaveSuccess, onSaveError);
    };

    const onSaveSuccess = (item) => {
        console.log(generateLogMessageString('onSaveSuccess', CLASS_NAME));
        //hide a spinner, show a message
        setLoadingProps({
            isLoading: false, message: null, inlineMessages: 
                [{ id: new Date().getTime(), severity: "success", body: `Item was saved`, isTimed: true }]
            , refreshProfileCount: true
        });

        setItem(JSON.parse(JSON.stringify(item)));
    };

    const onSaveError = (msg) => {
        console.log(generateLogMessageString('onSaveError', CLASS_NAME));
        //hide a spinner, show a message
        setLoadingProps({
            isLoading: false, message: msg, inlineMessages:
                [{ id: new Date().getTime(), severity: "critical", body: `An error occurred saving this item: ${msg}`, isTimed: false }]
        });
    };

    //bubble up search criteria changed so the parent page can control the search criteria
    const onSearchCriteriaChanged = (criteria) => {
        console.log(generateLogMessageString(`onSearchCriteriaChanged`, CLASS_NAME));
        //update state
        setSearchCriteria(criteria);
        //trigger api to get data
        setSearchCriteriaChanged(_searchCriteriaChanged + 1);
    };

    // TBD: need loop to remove and add styles for "nav-item" CSS animations
    const tabListener = (eventKey) => {
    }

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderHeaderRow = (caption) => {
        return (
            <div className="row pb-3">
                <div className="col-sm-7 mr-auto d-flex">
                    {renderTitleBlock(caption, _iconName, _iconColor)}
                </div>
                <div className="col-sm-5 d-flex align-items-center justify-content-end">
                    <span className="my-0 mr-2"><a href={`/profiles/library`} >Profile Library</a></span>
                    {(_mode.toLowerCase() !== "view") &&
                        <>
                        <Button variant="text-solo" className="mx-1 btn-auto auto-width" href={`/profiles/library`} >Cancel</Button>
                        <Button variant="secondary" type="button" className="mx-3 d-none d-lg-block" onClick={onSave} >Save</Button>
                        <Button variant="icon-outline" type="button" className="mx-1 d-lg-none" onClick={onSave} title="Save" ><i className="material-icons">save</i></Button>
                        </>
                    }
                    <ProfileActions item={_item} activeAccount={_activeAccount} />
                </div>
            </div>
        );
    };

    const renderTabbedView = () => {
        return (
            <div className="entity-details">
                {/* TABS */}
                <Tab.Container id="profile-definition" defaultActiveKey={_defaultTab} onSelect={tabListener}>
                    <Nav variant="pills" className="row mt-1 px-2 pr-md-3">
                        <Nav.Item className="col-sm-4 rounded p-0 pl-2" >
                            <Nav.Link eventKey="general" className="text-center text-md-left p-1 px-2 h-100" >
                                <span className="headline-3">General</span>
                            </Nav.Link>
                        </Nav.Item>
                        <Nav.Item className="col-sm-4 rounded p-0 px-md-0" >
                            <Nav.Link eventKey="typedefs" className="text-center text-md-left p-1 px-2 h-100" >
                                <span className="headline-3">Type Definitions</span>
                            </Nav.Link>
                        </Nav.Item>
                    </Nav>

                    <Tab.Content>
                        <Tab.Pane eventKey="general">
                            <Card className="">
                                <Card.Body className="pt-3">
                                    <ProfileEntityForm item={_item} onValidate={onValidate} isValid={_isValid} onChange={onChange} />
                                </Card.Body>
                            </Card>
                        </Tab.Pane>
                        <Tab.Pane eventKey="typedefs">
                            <Card className="">
                                <Card.Body className="pt-3">
                                    <ProfileTypeDefinitionListGrid searchCriteria={_searchCriteria}
                                        onSearchCriteriaChanged={onSearchCriteriaChanged} searchCriteriaChanged={_searchCriteriaChanged} />
                                </Card.Body>
                            </Card>
                        </Tab.Pane>
                    </Tab.Content>
                </Tab.Container>
            </div>
        );
    };

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    if (_item == null) return null;

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    const _name = `${_item?.title != null && _item?.title !== '' ? _item?.title : _item?.namespace}`;
    const _caption = `${_name === '' ? '' : _name + ' | '} Cloud Library Viewer | ${AppSettings.Titles.Main}`;
    return (
        <>
            <Helmet>
                <title>{_caption}</title>
            </Helmet>
            {renderHeaderRow(`Profile ${_name === '' ? '' : ' - ' + _name}`)}
            {_item != null &&
                renderTabbedView()
            }
        </>
    )
}

export default ProfileEntity;
