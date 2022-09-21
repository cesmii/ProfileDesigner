import React from "react";
import { Route, Redirect } from "react-router-dom";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";

import { isInRoles } from "../../utils/UtilityService";
import { InlineMessage } from "../InlineMessage";
import SideMenu from "../SideMenu";

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

    const { instance } = useMsal();
    const _isAuthenticated = useIsAuthenticated();
    const _activeAccount = instance.getActiveAccount();
    //Check for is authenticated. Check individual permissions - ie can manage profile designer.
    let isAuthorized = _isAuthenticated && _activeAccount != null && (rest.roles == null || isInRoles(_activeAccount, rest.roles));

    return (
        <Route
            {...rest}
            render={props => isAuthorized ?
                (<AdminLayout><Component {...props} /></AdminLayout>) :
                (<Redirect to="/" />)
            }
        />
    );
}
