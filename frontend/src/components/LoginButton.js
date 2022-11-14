import React from "react";
import Button from 'react-bootstrap/Button'
import { useMsal } from "@azure/msal-react";

import { generateLogMessageString } from '../utils/UtilityService'
import { useLoadingContext } from "../components/contexts/LoadingContext";
import { doLoginPopup } from "./OnLoginHandler";

const CLASS_NAME = "LoginButton";

function LoginButton() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { instance, inProgress, accounts } = useMsal();
    const { loadingProps, setLoadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Region: API call
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------

    const onLoginClick = (e) => {
        console.log(generateLogMessageString('onLoginClick', CLASS_NAME));
        doLoginPopup(instance, inProgress, accounts, setLoadingProps);
    }

    //if already logged in, don't show button
    //if (_isAuthenticated && _activeAccount != null) {
    //    return null;
    //}

    return (
        <>
            <Button variant="primary" className="mx-auto ml-2 border" type="submit" onClick={onLoginClick} disabled={loadingProps.isLoading ? "disabled" : ""} >
                Login
            </Button>
        </>
    );
}

export default LoginButton;
