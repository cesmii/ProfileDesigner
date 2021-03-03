import React from 'react'

//import { generateLogMessageString } from '../../utils/UtilityService'
import { SVGIcon } from '../../components/SVGIcon'
import color from '../../components/Constants'
import { getProfileIconName } from '../../utils/UtilityService';
import { getProfileEntityLink } from '../../services/ProfileService';

//const CLASS_NAME = "ProfileRenderHelpers";

//-------------------------------------------------------------------
// Region: Common Profile Render helpers
//-------------------------------------------------------------------
export const renderIcon = (item, currentUserId, size = 24, useMarginRight = true) => {
    if (item == null || item.type == null) return;

    var iconName = getProfileIconName(item);
    var iconColor = (currentUserId == null || currentUserId !== item.author.id) ? color.nevada : color.cornflower;

    var svg = (<SVGIcon name={iconName} size={size} fill={iconColor} alt={iconName} />);

    return (<span className={useMarginRight ? "d-flex align-items-center justify-content-center mr-2" : "d-flex align-items-center justify-content-center "} >{svg}</span>)
};

export const renderLinkedName = (item, cssClass = null ) => {
    if (item == null || item.type == null) return;
    var href = getProfileEntityLink(item);
    return (
        <a key={item.id} href={href} className={cssClass == null || cssClass === '' ? '' : cssClass} >{item.name}</a>
    );
};
