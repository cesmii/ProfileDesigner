import React, { useState } from 'react'
import { Dropdown } from 'react-bootstrap'

import axiosInstance from '../../services/AxiosService';
import { useLoadingContext } from "../../components/contexts/LoadingContext";
import { AppSettings } from '../../utils/appsettings';
import ConfirmationModal from '../../components/ConfirmationModal';
import { ErrorModal } from '../../services/CommonUtil';
import { generateLogMessageString, renderMenuIcon, scrollTop } from '../../utils/UtilityService';
import { SVGIcon } from '../../components/SVGIcon'
import color from '../../components/Constants';
import { renderProfilePublishStatus } from './ProfileRenderHelpers';

const CLASS_NAME = "ProfileActions";

//-------------------------------------------------------------------
// Region: Shared UI with profileEntity and profileItemRow - actions triggers
//-------------------------------------------------------------------
function ProfileCloudLibStatus(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { setLoadingProps } = useLoadingContext();
    const [_publishProfileModal, setPublishProfileModal] = useState({ show: false, item: null, message: null });
    const [_withdrawProfileModal, setWithdrawProfileModal] = useState({ show: false, item: null, message: null });
    const [_error, setError] = useState({ show: false, message: null, caption: null });

    //-------------------------------------------------------------------
    // Region: API call to perform publication to CloudLib
    //-------------------------------------------------------------------
    const publishToCloudLib = (item) => {
        console.log(generateLogMessageString(`publishToCloudLib||${item.namespace}`, CLASS_NAME));

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform publish call
        const data = !props.saveAndPublish ? { id: item.id } : item;
        const url = !props.saveAndPublish ? `profile/cloudlibrary/publish` : `profile/cloudlibrary/saveandpublish`;
        axiosInstance.post(url, data)
            .then(result => {
                if (result.data.isSuccess) {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            {
                                id: new Date().getTime(), severity: "success",
                                body: `Profile was submitted for publication. The CESMII team will review your submission shortly.`,
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
                if (props.onPublishProfileCallback != null) props.onPublishProfileCallback(result.data.isSuccess);

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
                if (props.onPublishProfileCallback != null) props.onPublishProfileCallback(false);
            });
    };

    const withdrawProfile = (item) => {
        console.log(generateLogMessageString(`withdrawProfile||${item.namespace}`, CLASS_NAME));

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform publish call
        const data = { id: item.id };
        const url = `cloudlibrary/publishcancel`;
        axiosInstance.post(url, data)
            .then(result => {
                if (result.data.isSuccess) {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            {
                                id: new Date().getTime(), severity: "success",
                                body: `Profile submission was withdrawn.`,
                                isTimed: true
                            }
                        ],
                    });
                }
                else {
                    setError({ show: true, caption: 'Cancel Error', message: `An error occurred withdrawing this submission: ${result.data.message}` });
                    //update spinner, messages
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: null
                    });
                }
                //raise callback
                if (props.onWithdrawProfileCallback != null) props.onWithdrawProfileCallback(result.data.isSuccess);

            })
            .catch(error => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: `An error occurred withdrawing this submission.`, isTimed: false }
                    ]
                });
                console.log(generateLogMessageString('cancelPublishToCloudLib||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
                console.log(error);
                //scroll back to top
                scrollTop();
                //raise callback
                if (props.onWithdrawProfileCallback != null) props.onWithdrawProfileCallback(false);
            });
    };

    //-------------------------------------------------------------------
    // Region: Event handlers
    //-------------------------------------------------------------------
    const onPublish = async () => {
        console.log(generateLogMessageString(`onPublishToCloudLib||start`, CLASS_NAME));

        let msg = null;
        //check required fields
        let requiredFields = [];
        if (props.item.title == null || props.item.title == '') requiredFields.push('Title');
        if (props.item.namespace == null || props.item.namespace == '') requiredFields.push('Namespace');
        if (props.item.publishDate == null || props.item.publishDate == '') requiredFields.push('Publish Date');
        if (props.item.description == null || props.item.description == '') requiredFields.push('Description');
        if (props.item.contributorName == null || props.item.contributorName == '') requiredFields.push('Contributor');
        if (props.item.copyrightText == null || props.item.copyrightText == '') requiredFields.push('Copyright');
        if (props.item.license == null || props.item.license == '') requiredFields.push('License');
        if (props.item.categoryName == null || props.item.categoryName == '') requiredFields.push('Category');
        if (requiredFields.length > 0) {
            msg = `${requiredFields.join(', ')} ${requiredFields.length === 1 ? 'is' : 'are'} required before publishing this profile.`
        }

        //check valid license selection
        if (props.item.license !== "MIT" && props.item.license !== "BSD-3-Clause") {
            msg = msg == null ? '' : msg + ' ';
            msg += "Profiles can only be published to the CESMII Cloud Library and Marketplace under the MIT or BSD-3-Clause license.";
        }

        //if message is not null, then show error and return
        if (msg != null) {
            setError({ show: true, caption: 'Publish Error - Invalid data', message: msg });
            return;
        }
        //all good, continue with publish
        setPublishProfileModal({ show: true, item: props.item, message: null });
    }

    const onPublishConfirm = async () => {
        console.log(generateLogMessageString(`onPublishConfirm||start`, CLASS_NAME));
        publishToCloudLib(_publishProfileModal.item);
        setPublishProfileModal({ show: false, item: null });
    }

    const onWithdrawProfile = async () => {
        console.log(generateLogMessageString(`onWithdrawProfile||start`, CLASS_NAME));
        setWithdrawProfileModal({ show: true, item: props.item, message: null });
    }

    const onWithdrawProfileConfirm = async () => {
        console.log(generateLogMessageString(`onWithdrawProfileConfirm||start`, CLASS_NAME));
        withdrawProfile(_withdrawProfileModal.item);
        setWithdrawProfileModal({ show: false, item: null });
    }

    const onErrorModalClose = () => {
        //console.log(generateLogMessageString(`onErrorMessageOK`, CLASS_NAME));
        setError({ show: false, caption: null, message: null });
    }

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderPublishConfirmation = () => {

        if (!_publishProfileModal.show) return;

        // TODO Detect and warn about unsaved changes in the form

        let caption = `Publish Profile`;

        let message =
            `You are about to submit your profile '${_publishProfileModal.item.namespace}' for publication. ` +
            `Once your profile is submitted, your profile will no longer be editable. ` +
            `After approval, the profile will appear in the CESMII Cloud Library and Marketplace. ` +
            `The CESMII team will review your submission. To check on the status of your submission, visit the Profile Library.` ;

        const agreementMessage =
            `I have the right to distribute this profile under the '${_publishProfileModal.item.license}' license, ` +
            `on behalf of the organization '${_publishProfileModal.item.contributorName}' and using the ` +
            `namespace '${_publishProfileModal.item.namespace}'.`;

        return (
            <>
                <ConfirmationModal showModal={_publishProfileModal.show} caption={caption} message={message}
                    icon={{ name: "cloud-upload", color: color.cornflower }}
                    confirm={{ caption: "Publish", callback: onPublishConfirm, buttonVariant: "primary" }}
                    requireAgreementText={agreementMessage}
                    cancel={{
                        caption: "Cancel",
                        callback: () => {
                            console.log(generateLogMessageString(`onPublishCancel`, CLASS_NAME));
                            setPublishProfileModal({ show: false, item: null });
                        },
                        buttonVariant: null
                    }} />
            </>
        );
    };

    const renderCancelPublishConfirmation = () => {

        if (!_withdrawProfileModal.show) return;

        let caption = `Cancel Publish Request`;

        let message =
            `You are about to cancel the request to publish profile '${_withdrawProfileModal.item.title}', [${_withdrawProfileModal.item.namespace}], ` +
            `which will remove the pending submission from the Cloud Library. ` +
            `The profile will become editable again and allow you to resubmit the profile at a later time.`;

        return (
            <>
                <ConfirmationModal showModal={_withdrawProfileModal.show} caption={caption} message={message}
                    icon={{ name: "undo", color: color.amber }}
                    confirm={{ caption: "Yes", callback: onWithdrawProfileConfirm, buttonVariant: "secondary" }}
                    cancel={{
                        caption: "Cancel",
                        callback: () => {
                            console.log(generateLogMessageString(`onWithdrawPublishCancel`, CLASS_NAME));
                            setWithdrawProfileModal({ show: false, item: null });
                        },
                        buttonVariant: null
                    }} />
            </>
        );
    };


    //-------------------------------------------------------------------
    // Region: Render Helpers - Column to show publish profile link OR publish profile status
    //-------------------------------------------------------------------
    /*
    const renderButton = () => {
        if (props.item.profileState === AppSettings.ProfileStateEnum.Local) {
            return (
                <button onClick={onPublish} className="btn btn-primary med-width d-inline-flex align-content-center mr-2" >
                    <span className="mr-1" alt="upload"><SVGIcon name="cloud-upload" size={18} fill={color.white} /></span>
                    <span className="" >Publish</span>
                </button>
            );
        }

        if (props.item.profileState === AppSettings.ProfileStateEnum.CloudLibPending ||
            props.item.profileState === AppSettings.ProfileStateEnum.CloudLibRejected) {
            return (
                <button onClick={onWithdrawProfile} className="btn btn-secondary med-width px-0 mr-2" >
                    Cancel Publish
                </button>
            );
        }

        return null;
    };
    */

    const renderButton = () => {
        if (props.item.profileState === AppSettings.ProfileStateEnum.Local) {
            return (
                <button onClick={onPublish} className="btn btn-primary med-width d-inline-flex align-content-center mr-2" >
                    <span className="mr-1" alt="upload"><SVGIcon name="cloud-upload" size={18} fill={color.white} /></span>
                    <span className="" >Publish</span>
                </button>
            );
        }

        if (props.item.profileState === AppSettings.ProfileStateEnum.CloudLibPending ||
            props.item.profileState === AppSettings.ProfileStateEnum.CloudLibRejected) {
            return (
                <div className={`d-inline-flex align-items-center`} >
                    <Dropdown className="" onClick={(e) => e.stopPropagation()} >
                        <Dropdown.Toggle drop="left" title="Click to change" variant="tertiary" className="d-flex align-items-center mr-2" >
                            {renderProfilePublishStatus(props.item, '', '', 'mr-1')}
                        </Dropdown.Toggle>
                        <Dropdown.Menu>
                            <Dropdown.Item key="moreVert1" onClick={onWithdrawProfile} >{renderMenuIcon("undo")}Cancel Publish Request</Dropdown.Item>
                        </Dropdown.Menu>
                    </Dropdown>
                </div>
            );
        }

        return null;
    };

    //-------------------------------------------------------------------
    // Region: Final render
    //-------------------------------------------------------------------
    //we only show the ui if the user owns this profile and it is not yet published. This is determined
    //on back end and we use profileState
    if (!props.item ||
        props.item.profileState === AppSettings.ProfileStateEnum.CloudLibPublished ||
        props.item.profileState === AppSettings.ProfileStateEnum.Core ||
        props.item.profileState === AppSettings.ProfileStateEnum.Unknown) return null;

    return (
        <>
            {props.showStatus &&
                renderProfilePublishStatus(props.item, 'Publish Status', '', 'mr-2')
            }
            {props.showButton &&
                <>
                    {renderButton()}
                    {renderPublishConfirmation()}
                    {renderCancelPublishConfirmation()}
                    <ErrorModal modalData={_error} callback={onErrorModalClose} />
                </>
            }
        </>
    );


}

export default ProfileCloudLibStatus;