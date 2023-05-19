import React, { useEffect } from 'react'
import { Helmet } from "react-helmet"
import { useHistory, useParams } from 'react-router-dom';
//import { useLoadingContext } from '../components/contexts/LoadingContext';
import { LoadingOverlayInline } from '../components/LoadingOverlay';
import { useLoginStatus } from '../components/OnLoginHandler';

import { AppSettings } from '../utils/appsettings'

//const CLASS_NAME = "LoginSuccess";

function LoginSuccess() {

    const history = useHistory();
    const { returnUrl } = useParams();
    //const { loadingProps, setLoadingProps } = useLoadingContext();
    const { isAuthenticated, isAuthorized } = useLoginStatus(null, null /*[AppSettings.AADUserRole]*/);

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
    //useLoginSilent();

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Helmet>
                <title>{`${AppSettings.Titles.Main} | Login`}</title>
            </Helmet>
            <LoadingOverlayInline msg="logging in..." show={true} />
        </>
    )
}

export default LoginSuccess;