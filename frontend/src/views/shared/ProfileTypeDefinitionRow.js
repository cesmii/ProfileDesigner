import React from 'react'

import { SVGIcon } from '../../components/SVGIcon'
import { isOwner, renderTypeIcon } from './ProfileRenderHelpers';
import { getProfileCaption } from '../../services/ProfileService'
import TypeDefinitionActions from './TypeDefinitionActions';
 
//const CLASS_NAME = "ProfileTypeDefinitionRow";

function ProfileTypeDefinitionRow(props) { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onRowSelect = () => {
        //only some modes allow selecting row
        if (props.selectMode == null) return;

        //toggle selection and bubble up to parent to update the state
        props.item.selected = !props.item.selected;
        if (props.onRowSelect) props.onRowSelect(props.item);
    }

    const IsRowSelected = (item) => {
        if (props.selectedItems == null) return;
        const x = props.selectedItems.findIndex(p => { return p.toString() === item.id.toString(); });
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
                <a href={`/type/extend/${props.item.id}/`} ><span alt="extend"><SVGIcon name="extend" /></span>Extend</a>

                <TypeDefinitionActions item={props.item} activeAccount={props.activeAccount} onDeleteCallback={props.onDeleteCallback} showExtend={false} className='ml-2' isReadOnly={props.item.isReadOnly} />
            </>
        );
    }

    const renderSelectIcon = (item) => {

        const iconSelected = props.selectMode === "single" ? "task_alt" : "check_box";
        const iconUnselected = props.selectMode === "single" ? "radio_button_unchecked" : "check_box_outline_blank";

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
            <div className="col-select mr-3 d-flex" >
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
        const isReadOnly = (props.item.isReadOnly || !isOwner(props.item, props.activeAccount));
        const cssClass = `row py-1 align-items-center ${props.cssClass} ${isReadOnly ? "" : "mine"} ${IsRowSelected(props.item) ? "selected" : ""} ${props.selectMode != null ? "selectable" : ""}`;
        const avatarCss = `col-avatar mt-1 mr-2 rounded-circle avatar info elevated clickable`;

        return (
            <div className={cssClass} onClick={onRowSelect}>
                <div className="col-sm-10 d-flex" >
                    {props.selectMode != null &&
                        renderSelectColumn(props.item)
                    }
                    <div className={avatarCss} >{renderTypeIcon(props.item, props.activeAccount, 20)}</div>
                    <div className="col-sm-8" >
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
                    <div className="col-sm-4 d-none d-lg-inline-flex flex-wrap metatags-col align-content-center" >
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
        const isReadOnly = (props.item.isReadOnly || !isOwner(props.item, props.activeAccount));
        const cssClass = `col-lg-4 col-md-6 ${IsRowSelected(props.item) ? "selected" : ""} ${props.selectMode != null ? "selectable" : ""}`;
        const avatarCss = `col-avatar d-inline-flex mr-2 rounded-circle avatar ${isReadOnly ? "locked" : "unlocked"} elevated clickable`;

        return (
            <div className={cssClass} onClick={onRowSelect}>
                <div className={`pb-4 h-100`} >
                    <div className={`${props.cssClass} h-100 p-3 ${isReadOnly ? "" : "mine"} ${IsRowSelected(props.item) ? "selected" : ""} ${props.selectMode != null ? "selectable" : ""}`} >
                        {props.selectMode != null &&
                            renderSelectFloat(props.item)
                        }
                        <p className="mb-1 d-flex align-items-center" >
                            <span className={avatarCss} >{renderTypeIcon(props.item, props.activeAccount, 20)}</span>
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