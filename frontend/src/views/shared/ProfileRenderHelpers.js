import React from 'react'

//import { generateLogMessageString } from '../../utils/UtilityService'
import { SVGIcon } from '../../components/SVGIcon'
import color from '../../components/Constants'
import { getTypeDefIconName } from '../../utils/UtilityService';
import { getTypeDefEntityLink } from '../../services/ProfileService';

//const CLASS_NAME = "ProfileRenderHelpers";

//-------------------------------------------------------------------
// Region: Common Profile Render helpers
//-------------------------------------------------------------------
export const renderTypeIcon = (item, currentUserId, size = 20, useMarginRight = true) => {
    if (item == null || item.type == null) return;

    var iconName = getTypeDefIconName(item);
    var iconColor = (item.isReadOnly || item.authorId == null || currentUserId !== item.author.id) ? color.shark : color.cornflower;

    var svg = (<SVGIcon name={iconName} size={size} fill={iconColor} alt={iconName} />);

    return (<span className={useMarginRight ? "d-flex align-items-center justify-content-center mr-2" : "d-flex align-items-center justify-content-center "} >{svg}</span>)
};

export const renderLinkedName = (item, cssClass = null ) => {
    if (item == null || item.type == null) return;
    var href = getTypeDefEntityLink(item);
    return (
        <a key={item.id} href={href} className={cssClass == null || cssClass === '' ? '' : cssClass} >{item.name}</a>
    );
};

//-------------------------------------------------------------------
// Region: Common Nodeset Render helpers
//-------------------------------------------------------------------
export const renderProfileIcon = (item, currentUserId, size = 20, useMarginRight = true) => {
    if (item == null) return;

    var iconName = (currentUserId == null || item.authorId == null || currentUserId !== item.authorId) ? 'folder-profile' : 'folder-shared';
    var iconColor = (item.isReadOnly || item.authorId == null || currentUserId !== item.authorId) ? color.nevada : color.cornflower;
    var svg = (<SVGIcon name={iconName} size={size} fill={iconColor} alt={iconName} />);
    return (<span className={useMarginRight ? "d-flex align-items-center justify-content-center mr-2" : "d-flex align-items-center justify-content-center "} >{svg}</span>)
};

