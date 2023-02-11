import React, { useState } from 'react'
import Modal from 'react-bootstrap/Modal'
import Form from 'react-bootstrap/Form'
import FormControl from 'react-bootstrap/FormControl'
import Button from 'react-bootstrap/Button'
import { AppSettings } from '../../utils/appsettings';

import { SVGIcon } from '../../components/SVGIcon'

//const CLASS_NAME = "AdminCloudLibApprovalModal";

function AdminCloudLibApprovalModal(props) { //props are item and config.callback

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [showModal, setShowModal] = useState(props.showModal);
    const [_formData, setFormData] = useState({
        approvalStatus: props.item.cloudLibApprovalStatus,
        description: props.item.cloudLibApprovalDescription
    });
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
                        <div className="col-sm-6">
                            <Form.Group className="mb-1">
                                <Form.Label>Status</Form.Label>
                                <Form.Control
                                    name="approvalStatus"
                                    as="select"
                                    placeholder="Status"
                                    aria-label="Status"
                                    onChange={onChange}
                                    value={_formData.approvalStatus}
                                >
                                    <option value={AppSettings.PublishProfileStatus.Approved}>Approve</option>
                                    <option value={AppSettings.PublishProfileStatus.Rejected}>Reject</option>
                                    <option value={AppSettings.PublishProfileStatus.Canceled}>Cancel</option>
                                    <option value={AppSettings.PublishProfileStatus.Pending}>Pending</option>
                                </Form.Control>
                            </Form.Group>
                        </div>
                        <div className="col-md-12">
                            <Form.Group className="mb-1">
                                <Form.Label>Explanation</Form.Label>
                                <Form.Control
                                    name="description"
                                    type="text"
                                    placeholder="Rejection Reason"
                                    aria-label="Rejection Reason"
                                    onChange={onChange}
                                    value={_formData.description}
                                />
                            </Form.Group>
                        </div>
                        <Form.Group>
                        </Form.Group>
                    </Form>
                </Modal.Body>
                <Modal.Footer>
                    <Button variant="text-solo" onClick={onHide}  >
                        Cancel
                    </Button>
                    <Button disabled={_formData.approvalStatus == null} style={{ minWidth: '128px' }} onClick={onConfirm} >
                        Change Status
                    </Button>
                </Modal.Footer>
            </Modal>
        </>
    );

};

export default AdminCloudLibApprovalModal;