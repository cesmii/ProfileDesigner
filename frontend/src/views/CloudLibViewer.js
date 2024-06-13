import React, { useState, useEffect } from 'react'
import { useParams, useNavigate } from 'react-router-dom'
import { Helmet } from "react-helmet"
import Card from 'react-bootstrap/Card'
import Nav from 'react-bootstrap/Nav'
import axiosInstance from "../services/AxiosService";

import { generateLogMessageString, renderTitleBlock, useQueryString } from '../utils/UtilityService'
import { AppSettings } from '../utils/appsettings'
import { useLoadingContext } from '../components/contexts/LoadingContext';
import { useDeleteImportMessage } from '../components/ImportMessage'
import { LoadingSpinner } from '../components/LoadingOverlay'
import ProfileEntityForm from './shared/ProfileEntity';
import { CloudLibraryImporter } from './shared/CloudLibraryImporter';
import ProfileTypeDefinitionListGrid from './shared/ProfileTypeDefinitionListGrid';
import { toggleSearchFilterSelected } from '../services/ProfileService';

import color from '../components/Constants';
import './styles/ProfileEntity.scss';

const CLASS_NAME = "CloudLibViewer";

function CloudLibViewer() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const navigate = useNavigate();
    const { id } = useParams();
    const _qsTab = useQueryString("tab");
    const _qsTitle = useQueryString("c");
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_item, setItem] = useState(null);
    const _iconName = 'folder-profile';
    const _iconColor = color.shark;
    const [_cloudLibItem, setCloudLibItem] = useState([]);
    const [_pollForCompletion, setPollForCompletion] = useState({counter: 0, importLogId: null, isComplete: null, isFailed: false});
    const [_searchCriteria, setSearchCriteria] = useState(null);
    const [_searchCriteriaChanged, setSearchCriteriaChanged] = useState(0);
    const [_defaultTab, setDefaultTab] = useState('general');
    const [_deleteMessageId, setDeleteMessageId] = useState(null);

    //pass to common form
    const _isValid = { namespace: true, namespaceFormat: true, description: true, type: true, symbolicName: true, licenseExpression: true };

    //-------------------------------------------------------------------
    // Region: hooks - get item by cloudlibid
    //-------------------------------------------------------------------
    useEffect(() => {

        async function fetchData() {
            console.log(generateLogMessageString('useEffect||fetchProfile||async', CLASS_NAME));
            //initialize spinner during loading
            setLoadingProps({ isLoading: true, message: null });

            let result = null;
            try {
                const data = { id: id };
                const url = `profile/getbycloudlibid`;
                result = await axiosInstance.post(url, data);
            }
            catch (err) {
                let msg = 'An error occurred retrieving this profile.';
                console.log(generateLogMessageString('useEffect||fetchData||error', CLASS_NAME, 'error'));
                //console.log(err.response.status);
                if (err != null && err.response != null && err.response.status === 404) {
                    msg += ' This item was not found.';
                    navigate.push('/404');
                }
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: msg, isTimed: false }]
                });
            }

            if (result == null) return;

            //if we get no data back, then we need to initiate an import from CloudLibrary
            //setting state causes this to trigger that to happen
            if (result.data == null || result.data === '') {
                console.log(generateLogMessageString('useEffect||fetchProfile||no profile imported || trigger import', CLASS_NAME));
                setCloudLibItem([{ cloudLibraryId: id, title: _qsTitle }]);
                setLoadingProps({ isLoading: false, message: null });
            }
            else
            {
                //set item state value
                setItem(result.data);
                //clear out import cloud library triggers
                setCloudLibItem([]);
                //re-get the latest type definition search filters to make sure this profile is included
                setLoadingProps({ isLoading: false, message: null, searchCriteria: null, refreshSearchCriteria: true });
            }
        }

        //if a failure occurs, bail out
        if (_pollForCompletion.isFailed) { return; }

        //fetch our data - isComplete == null - initial load of page
        //isComplete === true once we complete import and ready to pull imported data
        if ((id != null && (_pollForCompletion.isComplete == null || _pollForCompletion.isComplete === true))) {
            fetchData();
        }

    }, [id, _pollForCompletion.isComplete, _pollForCompletion.isFailed]);

    //-------------------------------------------------------------------
    // Region: hook - run a polling operation to check for completion of import
    //-------------------------------------------------------------------
    useEffect(() => {
        if (_pollForCompletion.counter === 0 || !_pollForCompletion.importLogId || _pollForCompletion.isComplete) return;

        setTimeout(() => {
            //scenario 1 - in progress - keep polling
            //scenario 2 - failed, update state so we can stop polling for completion and let user know it failed. 
            //scenario 3 - completed, update state so we can try and grab newly imported item

            //check on the status of this specific import by investigating import logs,
            //once it finishes, then attempt to get profile via cloudLibId again
            const inProgress = loadingProps.importingLogs.some(x =>
                x.id === _pollForCompletion.importLogId &&
                (x.status !== AppSettings.ImportLogStatus.Completed) &&
                (x.status !== AppSettings.ImportLogStatus.Failed));
            const isFailed = loadingProps.importingLogs.some(x =>
                x.id === _pollForCompletion.importLogId &&
                (x.status === AppSettings.ImportLogStatus.Failed));

            if (inProgress) {
                console.log(generateLogMessageString(`pollForCompletion || Import in progress...`, CLASS_NAME));
                //try again in 1000 seconds
                setPollForCompletion({ ..._pollForCompletion, counter: _pollForCompletion.counter + 1 });
            }
            else if (isFailed) {
                //error message - showed by importing log component
                console.log(generateLogMessageString(`pollForCompletion || Import Failed`, CLASS_NAME));
                setLoadingProps({isLoading: false, message: null, inlineMessages: []});
                setPollForCompletion({ counter: 0, importLogId: null, isComplete: null, isFailed: true });
            }
            //completed scenario
            else {
                console.log(generateLogMessageString(`pollForCompletion || Import Completed`, CLASS_NAME));
                setDeleteMessageId(_pollForCompletion.importLogId);
                setPollForCompletion({ counter: 0, importLogId: null, isComplete: true, isFailed: false });
                //re-get the latest type definition search filters to make sure this profile is included
                setLoadingProps({ isLoading: false, message: null, searchCriteria: null, refreshSearchCriteria: true });
            }
        }, 1000);

    }, [_pollForCompletion]);

    //-------------------------------------------------------------------
    // Region: search criteria check and populate
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
        if (_item?.id == null) return;

        //assign profile id as filter
        var criteria = JSON.parse(JSON.stringify(loadingProps.searchCriteria));
        toggleSearchFilterSelected(criteria, AppSettings.SearchCriteriaCategory.Profile, parseInt(_item.id));
        setSearchCriteria(criteria);
        //trigger api to get data
        setSearchCriteriaChanged(_searchCriteriaChanged + 1);

    }, [loadingProps.searchCriteria, _item?.id]);


    //-------------------------------------------------------------------
    // Region: hook - set the default tab on load if passed in
    //-------------------------------------------------------------------
    useEffect(() => {
        if (_qsTab != null) setDefaultTab(_qsTab);
    }, [_qsTab]);

    //-------------------------------------------------------------------
    // Region: hook - trigger delete of message on completion automatically
    //-------------------------------------------------------------------
    useDeleteImportMessage({ id: _deleteMessageId });

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onImportStarted = (importLogId) => {
        console.log(generateLogMessageString(`onImportStarted || start poll for completion`, CLASS_NAME));
        setPollForCompletion({ counter: 1, importLogId: importLogId, isComplete: false });
    }

    //bubble up search criteria changed so the parent page can control the search criteria
    const onSearchCriteriaChanged = (criteria) => {
        console.log(generateLogMessageString(`onSearchCriteriaChanged`, CLASS_NAME));
        //update state
        setSearchCriteria(criteria);
        //trigger api to get data
        setSearchCriteriaChanged(_searchCriteriaChanged + 1);
    };

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderHeaderRow = (caption) => {
        return (
            <div className="row pb-3">
                <div className="col-sm-8 me-auto d-flex">
                    {renderTitleBlock(caption, _iconName, _iconColor)}
                </div>
                <div className="col-sm-4 d-flex align-items-center justify-content-end">
                    <span className="my-0 me-2"><a href={`/profiles/library`} >Profile Library</a></span>
                </div>
            </div>
        );
    };

    const renderImportingPlaceholder = () => {
        return (
            <div className="row pb-3">
                <div className="col-sm-12">
                    <div className="mx-auto">
                        <LoadingSpinner />
                    </div>
                </div>
            </div>
        );
    };

    const renderTabbedView = () => {
        return (
            <div className="entity-details">
                <ul className="nav nav-tabs nav-fill" role="tablist">
                    <li className="nav-item">
                        <a className={`nav-link ${_defaultTab === 'general' ? "active" : ""}`} id="general-tab" data-toggle="tab" href="#generalPane" role="tab" aria-controls="generalPane" aria-selected="true">General</a>
                    </li>
                    <li className="nav-item">
                        <a className={`nav-link ${_defaultTab === 'typedefs' ? "active" : ""}`} id="typeDef-tab" data-toggle="tab" href="#typeDefPane" role="tab" aria-controls="typeDefPane" aria-selected="false">Type Definitions</a>
                    </li>
                </ul>
                <div className="tab-content" >
                    <div className={`tab-pane fade show ${_defaultTab === 'general' ? "show active" : ""}`} id="generalPane" role="tabpanel" aria-labelledby="general-tab">
                        {/* GENERAL CONTENT */}
                        <Card className="rounded-0 rounded-bottom border-top-0">
                            <Card.Body className="pt-3">
                                <ProfileEntityForm item={_item} isValid={_isValid} />
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

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    const _name = `${_item == null ? '' : _item?.title != null && _item?.title !== '' ? _item?.title : _item?.namespace}`;
    const _caption = `${_name === '' ? '' : _name + ' | ' } Cloud Library Viewer | ${AppSettings.Titles.Main}`;
    return (
        <>
            <Helmet>
                <title>{_caption}</title>
            </Helmet>
            {renderHeaderRow(`SM Profile Viewer ${_name === '' ? '' : ' - ' + _name }`)}
            {_item != null &&
                renderTabbedView()
            }
            {_pollForCompletion.importLogId != null &&
                renderImportingPlaceholder()
            }
            {_cloudLibItem.length > 0 &&
                <CloudLibraryImporter onImportStarted={onImportStarted} items={_cloudLibItem} />
            }
        </>
    )
}

export default CloudLibViewer;
