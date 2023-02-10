import React, { useState } from 'react'
import { Dropdown } from 'react-bootstrap'

import axiosInstance from '../../services/AxiosService';
import { useLoadingContext } from "../../components/contexts/LoadingContext";
import { AppSettings } from '../../utils/appsettings';
import ConfirmationModal from '../../components/ConfirmationModal';
import { ErrorModal } from '../../services/CommonUtil';
import { isOwner } from './ProfileRenderHelpers';
import { generateLogMessageString, scrollTop } from '../../utils/UtilityService';
import { SVGIcon, SVGDownloadIcon } from '../../components/SVGIcon'
import color from '../../components/Constants';

const CLASS_NAME = "ProfileActions";

//-------------------------------------------------------------------
// Region: Shared UI with profileEntity and profileItemRow - actions triggers
//-------------------------------------------------------------------
function ProfileCloudLibStatus(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { setLoadingProps } = useLoadingContext();
    const [_publishToCloudLibModal, setPublishToCloudLibModal] = useState({ show: false, item: null, message: null });
    const [_cancelPublishToCloudLibModal, cancelPublishToCloudLibModal] = useState({ show: false, item: null, message: null });
    const [_error, setError] = useState({ show: false, message: null, caption: null });

    //-------------------------------------------------------------------
    // Region: Event handlers
    //-------------------------------------------------------------------
    const onPublishToCloudLib = async () => {
        console.log(generateLogMessageString(`onPublishToCloudLib||start`, CLASS_NAME));
        if (props.item.title == null || props.item.description == null || props.item.contributorName == null || props.item.copyrightText == null || props.item.categoryName == null ) {
            setPublishToCloudLibModal({ show: true, item: null, message: "Please fill in Title, Description, Contributor, Copyright, License and Category before publishing the profile." });
        }
        else if (props.item.license === "MIT" || props.item.license === "GPL-2.0") {
            setPublishToCloudLibModal({ show: true, item: props.item, message: null });
        }
        else {
            setPublishToCloudLibModal({ show: true, item: null, message: "Profiles can only be published to the CESMII Cloud Library and Marketplace under the MIT or GPL-2.0 license." });
        }
    }
    const onPublishToCloudLibConfirm = async () => {
        console.log(generateLogMessageString(`onPublishToCloudLibConfirm||start`, CLASS_NAME));
        publishToCloudLib(_publishToCloudLibModal.item);
        setPublishToCloudLibModal({ show: false, item: null });
    }

    const onCancelPublishToCloudLib = async () => {
        console.log(generateLogMessageString(`onCancelPublishToCloudLib||start`, CLASS_NAME));
        cancelPublishToCloudLibModal({ show: true, item: props.item, message: null });
    }

    const onCancelPublishToCloudLibConfirm = async () => {
        console.log(generateLogMessageString(`onCancelPublishToCloudLibConfirm||start`, CLASS_NAME));
        cancelPublishToCloudLib(_cancelPublishToCloudLibModal.item);
        cancelPublishToCloudLibModal({ show: false, item: null });
    }

    const onErrorModalClose = () => {
        //console.log(generateLogMessageString(`onErrorMessageOK`, CLASS_NAME));
        setError({ show: false, caption: null, message: null });
    }


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

    const cancelPublishToCloudLib = (item) => {
        console.log(generateLogMessageString(`cancelPublishToCloudLib||${item.namespace}`, CLASS_NAME));

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform publish call
        const data = { id: item.id };
        const url = `profile/cloudlibrary/publishcancel`;
        axiosInstance.post(url, data)
            .then(result => {
                if (result.data.isSuccess) {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            {
                                id: new Date().getTime(), severity: "success",
                                body: `Profile submission was canceled`,
                                isTimed: true
                            }
                        ],
                    });
                }
                else {
                    setError({ show: true, caption: 'Cancel Error', message: `An error occurred canceling the publish request for this profile: ${result.data.message}` });
                    //update spinner, messages
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: null
                    });
                }
                //raise callback
                if (props.onCancelPublishToCloudLibCallback != null) props.onCancelPublishToCloudLibCallback(result.data.isSuccess);

            })
            .catch(error => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: `An error occurred canceling the publish request for this profile.`, isTimed: false }
                    ]
                });
                console.log(generateLogMessageString('cancelPublishToCloudLib||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
                console.log(error);
                //scroll back to top
                scrollTop();
                //raise callback
                if (props.onCancelPublishToCloudLibCallback != null) props.onCancelPublishToCloudLibCallback(false);
            });
    };


    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------

    const renderCloudIconName = () => {
        if (props.item.cloudLibApprovalStatus === AppSettings.PublishProfileStatus.Approved) {
            return "publish-approved";
        }
        if (props.item.cloudLibApprovalStatus === AppSettings.PublishProfileStatus.Rejected) {
            return "publish-rejected";
        }
        if (props.item.cloudLibApprovalStatus === AppSettings.PublishProfileStatus.Pending || props.item.cloudLibPendingApproval) {
            return "publish-pending";
        }
        if (props.item.cloudLibApprovalStatus == null && props.item.cloudLibraryId != null) {
            return "publish-imported";
        }
        if (props.item.cloudLibApprovalStatus == null && props.item.cloudLibraryId == null) {
            return "publish-none";
        }
    }



    const renderPublishConfirmation = () => {

        if (!_publishToCloudLibModal.show) return;

        // TODO Detect and warn about unsaved changes in the form

        let caption = `Publish Profile`;

        if (_publishToCloudLibModal.item == null) {

            return (
                <>
                    <ConfirmationModal showModal={_publishToCloudLibModal.show} caption={caption} message={_publishToCloudLibModal.message}
                        icon={{ name: "warning", color: color.trinidad }}
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

        }

        let message =
            `You are about to submit your profile '${_publishToCloudLibModal.item.namespace}' for publication. After approval the profile will appear in the CESMII Cloud Library and Marketplace.`;

        const agreementMessage = `I have the right to distribute this profile under the '${_publishToCloudLibModal.item.license}' license, on behalf of the organization '${_publishToCloudLibModal.item.contributorName}' and using the namespace '${_publishToCloudLibModal.item.namespace}'.`;

        return (
            <>
                <ConfirmationModal showModal={_publishToCloudLibModal.show} caption={caption} message={message}
                    icon={{ name: "warning", color: color.trinidad }}
                    confirm={{ caption: "Publish", callback: onPublishToCloudLibConfirm, buttonVariant: "danger" }}
                    requireAgreementText= {agreementMessage}
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

    const renderCancelPublishConfirmation = () => {

        if (!_cancelPublishToCloudLibModal.show) return;

        let caption = `Cancel Publish Request`;

        let message =
            `You are about to cancel the request to publish your profile '${_cancelPublishToCloudLibModal.item.namespace}'. This will make the profile editable again and let you resubmit it.`;

        return (
            <>
                <ConfirmationModal showModal={_cancelPublishToCloudLibModal.show} caption={caption} message={message}
                    icon={{ name: "warning", color: color.trinidad }}
                    confirm={{ caption: "Cancel Publish Request", callback: onCancelPublishToCloudLibConfirm, buttonVariant: "danger" }}
                    cancel={{
                        caption: "Cancel",
                        callback: () => {
                            console.log(generateLogMessageString(`onCancelPublishCancel`, CLASS_NAME));
                            cancelPublishToCloudLibModal({ show: false, item: null });
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
                <Dropdown className="cloudlib-action-menu icon-dropdown" onClick={(e) => e.stopPropagation()} >
                    <Dropdown.Toggle drop="left" title="Cloud Library Actions" >
                        <span className="my-0 mr-2"><SVGIcon size="50" name={renderCloudIconName()} /></span>
                        {props.item.cloudLibApprovalStatus === "REJECTED" &&
                            <span>{props.item.cloudLibApprovalDescription }</span>
                        }
                    </Dropdown.Toggle>
                    {isOwner(props.item, props.activeAccount) && (props.item.cloudLibraryId == null || (props.item.cloudLibraryId != null && props.item.cloudLibPendingApproval)) &&
                        <Dropdown.Menu>
                            {isOwner(props.item, props.activeAccount) && props.item.cloudLibraryId == null && !props.item.cloudLibPendingApproval &&
                                <Dropdown.Item key="moreVert7" onClick={onPublishToCloudLib} ><span className="mr-3" alt="arrow-drop-down"><SVGDownloadIcon name="publishToCloudLib" /></span>Publish to Cloud Library</Dropdown.Item>
                            }
                            {isOwner(props.item, props.activeAccount) && props.item.cloudLibraryId != null && props.item.cloudLibPendingApproval &&
                                <Dropdown.Item key="moreVert7" onClick={onCancelPublishToCloudLib} ><span className="mr-3" alt="arrow-drop-down"><SVGDownloadIcon name="cancelPublishToCloudLib" /></span>Cancel publication request</Dropdown.Item>
                            }
                        </Dropdown.Menu>
                    }
                </Dropdown>
                {renderPublishConfirmation()}
                {renderCancelPublishConfirmation()}
                <ErrorModal modalData={_error} callback={onErrorModalClose} />
            </>
        );
    }
    else {
        return (
            <>
                <ErrorModal modalData={_error} callback={onErrorModalClose} />
            </>
        );
    }

}

export default ProfileCloudLibStatus;