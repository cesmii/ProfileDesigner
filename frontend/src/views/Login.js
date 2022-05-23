import React, { useState } from "react";
import { Redirect } from "react-router-dom";
import { Helmet } from "react-helmet"
import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'
import Card from 'react-bootstrap/Card'
import axiosInstance from "../services/AxiosService";

import { AppSettings } from '../utils/appsettings'
import { generateLogMessageString } from '../utils/UtilityService'
import { useLoadingContext } from "../components/contexts/LoadingContext";
import { useAuthDispatch, useAuthState } from "../components/authentication/AuthContext";
import { login } from "../components/authentication/AuthActions";
import { WizardSettings } from "../services/WizardUtil";

const CLASS_NAME = "Login";

function Login() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [_loginData, setLoginData] = useState({ userName: '', password: '' });
    //const [_isLoggedIn, setLoggedIn] = useState(false);
    const [_error, setIsError] = useState({ success: true, message: 'An error occurred. Please try again.' });
    const [_isValid, setIsValid] = useState({ userName: true, password: true });
    const authTicket = useAuthState();
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const dispatch = useAuthDispatch() //get the dispatch method from the useDispatch custom hook
    const _currentPage = WizardSettings.panels.find(p => { return p.id === 'Welcome'; });

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
        //console.log(generateLogMessageString(`onBlur`, CLASS_NAME));

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

    const onLoginClick = (e) => {
        console.log(generateLogMessageString('onLoginClick', CLASS_NAME));

        e.preventDefault(); //prevent form.submit action

        //validate
        if (!validateForm()) return;

        //show a spinner
        setIsError({ ..._error, success: true });
        setLoadingProps({ isLoading: true, message: null });

        //Call API to perform check
        //If login successful, set global state with user data and isAuthenticated
        var data = { "UserName": _loginData.userName, "Password": _loginData.password };
        axiosInstance.post(`auth/login`, data).then(result => {
            if (result.status === 200) {
                //check if we got a successful response
                if (!result.data.isSuccess || result.data.data == null || result.data.data.token == null || result.data.data.user == null) {
                    setIsError({
                        success: false,
                        message: !result.data.isSuccess && !result.data.message != null ? result.data.message : _error.message
                    });
                    //hide a spinner
                    setLoadingProps({ isLoading: false, message: null });
                    return;
                }
                else {
                    var loginData = result.data.data;
                    //set token and logged in user
                    console.log(generateLogMessageString(`onLoginClick||success||${loginData.token.substring(loginData.token.length - 60)}`, CLASS_NAME));
                    let loginAction = login(dispatch, loginData) //loginUser action makes the request and handles all the neccessary state changes
                    if (!loginAction) {
                        console.error(generateLogMessageString(`onLoginClick||loginAction||an error occurred setting the login state.`, CLASS_NAME));
                    }
                    //trigger additional actions to pull back data once logged in
                    //setLoadingProps({ refreshTypeCount: true, hasSidebar: true });
                    setLoadingProps({
                        refreshProfileCount: true, hasSidebar: true,
                        refreshLookupData: true, refreshSearchCriteria: true, refreshFavoritesList: true
                    });

                    //setAuthTicket({ token: result.data.token, user: result.data.user });
                }
            } else {
                setIsError({ success: false, message: 'An error occurred. Please try again.' });
            }
            //hide a spinner
            setLoadingProps({ isLoading: false, message: null });
        }).catch(e => {
            if ((e.response && e.response.status === 401) || e.toString().indexOf('Network Error') > -1) {
                //do nothing, this is handled in routes.js using common interceptor
            }
            //bad request - this is how the API tells us the user name or password did not match
            if ((e.response && e.response.status === 400)) {
                //hide a spinner
                setLoadingProps({ isLoading: false, message: null });
                setIsError(true);
            }
            else {
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: 'An error occurred. Please contact your system administrator.', isTimed: false }]
                });
            }
        });
    }

    const renderIntroContent = () => {
        return (
            <>
                <p>
                    The Profile Designer allows disparate manufacturers and engineers to build manufacturing profiles that could be shared amongst a community of smart manufacturing entities. The profile is a class definition (or collection of class definitions) describing a piece of manufacturing equipment (or conceivably, a manufacturing process or manufactured good). Profiles have relationships to other profiles within the scope of the user's work context. These relationships are of the kinds typically seen in a UML (Unified Modeling Language) diagram, including inheritance, aggregation (or composition), interface implementation, and dependency.
                </p>
                <p>
                    <span className="font-weight-bold mr-1">Ready to get started?</span>
                    You are a few clicks away from building your own SM Profiles.
                    Login to begin or email us at <a href="mailto:devops@cesmii.org" >devops@cesmii.org</a> to get registered.
                    Please provide your project name or SOPO number with your request.
                </p>
            </>
           );
    }

    const renderLoginUI = () => {
        return (
            <>
                <Card body className="elevated mx-3">
                    {!_error.success &&
                        <div className="justify-content-center alert alert-danger">
                            {_error.message}
                        </div>
                    }
                    <form>
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
                            <Button variant="primary" className="mx-auto mt-2" type="submit" onClick={onLoginClick} disabled={loadingProps.isLoading ? "disabled" : ""} >
                                Login
                            </Button>
                        </div>
                        <p className="mt-3 mb-0 text-center" >
                            <span className="font-weight-bold mr-1" >Don't have an account?</span>
                            Email us at <a href="mailto:devops@cesmii.org" >devops@cesmii.org</a> to get registered.
                            Please provide your project name or SOPO number with your request.
                        </p>
                    </form>
                </Card>
            </>
        );
    }

    //if already logged in, go to home page
    if (authTicket != null && authTicket.token != null) {
        //    if (_isLoggedIn) {
        return <Redirect to="/" />;
    }

    return (
        <>
            <Helmet>
                <title>{AppSettings.Titles.Main + " | Login"}</title>
            </Helmet>
            <div className="row">
                <div className=" col-sm-12 mt-2 mb-3">
                    <h1 className="mb-0">Welcome!</h1>
                </div>
                <div className="col-sm-6 col-md-7">
                    {renderIntroContent()}
                </div>
                <div className="col-sm-6 col-md-5">
                    {renderLoginUI()}
                </div>
            </div>
        </>
    );
}

export default Login;
