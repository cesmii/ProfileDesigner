import React from 'react'
import { Helmet } from "react-helmet"
import { useHistory, useParams } from 'react-router-dom';
import { useIsAuthenticated, useMsal } from "@azure/msal-react";

import { useLoginSilent } from '../components/OnLoginHandler';
import { useLoadingContext } from '../components/contexts/LoadingContext';
import { AppSettings } from '../utils/appsettings'
import WelcomeContent from './shared/WelcomeContent';

//const CLASS_NAME = "Login";

function SingleSignOn() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();
    const { returnUrl } = useParams();
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const { instance } = useMsal();
    const _isAuthenticated = useIsAuthenticated();
    const _activeAccount = instance.getActiveAccount();

    //set this for downstream use post successful silent login
    if (returnUrl != null && loadingProps.returnUrl !== returnUrl) setLoadingProps({ returnUrl: returnUrl });

    //check for logged in status - redirect to home page if already logged in.
    if (_isAuthenticated && _activeAccount != null) {
        history.push(returnUrl ? returnUrl : '/');
    }

    //-------------------------------------------------------------------
    // Region: hooks
    //  trigger from some other component to kick off an import log refresh and start tracking import status
    //-------------------------------------------------------------------
    useLoginSilent();

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Helmet>
                <title>{`${AppSettings.Titles.Main} | Single Sign On`}</title>
            </Helmet>
            <WelcomeContent showLogin={true} />
        </>
    )
}

export default SingleSignOn;