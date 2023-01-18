import React , { useState } from 'react'

import Modal from 'react-bootstrap/Modal'

import { generateLogMessageString } from '../../utils/UtilityService'

import '../styles/ProfileList.scss';
import '../../components/styles/InfoPanel.scss';
import CloudLibraryListGrid from '../shared/CloudLibraryListGrid';

const CLASS_NAME = "CloudLibraryImporterModal";

function CloudLibraryImporterModal(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [_showModal, setShowModal] = useState(props.showModal);

    //-------------------------------------------------------------------
    // Region: Add/Update event handlers
    //-------------------------------------------------------------------
    const onImportCanceled = () => {
        setShowModal(false);
        console.log(generateLogMessageString(`onImportCanceled`, CLASS_NAME));
        if (props.onImportCanceled) props.onImportCanceled();
    };

    const onImportStarted = (importLogId) => {
        setShowModal(false);
        console.log(generateLogMessageString(`onImportStarted`, CLASS_NAME));
        if (props.onImportStarted) props.onImportStarted(importLogId);
    }

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Modal animation={false} show={_showModal} onHide={onImportCanceled} size="xl" centered>
                <Modal.Header className="py-0 align-items-center" closeButton>
                    <Modal.Title>
                        <div>Import from Cloud Library</div>
                    </Modal.Title>
                </Modal.Header>
                <Modal.Body className="my-1 pt-0 pb-2">
                    <CloudLibraryListGrid onImportStarted={onImportStarted} />
                </Modal.Body>
            {/*    <Modal.Footer>*/}
            {/*        <Button variant="secondary" className="mx-1" onClick={onImportCanceled}>Close</Button>*/}
            {/*    </Modal.Footer>*/}
            </Modal>
        </>
    )
}

export default CloudLibraryImporterModal;
