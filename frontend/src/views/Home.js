import React from 'react'
import { Helmet } from "react-helmet"

import { AppSettings } from '../utils/appsettings'
import ProfileImporter from './shared/ProfileImporter'
import HeaderNavHome from '../components/HeaderNavHome'

function Home() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const caption = 'Home';

    //-------------------------------------------------------------------
    // Region: Event Handling
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Helmet>
                <title>{AppSettings.Titles.Main + " | " + caption}</title>
            </Helmet>
            <HeaderNavHome caption="Import profiles" showSearch={false} />
            <div id="--cesmii-main-content">
                <div id="--cesmii-left-content">
                    <ProfileImporter />
                </div>
            </div>
        </>
    )
}

export default Home;