import React from "react";
import Button from 'react-bootstrap/Button'

import {BrowserAuthError, InteractionRequiredAuthError,InteractionStatus} from "@azure/msal-browser";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";

import axiosInstance from "../services/AxiosService";
import { generateLogMessageString } from '../utils/UtilityService'
import { useLoadingContext } from "../components/contexts/LoadingContext";
import { AppSettings } from "../utils/appsettings";

const CLASS_NAME = "LoginButton";

function LoginButton() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { instance, inProgress, accounts } = useMsal();
    const _isAuthenticated = useIsAuthenticated();
    const _activeAccount = instance.getActiveAccount();
    const { loadingProps, setLoadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Region: API call
    //-------------------------------------------------------------------
    const onAfterAADLogin = () => {
        console.log(generateLogMessageString('onAfterAADLogin', CLASS_NAME));

        //perform insert call
        var url = `auth/onAADLogin`;
        axiosInstance.post(url)
            .then(result => {
                if (result.data.isSuccess) {
                    console.log(generateLogMessageString(`onAfterAADLogin||${result.data.message}`, CLASS_NAME));
                }
                else {
                    console.warn(generateLogMessageString(`onAfterAADLogin||Initialize Failed||${result.data.message}`, CLASS_NAME));
                }
            })
            .catch(error => {
                console.log(generateLogMessageString('handleOnSave||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
                console.log(error);
            });
    };

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onLoginClick = (e) => {
        console.log(generateLogMessageString('onLoginClick', CLASS_NAME));

        e.preventDefault(); //prevent form.submit action

        //show a spinner
        setLoadingProps({ isLoading: true, message: null });

        if (inProgress === InteractionStatus.None) {

            var loginRequest = {
                scopes: AppSettings.MsalScopes,
                account: accounts[0],
            };

            // redirect anonymous user to login popup
            instance.loginPopup(loginRequest)
                //.acquireTokenSilent(loginRequest)
                .then((response) => {
                    //set the active account
                    instance.setActiveAccount(response.account);

                    //call API to inform user login successful. 
                    onAfterAADLogin();

                    //trigger additional actions to pull back data from mktplace once logged in
                    setLoadingProps({ isLoading: false, message: null });
                    setLoadingProps({ refreshMarketplaceCount: true, hasSidebar: true, refreshLookupData: true, refreshFavorites: true, refreshSearchCriteria: true });
                })
                .catch((error) => {
                    var msg = 'An error occurred attempting to launch the login window. Please contact the system administrator.';
                    if (error instanceof InteractionRequiredAuthError) {
                        instance.acquireTokenRedirect(loginRequest);
                        console.error(generateLogMessageString(`onLoginClick||loginPopup||${error}`, CLASS_NAME));
                    }
                    else if (error instanceof BrowserAuthError) {
                        switch (error.errorCode) {
                            case 'user_cancelled':
                                console.warn(generateLogMessageString(`onLoginClick||loginPopup||${error}`, CLASS_NAME));
                                //no need to inform user they cancelled
                                msg = null;
                                break;
                            case 'popup_blocker':
                                console.warn(generateLogMessageString(`onLoginClick||loginPopup||${error}`, CLASS_NAME));
                                msg = "The Login UI uses a popup window. Please disable Popup blocker for this site.";
                                break;
                            default:
                                console.error(generateLogMessageString(`onLoginClick||loginPopup||${error}`, CLASS_NAME));
                                msg = error.message;
                        }
                    }
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: msg == null ? [] :
                            [{ id: new Date().getTime(), severity: "danger", body: msg, isTimed: false }]
                    });
                });
        }
    }

    //if already logged in, don't show button
    if (_isAuthenticated && _activeAccount != null) {
        return null;
    }

    return (
        <>
            <Button variant="primary" className="mx-auto ml-2 border" type="submit" onClick={onLoginClick} disabled={loadingProps.isLoading ? "disabled" : ""} >
                Login
            </Button>
        </>
    );
}

export default LoginButton;
