import React from "react";
import { Route, Redirect } from "react-router-dom";

import { useAuthState } from "./AuthContext";
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

    const authTicket = useAuthState();

    //TBD - this would become more elaborate. Do more than just check for the existence of this value. Check for a token expiry, etc.
    return (
        <Route
            {...rest}
            render={props => (authTicket != null && authTicket.token != null) ?
                (<WizardLayout><Component {...props} /></WizardLayout>) :
                (<Redirect to="/login" />)
            }
        />
    );
}

export default WizardRoute;