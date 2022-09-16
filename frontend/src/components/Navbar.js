import React from 'react'
import { useHistory } from "react-router-dom"
import { InteractionStatus } from "@azure/msal-browser";
import { useIsAuthenticated, useMsal } from "@azure/msal-react";

import Dropdown from 'react-bootstrap/Dropdown'

import { isInRole } from '../utils/UtilityService';
import logo from './img/Logo-CESMII.svg'
import { SVGIcon } from './SVGIcon'
import Color from './Constants'

import './styles/Navbar.scss'
import { AppSettings } from '../utils/appsettings';
import LoginButton from './LoginButton';

//const CLASS_NAME = "Navbar";

function Navbar() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();
    const { instance, inProgress } = useMsal();
    const _isAuthenticated = useIsAuthenticated();
    const _activeAccount = instance.getActiveAccount();

    //-------------------------------------------------------------------
    // Region: Hooks
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: event handlers
    //-------------------------------------------------------------------
    const onLogoutClick = () => {
        //MSAL logout
        instance.logoutPopup();
        history.push(`/`);
    }

    const renderNav = () => {
        return (
            <nav className="navbar navbar-dark bg-primary navbar-expand-md">
                <div className="container-fluid pr-0">
                    <button className="navbar-toggler" type="button" data-toggle="collapse" data-target="#navbarNav" aria-controls="navbarNav" aria-expanded="false" aria-label="Toggle navigation">
                        <span className="navbar-toggler-icon"></span>
                    </button>
                    <div className="collapse navbar-collapse mt-2 mt-md-0" id="navbarNav">
                        <ul className="navbar-nav align-items-start align-items-md-center">

                            <LoginButton />
                            {renderAdminMenu()}
                        </ul>
                    </div>
                </div>
            </nav>
        );
    };

    const renderAdminMenu = () => {
        if (!_isAuthenticated || _activeAccount == null) return;
        return (
            <li className="nav-item" >
                <Dropdown>
                    <Dropdown.Toggle className="ml-0 ml-md-2 px-1 dropdown-custom-components d-flex align-items-center">
                        <SVGIcon name="account-circle" size="32" fill={Color.white} className="mr-2" />
                        {_activeAccount?.name}
                    </Dropdown.Toggle>
                    <Dropdown.Menu>
                        <Dropdown.Item eventKey="1" href="/account">Account Profile</Dropdown.Item>
                        <Dropdown.Divider />
                        {(isInRole(_activeAccount, 'cesmii.profiledesigner.admin')) &&
                            <Dropdown.Item eventKey="3" href="/admin/user/list">Manage Users</Dropdown.Item>
                        }
                        <Dropdown.Divider />
                        {(inProgress !== InteractionStatus.Startup && inProgress !== InteractionStatus.HandleRedirect) &&
                            <Dropdown.Item eventKey="10" onClick={onLogoutClick} >Logout</Dropdown.Item>
                        }
                    </Dropdown.Menu>
                </Dropdown>
            </li>
            );
    };


    return (
        <header>
            <div className="container-fluid d-flex h-100" >
                <div className="col-sm-12 px-0 px-sm-1 d-flex align-content-center" >
                    <div className="d-flex align-items-center">
                        <a className="navbar-brand d-flex align-items-center" href="/">
                            <img className="mr-3 mb-2 d-none d-md-block" src={logo} alt="CESMII Logo"></img>
                            <span className="headline-2 font-weight-bold">{AppSettings.Titles.Caption}</span>
                        </a>
                    </div>
                    <div className="d-flex align-items-center ml-auto">
                        {renderNav()}
                    </div>
                </div>
            </div>
        </header>
        )
}

export default Navbar