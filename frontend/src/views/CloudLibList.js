import React, { useState, useEffect } from 'react'
import { Helmet } from "react-helmet"

import { AppSettings } from '../utils/appsettings'
import { renderTitleBlock } from '../utils/UtilityService'
import { useLoadingContext } from "../components/contexts/LoadingContext";

import CloudLibraryImporter from "./shared/CloudLibraryImporter";

import color from '../components/Constants'
import './styles/ProfileList.scss';
import '../components/styles/InfoPanel.scss';

const CLASS_NAME = "CloudLibList";

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

    const renderIntroContent = () => {
        return (
            <div className="header-actions-row mb-3 pr-0">
                <p className="mb-2" >
                    Search the CESMII Cloud Library for Profiles and import Profiles into the Profile Library.
                </p>
                <p className="mb-2" >
                    Type definitions within these Profiles can then be viewed or extended to make new Type definitions, which can become part of one of your SM Profiles.
                </p>
            </div>
        );
    }


    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Helmet>
                <title>{AppSettings.Titles.Main + " | " + caption}</title>
            </Helmet>
            {renderHeaderRow()}
            {renderIntroContent()}
            <CloudLibraryImporter/>
        </>
    )
}

export default CloudLibList;