import React, { useState, useEffect } from 'react'
import { Helmet } from "react-helmet"

import { useMsal } from "@azure/msal-react";
import axiosInstance from "../services/AxiosService";

import { Button } from 'react-bootstrap'

import { AppSettings } from '../utils/appsettings'
import { generateLogMessageString, renderTitleBlock, scrollTop } from '../utils/UtilityService'
import { useLoadingContext } from "../components/contexts/LoadingContext";
import ConfirmationModal from '../components/ConfirmationModal';
import ProfileEntityModal from './modals/ProfileEntityModal';
import ProfileCloudLibImportModal from './modals/ProfileCloudLibImportModal';
import ProfileListGrid from './shared/ProfileListGrid';

import color from '../components/Constants'
import './styles/ProfileList.scss';
import '../components/styles/InfoPanel.scss';

import { ErrorModal } from '../services/CommonUtil';

const CLASS_NAME = "CloudLibList";

function CloudLibList() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const caption = 'Import from Cloud Library';
    const iconName = 'folder-shared';
    const iconColor = color.shark;
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_importConfirmModal, setImportConfirmModal] = useState({ show: false, items: null });
    //importer
    const [_errorMsg, setErrorMessage] = useState(null);
    const [_error, setError] = useState({ show: false, message: null, caption: null });
    //used in popup profile add/edit ui. Default to new version
    const [_profileEntityModal, setProfileEntityModal] = useState({ show: false, item: null });
    const [_profileCloudLibImportModal, setProfileCloudLibImportModal] = useState({ show: false, item: null });
    const [_initSearchCriteria, setInitSearchCriteria] = useState(true);
    const [_searchCriteria, setSearchCriteria] = useState(null);
    const [_searchCriteriaChanged, setSearchCriteriaChanged] = useState(0);
    const [_selectedCloudProfiles, setSelectedCloudProfiles] = useState([]);
    const [_selectedCloudProfileIds, setSelectedCloudProfileIds] = useState([]);

    //-------------------------------------------------------------------
    // Region: Pass profile id into component if profileId passed in from url
    //-------------------------------------------------------------------
    useEffect(() => {

        if (!_initSearchCriteria) return;

        //check for searchcriteria - trigger fetch of search criteria data - if not already triggered
        if ((loadingProps.profileSearchCriteria == null || loadingProps.profileSearchCriteria.filters == null) && !loadingProps.refreshProfileSearchCriteria) {
            setLoadingProps({ refreshProfileSearchCriteria: true });
        }
        //start with a blank criteria slate. Handle possible null scenario if criteria hasn't loaded yet. 
        const criteria = loadingProps.profileSearchCriteria == null ? null : JSON.parse(JSON.stringify(loadingProps.profileSearchCriteria));

        if (criteria != null) {
            criteria.filters[0].items[0].visible = false;
            criteria.filters[0].items[0].selected = false;

            criteria.filters[1].items[0].name = "Show imported profiles";
            criteria.filters[1].items[0].visible = true;
            criteria.filters[1].items[0].selected = false;

            criteria.filters[2].items[0].visible = false;
            criteria.filters[2].items[0].selected = true;
            //criteria = clearSearchCriteria(criteria);
        }

        //update state
        setInitSearchCriteria(false);
        if (criteria != null) {
            setSearchCriteria(criteria);
            setSearchCriteriaChanged(_searchCriteriaChanged + 1);
        }
        setLoadingProps(criteria);

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [_initSearchCriteria, loadingProps.profileSearchCriteriaRefreshed]);

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onGridRowSelect = (item) => {
        console.log(generateLogMessageString(`onGridRowSelect||Name: ${item.namespace}||selected: ${item.selected}`, CLASS_NAME));

        //var criteria = JSON.parse(JSON.stringify(_searchCriteria));
        //toggleSearchFilterSelected(criteria, 3, parseInt(item.id.toString()));
        //setSearchCriteria(criteria);

        setSelectedCloudProfiles(selectedCloudProfiles => {
            var existingIndex = selectedCloudProfiles.findIndex(i => i.id === item.id);
            if (item.selected) {
                if (existingIndex < 0) {
                    selectedCloudProfiles.push(item);
                }
            }
            else {
                if (existingIndex >= 0) {
                    selectedCloudProfiles.splice(existingIndex, 1);
                }
            }
            var selectedIds = _selectedCloudProfiles.map(p => p.id);
            setSelectedCloudProfileIds(selectedIds);
            return selectedCloudProfiles;
        });
    };

    const onSelectedItemClick = (e) => {
        console.log(generateLogMessageString(`onSelectedItemClick`, CLASS_NAME));
        const cloudLibIdToRemove = e.currentTarget.id;

        setSelectedCloudProfiles(selectedCloudProfiles => {
            var existingIndex = selectedCloudProfiles.findIndex(i => i.cloudLibraryId.toString() === cloudLibIdToRemove.toString());
            if (existingIndex >= 0) {
                selectedCloudProfiles.splice(existingIndex, 1);

                var selectedIds = _selectedCloudProfiles.map(p => p.id);
                setSelectedCloudProfileIds(selectedIds);
            }
            return selectedCloudProfiles;
        });
    };


    useEffect(() => {
        var selectedIds = _selectedCloudProfiles.map(p => p.id);
        setSelectedCloudProfileIds(selectedIds);
    }, [_selectedCloudProfiles]);

    //-------------------------------------------------------------------
    // Region: Add/Update event handlers
    //-------------------------------------------------------------------
    const onImport = (item) => {
        console.log(generateLogMessageString(`onImport`, CLASS_NAME));
        setProfileCloudLibImportModal({ show: true, item: item, import: true });
    };
    const onImportCancel = () => {
        console.log(generateLogMessageString(`onImportCancel`, CLASS_NAME));
        setProfileCloudLibImportModal({ show: false, item: null });
    };

    const onErrorModalClose = () => {
        //console.log(generateLogMessageString(`onErrorMessageOK`, CLASS_NAME));
        setError({ show: false, caption: null, message: null });
    }

    const onImportSelectedClick = () => {
        console.log(generateLogMessageString(`onImportSelectedClick`, CLASS_NAME));
        setImportConfirmModal({ show: true, items: _selectedCloudProfiles });
    };


    //on confirm click within the modal, this callback will then trigger the next step (ie call the API)
    const onImportConfirm = async () => {
        console.log(generateLogMessageString(`onDeleteConfirm`, CLASS_NAME));
        setImportConfirmModal({ show: false, item: null });
        await importItems(_importConfirmModal.items);
    };

    //render the delete modal when show flag is set to true
    //callbacks are tied to each button click to proceed or cancel
    const renderImportConfirmation = () => {

        if (!_importConfirmModal.show) return;
        if (_importConfirmModal.items.length == 0) {
            setImportConfirmModal({ show: false, item: null });
            return;
        }

        const message = _importConfirmModal.items.length === 1 ?
            `You are about to import profile '${_importConfirmModal.items[0].title}' and its dependent profiles. Are you sure?` :
            `You are about to import the following ${_importConfirmModal.items.length} profiles and their dependent profiles: '${_importConfirmModal.items.map(i => i.title).join("', '")}'. Are you sure?`;
        var caption = `Import Profile${_importConfirmModal.items.length === 1 ? "" : "s"}`;

        return (
            <>
                <ConfirmationModal showModal={_importConfirmModal.show} caption={caption} message={message}
                    icon={{ name: "warning", color: color.trinidad }}
                    confirm={{ caption: "Import", callback: onImportConfirm, buttonVariant: null }}
                    cancel={{
                        caption: "Cancel",
                        callback: () => {
                            console.log(generateLogMessageString(`onImportCancel`, CLASS_NAME));
                            setImportConfirmModal({ show: false, item: null });
                        },
                        buttonVariant: null
                    }} />
            </>
        );
    };

    const importItems = async (items) => {
        console.log(generateLogMessageString(`importItems||Count:${items.length}`, CLASS_NAME));

        setLoadingProps({
            isLoading: true, message: `Importing from Cloud Library...This may take a few minutes.`
        });

        //perform import call

        var url = `profile/cloudlibrary/import`;
        console.log(generateLogMessageString(`importFromCloudLibary||${url}`, CLASS_NAME));

        var data = //items.length === 1 ? { id: items[0].id.toString() } :
            items.map((item) => { return { id: item.cloudLibraryId.toString() }; });

        //var data = { id: props.item.cloudLibraryId };

        //show a processing message at top. One to stay for duration, one to show for timed period.
        //var msgImportProcessingId = new Date().getTime();
        setLoadingProps({
            isLoading: true, message: `Importing from Cloud Library...This may take a few minutes.`
        });

        await axiosInstance.post(url, data).then(result => {
            if (result.status === 200) {
                //check for success message OR check if some validation failed
                //remove processing message, show a result message
                //inline for isSuccess, pop-up for error
                var revisedMessages = null;
                if (result.data.isSuccess) {

                    //synch flow would wait, now we do async so we have to check import log on timer basis. 
                    //    revisedMessages = [{
                    //        id: new Date().getTime(),
                    //        severity: result.data.isSuccess ? "success" : "danger",
                    //        body: `Profiles were imported successfully.`,
                    //        isTimed: result.data.isSuccess
                    //    }];
                }
                else {
                    setError({ show: true, caption: 'Import Error', message: `An error occurred processing the import: ${result.data.message}` });
                }

                //asynch flow - trigger the component we use to show import messages, importing items changing is the trigger
                //update spinner, messages
                var importingLogs = loadingProps.importingLogs == null || loadingProps.importingLogs.length === 0 ? [] :
                    JSON.parse(JSON.stringify(loadingProps.importingLogs));
                importingLogs.push({ id: result.data.data, status: AppSettings.ImportLogStatus.InProgress, message: null });
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: revisedMessages,
                    importingLogs: importingLogs,
                    activateImportLog: true,
                    isImporting: false
                });

                setSelectedCloudProfiles([]);
                setSelectedCloudProfileIds([]);

                //bubble up to parent to let them know the import log id associated with this import. 
                //then they can track how this specific import is doing in terms of completed or not
                //if (props.onImportStarted) props.onImportStarted(result.data.data);

            } else {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, isImporting: false
                    //, inlineMessages: [{ id: new Date().getTime(), severity: "danger", body: `An error occurred processing the import file(s).`, isTimed: false, isImporting: false }]
                });
                setError({ show: true, caption: 'Import Error', message: `An error occurred processing the import file(s)` });
            }
        }).catch(e => {
            if (e.response && e.response.status === 401) {
                setLoadingProps({ isLoading: false, message: null, isImporting: false });
            }
            else {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, isImporting: false
                    //,inlineMessages: [{ id: new Date().getTime(), severity: "danger", body: e.response.data ? e.response.data : `An error occurred saving the imported profile.`, isTimed: false, isImporting: false }]
                });
                setError({ show: true, caption: 'Import Error', message: e.response && e.response.data ? e.response.data : `A system error has occurred during the profile import. Please contact your system administrator.` });
                console.log(generateLogMessageString('handleOnSave||saveFile||' + JSON.stringify(e), CLASS_NAME, 'error'));
                console.log(e);
            }
        })

    };

    //bubble up search criteria changed so the parent page can control the search criteria
    const onSearchCriteriaChanged = (criteria) => {
        console.log(generateLogMessageString(`onSearchCriteriaChanged`, CLASS_NAME));
        //update state
        setSearchCriteria(criteria);
        //trigger api to get data
        setSearchCriteriaChanged(_searchCriteriaChanged + 1);
    };

    const onView = (item) => {
        console.log(generateLogMessageString(`onEdit`, CLASS_NAME));
        setProfileEntityModal({ show: true, item: item });
    };
    const onViewClose = () => {
        console.log(generateLogMessageString(`onViewClose`, CLASS_NAME));
        setProfileEntityModal({ show: false, item: null });
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderHeaderRow = () => {
        return (
            <div className="row pb-3">
                <div className="col-sm-7 mr-auto d-flex">
                    {renderTitleBlock(caption, iconName, iconColor)}
                </div>
            </div>
        );
    };

    const renderIntroContent = () => {
        return (
            <div className="header-actions-row mb-3 pr-0">
                <p className="mb-2" >
                    Search the CESMII Cloud Library for Profiles and import Profiles into the Profile Library.
                </p>
                <p className="mb-2" >
                    Type definitions within these Profiles can then be viewed or extended to make new Type definitions, which can become part of one of your SM Profiles.
                </p>
            </div>
        );
    }

    //renderProfileEntity as a modal to force user to say ok.
    const renderProfileEntity = () => {

        if (!_profileEntityModal.show) return;

        return (
            <ProfileEntityModal item={_profileEntityModal.item} showModal={_profileEntityModal.show} onCancel={onViewClose} showSavedMessage={true} />
        );
    };
    //renderProfileCloudLibImport as a modal to force user to say ok.
    const renderProfileCloudLibImport = () => {
        if (!_profileCloudLibImportModal.show) return;

        return (
            <ProfileCloudLibImportModal item={_profileCloudLibImportModal.item} showModal={_profileCloudLibImportModal.show} onSave={onImport} onCancel={onImportCancel} showSavedMessage={true} />
        );
    };

    const renderSelectedItems = () => {
        const profiles = _selectedCloudProfiles.map((profile) => {
            return (
                <li id={`${profile.cloudLibraryId}`} key={`${profile.cloudLibraryId}`} className="m-1 d-inline-block"
                    onClick={onSelectedItemClick} data-parentid={profile.cloudLibraryId} data-id={profile.cloudLibraryId}>
                    <span className="selected py-1 px-2 d-flex">{profile.title}</span>
                </li>
            )
        });

        const buttonStyle = "auto-width mx-2" + (_selectedCloudProfiles.length > 0 ? "" : " disabled");

        return (
            <div className="d-block d-lg-inline mb-2 mb-lg-0" >
                <Button variant="secondary" type="button" className={buttonStyle} onClick={onImportSelectedClick} >Import selected...</Button>
                <ul className="m-0 p-0 d-inline" >
                    {profiles}
                </ul>
            </div>
        );
    }

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Helmet>
                <title>{AppSettings.Titles.Main + " | " + caption}</title>
            </Helmet>
            {renderHeaderRow()}
            {renderIntroContent()}
            <div className={`row selected-panel px-3 py-1 mb-1 rounded d-flex `} > {/*${props.cssClass ?? ''}*/}
                <div className="col-sm-12 px-0 align-items-start d-block d-lg-flex align-items-center" >
                    <div className="d-block d-lg-inline mb-2 mb-lg-0" >
                        {renderSelectedItems()}
                    </div>
                </div>
            </div>
            {(_searchCriteria != null) &&
                <ProfileListGrid searchCriteria={_searchCriteria} noSortOptions="true"
                    onGridRowSelect={onGridRowSelect} onEdit={onView} selectMode="multiple" selectedItems={_selectedCloudProfileIds}
                    onImport={onImport}
                    onSearchCriteriaChanged={onSearchCriteriaChanged} searchCriteriaChanged={_searchCriteriaChanged} />
            }
            //{renderProfileEntity()}
            //{renderProfileCloudLibImport()}
            {renderImportConfirmation()}
            <ErrorModal modalData={_error} callback={onErrorModalClose} />
        </>
    )
}

export default CloudLibList;