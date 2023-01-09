import React, { useEffect, useState } from 'react'
import { useMsal } from '@azure/msal-react';
import { InteractionStatus } from '@azure/msal-browser';

import { Helmet } from "react-helmet"
import { useHistory, useParams } from 'react-router-dom';
import { useLoadingContext } from '../components/contexts/LoadingContext';
import { useLoginSilent, useLoginStatus } from '../components/OnLoginHandler';

import { AppSettings } from '../utils/appsettings'
import WelcomeContent from './shared/WelcomeContent';
import { generateLogMessageString } from '../utils/UtilityService';
import { LoadingOverlayInline } from '../components/LoadingOverlay';

const CLASS_NAME = "Login";

function Login() {

    const history = useHistory();
    const { returnUrl } = useParams();
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const { isAuthenticated, isAuthorized } = useLoginStatus(null, null /*[AppSettings.AADUserRole]*/);
    const { inProgress } = useMsal();
    //default to true so we don't show and then immediately hide content, better to hide and then immediately show
    const [_inProgress, setInProgress] = useState(true);

    //-------------------------------------------------------------------
    // Region: hooks
    //-------------------------------------------------------------------
    useEffect(() => {

        //check for logged in status - redirect to home page if already logged in.
        if (isAuthenticated && isAuthorized) {
            history.push(returnUrl ? decodeURIComponent(returnUrl) : '/');
        }

    }, [isAuthenticated, isAuthorized]);

    //-------------------------------------------------------------------
    // Region: hooks
    //-------------------------------------------------------------------
    useEffect(() => {

        //set this for downstream use post successful silent login
        if (returnUrl != null && loadingProps.returnUrl !== returnUrl) setLoadingProps({ returnUrl: returnUrl });

    }, [returnUrl, loadingProps.returnUrl]);


    //-------------------------------------------------------------------
    // Region: show or hide processing indicator when MSAL is processing login
    //-------------------------------------------------------------------
    useEffect(() => {
        console.log(generateLogMessageString(`inProgress|${inProgress}`, CLASS_NAME));
        //not interactions which are not in progress
        const p = !(inProgress === InteractionStatus.Logout || inProgress === InteractionStatus.None || inProgress === InteractionStatus.SsoSilent);
        //only set state if value has changed
        if (p !== _inProgress) setInProgress(p);
    }, [inProgress]);

    //-------------------------------------------------------------------
    // Region: hooks
    //-------------------------------------------------------------------
    useLoginSilent();

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Helmet>
                <title>{`${AppSettings.Titles.Main} | Login`}</title>
            </Helmet>
            {!_inProgress && 
                <WelcomeContent showLogin={true} />
            }
            <LoadingOverlayInline msg="logging in..." show={_inProgress} />
        </>
    )
}

export default Login;