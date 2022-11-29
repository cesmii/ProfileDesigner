import React from 'react'
import Button from 'react-bootstrap/Button'

import { generateLogMessageString, renderTitleBlock } from '../../utils/UtilityService'

import CloudLibraryImporter from "../shared/CloudLibraryImporter";

import { SVGIcon } from '../../components/SVGIcon'
import '../../components/styles/RightPanel.scss';
import color from '../../components/Constants';

const CLASS_NAME = "CloudLibSlideOut";

function CloudLibSlideOut(props) { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------

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

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    var cssClass = "slide-in-panel " + (props.isOpen ? " open" : "") + " cloud-lib-list";

    //always render it so we can take advantage of a show/hide slide out effect
    return (
        <>
            <div className={cssClass} >
                <div className="header-title-block m-0 mb-3 p-3 pb-2 d-flex right-panel-header row">
                    {renderTitleBlock("Import from Cloud Library", "search", color.white)}
                    <div className="d-flex align-items-center ml-auto" >
                        <Button variant="icon-solo" onClick={closePanel} className="align-items-center" >
                            <span>
                                <SVGIcon name="close" fill={color.white} />
                            </span>
                        </Button>
                    </div>
                </div>
                <div className="header-actions-row mx-3 mb-3 pr-0">
                    <p className="mb-2" >
                        Search the CESMII Cloud Library for Profiles and import Profiles into the Profile Library.
                    </p>
                    <p className="mb-2" >
                        Type definitions within these Profiles can then be viewed or extended to make new Type definitions, which can become part of one of your SM Profiles.
                    </p>
                </div>

                <CloudLibraryImporter onImportStarted={onImportStarted} />
            </div>
        </>
    );
}

export default CloudLibSlideOut;