import React, { useEffect } from "react";
import { useHistory } from "react-router-dom";
import { Button, Card } from "react-bootstrap";
import { Helmet } from "react-helmet";

import { useAuthDispatch, useAuthState } from "./authentication/AuthContext";
import { useLoadingContext } from "./contexts/LoadingContext";
import { AppSettings } from "../utils/appsettings";
import { generateLogMessageString } from "../utils/UtilityService";
import { logout } from "./authentication/AuthActions";
import { useState } from "react";
import axiosInstance from "../services/AxiosService";

const CLASS_NAME = "ErrorPage";
function ErrorPage({ error, resetErrorBoundary }) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();
    const authTicket = useAuthState();
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_logError, setLogError] = useState(true);
    const dispatch = useAuthDispatch() //get the dispatch method from the useDispatch custom hook

    //-------------------------------------------------------------------
    // Region: hooks, events
    //-------------------------------------------------------------------
    //always turn off is processing indicator if error occurs
    useEffect(() => {
        
        if (loadingProps.isLoading) {
            setLoadingProps({ isLoading: false });
        }
        //if (_errorShown) {
        //    setLoadingProps({ isLoading: false });
        //    setErrorShown(true);
        //}

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||wizardProps||Cleanup', CLASS_NAME));
        };
    }, [loadingProps.isLoading]);

    //TBD - log exception to API - do not raise exception if fails
    useEffect(() => {

        if (!_logError) return;

        //only run one time per error
        console.log(generateLogMessageString('useEffect||logError||Cleanup', CLASS_NAME));
        setLogError(false);

        console.log(JSON.stringify(error));

        //Call API to perform check
        //If login successful, set global state with user data and isAuthenticated
        var data = { message: error.message, url: history.location.pathname };
        var url = `system/log/${(authTicket == null || authTicket.user == null ? "public" : "private")}`;
        axiosInstance.post(url, data).then(result => {
            if (result.status === 200) {
                console.log(generateLogMessageString(`logError||Error was logged to the server.`, CLASS_NAME));
            } else {
                console.warn(generateLogMessageString(`logError||Error was not logged to the server.`, CLASS_NAME));
            }
        }).catch(e => {
            console.warn(generateLogMessageString(`logError||Error occurred logging to the server.`, CLASS_NAME));
        });

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||wizardProps||Cleanup', CLASS_NAME));
        };
    }, [_logError]);

    //allow user to log out from error page
    const onLogoutClick = () => {
        //updates state and removes user auth ticket from local storage
        let logoutAction = logout(dispatch);
        if (!logoutAction) {
            console.error(generateLogMessageString(`onLogoutClick||logoutAction||An error occurred setting the logout state.`, CLASS_NAME));
        }
        else {
            history.push(`/`);
        }
    }

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Helmet>
                <title>{AppSettings.Titles.Main + " | Error" }</title>
            </Helmet>
            <div className="row mt-4 no-gutters vh-100">
            <div className="col-sm-6 mx-auto">
                <h1>
                    An error has occurred
                </h1>
                <Card body className="elevated my-2">
                    <div className="justify-content-center alert alert-danger">
                        <p className="text-center">
                            Please contact your system administrator or try again.
                        </p>
                        <p>
                            <span className="font-weight-bold d-block" >Error Details:</span>
                            {error.message}
                        </p>
                    </div>
                    <div className="d-flex justify-content-center">
                            <Button variant="secondary" href='/' >
                            Home
                        </Button>
                        {resetErrorBoundary &&
                                <Button variant="secondary" className="ml-2" onClick={resetErrorBoundary} >
                                Try Again
                            </Button>
                        }
                        {(authTicket != null && authTicket.user != null) &&
                            <Button variant="primary" className="ml-2" onClick={onLogoutClick} >
                                Logout
                            </Button>
                        }
                    </div>
                </Card>
                </div>
            </div>
        </>
    );
}

export default ErrorPage;
