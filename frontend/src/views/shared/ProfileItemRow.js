import React from 'react'
import { Dropdown } from 'react-bootstrap'

import { useLoadingContext } from "../../components/contexts/LoadingContext";
import { SVGIcon, SVGDownloadIcon } from '../../components/SVGIcon'
import color from '../../components/Constants'
import { renderProfileIcon } from './ProfileRenderHelpers';
import { cleanFileName, formatDate, generateLogMessageString } from '../../utils/UtilityService';
import { getProfileCaption } from '../../services/ProfileService'

const CLASS_NAME = "ProfileItemRow";

function ProfileItemRow(props) { //props are item, showActions

    const { loadingProps, setLoadingProps } = useLoadingContext();

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

    const downloadItem = async () => {
        console.log(generateLogMessageString(`downloadItem||start`, CLASS_NAME));
        //add a row to download messages and this will kick off download
        var msgs = loadingProps.downloadItems || [];
        msgs.push({ profileId: props.item.id, fileName: cleanFileName(props.item.namespace), immediateDownload: true });
        setLoadingProps({ downloadItems: JSON.parse(JSON.stringify(msgs)) });
    }

    const onDeleteItem = () => {
        props.onDeleteCallback(props.item);
    }

    const onEditItem = () => {
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
        var x = props.selectedItems.findIndex(p => { return p.toString() === item.id.toString(); });
        return x >= 0;
    }

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderActionsColumn = (item, showActions) => {

        if (!showActions) return;

        //if standard ua nodeset, author is null
        return (
            <div className="col-sm-4 ml-auto d-inline-flex justify-content-end align-items-center" >
                <span className="my-0 mr-2"><a href={`/types/library/p=${props.item.id}`} ><span className="mr-1" alt="view"><SVGIcon name="visibility" size="24" fill={color.shark} /></span>View Type Definitions</a></span>
                <Dropdown className="action-menu icon-dropdown" onClick={(e) => e.stopPropagation()} >
                    <Dropdown.Toggle drop="left" title="Actions" >
                        <SVGIcon name="more-vert" size="24" fill={color.shark} />
                    </Dropdown.Toggle>
                    <Dropdown.Menu>
                        {/*{(props.currentUserId != null && props.currentUserId === item.authorId) &&*/}
                        {/*    <Dropdown.Item key="moreVert2" href={getTypeDefinitionNewUrl()} ><span className="mr-3" alt="extend"><SVGIcon name="extend" size="24" fill={color.shark} /></span>New Type Definition</Dropdown.Item>*/}
                        {/*}*/}
                        {(props.currentUserId != null && props.currentUserId === item.authorId) &&
                            <Dropdown.Item key="moreVert3" onClick={onDeleteItem} ><span className="mr-3" alt="delete"><SVGIcon name="delete" size="24" fill={color.shark} /></span>Delete Profile</Dropdown.Item>
                        }
                        <Dropdown.Item key="moreVert4" onClick={downloadItem} ><span className="mr-3" alt="arrow-drop-down"><SVGDownloadIcon name="download" size="24" fill={color.shark} /></span>Download Profile</Dropdown.Item>
                    </Dropdown.Menu>
                </Dropdown>
            </div>
        );
    }

    const renderSelectColumn = (item) => {

        var iconSelected = props.selectMode === "single" ? "task_alt" : "check_box";
        var iconUnselected = props.selectMode === "single" ? "radio_button_unchecked" : "check_box_outline_blank";

        return (
            <div className="mr-3 d-flex align-items-center" >
                {IsRowSelected(item) ? 
                    <i className="material-icons mr-1" title="Check to de-select" >{iconSelected}</i>
                    :
                    <i className="material-icons mr-1" title="Check to select" >{iconUnselected}</i>
                }
            </div>
        );
    }

    //render simplified Profile show on profile type entity or profile type list 
    const renderRowSimple = () => {

        var isSelected = props.item != null && IsRowSelected(props.item) ? "selected" : "";
        var cssClass = `row py-1 align-items-center ${props.cssClass == null ? '' : props.cssClass} ${isSelected} ${props.selectMode != null ? "selectable" : ""}`;
        var avatarCss = `col-avatar mt-1 mr-2 rounded-circle avatar-${props.currentUserId == null || props.item == null || props.currentUserId !== props.item.authorId ? "locked" : "unlocked"} elevated`;
        //var colCss = `${props.actionUI == null ? "col-sm-12" : "col-sm-10"} d-flex align-items-center`;
        var caption = props.item == null ? "" : getProfileCaption(props.item);
        var profileIcon = props.item == null ?
            renderProfileIcon({ authorId: null }, props.currentUserId, 24, false) :
            renderProfileIcon(props.item, props.currentUserId, 24, false);

        return (
            <div className={cssClass} onClick={onRowSelect} >
                <div className="col-sm-12 d-flex align-items-center" >
                    <div className={avatarCss} >{profileIcon}</div>
                    <div className="col-sm-11" >
                    <span className="font-weight-bold mr-2" >{props.profileCaption == null ? "Profile: " : `${props.profileCaption}: `}</span>
                    {caption}
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

        var isSelected = props.item != null && IsRowSelected(props.item) ? "selected" : "";
        var cssClass = `row py-1 align-items-center ${props.cssClass == null ? '' : props.cssClass} ${isSelected} ${props.selectMode != null ? "selectable" : ""}`;

        return (
            <div className={cssClass} onClick={onRowSelect} >
                <div className="col-sm-8 d-flex" >
                    {props.selectMode != null &&
                        renderSelectColumn(props.item)
                    }
                    <div className={`col-avatar mt-1 mr-2 rounded-circle avatar-${props.currentUserId == null || props.currentUserId !== props.item.authorId ? "locked" : "unlocked"} elevated`} >
                        {renderProfileIcon(props.item, props.currentUserId, 24, false)}
                    </div>
                    <div className="col-sm-11 d-flex align-items-center" >
                        <div className="d-block" >
                            <p className="my-0">
                            {props.profileCaption == null ? "Namespace: " : `${props.profileCaption}: `}
                            {!props.showActions || props.selectMode != null ?
                                <span className="ml-2" >{props.item.namespace}</span>
                                :
                                <button className="ml-1 btn btn-link" onClick={onEditItem} >{props.item.namespace}</button>
                            }
                        </p>
                        {props.item.version != null &&
                                <p className="my-0" >
                                <small>Version: {props.item.version}</small>
                            </p>
                        }
                        {props.item.publishDate != null &&
                                <p className="my-0" >
                                <small>Published: {formatDate(props.item.publishDate)}</small>
                            </p>
                        }
                        </div>
                    </div>
                </div>
                {renderActionsColumn(props.item, props.showActions && props.selectMode == null)}
            </div>
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