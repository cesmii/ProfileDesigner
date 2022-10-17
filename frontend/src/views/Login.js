import React from 'react'
import { Helmet } from "react-helmet"
import { useHistory } from 'react-router-dom';
import { useIsAuthenticated, useMsal } from "@azure/msal-react";

import { AppSettings } from '../utils/appsettings'
import WelcomeContent from './shared/WelcomeContent';

//const CLASS_NAME = "Login";

function Login() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();
    const { instance } = useMsal();
    const _isAuthenticated = useIsAuthenticated();
    const _activeAccount = instance.getActiveAccount();

    //check for logged in status - redirect to home page if already logged in.
    if (_isAuthenticated && _activeAccount != null) {
        history.push('/');
    }

    //-------------------------------------------------------------------
    // Region: hooks
    //  trigger from some other component to kick off an import log refresh and start tracking import status
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Helmet>
                <title>{`${AppSettings.Titles.Main} | Login`}</title>
            </Helmet>
            <WelcomeContent showLogin={true} />
        </>
    )
}

export default Login;