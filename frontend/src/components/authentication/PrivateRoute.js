import React from "react";
import { Route, Redirect } from "react-router-dom";

import { InlineMessage } from "../InlineMessage";
import SideMenu from "../SideMenu";
import { ImportMessage } from "../ImportMessage";
import DownloadMessage from "../DownloadMessage";
import { useLoginStatus } from "../OnLoginHandler";
import ModalMessage from "../ModalMessage";

const PrivateLayout = ({ children }) => (

    <div id="--routes-wrapper" className="container-fluid sidebar p-0 d-flex" >
        <SideMenu />
        <div className="main-panel m-4 w-100">
            <InlineMessage />
            <ImportMessage />
            <DownloadMessage />
            {children}
        </div>
        <ModalMessage />
    </div>
);

function PrivateRoute({ component: Component, ...rest }) {

    const { isAuthenticated, isAuthorized, redirectUrl } = useLoginStatus(rest.location, rest.roles);

    return (
        <Route
            {...rest}
            render={props => isAuthenticated && isAuthorized ?
                (<PrivateLayout><Component {...props} /></PrivateLayout>) :
                (<Redirect to={redirectUrl} />)
            }
        />
    );
}

export default PrivateRoute;