import React from 'react'
import Form from 'react-bootstrap/Form'
import { Button } from 'react-bootstrap'

import { generateLogMessageString } from '../../utils/UtilityService';
import { SVGIcon } from '../../components/SVGIcon'
import { LookupData } from '../../utils/appsettings';
import '../styles/AdvancedSearch.scss';

const CLASS_NAME = "AdvancedSearchRow";

function AdvancedSearchRow(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------
    const validateForm_fieldName = (e) => {
        var isValid = e.target.value != null && e.target.value.toString() !== "-1";
        props.item.isValid = { ...props.item.isValid, fieldName: isValid };
        //bubble up
        props.onChange(props.item);
        //setIsValid({ ...props.item.isValid, fieldName: isValid });
    };

    const validateForm_operator = (e) => {
        //there is a dependency, don't make the operator appear invalid if the field name is not set yet.
        //if (props.item.fieldName == null || props.item.fieldName.toString() === "-1") return true;

        var isValid = e.target.value != null && e.target.value.toString() !== "-1";
        props.item.isValid = { ...props.item.isValid, operator: isValid };
        //bubble up
        props.onChange(props.item);
        //setIsValid({ ...props.item.isValid, operator: isValid });
    };

    const validateForm_val = (e) => {
        var isValid = e.target.value != null && e.target.value.trim() !== "";
        props.item.isValid = { ...props.item.isValid, val: isValid };
        //bubble up
        props.onChange(props.item);
        //setIsValid({ ...props.item.isValid, value: isValid });
    };

    const isValidAll = () => {
        return props.item.isValid.fieldName && props.item.isValid.operator && props.item.isValid.val;
    };

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    //on search criteria change
    const onChange = (e) => {
        console.log(generateLogMessageString('onChange', CLASS_NAME));
        props.item[e.target.id] = e.target.value;

        if (e.target.id === "fieldName") {
            //set the data type hidden field so that the operator knows what to show/hide
            var match = LookupData.searchFields.find(p => {return p.val === e.target.value;});
            props.item.dataType = match == null ? props.item.dataType : match.dataType;
        }

        //bubble up
        props.onChange(props.item);
    }

    const onAddSearchRow = () => {
        //bubble up to parent item
        props.onAdd();
    }

    const onDeleteSearchRow = (id) => {
        //bubble up to parent item
        props.onDelete(id);
    };

    //render the field name input
    const renderFieldNameDDL = (item) => {
        const options = LookupData.searchFields.map((item) => {
            return (<option key={item.val} value={item.val} >{item.caption}</option>)
        });

        return (
            <Form.Control id="fieldName" as="select" value={item.fieldName}
                onChange={onChange} onBlur={validateForm_fieldName}
                className={(!props.item.isValid.fieldName ? 'invalid-field minimal pr-5' : 'minimal pr-5')}  >
                <option key="-1|Select One" value="-1" >Select</option>
                {options}
            </Form.Control>
        );
    };

    //render the operator input
    const renderOperatorDDL = (item) => {

        //filter based on fieldName look up data type val
        var filteredOperators = LookupData.searchOperators.filter((o) => { return o.dataType === item.dataType; })
        const options = (item.fieldName == null || item.fieldName === '-1' || item.fieldName === -1) ? "" :
            filteredOperators.map((item) => {
                return (<option key={item.val} value={item.val} >{item.caption}</option>)
            });

        //enable once name is chosen
        return (
            <Form.Control id="operator" as="select" value={item.operator}
                onChange={onChange} onBlur={validateForm_operator}
                disabled={item.fieldName == null || item.fieldName === '-1' || item.fieldName === -1 ? 'disabled' : ''}
                className={(!props.item.isValid.operator ? 'invalid-field minimal pr-5' : 'minimal pr-5')} >
                <option key="-1|Select One" value="-1" >Select</option>
                {options}
            </Form.Control>
        );
    };

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    //header row - controlled by props flag
    if (props.isHeader) {
        return (
            <div key="searchCriteria_header" className='row header bottom no-border pb-2' >
                <div className="col col-30 left" >Field</div>
                <div className="col col-30 left" >Operator</div>
                <div className="col auto-size left" >Value</div>
                <div className="col col-x-small right nowrap" ></div>
                <div className="col col-x-small right nowrap" ></div>
            </div>
        );
    }

    //only do this for non header rows
    if (props.item === null || props.item === {}) return null;
    if (props.item.id == null) return null;

    var cssClass = "row " + props.cssClass;

    //grid row
    return (
        <div className={cssClass} key={`searchrow_${props.i}`} >
            <div className="col col-30 left" >{renderFieldNameDDL(props.item)}</div>
            <div className="col col-30 left" >{renderOperatorDDL(props.item)}</div>
            <div className="col auto-size left" >
                <Form.Control id="val" onBlur={validateForm_val}
                    className={(!props.item.isValid.val ? 'invalid-field me-3' : 'me-3')} 
                    type="" placeholder="Criteria" value={props.item.val} onChange={onChange} />
            </div>
            <div className="col col-x-small right nowrap" >
                {isValidAll() &&
                    <Button variant="inline-add" aria-label="Add criterion" onClick={onAddSearchRow} disabled={!isValidAll() ? 'disabled' : ''} >
                        <span>
                            <SVGIcon name="add" />
                        </span>
                    </Button>
                }
            </div>
            <div className="col col-x-small right nowrap" >
                {props.i > 0 &&
                    <Button variant="inline-add" aria-label="Delete criterion" onClick={() => onDeleteSearchRow(props.item.id)}>
                        <span>
                            <SVGIcon name="trash" />
                        </span>
                    </Button>
                }
            </div>
        </div>
    );
}

export default AdvancedSearchRow