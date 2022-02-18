import React, { useState, useEffect } from 'react'
import { useParams, useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import axiosInstance from "../../services/AxiosService";

import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'
import Dropdown from 'react-bootstrap/Dropdown'

import { AppSettings } from '../../utils/appsettings';
import { generateLogMessageString, validate_Email } from '../../utils/UtilityService'
import { useLoadingContext } from "../../components/contexts/LoadingContext";
import { useAuthState } from "../../components/authentication/AuthContext";

import { SVGIcon } from "../../components/SVGIcon";
import color from "../../components/Constants";
import ConfirmationModal from '../../components/ConfirmationModal';

const CLASS_NAME = "AdminUserEntity";

function AdminUserEntity() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();

    const { id, copyId } = useParams();
    //var pageMode = //state is not always present. If user types a url or we use an href link, state is null. history.location.state.viewMode;
    //see logic below for how we calculate.
    const [mode, setMode] = useState(initPageMode());
    const [_item, setItem] = useState({});

    const [isLoading, setIsLoading] = useState(true);
    const [isReadOnly, setIsReadOnly] = useState(true);
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const authTicket = useAuthState();
    const [_isValid, setIsValid] = useState({
        userName: true, firstName: true, lastName: true, email: true, emailFormat: true
        , password: true, confirmPassword: true, matchPassword: true
    });
    const [_deleteModal, setDeleteModal] = useState({ show: false, items: null });
    const [_error, setError] = useState({ show: false, message: null, caption: null });
    var caption = `${mode.toLowerCase() === "copy" || mode.toLowerCase() === "new" ? "Add": "Edit"} User`;

    //-------------------------------------------------------------------
    // Region: Hooks
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchData() {
            console.log(generateLogMessageString('useEffect||fetchData||async', CLASS_NAME));
            //initialize spinner during loading
            setLoadingProps({ isLoading: true, message: null });

            //mode not set right if we were on this page, save an copy and navigate into edit same marketplaceItem. Rely on
            // parentId, id. Then determine mode. for copy, we use parentId, for edit/view, we use id.
            var result = null;
            try {
                var data = { id: (copyId != null ? copyId : id) };
                var url = `user/${copyId == null ? 'getbyid' : 'copy'}`
                result = await axiosInstance.post(url, data);
            }
            catch (err) {
                var msg = 'An error occurred retrieving this user.';
                console.log(generateLogMessageString('useEffect||fetchData||error', CLASS_NAME, 'error'));
                //console.log(err.response.status);
                if (err != null && err.response != null && err.response.status === 404) {
                    msg += ' This user was not found.';
                    history.push('/404');
                }
                //403 error - user may be allowed to log in but not permitted to perform the API call they are attempting
                else if (err != null && err.response != null && err.response.status === 403) {
                    console.log(generateLogMessageString('useEffect||fetchData||Permissions error - 403', CLASS_NAME, 'error'));
                    msg += ' You are not permitted to edit users.';
                    history.goBack();
                }
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: msg, isTimed: false }]
                });
            }

            if (result == null) return;

            var thisMode = (copyId != null) ? 'copy' : 'edit';

            //set item state value
            setItem(result.data);
            setIsLoading(false);
            setLoadingProps({ isLoading: false, message: null });
            setMode(thisMode);

            // set form to readonly if we're in viewmode or is deleted (isActive = false)
            setIsReadOnly(thisMode.toLowerCase() === "view" || !result.data.isActive);

        }

        //get a blank user object from server
        async function fetchDataAdd() {
            console.log(generateLogMessageString('useEffect||fetchDataAdd||async', CLASS_NAME));
            //initialize spinner during loading
            setLoadingProps({ isLoading: true, message: null });

            var result = null;
            try {
                var url = `user/init`
                result = await axiosInstance.post(url);
            }
            catch (err) {
                var msg = 'An error occurred initializing the new user.';
                console.log(generateLogMessageString('useEffect||fetchDataAdd||error', CLASS_NAME, 'error'));
                //console.log(err.response.status);
                if (err != null && err.response != null && err.response.status === 404) {
                    msg += ' A problem occurred with the add user screen.';
                    history.push('/404');
                }
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: msg, isTimed: false }]
                });
            }

            if (result == null) return;

            //set item state value
            setItem(result.data);
            setIsLoading(false);
            setLoadingProps({ isLoading: false, message: null });
            //setMode(thisMode);
            setIsReadOnly(false);
        }

        //fetch our data 
        // for view/edit modes
        if ((id != null && id.toString() !== 'new') || copyId != null) {
            fetchData();
        }
        else {
            fetchDataAdd();
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [id, copyId, authTicket.user]);


    //-------------------------------------------------------------------
    // Region: 
    //-------------------------------------------------------------------
    function initPageMode() {
        //if path contains copy and parent id is set, mode is copy
        //else - we won't know the author ownership till we fetch data, default view
        if (copyId != null && history.location.pathname.indexOf('/copy/') > -1) return 'copy';

        //if path contains new, then go into a new mode
        if (id === 'new') {
            return 'new';
        }

        //if path contains id, then default to view mode and determine in fetch whether user is owner or not.
        return 'view';
    }

    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------
    const validateForm_userName = (e) => {
        var isValid = e.target.value != null && e.target.value.trim().length > 0;
        setIsValid({ ..._isValid, userName: isValid });
    };

    const validateForm_firstName = (e) => {
        var isValid = e.target.value != null && e.target.value.trim().length > 0;
        setIsValid({ ..._isValid, firstName: isValid });
    };

    const validateForm_lastName = (e) => {
        var isValid = e.target.value != null && e.target.value.trim().length > 0;
        setIsValid({ ..._isValid, lastName: isValid });
    };

    const validateForm_password = (e) => {
        var isValid = e.target.value != null && e.target.value.trim().length > 0;
        setIsValid({ ..._isValid, password: isValid });
    };

    const validateForm_confirmPassword = (e) => {
        var isValid = e.target.value != null && e.target.value.trim().length > 0;
        var isValidMatch = e.target.value === _item.password;
        setIsValid({ ..._isValid, confirmPassword: isValid, matchPassword: isValidMatch });
    };

    const validateForm_email = (e) => {
        var isValid = (e.target.value != null && e.target.value.trim().length > 0);
        var isValidEmail = validate_Email(e.target.value);
        setIsValid({ ..._isValid, email: isValid, emailFormat: isValidEmail });
    };

    ////update state for when search click happens
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        var isAdd = mode.toLowerCase() === "copy" || mode.toLowerCase() === "new";

        _isValid.userName = _item.userName != null && _item.userName.trim().length > 0;
        _isValid.firstName = _item.firstName != null && _item.firstName.trim().length > 0;
        _isValid.lastName = _item.lastName != null && _item.lastName.trim().length > 0;
        _isValid.email = _item.email != null && _item.email.trim().length > 0;
        _isValid.emailFormat = validate_Email(_item.email);
        //password validation
        _isValid.password = !isAdd || (_item.password != null && _item.password.trim().length > 0);
        _isValid.confirmPassword = !isAdd || (_item.confirmPassword != null && _item.confirmPassword.trim().length > 0);
        _isValid.matchPassword = !isAdd || _item.confirmPassword == _item.password;  

        setIsValid(JSON.parse(JSON.stringify(_isValid)));
        return (_isValid.userName && _isValid.firstName && _isValid.lastName
            && _isValid.email && _isValid.emailFormat
            && _isValid.password && _isValid.confirmPassword && _isValid.matchPassword);
    }

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onDeleteItem = () => {
        console.log(generateLogMessageString('onDeleteItem', CLASS_NAME));
        setDeleteModal({ show: true, item: _item });
    };

    const onDeleteConfirm = () => {
        console.log(generateLogMessageString('onDeleteConfirm', CLASS_NAME));

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform delete call
        var data = { id: _item.id };
        var url = `user/delete`;
        axiosInstance.post(url, data)  //api allows one or many
            .then(result => {

                if (result.data.isSuccess) {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            {
                                id: new Date().getTime(), severity: "success", body: `Item was deleted`, isTimed: true
                            }
                        ]
                    });
                    history.push('/admin/user/list');
                }
                else {
                    //update spinner, messages
                    setError({ show: true, caption: 'Delete Item Error', message: result.data.message });
                    setLoadingProps({ isLoading: false, message: null });
                    setDeleteModal({ show: false, item: null });
                }

            })
            .catch(error => {
                //hide a spinner, show a message
                setError({ show: true, caption: 'Delete Item Error', message: `An error occurred deleting this item.` });
                setLoadingProps({ isLoading: false, message: null });

                console.log(generateLogMessageString('deleteItem||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
                console.log(error);
                //scroll back to top
                window.scroll({
                    top: 0,
                    left: 0,
                    behavior: 'smooth',
                });
            });
    };

    const onCancel = () => {
        //raised from header nav
        console.log(generateLogMessageString('onCancel', CLASS_NAME));
        history.push('/admin/user/list');
    };

    const onSave = () => {
        //raised from header nav
        console.log(generateLogMessageString('onSave', CLASS_NAME));

        //do validation
        if (!validateForm()) {
            //alert("validation failed");
            return;
        }

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform insert call
        console.log(generateLogMessageString(`handleOnSave||${mode}`, CLASS_NAME));
        var isAdd = mode.toLowerCase() === "copy" || mode.toLowerCase() === "new";
        var url = isAdd ?
            `user/add/onestep` : `user/update`;
        axiosInstance.post(url, _item)
            .then(resp => {

                if (resp.data.isSuccess) {
                    //hide a spinner, show a message
                    //if add, show a message which includes the new password
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "success", body: `User was saved.`, isTimed: true }
                        ]
                    });

                    //now redirect to user list
                    history.push(`/admin/user/list`);
                }
                else {
                    setError({ show: true, caption: 'Save Error', message: resp.data.message });
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: null
                    });
                }

            })
            .catch(error => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: `An error occurred ${mode.toLowerCase() === "copy" ? "copying" : "saving"} this user.`, isTimed: false }
                    ]
                });
                console.log(generateLogMessageString('handleOnSave||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
                console.log(error);
                //scroll back to top
                window.scroll({
                    top: 0,
                    left: 0,
                    behavior: 'smooth',
                });
            });
    };

    //on change handler to update state
    const onChange = (e) => {
        //console.log(generateLogMessageString(`onEntityChange||e:${e.target}`, CLASS_NAME));

        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        switch (e.target.id) {
            case "userName":
            case "firstName":
            case "lastName":
            case "email":
            case "password":
            case "confirmPassword":
                _item[e.target.id] = e.target.value;
                break;
            default:
                return;
        }
        //update the state
        setItem(JSON.parse(JSON.stringify(_item)));
    }

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderMoreDropDown = () => {
        if (_item == null || (mode.toLowerCase() === "copy" || mode.toLowerCase() === "new")) return;

        //React-bootstrap bug if you launch modal, then the dropdowns don't work. Add onclick code to the drop down as a workaround - https://github.com/react-bootstrap/react-bootstrap/issues/5561
        return (
            <Dropdown className="action-menu icon-dropdown ml-2" onClick={(e) => e.stopPropagation()} >
                <Dropdown.Toggle drop="left">
                    <SVGIcon name="more-vert" size="24" fill={color.shark} />
                </Dropdown.Toggle>
                <Dropdown.Menu>
                    <Dropdown.Item href={`/admin/user/new`}>Add User</Dropdown.Item>
                    <Dropdown.Item href={`/admin/user/copy/${_item.id}`}>Copy '{_item.userName}'</Dropdown.Item>
                    <Dropdown.Item onClick={onDeleteItem} >Delete '{_item.userName}'</Dropdown.Item>
                </Dropdown.Menu>
            </Dropdown>
        );
    }

    const renderButtons = () => {
        if (mode.toLowerCase() !== "view") {
            return (
                <>
                    <Button variant="text-solo" className="ml-1" onClick={onCancel} >Cancel</Button>
                    <Button variant="secondary" type="button" className="ml-2" onClick={onSave} >Save</Button>
                </>
            );
        }
    }

    //render the delete modal when show flag is set to true
    //callbacks are tied to each button click to proceed or cancel
    const renderDeleteConfirmation = () => {

        if (!_deleteModal.show) return;

        var message = `You are about to delete '${_deleteModal.item.userName}'. This action cannot be undone. Are you sure?`;
        var caption = `Delete Item`;

        return (
            <>
                <ConfirmationModal showModal={_deleteModal.show} caption={caption} message={message}
                    icon={{ name: "warning", color: color.trinidad }}
                    confirm={{ caption: "Delete", callback: onDeleteConfirm, buttonVariant: "danger" }}
                    cancel={{
                        caption: "Cancel",
                        callback: () => {
                            console.log(generateLogMessageString(`onDeleteCancel`, CLASS_NAME));
                            setDeleteModal({ show: false, item: null });
                        },
                        buttonVariant: null
                    }} />
            </>
        );
    };

    //render error message as a modal to force user to say ok.
    const renderErrorMessage = () => {

        if (!_error.show) return;

        return (
            <>
                <ConfirmationModal showModal={_error.show} caption={_error.caption} message={_error.message}
                    icon={{ name: "warning", color: color.trinidad }}
                    confirm={null}
                    cancel={{
                        caption: "OK",
                        callback: () => {
                            //console.log(generateLogMessageString(`onErrorMessageOK`, CLASS_NAME));
                            setError({ show: false, caption: null, message: null });
                        },
                        buttonVariant: 'danger'
                    }} />
            </>
        );
    };

    const renderForm = () => {
        //console.log(item);
        var isAdd = mode.toLowerCase() === "copy" || mode.toLowerCase() === "new";
        return (
            <>
                <div className="row">
                    <div className="col-md-6">
                        <Form.Group>
                            <Form.Label htmlFor="userName" >User Name</Form.Label>
                            {!_isValid.userName &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            <Form.Control id="userName" className={(!_isValid.userName ? 'invalid-field minimal pr-5' : 'minimal pr-5')}
                                value={_item.userName} onBlur={validateForm_userName} onChange={onChange} readOnly={isAdd ? '' : 'readonly'} />
                        </Form.Group>
                    </div>
                </div>
                {isAdd &&
                    <>
                    <div className="row">
                        <div className="col-md-6">
                            <Form.Group>
                                <Form.Label htmlFor="password" >Password</Form.Label>
                                {!_isValid.password &&
                                    <span className="invalid-field-message inline">
                                        Required
                                    </span>
                                }
                                <Form.Control id="password" className={(!_isValid.password ? 'invalid-field minimal pr-5' : 'minimal pr-5')}
                                    value={_item.password} onBlur={validateForm_password} onChange={onChange} type="password" />
                            </Form.Group>
                        </div>
                    </div>
                    <div className="row">
                        <div className="col-md-6">
                            <Form.Group>
                                <Form.Label htmlFor="confirmPassword" >Confirm Password</Form.Label>
                                {!_isValid.confirmPassword &&
                                    <span className="invalid-field-message inline">
                                        Required
                                    </span>
                                }
                                {!_isValid.matchPassword &&
                                    <span className="invalid-field-message inline">
                                        Password and confirm password do not match
                                    </span>
                                }
                                <Form.Control id="confirmPassword" className={(!_isValid.confirmPassword || !_isValid.matchPassword ? 'invalid-field minimal pr-5' : 'minimal pr-5')}
                                    value={_item.confirmPassword} onBlur={validateForm_confirmPassword} onChange={onChange} type="password" />
                            </Form.Group>
                        </div>
                    </div>
                    </>
                }
                <div className="row">
                    <div className="col-md-6">
                        <Form.Group>
                            <Form.Label htmlFor="firstName" >First Name</Form.Label>
                            {!_isValid.firstName &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            <Form.Control id="firstName" className={(!_isValid.firstName ? 'invalid-field minimal pr-5' : 'minimal pr-5')}
                                value={_item.firstName} onBlur={validateForm_firstName} onChange={onChange} />
                        </Form.Group>
                    </div>
                </div>
                <div className="row">
                    <div className="col-md-6">
                        <Form.Group>
                            <Form.Label htmlFor="lastName" >Last Name</Form.Label>
                            {!_isValid.lastName &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            <Form.Control id="lastName" className={(!_isValid.lastName ? 'invalid-field minimal pr-5' : 'minimal pr-5')}
                                value={_item.lastName} onBlur={validateForm_lastName} onChange={onChange} />
                        </Form.Group>
                    </div>
                </div>
                <div className="row">
                    <div className="col-md-6">
                        <Form.Group>
                            <Form.Label htmlFor="email" >Email</Form.Label>
                            {!_isValid.email &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            {!_isValid.emailFormat &&
                                <span className="invalid-field-message inline">
                                    Invalid Format (ie. jdoe@abc.com)
                                </span>
                            }
                            <Form.Control id="email" type="email" className={(!_isValid.email || !_isValid.emailFormat ? 'invalid-field minimal pr-5' : 'minimal pr-5')}
                                value={_item.email} onBlur={validateForm_email} onChange={onChange} />
                        </Form.Group>
                    </div>
                </div>
                { _item.permissionNames != null &&
                    <>
                    <hr className="my-3" />
                    <div className="row">
                        <div className="col-md-6">
                            <h2>Permissions</h2>
                            <span className="">
                                {_item.permissionNames.join(", ")}
                            </span>
                        </div>
                    </div>
                    </>
                }
            </>
        )
    }

    const renderHeaderRow = () => {
        return (
            <div className="row py-2 pb-4">
                <div className="col-sm-12 d-flex align-items-center" >
                    {renderHeaderBlock()}
                </div>
            </div>
        );
    };

    const renderHeaderBlock = () => {

        return (
            <>
                <h1 className="m-0 mr-2">
                    Admin | {caption}
                </h1>
                <div className="ml-auto d-flex align-items-center" >
                    {renderButtons()}
                    {renderMoreDropDown()}
                </div>
            </>
        )
    }

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    if (loadingProps.isLoading || isLoading) return null;

    //return final ui
    return (
        <>
            <Helmet>
                <title>{AppSettings.Titles.Main + " Admin | " + caption}</title>
            </Helmet>
            <Form noValidate>
                {renderHeaderRow()}
                <div className="row" >
                    <div className="col-sm-12 mb-4" >
                        {renderForm()}
                    </div>
                </div>
            </Form>
            {renderDeleteConfirmation()}
            {renderErrorMessage()}
        </>
    )
}

export default AdminUserEntity;
