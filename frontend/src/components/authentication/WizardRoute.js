import React from "react";
import { Route, Redirect } from "react-router-dom";

import { InlineMessage } from "../InlineMessage";
import SideMenu from "../SideMenu";
import { ImportMessage } from "../ImportMessage";
import DownloadMessage from "../DownloadMessage";
import { WizardContextProvider } from "../contexts/WizardContext";
import WizardMaster from "../../views/shared/WizardMaster";
import { useLoginStatus } from "../OnLoginHandler";
import ModalMessage from "../ModalMessage";

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
        <ModalMessage />
    </div>
);

function WizardRoute({ component: Component, ...rest }) {

    const { isAuthenticated, isAuthorized, redirectUrl } = useLoginStatus(rest.location, rest.roles);

    return (
        <Route
            {...rest}
            render={props => isAuthenticated && isAuthorized ?
                (<WizardLayout><Component {...props} /></WizardLayout>) :
                (<Redirect to={redirectUrl} />)
            }
        />
    );
}

export default WizardRoute;