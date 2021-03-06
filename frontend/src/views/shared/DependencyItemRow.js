import React from 'react'

//import { generateLogMessageString } from '../../utils/UtilityService'
import { renderLinkedName } from './ProfileRenderHelpers';
import { getProfileIconName } from '../../utils/UtilityService';
import { SVGIcon } from '../../components/SVGIcon'
import color from '../../components/Constants'

//const CLASS_NAME = "DependencyItemRow";

function DependencyItemRow(props) { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderIcon = () => {
        if (props.item == null || props.item.type == null) return;

        var iconName = getProfileIconName(props.item);
        var iconColor = (props.currentUserId == null || props.currentUserId !== props.item.author.id) ? color.nevada : color.cornflower;

        return (<span className="mr-2" ><SVGIcon name={iconName} size="24" fill={iconColor} alt={iconName} /></span>)
    }

    //build the row
    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    //TBD - improve this check
    if (!props.isHeader && (props.item === null || props.item === {})) return null;
    if (!props.isHeader && props.item.name == null) return null;

    var cssClass = "row " + props.cssClass + (props.isHeader ? " bottom header" : " center");

    //header row - controlled by props flag
    if (props.isHeader) {
        return (
            <div className={cssClass}>
                <div className="col col-x-small left pl-3" >&nbsp;</div>
                <div className="col col-25 left" >Name</div>
                <div className="col left auto-size" >Description</div>
            </div>
        );
    }

    return (
        <>
            <div className={cssClass}>
                <div className="col col-x-small left pl-3" >{renderIcon()}</div>
                <div className="col col-25 left" >{renderLinkedName(props.item)}</div>
                <div className="col left auto-size" >{props.item.description}</div>
            </div>
        </>
    );
}

export default DependencyItemRow;