import React, { useState, useEffect } from 'react'
import { useParams, useNavigate, useLocation } from 'react-router-dom'
import { Helmet } from "react-helmet"
import Card from 'react-bootstrap/Card'
import Nav from 'react-bootstrap/Nav'
import { Button } from 'react-bootstrap';
import { useMsal } from "@azure/msal-react";
import axiosInstance from "../services/AxiosService";

import { generateLogMessageString, getIconColorByProfileState, renderTitleBlock, useQueryString } from '../utils/UtilityService'
import { AppSettings } from '../utils/appsettings'
import { useLoadingContext, UpdateRecentFileList } from '../components/contexts/LoadingContext';
import { isOwner } from './shared/ProfileRenderHelpers';
import { findSearchFilter, isProfileValid, profileNew, saveProfile, validate_All } from '../services/ProfileService';
import ProfileEntityForm from './shared/ProfileEntity';
import ProfileTypeDefinitionListGrid from './shared/ProfileTypeDefinitionListGrid';
import ProfileActions from './shared/ProfileActions';
import ProfileCloudLibStatus from './shared/ProfileCloudLibStatus'

import './styles/ProfileEntity.scss';

const CLASS_NAME = "ProfileEntity";

function ProfileEntity() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const navigate = useNavigate();
    const location = useLocation();
    const { id } = useParams();
    const _tab = useQueryString("tab");
    const { instance } = useMsal();
    const _activeAccount = instance.getActiveAccount();
    const [_mode, setMode] = useState(initPageMode());
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_item, setItem] = useState(null);
    const [_isValid, setIsValid] = useState({ namespace: true, namespaceFormat: true, description: true, type: true, symbolicName: true, licenseExpression: true });
    const _iconName = AppSettings.IconMapper.Profile;

    const [_searchCriteria, setSearchCriteria] = useState(null);
    const [_searchCriteriaChanged, setSearchCriteriaChanged] = useState(0);
    const [_defaultTab, setDefaultTab] = useState('general');
    const [_forceReload, setForceReload] = useState(0); //increment this value to cause a re-get of the latest data.

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

            let result = null;
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
                    navigate('/404');
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
                const revisedList = UpdateRecentFileList(loadingProps.recentFileList, {
                    url: location.pathname,
                    caption: friendlyCaption,
                    iconName: _iconName,
                    authorId: result.data.author != null && result.data.isReadOnly === false ? result.data.author.objectIdAAD : null
                });
                setLoadingProps({ recentFileList: revisedList });
            }
        }

        //get a blank object from server
        async function fetchDataAdd() {
            console.log(generateLogMessageString('useEffect||fetchDataAdd||async', CLASS_NAME));
            var newthing = JSON.parse(JSON.stringify(profileNew));
            newthing.contributorName = loadingProps.organizationName;
            setItem(newthing);
            setLoadingProps({ isLoading: false, message: null });
        }

        //fetch our data
        // for view/edit modes
        //this will execute on initial load and then anytime _forceReload increments - which happens on publish change.
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
    }, [id, _forceReload]);

    //-------------------------------------------------------------------
    // Region: hook - trigger search criteria change to get the type definitions
    //-------------------------------------------------------------------
    useEffect(() => {
        //check for searchcriteria - trigger fetch of search criteria data - if not already triggered
        if ((loadingProps.searchCriteria == null || loadingProps.searchCriteria.filters == null) && !loadingProps.refreshSearchCriteria) {
            setLoadingProps({ refreshSearchCriteria: true });
            return;
        }
        else if (loadingProps.searchCriteria == null || loadingProps.searchCriteria.filters == null) {
            return;
        }
        //implies it is in progress on re-loading criteria
        else if (loadingProps.refreshSearchCriteria) {
            return;
        }

        //we only need to update search criteria when there is a profile id. 
        if (id == null || id === "new") return;

        //assign profile id as filter
        let criteria = JSON.parse(JSON.stringify(loadingProps.searchCriteria));

        //sometimes the cached version will not yet have this profile id. if that happens, add it by refreshing criteria list
        const item = findSearchFilter(criteria, AppSettings.SearchCriteriaCategory.Profile, parseInt(id));
        if (item == null) {
            setLoadingProps({ refreshSearchCriteria: true });
            return;
        }
        else {
            item.selected = true;
        }

        setSearchCriteria(criteria);
        //trigger api to get data - unless in new mode
        setSearchCriteriaChanged(_searchCriteriaChanged + 1);

    }, [loadingProps.searchCriteria, id]);


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
        if (isValid.namespace != null) {
            setIsValid(previous => (
                {
                    ...previous,
                    namespace: isValid.namespace,
                    namespaceFormat: isValid.namespaceFormat
                })
            );

        }
        if (isValid.licenseExpression != null) {
            setIsValid(previous => (
                {
                    ...previous,
                    licenseExpression: isValid.licenseExpression
                })
            );
        }
    }

    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));
        const isValid = validate_All(_item);
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

        // Set when the user saved from shared/ProfileEntity.js
        setLoadingProps({ bIsProfileEditUnsaved: false });
    };

    const onSaveSuccess = (item) => {
        console.log(generateLogMessageString('onSaveSuccess', CLASS_NAME));
        //hide a spinner, show a message
        setLoadingProps({
            isLoading: false, message: null, inlineMessages:
                [{ id: new Date().getTime(), severity: "success", body: `Item was saved`, isTimed: true }]
            , refreshProfileCount: true
            , refreshSearchCriteria: true
        });
        navigate(`/profile/${item.id}`);
    };

    const onSaveError = (msg) => {
        console.log(generateLogMessageString('onSaveError', CLASS_NAME));
        //hide a spinner, show a message
        setLoadingProps({
            isLoading: false, message: msg, inlineMessages:
                [{ id: new Date().getTime(), severity: "critical", body: `An error occurred saving this item: ${msg}`, isTimed: false }]
        });
    };

    //if delete goes through, navigate to profiles library page
    const onDelete = (success) => {
        console.log(generateLogMessageString(`onDelete || ${success}`, CLASS_NAME));
        if (success) {
            navigate(`/profiles/library`);
        }
    };

    //bubble up search criteria changed so the parent page can control the search criteria
    const onSearchCriteriaChanged = (criteria) => {
        console.log(generateLogMessageString(`onSearchCriteriaChanged`, CLASS_NAME));
        //update state
        setSearchCriteria(criteria);
        //trigger api to get data
        setSearchCriteriaChanged(_searchCriteriaChanged + 1);
    };

    const onPublishChange = (success) => {
        console.log(generateLogMessageString(`onPublishChange`, CLASS_NAME));

        //re-display current page to get updated status
        setForceReload(_forceReload + 1);
    }

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderHeaderRow = (caption) => {
        const iconColor = getIconColorByProfileState(_item?.profileState);
        return (
            <div className="row pb-3">
                <div className="col-sm-7 mr-auto d-flex">
                    {renderTitleBlock(caption, _iconName, iconColor)}
                </div>
                <div className="col-sm-5 d-flex align-items-center justify-content-end">
                    {(_item != null) &&
                        <ProfileCloudLibStatus item={_item} activeAccount={_activeAccount} saveAndPublish={true} showButton={true} showStatus={false}
                            onPublishProfileCallback={onPublishChange} onWithdrawProfileCallback={onPublishChange} />
                    }
                    {(_mode.toLowerCase() !== "view") &&
                        <>
                            <Button variant="text-solo" className="mx-1 btn-auto auto-width" href={`/profiles/library`} >Cancel</Button>
                            <Button variant="secondary" type="button" className="mx-3 d-none d-lg-block" onClick={onSave} >Save</Button>
                            <Button variant="icon-outline" type="button" className="mx-1 d-lg-none" onClick={onSave} title="Save" ><i className="material-icons">save</i></Button>
                        </>
                    }
                    {(_mode.toLowerCase() !== "new") &&
                        <ProfileActions item={_item} activeAccount={_activeAccount} onDeleteCallback={onDelete} />
                    }
                </div>
            </div>
        );
    };

    const renderTabbedView = () => {
        return (
            <div className="entity-details">
                <ul className="nav nav-tabs nav-fill" role="tablist">
                    <li className="nav-item">
                        <a className={`nav-link ${_defaultTab === 'general' ? "active" : "" }`} id="general-tab" data-toggle="tab" href="#generalPane" role="tab" aria-controls="generalPane" aria-selected="true">General</a>
                    </li>
                    <li className="nav-item">
                        <a className={`nav-link ${_defaultTab === 'typedefs' ? "active" : "" }`} id="typeDef-tab" data-toggle="tab" href="#typeDefPane" role="tab" aria-controls="typeDefPane" aria-selected="false">Type Definitions</a>
                    </li>
                </ul>
                <div className="tab-content" >
                    <div className={`tab-pane fade show ${_defaultTab === 'general' ? "show active" : ""}`} id="generalPane" role="tabpanel" aria-labelledby="general-tab">
                        {/* GENERAL CONTENT */}
                        <Card className="rounded-0 rounded-bottom border-top-0">
                            <Card.Body className="pt-3">
                                <ProfileEntityForm item={_item} onValidate={onValidate} isValid={_isValid} onChange={onChange} />
                            </Card.Body>
                        </Card>
                    </div>
                    <div className={`tab-pane fade ${_defaultTab === 'typedefs' ? "show active" : ""}`} id="typeDefPane" role="tabpanel" aria-labelledby="typeDef-tab">
                        {/* Typ Def CONTENT */}
                        <Card className="rounded-0 rounded-bottom border-top-0">
                            <Card.Body className="pt-3">
                                <ProfileTypeDefinitionListGrid searchCriteria={_searchCriteria}
                                    onSearchCriteriaChanged={onSearchCriteriaChanged} searchCriteriaChanged={_searchCriteriaChanged} />
                            </Card.Body>
                        </Card>
                    </div>
                </div>
            </div>
        );
    };

    const renderNewView = () => {
        return (
            <div className="entity-details px-2">
                <ProfileEntityForm item={_item} onValidate={onValidate} isValid={_isValid} onChange={onChange} />
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
            {(_item != null && _item.profileState === AppSettings.ProfileStateEnum.CloudLibRejected &&
                _item.cloudLibApprovalDescription != null &&
                _item.cloudLibApprovalDescription !== '') &&
                <div className="col-sm-12 d-flex" >
                    <p className="alert alert-danger my-2 small-size w-100" >Publish Rejection Reason: {_item.cloudLibApprovalDescription}</p>
                </div>
            }
            {(_item != null && id !== "new") &&
                renderTabbedView()
            }
            {(_item != null && id === "new") &&
                renderNewView()
            }
        </>
    )
}

export default ProfileEntity;
