import React from 'react'
import { Helmet } from "react-helmet"

import { AppSettings } from '../utils/appsettings'
import { renderTitleBlock } from '../utils/UtilityService'

import CloudLibraryListGrid from "./shared/CloudLibraryListGrid";

import color from '../components/Constants'
import './styles/ProfileList.scss';
import '../components/styles/InfoPanel.scss';

//const CLASS_NAME = "CloudLibList";

function CloudLibList() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const caption = 'Import from Cloud Library';
    const iconName = 'folder-shared';
    const iconColor = color.shark;

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderHeaderRow = () => {
        return (
            <div className="row pb-3">
                <div className="col-sm-7 mr-auto d-flex">
                    {renderTitleBlock(caption, iconName, iconColor)}
                </div>
            </div>
        );
    };

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Helmet>
                <title>{AppSettings.Titles.Main + " | " + caption}</title>
            </Helmet>
            {renderHeaderRow()}
            <CloudLibraryListGrid/>
        </>
    )
}

export default CloudLibList;