import React, { useState, useEffect } from 'react'
import { useParams, useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import Card from 'react-bootstrap/Card'
import Tab from 'react-bootstrap/Tab'
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
    const history = useHistory();
    const { id } = useParams();
    const _qsTab = useQueryString("tab");
    const _qsTitle = useQueryString("c");
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_item, setItem] = useState(null);
    const _iconName = 'folder-profile';
    const _iconColor = color.shark;
    const [_cloudLibItem, setCloudLibItem] = useState([]);
    const [_pollForCompletion, setPollForCompletion] = useState({counter: 0, importLogId: null, isComplete: null});
    const [_searchCriteria, setSearchCriteria] = useState(null);
    const [_searchCriteriaChanged, setSearchCriteriaChanged] = useState(0);
    const [_defaultTab, setDefaultTab] = useState('general');
    const [_deleteMessageId, setDeleteMessageId] = useState(null);

    //pass to common form
    const _isValid = { namespace: true, namespaceFormat: true, description: true, type: true, symbolicName: true };

    //-------------------------------------------------------------------
    // Region: hooks - get item by cloudlibid
    //-------------------------------------------------------------------
    useEffect(() => {

        async function fetchData() {
            console.log(generateLogMessageString('useEffect||fetchProfile||async', CLASS_NAME));
            //initialize spinner during loading
            setLoadingProps({ isLoading: true, message: null });

            var result = null;
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
                    history.push('/404');
                }
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: msg, isTimed: false }]
                });
            }

            if (result == null) return;

            setLoadingProps({ isLoading: false, message: null });

            //if we get no data back, then we need to initiate an import from CloudLibrary
            //setting state causes this to trigger that to happen
            if (result.data == null || result.data === '') {
                console.log(generateLogMessageString('useEffect||fetchProfile||no profile imported || trigger import', CLASS_NAME));
                setCloudLibItem([{ cloudLibraryId: id, title: _qsTitle }]);
            }
            else
            {
                //set item state value
                setItem(result.data);
                //clear out import cloud library triggers
                setCloudLibItem([]);
            }
        }

        //fetch our data - isComplete == null - initial load of page
        //isComplete === true once we complete import and ready to pull imported data
        if ((id != null && (_pollForCompletion.isComplete == null || _pollForCompletion.isComplete === true))) {
            fetchData();
        }

    }, [id, _pollForCompletion.isComplete]);

    //-------------------------------------------------------------------
    // Region: hook - run a polling operation to check for completion of import
    //-------------------------------------------------------------------
    useEffect(() => {
        if (_pollForCompletion.counter === 0 || !_pollForCompletion.importLogId || _pollForCompletion.isComplete) return;

        setTimeout(() => {
            //check on the status of this specific import by investigating import logs, 
            //once it finishes, then attempt to get profile via cloudLibId again
            const hasMatch = loadingProps.importingLogs.some(x =>
                x.id === _pollForCompletion.importLogId && (x.status !== AppSettings.ImportLogStatus.Completed));
            console.log(generateLogMessageString(`pollForCompletion || Import Completed: ${hasMatch}`, CLASS_NAME));
            if (hasMatch) {
                //try again in 1000 seconds
                setPollForCompletion({ ..._pollForCompletion, counter: _pollForCompletion.counter + 1 });
            }
            else {
                console.log(generateLogMessageString(`pollForCompletion || Import Completed`, CLASS_NAME));
                setDeleteMessageId(_pollForCompletion.importLogId);
                setPollForCompletion({ counter: 0, importLogId: null, isComplete: true });
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

    // TBD: need loop to remove and add styles for "nav-item" CSS animations
    const tabListener = (eventKey) => {
    }

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderHeaderRow = (caption) => {
        return (
            <div className="row pb-3">
                <div className="col-sm-8 mr-auto d-flex">
                    {renderTitleBlock(caption, _iconName, _iconColor)}
                </div>
                <div className="col-sm-4 d-flex align-items-center justify-content-end">
                    <span className="my-0 mr-2"><a href={`/profiles/library`} >Profile Library</a></span>
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
                                    <ProfileEntityForm item={_item} isValid={_isValid} />
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
