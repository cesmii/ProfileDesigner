import React from 'react'
import { Dropdown } from 'react-bootstrap'
import { AppSettings } from '../../utils/appsettings';

import { formatDateUtc, generateLogMessageString, renderMenuColorIcon } from '../../utils/UtilityService';
import { renderProfileAvatarBgCss, renderProfileIcon, renderProfilePublishStatus } from '../shared/ProfileRenderHelpers';

const CLASS_NAME = "AdminCloudLibApprovalRow";

function AdminCloudLibApprovalRow(props) { 

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onApprove = () => {
        console.log(generateLogMessageString('onApprove', CLASS_NAME));
        if (props.onApprove) props.onApprove(props.item);
    }

    const onReject = () => {
        console.log(generateLogMessageString('onReject', CLASS_NAME));
        if (props.onReject) props.onReject(props.item);
    }

    const onCancel = () => {
        console.log(generateLogMessageString('onCancel', CLASS_NAME));
        if (props.onCancel) props.onCancel(props.item);
    }

    const onSetPending = () => {
        console.log(generateLogMessageString('onSetPending', CLASS_NAME));
        if (props.onSetPending) props.onSetPending(props.item);
    }

    const onEditItem = (e) => {
        e.stopPropagation();
        //format date if present
        //props.item.publishDate = formatDate(props.item.publishDate);
        props.onEditCallback(props.item);
    }

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderActionsColumn = (className = 'col-sm-3') => {

        return (
            <div className={`${className} ml-auto d-inline-flex justify-content-end align-items-center`} >
                <Dropdown className="" onClick={(e) => e.stopPropagation()} >
                    <Dropdown.Toggle drop="left" title="Click to change" variant="tertiary" className="d-flex align-items-center" >
                        {renderProfilePublishStatus(props.item, '', '', 'mr-1')}
                    </Dropdown.Toggle>
                    <Dropdown.Menu>
                        {(props.item.profileState === AppSettings.ProfileStateEnum.CloudLibPending ||
                            props.item.profileState === AppSettings.ProfileStateEnum.Unknown) &&
                            <>
                            <Dropdown.Item key="moreVert1" onClick={onApprove} >{renderMenuColorIcon("check",null,"#6AA342")}Approve</Dropdown.Item>
                            <Dropdown.Item key="moreVert2" onClick={onReject} >{renderMenuColorIcon("close", null, "#D2222D")}Reject</Dropdown.Item>
                            </>
                        }
                        {(props.item.profileState === AppSettings.ProfileStateEnum.CloudLibRejected) &&
                            <>
                            <Dropdown.Item key="moreVert3" onClick={onSetPending} >{renderMenuColorIcon("cloud-upload",null,"#ffbf00")}Requeue</Dropdown.Item>
                            </>
                        }
                        <Dropdown.Item key="moreVert4" onClick={onCancel} >{renderMenuColorIcon("undo",null,"#D2222D")}Cancel Publish Request</Dropdown.Item>
                    </Dropdown.Menu>
                </Dropdown>
            </div>
        );
    }

    const renderTitleNamespace = () => {

        let profileCaption = null;
        let profileValue = null;
        if (props.item.title == null || props.item.title === '') {
            profileCaption = props.profileCaption == null ? "Namespace: " : `${props.profileCaption}: `;
            profileValue = props.item.namespace;
        }
        else {
            profileCaption = props.profileCaption == null ? "Title: " : `${props.profileCaption}: `;
            profileValue = props.item.title;
        }

        return (
            <>
                {profileCaption}
                {props.navigateModal &&
                    <button className="ml-1 mr-2 btn btn-link" onClick={onEditItem} >{profileValue}</button>
                }
                {(!props.navigateModal && props.item != null) &&
                    <a className="mx-2" href={`/cloudlibrary/viewer/${props.item.cloudLibraryId}`} >{profileValue}</a>
                }
            </>
        );
    }

    //-------------------------------------------------------------------
    // Region: Render row
    //-------------------------------------------------------------------
    const renderRow = () => {

        const className = `row py-2 mb-2 border-bottom align-items-center ${props.className == null ? '' : props.className}`;

        return (
            <div className={className}> 
                <div className="col-sm-1 align-self-start" >
                    <div className={`col-avatar mt-1 mr-2 rounded-circle ${renderProfileAvatarBgCss(props.item)} elevated`} >
                        {renderProfileIcon(props.item, 24)}
                    </div>
                </div>
                <div className="row col-sm-8" >
                    <div className="col-sm-12 align-items-center" >
                        <p className="my-0 d-flex align-items-center">
                            {renderTitleNamespace()}
                        </p>
                    </div>
                    <div className="col-sm-6 align-items-center" >
                        <div className="d-block" >
                            {(props.item.title != null && props.item.title !== '') &&
                                <p className="my-0 small-size" >Namespace: {props.item.namespace}</p>
                            }
                            {props.item.version != null &&
                                <p className="my-0 small-size" >Version: {props.item.version}</p>
                            }
                            {props.item.publishDate != null &&
                                <p className="my-0 small-size" >Published: {formatDateUtc(props.item.publishDate)}</p>
                            }
                            {props.item.categoryName != null &&
                                <p className="my-0 small-size" >Category: {props.item.categoryName}</p>
                            }
                        </div>
                    </div>
                    <div className="col-sm-6 align-items-center" >
                        <div className="d-block" >
                            {props.item.author != null &&
                                <p className="my-0 small-size" >Author: <a href={`/admin/user/${props.item.author.id}`}>{props.item.author.displayName}</a></p>
                            }
                            {props.item.contributorName != null &&
                                <p className="my-0 small-size" >Contributor: {props.item.contributorName}</p>
                            }
                            {props.item.license != null &&
                                <p className="my-0 small-size" >License: {props.item.license}</p>
                            }
                        </div>
                    </div>
                </div>
                {renderActionsColumn('col-sm-3')}
                {props.item.description != null &&
                    <div className="row col-sm-12" >
                        <div className="col-sm-11 offset-1" >
                            <p className="my-0 small-size" >Copyright: {props.item.copyrightText}</p>
                        </div>
                    </div>
                }
                {props.item.description != null &&
                    <div className="row col-sm-12" >
                        <div className="col-sm-11 offset-1" >
                            <p className="my-0 small-size" >Description: {props.item.description.length > 160 ? props.item.description.substr(0, 160) + '...' : props.item.description}</p>
                        </div>
                    </div>
                }
                {(props.item.profileState === AppSettings.ProfileStateEnum.CloudLibRejected &&
                    props.item.cloudLibApprovalDescription != null &&
                    props.item.cloudLibApprovalDescription !== '') &&
                    <div className="col-sm-12 d-flex" >
                    <p className="alert alert-danger my-2 small-size w-100" >Publish Rejection Reason: {props.item.cloudLibApprovalDescription}</p>
                    </div>
                }
                {(props.item.profileState === AppSettings.ProfileStateEnum.CloudLibApproved &&
                    props.item.cloudLibApprovalDescription != null &&
                    props.item.cloudLibApprovalDescription !== '') &&
                    <div className="col-sm-12 d-flex" >
                    <p className="alert alert-success my-2 small-size w-100" >Publish Approval Reason: {props.item.cloudLibApprovalDescription}</p>
                    </div>
                }
            </div >
        );
    };

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    if (props.item === null || props.item === {}) return null;

    return (
        renderRow()
    );

}

export default AdminCloudLibApprovalRow;