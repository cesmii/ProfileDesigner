import React from 'react'
import { useHistory } from "react-router-dom"
import { InteractionStatus } from "@azure/msal-browser";
import { useMsal } from "@azure/msal-react";

import Dropdown from 'react-bootstrap/Dropdown'

import { isInRole } from '../utils/UtilityService';
import logo from './img/Logo-CESMII.svg'
import { SVGIcon } from './SVGIcon'
import Color from './Constants'

import './styles/Navbar.scss'
import { AppSettings } from '../utils/appsettings';
import { doLogout, useLoginStatus } from './OnLoginHandler';

//const CLASS_NAME = "Navbar";

function Navbar() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();
    const { instance, inProgress } = useMsal();
    const _activeAccount = instance.getActiveAccount();
    const { isAuthenticated, isAuthorized } = useLoginStatus(null, null /*[AppSettings.AADUserRole]*/);

    //-------------------------------------------------------------------
    // Region: Hooks
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: event handlers
    //-------------------------------------------------------------------
    const onLogoutClick = (e) => {
        doLogout(history, instance, '/login',true, true);
        e.preventDefault();
    }


    //-------------------------------------------------------------------
    // Region: render helpers
    //-------------------------------------------------------------------
    const renderNav = () => {
        return (
            <nav className="navbar navbar-expand-md navbar-dark bg-primary">
                <div className={`container-fluid ${isAuthenticated && _activeAccount != null ? "" : "container-lg"}`}>
                    <a className="navbar-brand d-flex align-items-center" href="/">
                        <img className="mr-3 mb-2 d-none d-md-block" src={logo} alt="CESMII Logo"></img>
                        <span className="headline-2">{AppSettings.Titles.Caption}</span>
                    </a>
                    {isAuthenticated && _activeAccount != null &&
                        <button className="navbar-toggler" type="button" data-toggle="collapse" data-target="#navbarMain" aria-controls="navbarMain" aria-expanded="false" aria-label="Toggle navigation">
                            <span className="navbar-toggler-icon"></span>
                        </button>
                    }
                    <div className="navbar-collapse collapse" id="navbarMain">
                        <div className="ml-auto my-2 my-lg-0 nav navbar-nav  align-items-md-center" >
                            {renderAdminMenu()}
                        </div>
                    </div>
                </div>
            </nav>
        );
    };

    const renderAdminMenu = () => {
        if (!isAuthenticated && !isAuthorized) return;
        return (
            <div className="nav-item" >
                <Dropdown>
                    <Dropdown.Toggle className="ml-0 ml-md-2 px-1 dropdown-custom-components d-flex align-items-center" title={_activeAccount?.username}>
                        <SVGIcon name="account-circle" size="32" fill={Color.white} className="mr-2" />
                        {_activeAccount?.name}
                    </Dropdown.Toggle>
                    <Dropdown.Menu>
                        {(isInRole(_activeAccount, 'cesmii.profiledesigner.admin')) &&
                            <>
                            <Dropdown.Item eventKey="3" href="/admin/user/list">View Users</Dropdown.Item>
                            <Dropdown.Divider />
                            </>
                        }
                        {(inProgress !== InteractionStatus.Startup && inProgress !== InteractionStatus.HandleRedirect) &&
                            <Dropdown.Item eventKey="10" onClick={onLogoutClick} >Logout</Dropdown.Item>
                        }
                    </Dropdown.Menu>
                </Dropdown>
            </div>
            );
    };

    return (
        <header>
            {renderNav()}
        </header>
    )
}

export default Navbar