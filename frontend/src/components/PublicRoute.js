import React from "react";
import { Outlet } from "react-router-dom";
import { InlineMessage } from "./InlineMessage";
import ModalMessage from "./ModalMessage";

export function PublicRoute() {
    return (
        <div id="--routes-wrapper" className="container-fluid" >
            <div className="main-panel m-4">
                <InlineMessage />
                <Outlet />
            </div>
            <ModalMessage />
        </div>
    );
}

///fixed means the width is fixed rather than comsuming the entire width
export function PublicFixedRoute() {
    return (
        <div id="--routes-wrapper" className="container" >
            <div className="main-panel m-4">
                <InlineMessage />
                <Outlet />
            </div>
            <ModalMessage />
        </div>
    );
}


