import React from 'react'

//import { generateLogMessageString } from '../../utils/UtilityService'
import { SVGIcon } from '../../components/SVGIcon'
import color from '../../components/Constants'
import { getTypeDefIconName } from '../../utils/UtilityService';
import { getTypeDefEntityLink } from '../../services/ProfileService';
import { AppSettings } from '../../utils/appsettings';

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
export const renderProfileIcon = (item, size = 24, useMarginRight = true) => {
    if (item == null) return;

    let iconName = "dashboard";
    let iconColor = null;

    switch (item.profileState) {
        case AppSettings.ProfileStateEnum.CloudLibPending:
            iconColor = color.amber;
            break;
        case AppSettings.ProfileStateEnum.CloudLibRejected:
            iconColor = color.cardinal;
            break;
        case AppSettings.ProfileStateEnum.Local:
            iconColor = color.cornflower;
            break;
        case AppSettings.ProfileStateEnum.CloudLibPublished:
        case AppSettings.ProfileStateEnum.Core:
        default:
            iconColor = color.nevada;
    }

    const svg = (<SVGIcon name={iconName} size={size} fill={iconColor} alt={iconName} size={size} />);
    return (<span className={`d-flex align-items-center justify-content-center ${useMarginRight ? "mr-2" : ""}`} >{svg}</span>)
};

export const renderProfileAvatarBgCss = (item) => {
    if (item == null) return 'avatar info';

    switch (item.profileState) {
        ///*
        case AppSettings.ProfileStateEnum.CloudLibPending:
            return 'avatar warning';
        case AppSettings.ProfileStateEnum.CloudLibRejected:
            return 'avatar error';
        //*/
        case AppSettings.ProfileStateEnum.Local:
        case AppSettings.ProfileStateEnum.CloudLibPublished:
        case AppSettings.ProfileStateEnum.Core:
        default:
            return 'avatar info';
    }
};

export const renderProfilePublishStatus = (item, caption = 'Status', className = 'mr-2') => {
    if (item == null) return null;

    //only for certain statuses
    if (item.profileState !== AppSettings.ProfileStateEnum.CloudLibPending &&
        item.profileState !== AppSettings.ProfileStateEnum.CloudLibRejected) return;

    const statusName = item.profileState === AppSettings.ProfileStateEnum.CloudLibPending ? "Pending" : "Rejected";
    const iconColor = item.profileState === AppSettings.ProfileStateEnum.CloudLibPending ? color.amber : color.cardinal;
    return (
        <span className={`my-0 d-flex align-items-center ${className}`} >
            <span className="font-weight-bold mr-2">{caption}:</span>
            <span className="mr-1" alt="upload"><SVGIcon name="cloud-upload" size={24} fill={iconColor} /></span>
            {statusName}
        </span>
    );
}

//-------------------------------------------------------------------
// Region: Common Is profile or type definition author/owner for this item
//-------------------------------------------------------------------
export const isOwner = (item, account) => {
    if (item == null) return false;
    if (item.author == null) return false;
    if (account == null) return false;
    if (account.idTokenClaims == null) return false;

    //check if oid is match in item.author.oid
    const oid = account.idTokenClaims?.oid;
    return oid != null && oid === item.author.objectIdAAD;
}
