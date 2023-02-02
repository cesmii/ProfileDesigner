import React, { useState } from 'react'
import { Dropdown } from 'react-bootstrap'

import axiosInstance from '../../services/AxiosService';
import { useLoadingContext } from "../../components/contexts/LoadingContext";
import { AppSettings } from '../../utils/appsettings';
import ConfirmationModal from '../../components/ConfirmationModal';
import { ErrorModal } from '../../services/CommonUtil';
import { isOwner } from './ProfileRenderHelpers';
import { cleanFileName, generateLogMessageString, scrollTop } from '../../utils/UtilityService';
import { SVGIcon, SVGDownloadIcon } from '../../components/SVGIcon'
import color from '../../components/Constants';

const CLASS_NAME = "ProfileActions";

//-------------------------------------------------------------------
// Region: Shared UI with profileEntity and profileItemRow - actions triggers
//-------------------------------------------------------------------
function ProfileActions(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_deleteModal, setDeleteModal] = useState({ show: false, items: null });
    const [_publishToCloudLibModal, setPublishToCloudLibModal] = useState({ show: false, item: null });
    const [_error, setError] = useState({ show: false, message: null, caption: null });

    //-------------------------------------------------------------------
    // Region: Event handlers
    //-------------------------------------------------------------------
    const downloadItem = async () => {
        console.log(generateLogMessageString(`downloadItem||start`, CLASS_NAME));
        //add a row to download messages and this will kick off download
        var msgs = loadingProps.downloadItems || [];
        msgs.push({ profileId: props.item.id, fileName: cleanFileName(props.item.namespace), immediateDownload: true });
        setLoadingProps({ downloadItems: JSON.parse(JSON.stringify(msgs)) });
    }
    const downloadItemAsAASX = async () => {
        console.log(generateLogMessageString(`downloadItemAsAASX||start`, CLASS_NAME));
        //add a row to download messages and this will kick off download
        var msgs = loadingProps.downloadItems || [];
        msgs.push({ profileId: props.item.id, fileName: cleanFileName(props.item.namespace), immediateDownload: true, downloadFormat: AppSettings.ExportFormatEnum.AASX });
        setLoadingProps({ downloadItems: JSON.parse(JSON.stringify(msgs)) });
    }
    const downloadItemAsSmipJson = async () => {
        console.log(generateLogMessageString(`downloadItemAsSmipJson||start`, CLASS_NAME));
        //add a row to download messages and this will kick off download
        var msgs = loadingProps.downloadItems || [];
        msgs.push({ profileId: props.item.id, fileName: cleanFileName(props.item.namespace), immediateDownload: true, downloadFormat: AppSettings.ExportFormatEnum.SmipJson });
        setLoadingProps({ downloadItems: JSON.parse(JSON.stringify(msgs)) });
    }
    const onPublishToCloudLib = async () => {
        console.log(generateLogMessageString(`onPublishToCloudLib||start`, CLASS_NAME));
        setPublishToCloudLibModal({ show: true, item: props.item });
    }
    const onPublishToCloudLibConfirm = async () => {
        console.log(generateLogMessageString(`onPublishToCloudLibConfirm||start`, CLASS_NAME));
        publishToCloudLib(_publishToCloudLibModal.item);
        setPublishToCloudLibModal({ show: false, item: null });
    }

    const onImportItem = () => {
        //format date if present
        //props.item.publishDate = formatDate(props.item.publishDate);
        props.onImportCallback(props.item);
    }

    const onDeleteItem = () => {
        console.log(generateLogMessageString(`onDeleteItem`, CLASS_NAME));
        setDeleteModal({ show: true, items: [props.item] });
    }

    //on confirm click within the modal, this callback will then trigger the next step (ie call the API)
    const onDeleteConfirm = () => {
        console.log(generateLogMessageString(`onDeleteConfirm`, CLASS_NAME));
        deleteItems(_deleteModal.items);
        setDeleteModal({ show: false, item: null });
    };

    const onErrorModalClose = () => {
        //console.log(generateLogMessageString(`onErrorMessageOK`, CLASS_NAME));
        setError({ show: false, caption: null, message: null });
    }


    //-------------------------------------------------------------------
    // Region: API call to perform delete
    //-------------------------------------------------------------------
    const deleteItems = (items) => {
        console.log(generateLogMessageString(`deleteItems||Count:${items.length}`, CLASS_NAME));

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform delete call
        const data = items.length === 1 ? { id: items[0].id } :
            items.map((item) => { return { id: item.id }; });
        const url = items.length === 1 ? `profile/delete` : `profile/deletemany`;
        axiosInstance.post(url, data)  //api allows one or many
            .then(result => {

                if (result.data.isSuccess) {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            {
                                id: new Date().getTime(), severity: "success",
                                body: items.length === 1 ? `Profile was deleted` : `${items.length} Profiles were deleted`, isTimed: true
                            }
                        ],
                        //get profile count from server...this will trigger that call on the side menu
                        refreshProfileCount: true,
                        refreshProfileList: true,
                        refreshSearchCriteria: true //refresh this list to make sure list of profiles is accurate in the filters
                    });
                }
                else {
                    setError({ show: true, caption: 'Delete Error', message: `An error occurred deleting ${items.length === 1 ? "this profile" : "these profiles"} : ${result.data.message}` });
                    //update spinner, messages
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: null
                    });
                }
                //raise callback
                if (props.onDeleteCallback != null) props.onDeleteCallback(result.data.isSuccess);

            })
            .catch(error => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: `An error occurred deleting ${items.length === 1 ? "this profile" : "these profiles"}.`, isTimed: false }
                    ]
                });
                console.log(generateLogMessageString('deleteProfiles||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
                console.log(error);
                //scroll back to top
                scrollTop();
                //raise callback
                if (props.onDeleteCallback != null) props.onDeleteCallback(false);
            });
    };

    //-------------------------------------------------------------------
    // Region: API call to perform publication to CloudLib
    //-------------------------------------------------------------------
    const publishToCloudLib = (item) => {
        console.log(generateLogMessageString(`publishToCloudLib||${item.namespace}`, CLASS_NAME));

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform publish call
        const data = { id: item.id };
        const url = `profile/cloudlibrary/publish`;
        axiosInstance.post(url, data)
            .then(result => {
                if (result.data.isSuccess) {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            {
                                id: new Date().getTime(), severity: "success",
                                body: `Profile was submitted for publication`,
                                isTimed: true
                            }
                        ],
                    });
                }
                else {
                    setError({ show: true, caption: 'Publish Error', message: `An error occurred publishing this profile: ${result.data.message}` });
                    //update spinner, messages
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: null
                    });
                }
                //raise callback
                if (props.onPublishToCloudLibCallback != null) props.onPublishToCloudLibCallback(result.data.isSuccess);

            })
            .catch(error => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: `An error occurred publishing this profile.`, isTimed: false }
                    ]
                });
                console.log(generateLogMessageString('publishProfile||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
                console.log(error);
                //scroll back to top
                scrollTop();
                //raise callback
                if (props.onPublishToCloudLibCallback != null) props.onPublishToCloudLibCallback(false);
            });
    };

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    //render the delete modal when show flag is set to true
    //callbacks are tied to each button click to proceed or cancel
    const renderDeleteConfirmation = () => {

        if (!_deleteModal.show) return;

        let message = _deleteModal.items.length === 1 ?
            `You are about to delete your profile '${_deleteModal.items[0].namespace}'. This will delete all type definitions associated with this profile. This action cannot be undone. Are you sure?` :
            `You are about to delete ${_deleteModal.items.length} profiles. This will delete all type definitions associated with these profiles. This action cannot be undone. Are you sure?`;
        let caption = `Delete Profile${_deleteModal.items.length === 1 ? "" : "s"}`;

        return (
            <>
                <ConfirmationModal showModal={_deleteModal.show} caption={caption} message={message}
                    icon={{ name: "warning", color: color.trinidad }}
                    confirm={{ caption: "Delete", callback: onDeleteConfirm, buttonVariant: "danger" }}
                    cancel={{
                        caption: "Cancel",
                        callback: () => {
                            console.log(generateLogMessageString(`onDeleteCancel`, CLASS_NAME));
                            setDeleteModal({ show: false, item: null });
                        },
                        buttonVariant: null
                    }} />
            </>
        );
    };

    const renderPublishConfirmation = () => {

        if (!_publishToCloudLibModal.show) return;

        // TODO Detect and warn about unsaved changes in the form

        let message =
            `You are about to submit your profile '${_publishToCloudLibModal.item.namespace}' for publication. After approval the profile will appear in the CESMII Cloud Library and Marketplase. Are you sure?`;
        let caption = `Publish Profile`;

        return (
            <>
                <ConfirmationModal showModal={_publishToCloudLibModal.show} caption={caption} message={message}
                    icon={{ name: "warning", color: color.trinidad }}
                    confirm={{ caption: "Publish", callback: onPublishToCloudLibConfirm, buttonVariant: "danger" }}
                    cancel={{
                        caption: "Cancel",
                        callback: () => {
                            console.log(generateLogMessageString(`onPublishCancel`, CLASS_NAME));
                            setPublishToCloudLibModal({ show: false, item: null });
                        },
                        buttonVariant: null
                    }} />
            </>
        );
    };

    //-------------------------------------------------------------------
    // Region: Final render
    //-------------------------------------------------------------------
    if (!props.item) return null;

    if (props.item.hasLocalProfile == null || props.item.hasLocalProfile) {
        //if standard ua nodeset, author is null
        return (
            <>
                <Dropdown className="action-menu icon-dropdown" onClick={(e) => e.stopPropagation()} >
                    <Dropdown.Toggle drop="left" title="Actions" >
                        <SVGIcon name="more-vert" />
                    </Dropdown.Toggle>
                    <Dropdown.Menu>
                        {/*{(props.currentUserId != null && props.currentUserId === item.authorId) &&*/}
                        {/*    <Dropdown.Item key="moreVert2" href={getTypeDefinitionNewUrl()} ><span className="mr-3" alt="extend"><SVGIcon name="extend" /></span>New Type Definition</Dropdown.Item>*/}
                        {/*}*/}
                        {isOwner(props.item, props.activeAccount) &&
                            <Dropdown.Item key="moreVert3" onClick={onDeleteItem} ><span className="mr-3" alt="delete"><SVGIcon name="delete" /></span>Delete Profile</Dropdown.Item>
                        }
                        <Dropdown.Item key="moreVert4" onClick={downloadItem} ><span className="mr-3" alt="arrow-drop-down"><SVGDownloadIcon name="download" /></span>Download Profile</Dropdown.Item>
                        <Dropdown.Item key="moreVert5" onClick={downloadItemAsAASX} ><span className="mr-3" alt="arrow-drop-down"><SVGDownloadIcon name="downloadAASX" /></span>Download Profile as AASX</Dropdown.Item>
                        <Dropdown.Item key="moreVert6" onClick={downloadItemAsSmipJson} ><span className="mr-3" alt="arrow-drop-down"><SVGDownloadIcon name="downloadSmipJson" /></span>Download Profile for SMIP import (experimental)</Dropdown.Item>
                        {isOwner(props.item, props.activeAccount) && props.item.standardProfileID == null &&
                            <Dropdown.Item key="moreVert7" onClick={onPublishToCloudLib} ><span className="mr-3" alt="arrow-drop-down"><SVGDownloadIcon name="publishToCloudLib" /></span>Publish to Cloud Library</Dropdown.Item>
                        }
                    </Dropdown.Menu>
                </Dropdown>
                {renderDeleteConfirmation()}
                {renderPublishConfirmation()}
                <ErrorModal modalData={_error} callback={onErrorModalClose} />
            </>
        );
    }
    else {
        return (
            <>
                <div className="col-sm-4 ml-auto d-inline-flex justify-content-end align-items-center" >
                    <button className="ml-1 btn btn-link" onClick={onImportItem} ><span className="mr-1" alt="view"><SVGDownloadIcon name="import" /></span>Import from Cloud Library</button>
                    {/*    <Dropdown className="action-menu icon-dropdown" onClick={(e) => e.stopPropagation()} >*/}
                    {/*        <Dropdown.Toggle drop="left" title="Actions" >*/}
                    {/*            <SVGIcon name="more-vert" />*/}
                    {/*        </Dropdown.Toggle>*/}
                    {/*        <Dropdown.Menu>*/}
                    {/*            <Dropdown.Item key="moreVert4" onClick={importItem} ><span className="mr-3" alt="arrow-drop-down"><SVGIcon name="visibility" /></span>View profile description</Dropdown.Item>*/}
                    {/*        </Dropdown.Menu>*/}
                    {/*    </Dropdown>*/}
                </div>
                {renderDeleteConfirmation()}
                <ErrorModal modalData={_error} callback={onErrorModalClose} />
            </>
        );
    }

}

export default ProfileActions;