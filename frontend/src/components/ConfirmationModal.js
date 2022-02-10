import React, { useState } from 'react'
import Button from 'react-bootstrap/Button'
import Modal from 'react-bootstrap/Modal'

import { SVGIcon } from './SVGIcon'
//import color from './Constants'
//import { generateLogMessageString } from '../utils/UtilityService'

//const CLASS_NAME = "ConfirmationModal";

function ConfirmationModal(props) { //props are item, showActions
    
    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [showModal, setShowModal] = useState(props.showModal);

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
        if (props.confirm.callback != null) props.confirm.callback(e);
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
            <Modal animation={false} show={showModal} onHide={onHide} centered>
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
                    {(props.message == null || props.message === '') ?
                        "Are you sure?" : props.message
                    }
                </Modal.Body>
                <Modal.Footer>
                    {props.cancel != null &&
                    <Button variant={props.cancel == null || props.cancel.buttonVariant == null ? "text-solo" : props.cancel.buttonVariant}
                        onClick={onHide}>
                        {(props.cancel == null || props.cancel.caption == null || props.cancel.caption === '') ?
                            "Cancel" : props.cancel.caption
                        }
                    </Button>
                    }
                    {props.confirm != null &&
                        <Button variant={props.confirm == null || props.confirm.buttonVariant == null ? "" : props.confirm.buttonVariant}
                            style={{ minWidth: '128px' }} onClick={onConfirm} >
                            {(props.confirm == null || props.confirm.caption == null || props.confirm.caption === '') ?
                                "OK" : props.confirm.caption
                            }
                        </Button>
                    }
                </Modal.Footer>
            </Modal>
        </>
    );

};

export default ConfirmationModal;