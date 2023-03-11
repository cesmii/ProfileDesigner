import React from 'react'
import Button from 'react-bootstrap/Button'

import AttributeList from './AttributeList';
import AttributeEntity from './AttributeEntity'
import { generateLogMessageString } from '../../utils/UtilityService'

import { SVGIcon } from '../../components/SVGIcon'
import '../../components/styles/RightPanel.scss';
 
const CLASS_NAME = "AttributeSlideOut";

function AttributeSlideOut(props) { //props are item, showActions
    
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

    const onUpdate = (item) => {
        //raised from update button click in child component
        console.log(generateLogMessageString(`onUpdate||item id:${item.id}`, CLASS_NAME));

        //call parent to update item collection, update state
        //var attributes = props.onAttributeUpdate(item);
        props.onUpdate(item);
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    var cssClass = "slide-in-panel " + (props.isOpen ? " open" : "") ;

    //always render it so we can take advantage of a show/hide slide out effect
    return (
        <>
            <div className={cssClass} >
                
                <div className="m-0 mb-3 p-3 pb-2 right-panel-header row">
                    {(props.item != null && !props.showDetail) &&
                        <div className="h5 d-flex m-0 align-items-center">{props.item == null || props.item === {} ? "" : props.item.name}</div>
                    }
                    <div className="d-flex align-items-center ml-auto" >
                        <Button variant="icon-solo" onClick={closePanel} className="align-items-center" >
                            <span>
                                <SVGIcon name="close" />
                            </span>
                        </Button>
                    </div>
                </div>

                <div className="col-sm-12">
                    {(props.item != null && !props.showDetail) &&
                        <AttributeList typeDefinition={props.item} profileAttributes={props.item.profileAttributes}
                            extendedProfileAttributes={props.item.extendedProfileAttributes} readOnly={true} isPopout={true}
                            activeAccount={props.activeAccount} />

                    }
                    {(props.item != null && props.showDetail) &&
                        <AttributeEntity item={props.item} allAttributes={props.allAttributes} readOnly={props.readOnly}
                        onUpdate={onUpdate} lookupDataTypes={props.lookupDataTypes} lookupVariableTypes={props.lookupVariableTypes}
                        lookupAttributeTypes={props.lookupAttributeTypes}
                        lookupCompositions={props.lookupCompositions} lookupInterfaces={props.lookupInterfaces}
                        lookupEngUnits={props.lookupEngUnits} onClosePanel={props.onClosePanel} />
            }
                </div>
            </div>
        </>
    );
}

export default AttributeSlideOut;