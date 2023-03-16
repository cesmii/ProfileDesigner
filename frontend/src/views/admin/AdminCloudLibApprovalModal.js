import React, { useState } from 'react'
import Modal from 'react-bootstrap/Modal'
import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'

import { SVGIcon } from '../../components/SVGIcon'

//const CLASS_NAME = "AdminCloudLibApprovalModal";

function AdminCloudLibApprovalModal(props) { //props are item and config.callback

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [showModal, setShowModal] = useState(props.showModal);
    const [_formData, setFormData] = useState({
        description: ''
    });
    const [_isValid, setIsValid] = useState({ description: true });

    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onHide = (e) => {
        setShowModal(false);
        if (props.cancel.callback != null) props.cancel.callback(e);
    };

    const onConfirm = (e) => {
        //check required fields
        const isValid = !(_formData.description == null || _formData.description === '')
        if (!isValid) {
            setIsValid({ description: isValid });
            return;
        }

        //close modal if all good
        setShowModal(false);
        if (props.confirm.callback != null) props.confirm.callback(_formData);
    };

    const onChange = (e) => {
        e.preventDefault();
        setFormData({
            ..._formData,
            [e.target.name]: e.target.value
        });
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            {/* Add animation=false to prevent React warning findDomNode is deprecated in StrictMode*/}
            <Modal key={props.MsgId} animation={false} show={showModal} onHide={onHide} data-id={props.msgId} centered>
                <Modal.Header closeButton>
                    <Modal.Title>
                        {props.icon != null &&
                            <SVGIcon name={props.icon.name} size="36" fill={props.icon.color} className="mr-2" />
                        }
                        {(props.caption == null || props.caption === '') ?
                            "Confirm" : props.caption
                        }
                    </Modal.Title>
                </Modal.Header>
                <Modal.Body className="my-3">
                    <p>
                        {(props.message == null || props.message === '') ?
                            "Are you sure?" : props.message
                        }
                    </p>
                    <Form className={`header-search-block mx-3"`}>
                        <Form.Group className="mb-1">
                            <Form.Label>Reviewer Comments*</Form.Label>
                            {!_isValid.description &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            <Form.Control
                                as="textarea"
                                name="description"
                                type="text"
                                aria-label="Reason"
                                onChange={onChange}
                                value={_formData.description}
                            />
                        </Form.Group>
                    </Form>
                </Modal.Body>
                <Modal.Footer>
                    <Button variant="text-solo" onClick={onHide}  >
                        Cancel
                    </Button>
                    <Button disabled={_formData.description == null} style={{ minWidth: '128px' }} onClick={onConfirm} >
                        {(props.confirm?.caption == null || props.confirm?.caption === '') ?
                            "Change Status" : props.confirm.caption
                        }
                    </Button>
                </Modal.Footer>
            </Modal>
        </>
    );

};

export default AdminCloudLibApprovalModal;