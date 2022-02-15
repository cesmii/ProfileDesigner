import React from "react";
import { Route, Redirect } from "react-router-dom";

import { useAuthState } from "./AuthContext";
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

function AdminRoute({ component: Component, ...rest }) {

    const authTicket = useAuthState();

    //TBD - this would become more elaborate. Do more than just check for the existence of this value. Check for a token expiry, etc.
    //TBD - check individual permissions - ie can manage users, etc.
    //check if can manage users
    var isAdmin =
        authTicket != null && authTicket.token != null && 
        authTicket.user.permissionNames.findIndex(x => x === 'CanManageUsers') >= 0;

    return (
        <Route
            {...rest}
            render={props => (isAdmin) ?
                (<AdminLayout><Component {...props} /></AdminLayout>) :
                (<Redirect to="/login" />)
            }
        />
    );
}

export default AdminRoute;