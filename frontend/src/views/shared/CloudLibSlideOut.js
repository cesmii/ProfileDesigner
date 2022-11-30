import React from 'react'
import Button from 'react-bootstrap/Button'

import { generateLogMessageString } from '../../utils/UtilityService'
import CloudLibraryImporter from "../shared/CloudLibraryImporter";

import '../../components/styles/RightPanel.scss';

const CLASS_NAME = "CloudLibSlideOut";

function CloudLibSlideOut(props) { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const _title = "Import from Cloud Library";

    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const closePanel = (e) => {
        console.log(generateLogMessageString(`closePanel||Current state:${props.isOpen ? "open" : "closed"}`, CLASS_NAME));
        props.onClosePanel(false, null);
    };

    const onImportStarted = (importLogId) => {
        console.log(generateLogMessageString(`onImportStarted`, CLASS_NAME));
        if (props.onImportStarted) props.onImportStarted(importLogId);
    }

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderHeading = () => {
        return (
            <div className="row right-panel-header d-flex align-items-center mx-0 mb-2 pl-3 pr-2">
                <div className="header-title-block d-flex align-items-center">
                    <span className="mr-3">
                        <i className="material-icons">search</i>
                    </span>
                    <span className="headline-2 font-weight-bold">{_title}</span>
                </div>
                <div className="d-flex align-items-center ml-auto" >
                    <Button variant="icon-solo" onClick={closePanel} className="align-items-center">
                        <span>
                            <i className="material-icons text-white">close</i>
                        </span>
                    </Button>
                </div>
            </div>
        );
    }


    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    const cssClass = "slide-in-panel " + (props.isOpen ? " open" : "") + " cloud-lib-list";

    //always render it so we can take advantage of a show/hide slide out effect
    return (
        <>
            <div className={cssClass} >
                {renderHeading()}
                <div className="header-actions-row mx-3 mb-3 pr-0">
                    <p className="mb-2" >
                        Search the CESMII Cloud Library for Profiles and import Profiles into the Profile Library.
                    </p>
                    <p className="mb-2" >
                        Type definitions within these Profiles can then be viewed or extended to make new Type definitions, which can become part of one of your SM Profiles.
                    </p>
                </div>

                <CloudLibraryImporter onImportStarted={onImportStarted} isOpen={props.isOpen} />
            </div>
        </>
    );
}

export default CloudLibSlideOut;