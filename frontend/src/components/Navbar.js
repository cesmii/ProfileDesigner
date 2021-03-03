import React from 'react'
import { useAuthContext } from "./authentication/AuthContext"

import Dropdown from 'react-bootstrap/Dropdown'
import DropdownButton from 'react-bootstrap/DropdownButton'

import logo from './img/Logo-CESMII.svg'
import { SVGIcon } from './SVGIcon'
import Color from './Constants'

import './styles/Navbar.scss'

function Navbar() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { authTicket, setAuthTicket } = useAuthContext();

    //-------------------------------------------------------------------
    // Region: event handlers
    //-------------------------------------------------------------------
    const onLogoutClick = () => {
        setAuthTicket(null); //the call of this will clear the current user and the token
    }

    const renderLogoutButton = () => {
        if (authTicket == null) return;
        return (
            <div className="d-flex justify-content-center" >
                <SVGIcon name="account-circle" size="32" fill={Color.white} className="mr-2"/>
                {/*React-bootstrap bug if you launch modal, then the dropdowns don't work. Add onclick code to the drop down as a workaround - https://github.com/react-bootstrap/react-bootstrap/issues/5561*/}
                <DropdownButton onClick={(e) => e.stopPropagation()} 
                    menuAlign="right"
                    title={authTicket.user.fullName}
                    id="dropdown-menu-align-right"
                    >
                    <Dropdown.Item eventKey="1">Account details</Dropdown.Item>
                    <Dropdown.Item eventKey="2">Settings</Dropdown.Item>
                    <Dropdown.Divider />
                    <Dropdown.Item eventKey="4" onClick={onLogoutClick}>Logout</Dropdown.Item>
                </DropdownButton>
            </div>
        );
    };


    return (
        // <div id="--cesmii-navbar" >
        //     <nav className="navbar navbar-light">
                // <a className="navbar-brand" href="/">
                //     <img src={logo} width="156" height="40" alt="CESMII Logo"></img>
                // </a>
        //         <div>
        //             LOGOUT
        //             {renderLogoutButton()}
        //         </div>
        //     </nav>
        // </div>
        <>
            <nav id="--cesmii-navbar" className="navbar bg-primary">
                <a className="navbar-brand" href="/">
                    <img src={logo} width="156" height="40" alt="CESMII Logo"></img>
                </a>
                <div className="d-flex align-items-center ml-auto mr-5">
                    <div className="nav-item">
                        {renderLogoutButton()}
                    </div>
                </div>
            </nav>
        </>
    )

}

export default Navbar