import React from "react";
import { Route, Redirect } from "react-router-dom";

import { InlineMessage } from "../InlineMessage";
import SideMenu from "../SideMenu";
import { useLoginStatus } from "../OnLoginHandler";

const AdminLayout = ({ children }) => (

    <div id="--routes-wrapper" className="container-fluid sidebar p-0 d-flex" >
        <SideMenu />
        <div className="main-panel m-4 w-100">
            <InlineMessage />
            {children}
        </div>
    </div>
);

export function AdminRoute({ component: Component, ...rest }) {

    const { isAuthenticated, isAuthorized, redirectUrl } = useLoginStatus(rest.location, rest.roles);

    return (
        <Route
            {...rest}
            render={props => isAuthenticated && isAuthorized ?
                (<AdminLayout><Component {...props} /></AdminLayout>) :
                (<Redirect to={redirectUrl} />)
            }
        />
    );
}
