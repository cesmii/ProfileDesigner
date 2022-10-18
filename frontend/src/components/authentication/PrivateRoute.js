import React from "react";
import { Route, Redirect } from "react-router-dom";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";

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

    const { instance } = useMsal();
    const _isAuthenticated = useIsAuthenticated();
    const _activeAccount = instance.getActiveAccount();
    //Check for is authenticated. Check individual permissions - ie can manage marketplace items.
    const _isAuthorized = _isAuthenticated && _activeAccount != null;

    //track a redirect url for post login
    const redirectUrl = rest.location?.pathname && rest.location?.pathname !== '/' ? `/sso?returnUrl=${rest.location?.pathname}` : '/sso';

    return (
        <Route
            {...rest}
            render={props => _isAuthorized ?
                (<PrivateLayout><Component {...props} /></PrivateLayout>) :
                (<Redirect to={redirectUrl} />)
            }
        />
    );
}

export default PrivateRoute;