    import React, { useEffect, useState } from 'react'
import Modal from 'react-bootstrap/Modal'
import { useMsal } from "@azure/msal-react";
import axiosInstance from "../../services/AxiosService";

import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'

import { generateLogMessageString } from '../../utils/UtilityService'
import { useLoadingContext } from "../../components/contexts/LoadingContext";

import { SVGIcon } from "../../components/SVGIcon";
import { getProfileCaption, isProfileValid, profileNew } from '../../services/ProfileService';
import { Nav } from 'react-bootstrap';
import ProfileEntity from '../shared/ProfileEntity';
import { validate_All } from '../../services/ProfileService';
import { isOwner } from '../shared/ProfileRenderHelpers';
import '../styles/ProfileEntity.scss';

const CLASS_NAME = "ProfileEntityModal";

function ProfileEntityModal(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { instance } = useMsal();
    const _activeAccount = instance.getActiveAccount();
    const { setLoadingProps } = useLoadingContext();
    const [_isValid, setIsValid] = useState({ namespace: true, namespaceFormat: true, selectedItem: true });
    const [showModal, setShowModal] = useState(props.showModal);
    const [_errorMsg, setErrorMessage] = useState(null);
    //Note: _item - child component updates values and bubbles up to update state
    const [_item, setItem] = useState(props.item != null && !props.showSelectUI ? props.item : JSON.parse(JSON.stringify(profileNew)));
    const [selectedItem, setSelectedItem] = useState(props.item == null ? null : JSON.parse(JSON.stringify(props.item)));
    const [_lookupProfiles, setLookupProfiles] = useState([]);
    const [_activeTab, setActiveTab] = useState('profile-select');

    function buildTitleCaption(mode) {
        var caption = "Profile";
        if (mode != null) {
            switch (mode.toLowerCase()) {
                case "new":
                    caption = `New ${caption}`
                    break;
                case "edit":
                    caption = `Edit ${caption}`;
                    break;
                case "view":
                default:
                    caption = `View ${caption}`;
                    break;
            }
        }
        return caption;
    }


    //-------------------------------------------------------------------
    // Region: fetch lookup data
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchProfiles() {
            //Filter out anything 
            const url = `profile/mine`;
            console.log(generateLogMessageString(`useEffect||fetchProfiles||${url}`, CLASS_NAME));
            //TBD - come back to this when the model allows null for take, skip
            const data = { Query: null, Skip: 0, Take: 10000 };
            const result = await axiosInstance.post(url, data);
            setLookupProfiles(result.data.data);
        }

        //TBD - only get this in edit/add mode
        if (props.showSelectUI) fetchProfiles();

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [props.showSelectUI]);

    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));
        var isValid = validate_All(_item);
        setIsValid(isValid);
        return isProfileValid(isValid);
    }

    const validateForm_selected = () => {
        //TBD - only do this if in select mode
        var result = false;
        if (!props.showSelectUI) result = true;
        //make sure item is selected
        result = selectedItem != null;
        setIsValid({ ..._isValid, selectedItem: result });
        return result;
    };


    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const appendItemToList = (p) => {
        //add the new profile to the list
        _lookupProfiles.push(p);
        _lookupProfiles.sort((a, b) => {
            var versionA = a.version == null ? '' : a.version;
            var versionB = b.version == null ? '' : b.version;
            var sortA = `${a.namespace}+${versionA.toString()}`.toLowerCase();
            var sortB = `${b.namespace}+${versionB.toString()}`.toLowerCase();
            if (sortA < sortB) return -1;
            if (sortA > sortB) return 1;
            return 0;
        }); //sort by name+version
        setLookupProfiles(JSON.parse(JSON.stringify(_lookupProfiles)));
    };

    const onSelect = () => {
        console.log(generateLogMessageString('onSelect', CLASS_NAME));
        if (!validateForm_selected()) {
            console.log(generateLogMessageString('onSelect||selectItem==null...validation failed', CLASS_NAME));
            return;
        }
        setShowModal(false);
        setErrorMessage(null);
        if (props.onSelect != null) props.onSelect(selectedItem);
    };

    const onCancel = () => {
        console.log(generateLogMessageString('onCancel', CLASS_NAME));
        setShowModal(false);
        setErrorMessage(null);
        if (props.onCancel != null) props.onCancel();
    };
     
    const onSave = () => {
        console.log(generateLogMessageString('onSave', CLASS_NAME));
        setErrorMessage(null);

        //do validation
        if (!validateForm()){
            //alert("validation failed");
            return;
        } 

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform insert/update call
        console.log(generateLogMessageString(`handleOnSave||${mode}`, CLASS_NAME));
        const url = _item.id == null || _item.id === 0 ? `profile/add` : `profile/update`;
        axiosInstance.post(url, _item)
            .then(resp => {

                if (resp.data.isSuccess) {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: props.showSavedMessage ?
                            [{ id: new Date().getTime(), severity: "success", body: `Item was saved`, isTimed: true }] :
                            []
                        , refreshProfileCount: true
                        , refreshSearchCriteria: true
                    });

                    //console.log(resp.data);
                    //on successful save, hide this component and inform parent of save
                    setShowModal(false);
                    _item.id = resp.data.data;

                    //add the new item to lookup profiles and sort
                    appendItemToList(_item);
                    //callback to parent to assign items
                    if (props.onSave != null) props.onSave(_item);
                }
                else {
                    setLoadingProps({isLoading: false, message: null });
                    setErrorMessage(resp.data.message);
                }

            })
            .catch(error => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: []
                });
                setErrorMessage(`An error occurred saving this item.`);
                console.log(generateLogMessageString('handleOnSave||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
            });
    };


    //on validate handler from child form
    const onValidate = (isValid) => {
        setIsValid(current => {
            return { ...current, ...isValid };
        });
    }

    //on change handler to update state
    const onChangeEntity = (item) => {
        console.log(generateLogMessageString(`onChangeEntity`, CLASS_NAME));
        setItem(JSON.parse(JSON.stringify(item)));
    }

    //on change handler to update state
    const onChangeProfile = (e) => {
        //console.log(generateLogMessageString(`onEntityChange||e:${e.target}`, CLASS_NAME));

        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        var selectedItem = null;
        switch (e.target.id) {
            case "profile":
                if (e.target.value.toString() === "-1") {
                    selectedItem = null;
                }
                else {
                    selectedItem = _lookupProfiles.find(p => { return p.id.toString() === e.target.value; });
                }
                break;
            default:
                return;
        }
        //update the state
        setSelectedItem(selectedItem == null ? null : JSON.parse(JSON.stringify(selectedItem)));
    }

    const onDismissMessage = (e) => {
        console.log(generateLogMessageString('onDismissMessage||', CLASS_NAME));
        setErrorMessage(null);
    }

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderAddUI = () => {
        if (props.showSelectUI && _activeTab !== "profile-new") return;
        return (
            <ProfileEntity item={_item} onChange={onChangeEntity} onValidate={onValidate} isValid={_isValid} />
        );
    };

    const renderProfileSelect = () => {
        if (!props.showSelectUI || _activeTab !== "profile-select") return;
        //in edit, add, user can only choose certain profiles that they own.
        //show drop down list for edit, extend mode
        const options = _lookupProfiles.map((p) => {
            return (<option key={p.id} value={p.id} >{getProfileCaption(p)}</option>)
        });

        return (
            <div className="row mb-2">
                <div className="col-12">
                    <Form.Group>
                        <Form.Label>Profile</Form.Label>
                        {!_isValid.selectedItem &&
                            <span className="invalid-field-message inline">
                                Required
                            </span>
                        }
                        <Form.Control id="profile" as="select" className={(!_isValid.selectedItem ? 'invalid-field minimal pr-5' : 'minimal pr-5')} value={selectedItem == null ? "-1" : selectedItem.id}
                            onBlur={validateForm_selected} onChange={onChangeProfile} >
                            <option key="-1|Select One" value="-1" >Select</option>
                            {options}
                            {/*<option key="-99|New" value="-99" >[*New Profile]</option>*/}
                        </Form.Control>
                    </Form.Group>
                </div>
            </div>
        )
    };

    const renderAddForm = () => {
        return (
            <Form noValidate>
                {renderAddUI()}
            </Form>
        );
    };

    const renderTabbedForm = () => {
        return (
            <Form noValidate>
                {renderAddUI()}
                {renderProfileSelect()}
            </Form>
        );
    };
 
    const renderErrorMessage = () => {
        if (_errorMsg == null || _errorMsg === '') return;

        return (
            <div className="alert alert-danger my-2" >
                <div className="dismiss-btn">
                    <Button variant="icon-solo square" onClick={onDismissMessage} className="align-items-center" ><i className="material-icons">close</i></Button>
                </div>
                <div className="text-center" >
                    {_errorMsg}
                </div>
            </div>
        );
    };

    const renderHeader = () => {
        if (!props.showSelectUI) {
            return (
                <>
                    {props.icon != null &&
                            <SVGIcon name={props.icon.name} size="36" fill={props.icon.color} className="me-2" />
                    }
                    <div className="py-2" >{ caption}</div>
                </>
            );
        }
        else {
            return (
                <Nav activeKey={_activeTab} onSelect={(selectedKey) => setActiveTab(selectedKey)} className="align-items-baseline" >
                    <Nav.Item>
                        <Nav.Link eventKey="profile-select" className="profile-modal" >Select Profile</Nav.Link>
                    </Nav.Item>
                    <Nav.Item>
                        <Nav.Link eventKey="profile-new" className="profile-modal" >New Profile</Nav.Link>
                    </Nav.Item>
                </Nav>
            );
        }
    };

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    if (_item == null) return;

    var mode = "view";
    if (_item.id === null || _item.id === 0) mode = "new";
    else if (!_item.isReadOnly && isOwner(_item, _activeAccount) && _item.id > 0) mode = "edit";

    var caption = buildTitleCaption(mode);

    //return final ui
    return (
        <>
            {/* Add animation=false to prevent React warning findDomNode is deprecated in StrictMode*/}
            <Modal animation={false} show={showModal} onHide={onCancel} centered>
                <Modal.Header className="py-0 align-items-center" closeButton>
                    <Modal.Title>
                        {renderHeader()}
                    </Modal.Title>
                </Modal.Header>
                <Modal.Body className="my-1 pt-0 pb-2">
                    {renderErrorMessage()}
                    {!props.showSelectUI && renderAddForm() }
                    { props.showSelectUI && renderTabbedForm() }
                </Modal.Body>
                <Modal.Footer>
                    {((!_item.isReadOnly && isOwner(_item, _activeAccount)) || (_item.id == null || _item.id === 0)) &&
                        <>
                            <Button variant="text-solo" className="mx-1" onClick={onCancel} >Cancel</Button>
                            {(!props.showSelectUI || _activeTab === "profile-new") &&
                                <Button variant="secondary" type="button" className="mx-3" onClick={onSave} >Save</Button>
                            }
                        {(props.showSelectUI && _activeTab === "profile-select") &&
                            <Button variant="secondary" type="button" className="mx-3" onClick={onSelect} >Select</Button>
                            }
                        </>
                    }
                    {(_item.isReadOnly || (!isOwner(_item, _activeAccount) && _item.id !== null && _item.id !== 0)) &&
                        <Button variant="secondary" className="mx-1" onClick={onCancel} >Close</Button>
                    }
                </Modal.Footer>
            </Modal>
        </>
    )
}

export default ProfileEntityModal;
