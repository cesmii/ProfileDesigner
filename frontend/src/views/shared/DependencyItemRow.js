import React from 'react'

//import { generateLogMessageString } from '../../utils/UtilityService'
import { renderLinkedName, renderTypeIcon } from './ProfileRenderHelpers';
import { getProfileCaption } from '../../services/ProfileService';

//const CLASS_NAME = "DependencyItemRow";

function DependencyItemRow(props) { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------

    //build the row
    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    //TBD - improve this check
    if (!props.isHeader && (props.item === null || props.item === {})) return null;
    if (!props.isHeader && props.item.name == null) return null;

    const cssClass = "row py-1 align-items-center " + props.cssClass + (props.isHeader ? " bottom header" : " center");
    const avatarCss = `col-avatar mt-1 mr-2 rounded-circle avatar info elevated`;

    //header row - controlled by props flag
    if (props.isHeader) {
        return (
            <div className={cssClass}>
                <div className={`col-avatar font-weight-bold`} >Name</div>
                <div className="col-sm-3 left font-weight-bold" ></div>
                <div className="col-sm-4 left font-weight-bold" >Profile</div>
                <div className="col-sm-4 left font-weight-bold" >Description</div>
            </div>
        );
    }

    return (
        <>
            <div className={cssClass}>
                <div className={`${avatarCss}`} >{renderTypeIcon(props.item, props.activeAccount, 20, false)}</div>
                <div className="col-sm-3 left" >{renderLinkedName(props.item)}</div>
                <div className="col-sm-4 left" >{getProfileCaption(props.item.profile)}</div>
                <div className="col-sm-4 left" >{props.item.description}</div>
            </div>
        </>
    );
}

export default DependencyItemRow;