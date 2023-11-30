import React from "react";
import { Navigate, Outlet, useLocation } from "react-router-dom";

import { InlineMessage } from "../InlineMessage";
import SideMenu from "../SideMenu";
import { ImportMessage } from "../ImportMessage";
import DownloadMessage from "../DownloadMessage";
import { useLoginStatus } from "../OnLoginHandler";
import ModalMessage from "../ModalMessage";

function PrivateLayout() {
    return (
        <div id="--routes-wrapper" className="container-fluid sidebar p-0 d-flex" >
            <SideMenu />
            <div className="main-panel m-4 w-100">
                <InlineMessage />
                <ImportMessage />
                <DownloadMessage />
                <Outlet />
            </div>
            <ModalMessage />
        </div>
    );
}

function PrivateRoute(props) {

    let location = useLocation();

    const { isAuthenticated, isAuthorized, redirectUrl } = useLoginStatus(location, props.roles);

    return isAuthenticated && isAuthorized ? PrivateLayout() : (<Navigate to={redirectUrl} />);
}

export default PrivateRoute;
