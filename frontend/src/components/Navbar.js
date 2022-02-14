import React from 'react'
import { useHistory } from 'react-router-dom'
import { useAuthDispatch, useAuthState } from "./authentication/AuthContext";
import { logout } from "./authentication/AuthActions";

import Dropdown from 'react-bootstrap/Dropdown'

import { generateLogMessageString } from '../utils/UtilityService';
import logo from './img/Logo-CESMII.svg'
import { SVGIcon } from './SVGIcon'
import Color from './Constants'

import './styles/Navbar.scss'
import { AppSettings } from '../utils/appsettings';

const CLASS_NAME = "Navbar";

function Navbar() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();
    const authTicket = useAuthState();
    const dispatch = useAuthDispatch() //get the dispatch method from the useDispatch custom hook

    //-------------------------------------------------------------------
    // Region: event handlers
    //-------------------------------------------------------------------
    const onLogoutClick = () => {
        //updates state and removes user auth ticket from local storage
        let logoutAction = logout(dispatch);
        if (!logoutAction) {
            console.error(generateLogMessageString(`onLogoutClick||logoutAction||An error occurred setting the logout state.`, CLASS_NAME));
        }
        else {
            history.push(`/`);
        }
        //setAuthTicket(null);
    }

    const renderNav = () => {
        if (authTicket == null || authTicket.user == null) return;
        return (
            <nav className="navbar navbar-dark bg-primary navbar-expand-md">
                <div className="container-fluid pr-0">
                    <button className="navbar-toggler" type="button" data-toggle="collapse" data-target="#navbarNav" aria-controls="navbarNav" aria-expanded="false" aria-label="Toggle navigation">
                        <span className="navbar-toggler-icon">
                            {/*<i className="material-icons">menu</i>*/}
                        </span>
                    </button>
                    <div className="collapse navbar-collapse" id="navbarNav">
                        <ul className="navbar-nav">
                            {renderLogoutButton()}
                        </ul>
                    </div>
                </div>
            </nav>
        );
    };

    const renderLogoutButton = () => {
        if (authTicket == null || authTicket.user == null) return;

        //check if can manage users
        var canManageUsers = authTicket.user.permissionNames.findIndex(x => x === 'CanManageUsers') >= 0;

        return (
            <li className="nav-item" >
                <Dropdown>
                    <Dropdown.Toggle className="ml-0 ml-md-2 px-1 dropdown-custom-components d-flex align-items-center">
                        <SVGIcon name="account-circle" size="32" fill={Color.white} className="mr-2" />
                        {authTicket.user.fullName}
                    </Dropdown.Toggle>
                    <Dropdown.Menu>
                        <Dropdown.Item eventKey="1" href="/user">Account details</Dropdown.Item>
                        {canManageUsers &&
                            <Dropdown.Item eventKey="3" href="/admin/user/list">Manage Users</Dropdown.Item>
                        }
                        <Dropdown.Divider />
                        <Dropdown.Item eventKey="2" onClick={onLogoutClick} >Logout</Dropdown.Item>
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