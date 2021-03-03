import React from "react";
import { Route, Redirect } from "react-router-dom";
import { useAuthContext } from "./AuthContext";

function PrivateRoute({ component: Component, ...rest }) {
    //TBD - this would become more elaborate. Do more than just check for the existence of this value. Check for a token expiry, etc. 
    const { authTicket } = useAuthContext();
    return (
        <Route
            {...rest}
            render={props => (authTicket != null && authTicket.token != null)  ?
                (<Component {...props} />) :
                (<Redirect to="/login" />)
            }
        />
    );
}

export default PrivateRoute;