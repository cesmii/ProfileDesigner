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

const SimpleFixedLayout = ({ children }) => (

    <div id="--routes-wrapper" className="container" >
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

///fixed means the width is fixed rather than comsuming the entire width
export function PublicFixedRoute({ component: Component, ...rest }) {

    return (
        <Route
            {...rest}
            render={props =>
                (<SimpleFixedLayout><Component {...props} /></SimpleFixedLayout>)
            }
        />
    );
}


