import React from 'react'

import { renderProfileAvatarBgCss, renderProfileIcon, renderProfilePublishStatus } from './ProfileRenderHelpers';
import { formatDateUtc } from '../../utils/UtilityService';
import { getProfileCaption } from '../../services/ProfileService'
import { SVGIcon } from '../../components/SVGIcon';
import ProfileActions from './ProfileActions';
import ProfileCloudLibStatus from './ProfileCloudLibStatus';
import { AppSettings } from '../../utils/appsettings';

//const CLASS_NAME = "ProfileItemRow";

function ProfileItemRow(props) { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    //const getTypeDefinitionsUrl = () => {
    //    //var val = encodeURIComponent(props.item.namespace);
    //    return (props.item.isReadOnly || props.item.authorId == null || props.currentUserId !== props.item.authorId) ?
    //        `/types/library/p=${props.item.id}` : `/types/mine/p=${props.item.id}`;
    //};

    //const getTypeDefinitionNewUrl = () => {
    //    return `/type/new/p=${props.item.id}`; 
    //};


    const onEditItem = (e) => {
        e.stopPropagation();
        //format date if present
        //props.item.publishDate = formatDate(props.item.publishDate);
        props.onEditCallback(props.item);
    }

    const onRowSelect = () => {
        //only some modes allow selecting row
        if (props.selectMode == null) return;

        //toggle selection and bubble up to parent to update the state
        props.item.selected = !props.item.selected;
        if (props.onRowSelect) props.onRowSelect(props.item);
    }

    const IsRowSelected = (item) => {
        if (props.selectedItems == null) return;
        const x = item.hasLocalProfile || props.selectedItems.findIndex(p => { return p.toString() === item.id.toString(); }); // TODO make the local profile selection configurable
        return x >= 0;
    }

    const onRowChanged = (e) => {
        if (props.onRowChanged) props.onRowChanged(e);
    }

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------

    const renderActionsColumn = (showActions) => {

        if (!showActions) return;

        return (
            <>
            <div className="col-sm-3 ml-auto d-inline-flex justify-content-end align-items-center" >
                    <ProfileCloudLibStatus item={props.item} activeAccount={props.activeAccount} showButton={true} showStatus={false}
                        onPublishProfileCallback={onRowChanged} onWithdrawProfileCallback={onRowChanged} />
            </div>
            <div className="col-sm-3 ml-auto d-inline-flex justify-content-end align-items-center" >
                <span className="my-0 mr-2"><a href={`/profile/${props.item.id}?tab=typedefs`} ><span className="mr-1" alt="view"><SVGIcon name="visibility" /></span>View Type Definitions</a></span>
                <ProfileActions item={props.item} activeAccount={props.activeAccount} />
            </div>
            </>
        );
    }

    const renderSelectColumn = (item, isReadOnly) => {

        const iconSelected = props.selectMode === "single" ? "task_alt" : "check_box";
        const iconUnselected = props.selectMode === "single" ? "radio_button_unchecked" : "check_box_outline_blank";

        return (
            <div className="col-select mr-3 d-flex" >
                {IsRowSelected(item) ?
                    <i className={`material-icons mr-1 ${isReadOnly ? "disabled" : ""} `}
                        title={isReadOnly ? "" : "Check to de-select"} >{iconSelected}</i>
                    :
                    <i className="material-icons mr-1" title="Check to select" >{iconUnselected}</i>
                }
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

        if (!props.showActions) {
            return (
                <>
                    {profileCaption}
                    <span className="ml-2" >{profileValue}</span>
                    {renderProfilePublishStatus(props.item, 'Publish Status', 'ml-auto mr-2')}
                </>
            );
        }
        else {
            return (
                <>
                    {profileCaption}
                    {props.navigateModal ?
                        <button className="ml-1 mr-2 btn btn-link" onClick={onEditItem} >{profileValue}</button>
                        :
                        <a className="mx-2" href={`/profile/${props.item.id}`} >{profileValue}</a>
                    }
                    {renderProfilePublishStatus(props.item, 'Publish Status', 'ml-auto mr-2')}
                </>
            );
        }
    }

    //render simplified Profile show on profile type entity or profile type list
    const renderRowSimple = () => {

        const isSelected = props.item != null && IsRowSelected(props.item) ? "selected" : "";
        const cssClass = `row py-1 align-items-center ${props.cssClass == null ? '' : props.cssClass} ${isSelected} ${props.selectMode != null ? "selectable" : ""}`;
        const avatarCss = `col-avatar mr-2 rounded-circle ${renderProfileAvatarBgCss(props.item)} elevated`;
        //var colCss = `${props.actionUI == null ? "col-sm-12" : "col-sm-10"} d-flex align-items-center`;
        const caption = props.item == null ? "" : getProfileCaption(props.item);
        const profileIcon = props.item == null ?
            renderProfileIcon({ authorId: null }, 24) :
            renderProfileIcon(props.item, 24);

        return (
            <div className={cssClass} onClick={onRowSelect} >
                <div className="col-sm-12 d-flex align-items-center" >
                    <div className={avatarCss} >{profileIcon}</div>
                    <div className="col-sm-11" >
                        <span className="font-weight-bold mr-2" >{props.profileCaption == null ? "Profile: " : `${props.profileCaption}: `}</span>
                        <a className="mx-2" href={`/profile/${props.item.id}`} >{caption}</a>
                        {(props.actionUI != null) &&
                            <div className="ml-2 d-inline-flex" >
                                {props.actionUI}
                            </div>
                        }
                    </div>
                </div>
            </div>
        );
    };

    //render typical row that is shown in a list/grid
    const renderRow = () => {

        const isSelected = props.item != null && IsRowSelected(props.item) ? "selected" : "";
        const isReadonly = props.item?.hasLocalProfile;
        const cssClass = `row py-1 align-items-center ${props.cssClass == null ? '' : props.cssClass} ${isSelected} ${props.selectMode != null ? "selectable" : ""} ${isReadonly ? "select-readonly" : ""}`;

        return (
            <div className={cssClass} onClick={props.item.hasLocalProfile ? null : onRowSelect}> {/*TODO Make the local profile selection configurable */}
                <div className="col-sm-6 d-flex" >
                    {props.selectMode != null &&
                        renderSelectColumn(props.item, isReadonly)
                    }
                    <div className={`col-avatar mt-1 mr-2 rounded-circle ${renderProfileAvatarBgCss(props.item)} elevated`} >
                        {renderProfileIcon(props.item, 24)}
                    </div>
                    <div className="col-sm-11 d-flex align-items-center" >
                        <div className="d-block" >
                            <p className="my-0 d-flex align-items-center">
                                {renderTitleNamespace()}
                            </p>
                            {(props.item.title != null && props.item.title != '') &&
                                <p className="my-0 small-size" >Namespace: {props.item.namespace}</p>
                            }
                            {props.item.version != null &&
                                <p className="my-0 small-size" >Version: {props.item.version}</p>
                            }
                            {props.item.publishDate != null &&
                                <p className="my-0 small-size" >Published: {formatDateUtc(props.item.publishDate)}</p>
                            }
                        </div>
                    </div>
                </div>
                {renderActionsColumn(props.showActions && props.selectMode == null)}
                {props.item.description != null &&
                    <div className="col-sm-12 d-flex" >
                        {props.selectMode != null &&
                            <div className="col-spacer mr-1" >
                            </div>
                        }
                        <div className="col-spacer mr-2" >
                        </div>
                        <div className="col-sm-11" >
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
            </div >
        );
    };

    //build the row
    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    //two different modes supported
    if (props.mode == null || props.mode === "normal") {
        if (props.item === null || props.item === {}) return null;
        if (props.item.namespace == null) return null;

        return (
            <>
                {renderRow()}
            </>
        );
    }
    else if (props.mode === "simple") {
        return (
            <>
                {renderRowSimple()}
            </>
        );
    }
    return null;
}

export default ProfileItemRow;