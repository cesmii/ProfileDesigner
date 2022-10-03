import React from "react";
import { Route, Redirect } from "react-router-dom";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";

import { InlineMessage } from "../InlineMessage";
import SideMenu from "../SideMenu";
import { ImportMessage } from "../ImportMessage";
import DownloadMessage from "../DownloadMessage";
import { WizardContextProvider } from "../contexts/WizardContext";
import WizardMaster from "../../views/shared/WizardMaster";

const WizardLayout = ({ children }) => (

    <div id="--routes-wrapper" className="container-fluid sidebar p-0 d-flex" >
        <SideMenu />
        <div className="main-panel m-4 w-100">
            <InlineMessage />
            <ImportMessage />
            <DownloadMessage />
            <WizardContextProvider>
                <WizardMaster>
                    {children}
                </WizardMaster>
            </WizardContextProvider>
        </div>
    </div>
);

function WizardRoute({ component: Component, ...rest }) {

    const { instance } = useMsal();
    const _isAuthenticated = useIsAuthenticated();
    const _activeAccount = instance.getActiveAccount();
    //Check for is authenticated. Check individual permissions - ie can manage marketplace items.
    var isAuthorized = _isAuthenticated && _activeAccount != null;

    return (
        <Route
            {...rest}
            render={props => isAuthorized ?
                (<WizardLayout><Component {...props} /></WizardLayout>) :
                (<Redirect to="/login" />)
            }
        />
    );
}

export default WizardRoute;