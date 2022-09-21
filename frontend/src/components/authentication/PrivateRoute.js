import React from "react";
import { Route, Redirect } from "react-router-dom";
import { useIsAuthenticated } from "@azure/msal-react";

import { InlineMessage } from "../InlineMessage";
import SideMenu from "../SideMenu";
import { ImportMessage } from "../ImportMessage";
import DownloadMessage from "../DownloadMessage";

const PrivateLayout = ({ children }) => (

    <div id="--routes-wrapper" className="container-fluid sidebar p-0 d-flex" >
        <SideMenu />
        <div className="main-panel m-4 w-100">
            <InlineMessage />
            <ImportMessage />
            <DownloadMessage />
            {children}
        </div>
    </div>
);

function PrivateRoute({ component: Component, ...rest }) {

    const _isAuthenticated = useIsAuthenticated();

    return (
        <Route
            {...rest}
            render={props => _isAuthenticated  ?
                (<PrivateLayout><Component {...props} /></PrivateLayout>) :
                (<Redirect to="/" />)
            }
        />
    );
}

export default PrivateRoute;