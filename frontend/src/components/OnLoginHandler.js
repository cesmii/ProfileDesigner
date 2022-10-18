import { useEffect } from "react";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";
import { BrowserAuthError, InteractionRequiredAuthError, InteractionStatus } from "@azure/msal-browser";

import axiosInstance from "../services/AxiosService";
import { generateLogMessageString } from '../utils/UtilityService'
import { useLoadingContext } from "../components/contexts/LoadingContext";
import { AppSettings } from "../utils/appsettings";

const CLASS_NAME = "OnLoginHandler";

//-------------------------------------------------------------------
// onAfterAADLogin: after login, let API know and wire up some stuff for downstream
//-------------------------------------------------------------------
export const onAfterAADLogin = (setLoadingProps) => {
    console.log(generateLogMessageString('onAfterAADLogin', CLASS_NAME));

    //perform insert call
    const url = `auth/onAADLogin`;
    axiosInstance.post(url)
        .then(result => {
            if (result.data.isSuccess) {
                console.log(generateLogMessageString(`onAfterAADLogin||${result.data.message}`, CLASS_NAME));
                setLoadingProps({ refreshLookupData: true, refreshSearchCriteria: true, refreshFavorites: true, doLogin: false });
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
// handleLoginError: based on type of error, handle login error
//-------------------------------------------------------------------
export const handleLoginError = (error, setLoadingProps) => {
    var msg = 'An error occurred attempting to launch the login window. Please contact the system administrator.';
    if (error instanceof InteractionRequiredAuthError) {
        console.error(generateLogMessageString(`onLoginClick||loginPopup||${error}`, CLASS_NAME));
    }
    else if (error instanceof BrowserAuthError) {
        switch (error.errorCode) {
            case 'user_cancelled':
                console.warn(generateLogMessageString(`onLoginClick||loginPopup||${error}`, CLASS_NAME));
                //no need to inform user they cancelled
                msg = null;
                break;
            case 'popup_window_error':
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
}

//-------------------------------------------------------------------
// Region: MSAL Login Actions - login popup
//-------------------------------------------------------------------
//Login w/ popup - component
export const doLoginPopup = async (instance, inProgress, accounts, setLoadingProps) => {

    //show a spinner
    setLoadingProps({ isLoading: true, message: null });

    if (inProgress === InteractionStatus.None || inProgress === InteractionStatus.Startup) {

        const loginRequest = {
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
                onAfterAADLogin(setLoadingProps);

                //trigger additional actions to pull back data from mktplace once logged in
                setLoadingProps({ isLoading: false, message: null });
            })
            .catch((error) => {
                if (error instanceof InteractionRequiredAuthError) {
                    instance.acquireTokenRedirect(loginRequest);
                    console.error(generateLogMessageString(`onLoginClick||loginPopup||${error}`, CLASS_NAME));
                }
                else {
                    handleLoginError(error, setLoadingProps);
                }
            });
    }
}

//-------------------------------------------------------------------
// UseLoginSilent - attempt a silent login
//-------------------------------------------------------------------
export const useLoginSilent = () => {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    //const history = useHistory();
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const { instance, inProgress, accounts } = useMsal();
    const _activeAccount = instance.getActiveAccount();
    const _isAuthenticated = useIsAuthenticated() && _activeAccount != null;

    //console.log(generateLogMessageString(`Initialize||Active Account: ${_activeAccount?.userName} ||Authenticated: ${_isAuthenticated}`, CLASS_NAME));

    //-------------------------------------------------------------------
    // Region: Hooks - if not logged in, try silently
    //-------------------------------------------------------------------
    useEffect(() => {
        //Login silently
        async function doLoginSilent() {

            if (inProgress !== InteractionStatus.None && inProgress !== InteractionStatus.Startup) return;

            const msgId = new Date().getTime();
            //setLoadingProps({
            //    isLoading: false, message: null, inlineMessages: [
            //        { id: msgId, severity: "info", body: 'Single Sign On. Attempting single sign on...', isTimed: true }]
            //});

            const loginRequest = {
                scopes: AppSettings.MsalScopes,
                account: accounts[0],
                redirectUri: loadingProps.returnUrl ? loadingProps.returnUrl : '/'
            };

            try {
                const response = await instance.ssoSilent(loginRequest);
                //set the active account
                instance.setActiveAccount(response.account);

                console.info(generateLogMessageString(`doLoginSilent||SSO...Automatically signing in...`, CLASS_NAME));
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: msgId, severity: "success", body: 'Single Sign On. Success...', isTimed: true }]
                });

                //call API to inform user login successful.this will also redirect.
                onAfterAADLogin(setLoadingProps);

                setLoadingProps({ isLoading: false, message: null });

            } catch (error) {
                if (error instanceof InteractionRequiredAuthError) {
                    //we will have the login button and user can initiate loginPopup that way
                    //doLoginPopup();
                    console.warn(generateLogMessageString(`doLoginSilent||Auth Required||${error}`, CLASS_NAME));
                } else {
                    // handle error
                    handleLoginError(error, setLoadingProps);
                }
            }
        }

        //if on the /login page, then don't attempt login silent.
        if (window.location.href.indexOf('/login') > -1) return;

        //if not logged in, try silently
        if (!_activeAccount || !_isAuthenticated) {
            console.info(generateLogMessageString(`useEffect||doLoginSilent`, CLASS_NAME));
            doLoginSilent();
        }
    }, [_activeAccount, _isAuthenticated ]);

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    // renders nothing, since nothing is needed
    return null;
}








