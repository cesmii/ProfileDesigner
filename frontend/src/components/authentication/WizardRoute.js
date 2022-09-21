import React from "react";
import { Route } from "react-router-dom";
import { useIsAuthenticated } from "@azure/msal-react";

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

const WizardLayoutPublic = ({ children }) => (

    <div id="--routes-wrapper" className="container" >
        <div className="main-panel m-4">
            <InlineMessage />
            <WizardContextProvider>
                <WizardMaster>
                    {children}
                </WizardMaster>
            </WizardContextProvider>
        </div>
    </div>
);

function WizardRoute({ component: Component, ...rest }) {

    const _isAuthenticated = useIsAuthenticated();

    return (
        <Route
            {...rest}
            render={props => _isAuthenticated ?
                (<WizardLayout><Component {...props} /></WizardLayout>) :
                (<WizardLayoutPublic><Component {...props} /></WizardLayoutPublic>)
            }
        />
    );
}

export default WizardRoute;