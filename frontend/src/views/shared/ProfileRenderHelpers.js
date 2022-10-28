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
export const renderTypeIcon = (item, account, size = 20, useMarginRight = true) => {
    if (item == null || item.type == null) return;

    const isOwnerBool = isOwner(item, account);
    const iconName = getTypeDefIconName(item);
    const iconColor = (item.isReadOnly || !isOwnerBool) ? color.shark : color.cornflower;

    const svg = (<SVGIcon name={iconName} size={size} fill={iconColor} alt={iconName} />);

    return (<span className={useMarginRight ? "d-flex align-items-center justify-content-center mr-2" : "d-flex align-items-center justify-content-center "} >{svg}</span>)
};

export const renderLinkedName = (item, cssClass = null ) => {
    if (item == null || item.type == null) return;
    const href = getTypeDefEntityLink(item);
    return (
        <a key={item.id} href={href} className={cssClass == null || cssClass === '' ? '' : cssClass} >{item.name}</a>
    );
};

//-------------------------------------------------------------------
// Region: Common Nodeset Render helpers
//-------------------------------------------------------------------
export const renderProfileIcon = (item, account, size = 20, useMarginRight = true) => {
    if (item == null) return;

    const isOwnerBool = isOwner(item, account);
    const iconName = (!isOwnerBool) ? 'folder-profile' : 'folder-shared';
    // TODO sort this out properly when isOwner is working etc.
    var iconColor = color.amber;
    if (item.hasLocalProfile != false) {
        if (item.isReadOnly) {
            iconColor = color.coolGray;
        }
        else {
            iconColor = color.apple;
        }
    }
    else if (item.cloudLibraryId != null) {
        iconColor = color.blazeOrange;
    }
    //(item.isReadOnly || !isOwnerBool) ?
    //    (item.cloudLibraryId != null ? color.blazeOrange : color.amber)
    //    : color.apple;
    const svg = (<SVGIcon name={iconName} size={size} fill={iconColor} alt={iconName} />);
    return (<span className={useMarginRight ? "d-flex align-items-center justify-content-center mr-2" : "d-flex align-items-center justify-content-center "} >{svg}</span>)
};


//-------------------------------------------------------------------
// Region: Common Is profile or type definition author/owner for this item
//-------------------------------------------------------------------
export const isOwner = (item, account) => {
    return true;
    if (item == null) return false;
    if (item.author == null) return false;
    if (account == null) return false;
    if (account.idTokenClaims == null) return false;

    const oid = account.idTokenClaims?.oid;
    if (oid == null) return false;

    //check if oid is match in item.author.oid
    return true;
    return oid === item.author.objectIdAAD;
}
