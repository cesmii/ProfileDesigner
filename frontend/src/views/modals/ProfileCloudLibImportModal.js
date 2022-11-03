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
import '../styles/ProfileEntity.scss';
import { isOwner } from '../shared/ProfileRenderHelpers';

import { AppSettings } from '../../utils/appsettings';

const CLASS_NAME = "ProfileCloudLibImportModal";

function ProfileCloudLibImportModal(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { instance } = useMsal();
    const _activeAccount = instance.getActiveAccount();
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_isValid, setIsValid] = useState({ namespace: true, namespaceFormat: true, selectedItem: true });
    const [showModal, setShowModal] = useState(props.showModal);
    const [_errorMsg, setErrorMessage] = useState(null);
    //Note: _item - child component updates values and bubbles up to update state
    const [_item, setItem] = useState(props.item != null && !props.showSelectUI ? props.item : JSON.parse(JSON.stringify(profileNew)));
    const [selectedItem, setSelectedItem] = useState(props.item == null ? null : JSON.parse(JSON.stringify(props.item)));
    const [_lookupProfiles, setLookupProfiles] = useState([]);
    const [_activeTab, setActiveTab] = useState('profile-select');

    const [_error, setError] = useState({ show: false, message: null, caption: null });

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
     
    const onImport= async () => {
        console.log(generateLogMessageString('onSave', CLASS_NAME));
        setShowModal(false);
        setErrorMessage(null);
        if (props.onCancel != null) props.onCancel();

        //do validation
        if (!validateForm()) {
            //alert("validation failed");
            return;
        }

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform insert/update call

        console.log(generateLogMessageString(`importItem||start`, CLASS_NAME));
        var url = `profile/cloudlibrary/import`;
        console.log(generateLogMessageString(`importFromCloudLibary||${url}`, CLASS_NAME));

        var data = [ { id: props.item.cloudLibraryId } ];

        //show a processing message at top. One to stay for duration, one to show for timed period.
        //var msgImportProcessingId = new Date().getTime();
        setLoadingProps({
            isLoading: true, message: `Importing from Cloud Library...This may take a few minutes.`
        });

        await axiosInstance.post(url, data).then(result => {
            if (result.status === 200) {
                //check for success message OR check if some validation failed
                //remove processing message, show a result message
                //inline for isSuccess, pop-up for error
                var revisedMessages = null;
                if (result.data.isSuccess) {

                    //synch flow would wait, now we do async so we have to check import log on timer basis. 
                    //    revisedMessages = [{
                    //        id: new Date().getTime(),
                    //        severity: result.data.isSuccess ? "success" : "danger",
                    //        body: `Profiles were imported successfully.`,
                    //        isTimed: result.data.isSuccess
                    //    }];
                }
                else {
                    setError({ show: true, caption: 'Import Error', message: `An error occurred processing the import file(s): ${result.data.message}` });
                }

                //asynch flow - trigger the component we use to show import messages, importing items changing is the trigger
                //update spinner, messages
                var importingLogs = loadingProps.importingLogs == null || loadingProps.importingLogs.length === 0 ? [] :
                    JSON.parse(JSON.stringify(loadingProps.importingLogs));
                importingLogs.push({ id: result.data.data, status: AppSettings.ImportLogStatus.InProgress, message: null });
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: revisedMessages,
                    importingLogs: importingLogs,
                    activateImportLog: true,
                    isImporting: false
                });

                //bubble up to parent to let them know the import log id associated with this import. 
                //then they can track how this specific import is doing in terms of completed or not
                if (props.onImportStarted) props.onImportStarted(result.data.data);

            } else {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, isImporting: false
                    //, inlineMessages: [{ id: new Date().getTime(), severity: "danger", body: `An error occurred processing the import file(s).`, isTimed: false, isImporting: false }]
                });
                setError({ show: true, caption: 'Import Error', message: `An error occurred processing the import file(s)` });
            }
        }).catch(e => {
            if (e.response && e.response.status === 401) {
                setLoadingProps({ isLoading: false, message: null, isImporting: false });
            }
            else {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, isImporting: false
                    //,inlineMessages: [{ id: new Date().getTime(), severity: "danger", body: e.response.data ? e.response.data : `An error occurred saving the imported profile.`, isTimed: false, isImporting: false }]
                });
                setError({ show: true, caption: 'Import Error', message: e.response && e.response.data ? e.response.data : `A system error has occurred during the profile import. Please contact your system administrator.` });
                console.log(generateLogMessageString('handleOnSave||saveFile||' + JSON.stringify(e), CLASS_NAME, 'error'));
                console.log(e);
            }
        })
    };


    //on validate handler from child form
    const onValidate = (isValid) => {
        setIsValid({
            namespace: isValid.namespace,
            namespaceFormat: isValid.namespaceFormat
        });
    }

    const onDismissMessage = (e) => {
        console.log(generateLogMessageString('onDismissMessage||', CLASS_NAME));
        setErrorMessage(null);
    }

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
                            onBlur={validateForm_selected} >
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
            </Form>
        );
    };

    const renderTabbedForm = () => {
        return (
            <Form noValidate>
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
                            <SVGIcon name={props.icon.name} size="36" fill={props.icon.color} className="mr-2" />
                    }
                    <div className="py-2" >{ caption}</div>
                </>
            );
        }
        else {
            return (
                <Nav activeKey={_activeTab} onSelect={(selectedKey) => setActiveTab(selectedKey)} className="align-items-baseline" >
                    <Nav.Item>
                        <Nav.Link eventKey="profile-select" >Select Profile</Nav.Link>
                    </Nav.Item>
                    <Nav.Item>
                        <Nav.Link eventKey="profile-new" >New Profile</Nav.Link>
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

    var caption = "Import from Cloud Library";

    //return final ui
    return (
        <>
            {/* Add animation=false to prevent React warning findDomNode is deprecated in StrictMode*/}
            <Modal size="lg" animation={false} show={showModal} onHide={onCancel} centered>
                <Modal.Header className="py-0 align-items-center" closeButton>
                    <Modal.Title>
                        {renderHeader()}
                    </Modal.Title>
                </Modal.Header>
                <Modal.Body className="my-1 pt-0 pb-2">
                    {renderErrorMessage()}
                    {!props.showSelectUI && renderAddForm() }
                    {props.showSelectUI && renderTabbedForm()}
                    <ProfileEntity item={_item} onValidate={onValidate} isValid={_isValid} />
                </Modal.Body>
                <Modal.Footer>
                    <Button variant="secondary" type="button" className="mx-3" onClick={onImport} >Import</Button>
                    <Button variant="secondary" className="mx-1" onClick={onCancel} >Close</Button>
                </Modal.Footer>
            </Modal>
        </>
    )
}

export default ProfileCloudLibImportModal;
