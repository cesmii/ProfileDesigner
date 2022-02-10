import React from "react";
import { Route, Redirect } from "react-router-dom";

import { useAuthState } from "./AuthContext";
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

    const authTicket = useAuthState();

    //TBD - this would become more elaborate. Do more than just check for the existence of this value. Check for a token expiry, etc.
    return (
        <Route
            {...rest}
            render={props => (authTicket != null && authTicket.token != null)  ?
                (<PrivateLayout><Component {...props} /></PrivateLayout>) :
                (<Redirect to="/login" />)
            }
        />
    );
}

export default PrivateRoute;