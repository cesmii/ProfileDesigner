import React from "react";
import Button from 'react-bootstrap/Button'
import { useMsal } from "@azure/msal-react";

import { generateLogMessageString } from '../utils/UtilityService'
import { useLoadingContext } from "../components/contexts/LoadingContext";
import { doCreateAccount, doLoginPopup, doLoginRedirect } from "./OnLoginHandler";

const CLASS_NAME = "LoginButton";

function LoginButton() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { instance, inProgress } = useMsal();
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const _mode = "redirect";

    //-------------------------------------------------------------------
    // Region: API call
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onLoginClick = (e) => {
        console.log(generateLogMessageString('onLoginClick', CLASS_NAME));
        if (_mode === "popup") doLoginPopup(instance, inProgress, setLoadingProps);
        else doLoginRedirect(instance, inProgress);
    }

    const onCreateAccountClick = (e) => {
        console.log(generateLogMessageString('onCreateAccountClick', CLASS_NAME));
        doCreateAccount(instance, inProgress);
    }

    //if already logged in, don't show button
    //if (_isAuthenticated && _activeAccount != null) {
    //    return null;
    //}

    return (
        <div className="elevated mx-3 card mt-3 mt-md-5">
            <div className="card-body">
            <h2 className="text-center mb-3">Returning Users</h2>
            <div className="d-flex mt-auto mx-auto">
                <Button variant="primary" className="mx-auto ml-2 border" type="submit" onClick={onLoginClick} disabled={loadingProps.isLoading ? "disabled" : ""} >
                    Login
                </Button>
            </div>
            <p className="mt-3 mb-2 text-center" >
                <span className="font-weight-bold mr-1" >Don't have an account?</span>
                <Button variant="link" className="link m-0 p-0 pr-1" type="button" onClick={onCreateAccountClick} >
                    Create Account
                </Button>
                or email us at <a href="mailto:devops@cesmii.org" >devops@cesmii.org</a> to get registered.
                Please provide your project name or SOPO number with your request.
            </p>
            </div>
        </div>
    );
}

export default LoginButton;
