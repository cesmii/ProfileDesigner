import React from 'react'
import { Dropdown } from 'react-bootstrap'

import { useLoadingContext } from '../../components/contexts/LoadingContext'
import { SVGIcon, SVGDownloadIcon } from '../../components/SVGIcon'
import color from '../../components/Constants'
import { renderTypeIcon } from './ProfileRenderHelpers';
import { cleanFileName, generateLogMessageString } from '../../utils/UtilityService';
import { getProfileCaption } from '../../services/ProfileService'
 
const CLASS_NAME = "ProfileTypeDefinitionRow";

function ProfileTypeDefinitionRow(props) { //props are item, showActions

    const { loadingProps, setLoadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const downloadProfile = async () => {
        console.log(generateLogMessageString(`downloadProfile||start`, CLASS_NAME));
        //add a row to download messages and this will kick off download
        var msgs = loadingProps.downloadItems || [];
        msgs.push({ profileId: props.item.profile?.id, fileName: cleanFileName(props.item.profile?.namespace), immediateDownload: true });
        setLoadingProps({ downloadItems: JSON.parse(JSON.stringify(msgs)) });
    }

    const onDeleteItem = () => {
        console.log(generateLogMessageString(`onDeleteItem`, CLASS_NAME));
        props.onDeleteCallback(props.item);
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
    const renderMetaTagItem = (item) => {
        return (
            item.metaTags.map((tag) => {
                return (
                    <span key={tag} className="metatag badge meta">
                        {tag}
                    </span>
                )
            })
        )
    }

    const renderActionsColumn = (item, showActions) => {

        if (!showActions) return;

        return (
            <>
                <a href={`/type/extend/${props.item.id}/`} ><span alt="extend"><SVGIcon name="extend" size="24" fill={color.shark} /></span>Extend</a>

                <Dropdown className="action-menu icon-dropdown ml-2" onClick={(e) => e.stopPropagation()} >
                    <Dropdown.Toggle drop="left" title="Actions" >
                        <SVGIcon name="more-vert" size="24" fill={color.shark} />
                    </Dropdown.Toggle>
                    <Dropdown.Menu>
                        {(!item.isReadOnly && props.currentUserId != null && props.currentUserId === item.authorId) &&
                            <Dropdown.Item key="moreVert3" onClick={onDeleteItem} ><span className="mr-3" alt="delete"><SVGIcon name="delete" size="24" fill={color.shark} /></span>Delete Type Definition</Dropdown.Item>
                        }
                        <Dropdown.Item key="moreVert5" onClick={downloadProfile} ><span className="mr-3" alt="arrow-drop-down"><SVGDownloadIcon name="downloadNodeset" size="24" fill={color.shark} /></span>Download Profile '{getProfileCaption(props.item.profile)}'</Dropdown.Item>
                    </Dropdown.Menu>
                </Dropdown>
            </>
        );
    }

    const renderSelectIcon = (item) => {

        var iconSelected = props.selectMode === "single" ? "task_alt" : "check_box";
        var iconUnselected = props.selectMode === "single" ? "radio_button_unchecked" : "check_box_outline_blank";

        return (
            <>
            { IsRowSelected(item) ?
                <i className="material-icons mr-1" title="Check to de-select" >{iconSelected}</i>
                :
                <i className="material-icons mr-1" title="Check to select" >{iconUnselected}</i>
                }
            </>
        );
    }

    const renderSelectColumn = (item) => {

        return (
            <div className="mr-3 d-flex align-items-center" >
                {renderSelectIcon(item)}
            </div>
        );
    }

    const renderSelectFloat = (item) => {

        return (
            <span className="float-right" >
                {renderSelectIcon(item)}
            </span>
        );
    }

    const renderRowView = () => {
        var isReadOnly = (props.currentUserId == null || props.currentUserId !== props.item.authorId || props.item.isReadOnly);
        var cssClass = `row py-1 align-items-center ${props.cssClass} ${isReadOnly ? "" : "mine"} ${IsRowSelected(props.item) ? "selected" : ""} ${props.selectMode != null ? "selectable" : ""}`;
        var avatarCss = `col-avatar mt-1 mr-2 rounded-circle avatar-${isReadOnly ? "locked" : "unlocked"} elevated clickable`;

        return (
            <div className={cssClass} onClick={onRowSelect}>
                <div className="col-sm-10 d-flex" >
                    {props.selectMode != null &&
                        renderSelectColumn(props.item)
                    }
                    <div className={avatarCss} >{renderTypeIcon(props.item, props.currentUserId, 24, false)}</div>
                    <div className="col-sm-6" >
                        <p className="mb-1" >
                            {props.selectMode != null ?
                                props.item.name
                                :
                                <a href={`/type/${props.item.id}`} >{props.item.name}</a>
                            }
                        </p>
                        {props.item.profile != null &&
                            <p className="mb-1 small-size" >{getProfileCaption(props.item.profile)}
                            </p>
                        }
                        {props.item.description != null &&
                            <div className="small-size" >{props.item.description}
                            </div>
                        }
                    </div>
                    <div className="col-sm-6 d-none d-lg-inline-flex flex-wrap metatags-col align-content-center" >
                        {renderMetaTagItem(props.item)}
                    </div>
                </div>
                {props.selectMode == null &&
                    <div className="col-sm-2 ml-auto d-inline-flex justify-content-end align-items-center" >
                        {renderActionsColumn(props.item, props.showActions)}
                    </div>
                }
            </div>
        );
    }

    const renderTileView = () => {
        var isReadOnly = (props.currentUserId == null || props.currentUserId !== props.item.authorId || props.item.isReadOnly);
        var cssClass = `col-lg-4 col-md-6 ${IsRowSelected(props.item) ? "selected" : ""} ${props.selectMode != null ? "selectable" : ""}`;
        var avatarCss = `col-avatar d-inline-flex mr-2 rounded-circle avatar-${isReadOnly ? "locked" : "unlocked"} elevated clickable`;

        return (
            <div className={cssClass} onClick={onRowSelect}>
                <div className={`pb-4 h-100`} >
                    <div className={`${props.cssClass} h-100 p-3 ${isReadOnly ? "" : "mine"} ${IsRowSelected(props.item) ? "selected" : ""} ${props.selectMode != null ? "selectable" : ""}`} >
                        {props.selectMode != null &&
                            renderSelectFloat(props.item)
                        }
                        <p className="mb-1 d-flex align-items-center" >
                            <span className={avatarCss} >{renderTypeIcon(props.item, props.currentUserId, 24, false)}</span>
                            {props.selectMode != null ?
                                props.item.name
                                :
                                <a href={`/type/${props.item.id}`} >{props.item.name}</a>
                            }
                        </p>
                        {props.item.profile != null &&
                            <p className="mb-1 ml-3 small-size" >
                                {getProfileCaption(props.item.profile)}
                            </p>
                        }
                        {props.item.description != null &&
                            <div className="ml-3 mb-1 small-size" >{props.item.description}
                            </div>
                        }
                        {(props.item.metaTags != null && props.item.metaTags.length > 0) &&
                            <div className="ml-3 my-1 d-none d-lg-block flex-wrap metatags-col align-content-center" >
                                {renderMetaTagItem(props.item)}
                            </div>
                        }
                        {props.selectMode == null &&
                            <div className="ml-3 d-inline-flex align-items-center" >
                                {renderActionsColumn(props.item, props.showActions)}
                            </div>
                        }
                    </div>
                </div>
            </div>
        );
    }

    //build the row
    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    if (props.item === null || props.item === {}) return null;

    if (props.displayMode === "tile") {
        return renderTileView();
    }
    else {
        return renderRowView();
    }
}

export default ProfileTypeDefinitionRow;