import React, { useState } from "react";
import { Redirect } from "react-router-dom";
import { Helmet } from "react-helmet"
import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import axios from 'axios'

import { AppSettings } from '../utils/appsettings'
import { generateLogMessageString } from '../utils/UtilityService'
import { useAuthContext } from "../components/authentication/AuthContext";
import { useLoadingContext } from "../components/contexts/LoadingContext";

const CLASS_NAME = "Login";

function Login() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const caption = 'Login';
    const [_loginData, setLoginData] = useState({ userName: '', password: '' });
    //const [_isLoggedIn, setLoggedIn] = useState(false);
    const [_isError, setIsError] = useState(false);
    const [_isValid, setIsValid] = useState({ userName: true, password: true });
    const { authTicket, setAuthTicket } = useAuthContext();
    const { loadingProps, setLoadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onChange = (e) => {
        //console.log(generateLogMessageString(`onAttributeChange||e:${e.target}`, CLASS_NAME));

        switch (e.target.id.toLowerCase()) {
            case "username":
                _loginData.userName = e.target.value;
                break;
            case "password":
                _loginData.password = e.target.value;
                break;
            default:
                return;
        }
        setLoginData(JSON.parse(JSON.stringify(_loginData)));
    }

    //update state for when search click happens
    const onBlur = (e) => {
        console.log(generateLogMessageString(`onBlur`, CLASS_NAME));

        switch (e.target.id.toLowerCase()) {
            case "username":
                _isValid.userName = e.target.value.trim().length > 0;
                break;
            case "password":
                _isValid.password = e.target.value.trim().length > 0;
                break;
            default:
                return;
        }
        setIsValid(JSON.parse(JSON.stringify(_isValid)));
    }

    ////update state for when search click happens
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        _isValid.userName = _loginData.userName != null && _loginData.userName.trim().length > 0;
        _isValid.password = _loginData.password != null && _loginData.password.trim().length > 0;
        setIsValid(JSON.parse(JSON.stringify(_isValid)));
        return (_isValid.userName && _isValid.password);
    }

    const onLoginClick = () => {
        console.log(generateLogMessageString('onLoginClick', CLASS_NAME));

        //validate
        if (!validateForm()) return;

        //show a spinner
        setLoadingProps({ isLoading: true, message: null });

        //Call API to perform check
        //TBD - For proto, just do a simple check on user list for username found in list. In the phase II
        // implementation, this would be swapped out with a proper authentication call (post) and JWT token returned.
        //If login successful, set global state with user data and isAuthenticated
        axios(`${AppSettings.BASE_API_URL}/user?userName=${_loginData.userName}`).then(result => {
            if (result.status === 200) {
                //TBD - proto - simple check to force user pw to equal user name for proto. If this is not the case, fail the login
                if (_loginData.userName !== _loginData.password) {
                    setIsError(true);
                    //hide a spinner
                    setLoadingProps({ isLoading: false, message: null });
                    return;
                }
                //now find if the user exists in the data by matching user name
                var user = result.data.find(item => { return item.userName === _loginData.userName; });
                if (user == null) {
                    setIsError(true);
                }
                else {
                    //TBD-Temp for proto - token val
                    setAuthTicket({ token: (new Date()).getTime(), user: user });
                    //setLoggedIn(true);
                }
            } else {
                setIsError(true);
            }
            //hide a spinner
            setLoadingProps({ isLoading: false, message: null });
        }).catch(e => {
            setIsError(true);
            //hide a spinner
            setLoadingProps({ isLoading: false, message: null });
        });
    }

    //if already logged in, go to home page
    if (authTicket != null && authTicket.token != null) {
//    if (_isLoggedIn) {
        return <Redirect to="/" />;
    }

    return (
        <>
            <Helmet>
                <title>{AppSettings.Titles.Main + " | " + caption}</title>
            </Helmet>
            <div id="--cesmii-main-content" className="login-wrapper">
                <div id="--cesmii-left-content" className="d-flex align-content-center justify-content-center">
                    <Card body className="elevated mt-5">
                        {_isError &&
                            <div className="justify-content-center alert alert-danger">
                                Invalid user name and password combination.
                            </div>
                        }
                        <div className="d-flex">
                            <Form.Group className="flex-grow-1">
                                <Form.Label>User Name</Form.Label>
                                {!_isValid.userName &&
                                    <span className="invalid-field-message inline">
                                        Required
                                    </span>
                                }
                                <Form.Control id="userName" type="" placeholder="Enter user name" value={_loginData.email} onChange={onChange} onBlur={onBlur} />
                            </Form.Group>
                        </div>
                        <div className="d-flex">
                            <Form.Group className="flex-grow-1">
                                <Form.Label>Password</Form.Label>
                                {!_isValid.password &&
                                    <span className="invalid-field-message inline">
                                        Required
                                    </span>
                                }
                                <Form.Control id="password" type="password" value={_loginData.password} onChange={onChange} onBlur={onBlur} />
                            </Form.Group>
                        </div>
                        <div className="d-flex">
                            <Button variant="primary" type="button" onClick={onLoginClick} disabled={loadingProps.isLoading ? "disabled" : ""} >
                                Login
                            </Button>
                        </div>
                    </Card>
                </div>
            </div>
        </>
    );
}

export default Login;
