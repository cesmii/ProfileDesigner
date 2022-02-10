import React from "react";
import { Route } from "react-router-dom";
import { InlineMessage } from "./InlineMessage";


const SimpleLayout = ({ children }) => (

    <div id="--routes-wrapper" className="container-fluid" >
        <div className="main-panel m-4">
            <InlineMessage />
            {children}
        </div>
    </div>
);

export function PublicRoute({ component: Component, ...rest }) {

    return (
        <Route
            {...rest}
            render={props =>
                (<SimpleLayout><Component {...props} /></SimpleLayout>)
            }
        />
    );
}


