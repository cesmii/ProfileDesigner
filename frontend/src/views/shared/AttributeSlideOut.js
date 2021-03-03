import React from 'react'
import Button from 'react-bootstrap/Button'

import AttributeList from './AttributeList';
import AttributeEntity from './AttributeEntity'
import { generateLogMessageString } from '../../utils/UtilityService'

import { SVGIcon } from '../../components/SVGIcon'
import color from '../../components/Constants'
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
                
                <div className="p-3 pb-2 mb-3 right-panel-header">
                    <Button variant="icon-solo" onClick={closePanel} className="align-items-center" >
                        <span>
                            <SVGIcon name="close" size="24" fill={color.shark} />
                        </span>
                    </Button>

                    {(props.item != null && !props.showDetail) &&
                        <p className="h5 m-0 p-0 mt-2">{props.item == null || props.item === {} ? "" : props.item.name}</p>
                    }
                </div>

                <div className="col-sm-12">
                    {(props.item != null && !props.showDetail) &&
                        <AttributeList profile={props.item} profileAttributes={props.item.profileAttributes} extendedProfileAttributes={props.item.extendedProfileAttributes} readOnly={true} />
                    }
                    {(props.item != null && props.showDetail) &&
                        <AttributeEntity item={props.item} allAttributes={props.allAttributes} readOnly={props.readOnly}
                        onUpdate={onUpdate} lookupDataTypes={props.lookupDataTypes} onClosePanel={props.onClosePanel} />
            }
                </div>
            </div>
        </>
    );
}

export default AttributeSlideOut;