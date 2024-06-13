import React from "react";
import { Outlet, Navigate, useLocation } from "react-router-dom";

import { InlineMessage } from "../InlineMessage";
import SideMenu from "../SideMenu";
import { ImportMessage } from "../ImportMessage";
import DownloadMessage from "../DownloadMessage";
import { WizardContextProvider } from "../contexts/WizardContext";
import WizardMaster from "../../views/shared/WizardMaster";
import { useLoginStatus } from "../OnLoginHandler";
import ModalMessage from "../ModalMessage";

function WizardLayout() {
    return (
        <div id="--routes-wrapper" className="container-fluid sidebar p-0 d-flex" >
            <SideMenu />
            <div className="main-panel m-4 w-100">
                <InlineMessage />
                <ImportMessage />
                <DownloadMessage />
                <WizardContextProvider>
                    <WizardMaster>
                        <Outlet />
                    </WizardMaster>
                </WizardContextProvider>
            </div>
            <ModalMessage />
        </div>
    );
}

function WizardRoute(props) {

    let location = useLocation();

    const { isAuthenticated, isAuthorized, redirectUrl } = useLoginStatus(location, props.roles);

    return isAuthenticated && isAuthorized ? WizardLayout() : (<Navigate to={redirectUrl} />);
}

export default WizardRoute;