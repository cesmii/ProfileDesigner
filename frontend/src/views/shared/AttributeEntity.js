import React, { useState } from 'react'
import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'

import { SVGIcon } from '../../components/SVGIcon'
import color from '../../components/Constants'
//import color from './Constants'
import { generateLogMessageString, onChangeNumericKeysOnly, validateNumeric, convertToNumeric, toInt } from '../../utils/UtilityService'
import { LookupData } from '../../utils/appsettings';
import { validate_name, validate_nameDuplicate, validate_dataType, validate_minMax, validate_engUnit, validate_All } from '../../services/AttributesService';

const CLASS_NAME = "AttributeEntity";

function AttributeEntity(props) { //props are item, showActions
    
    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const initEditSettings = () => {
        if (props.isHeader) return null;
        if (props.lookupDataTypes == null || props.lookupDataTypes.length === 0) {
            return {
                useMinMax: true,
                useEngUnit: true,
                changeDataType: props.item.dataType !== "composition" && props.item.dataType !== "interface",
                isVariableType: props.item.variableType != null
            };
        }
        var lookupItem = props.lookupDataTypes.find(dt => { return dt.val === props.item.dataType; });
        return {
            useMinMax: lookupItem != null && lookupItem.useMinMax,
            useEngUnit: lookupItem != null && lookupItem.useEngUnit,
            changeDataType: props.item.dataType !== "composition" && props.item.dataType !== "interface",
            isVariableType: props.item.variableType != null
        };
    };

    var [_editItem, setEditItem] = useState(JSON.parse(JSON.stringify(props.item)));
    const [_isValid, setIsValid] = useState({
        name: true,
        nameDuplicate: true,
        dataType: true,
        minMax: true,
        minIsNumeric: true,
        maxIsNumeric: true,
        instrumentMinMax: true,
        instrumentMinIsNumeric: true,
        instrumentMaxIsNumeric: true,
        engUnit: true
    });
    const [_editSettings, setEditSettings] = useState(initEditSettings());

    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------
    const validateForm_name = (e) => {
        var isValid = validate_name(e.target.value, _editItem);
        //dup check
        var isValidDup = validate_nameDuplicate(e.target.value, _editItem, props.allAttributes);
        setIsValid({ ..._isValid, name: isValid, nameDuplicate: isValidDup });
    };

    const validateForm_dataType = (e) => {
        setIsValid({ ..._isValid, dataType: validate_dataType(e.target.value) });
    };

    const validateForm_minMax = (e) => {
        var isValid = validate_minMax(_editItem.minValue, _editItem.maxValue, _editItem.dataType, _editSettings);
        var minIsNumeric = !_editSettings.useMinMax || _editItem.minValue == null || validateNumeric(_editItem.dataType, _editItem.minValue);
        var maxIsNumeric = !_editSettings.useMinMax || _editItem.maxValue == null || validateNumeric(_editItem.dataType, _editItem.maxValue);

        setIsValid({ ..._isValid, minMax: isValid, minIsNumeric: minIsNumeric, maxIsNumeric: maxIsNumeric });
    };

    const validateForm_instrumentMinMax = (e) => {
        var isValid = validate_minMax(_editItem.instrumentMinValue, _editItem.instrumentMaxValue, _editItem.dataType, _editSettings);
        var minIsNumeric = !_editSettings.useMinMax || _editItem.instrumentMinValue == null || validateNumeric(_editItem.dataType, _editItem.instrumentMinValue);
        var maxIsNumeric = !_editSettings.useMinMax || _editItem.instrumentMaxValue == null || validateNumeric(_editItem.dataType, _editItem.instrumentMaxValue);

        setIsValid({ ..._isValid, instrumentMinMax: isValid, instrumentMinIsNumeric: minIsNumeric, instrumentMaxIsNumeric: maxIsNumeric });
    };

    const validateForm_engUnit = (e) => {
        setIsValid({ ..._isValid, engUnit: validate_engUnit(e.target.value, _editSettings) });
    };

    //validate all - call from button click
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        var isValid = validate_All(_editItem, _editSettings, props.allAttributes);

        setIsValid(JSON.parse(JSON.stringify(isValid)));
        return (isValid.name && isValid.nameDuplicate && isValid.minMax && isValid.dataType && isValid.engUnit
            && isValid.minIsNumeric && isValid.maxIsNumeric);
    }

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onUpdateClick = (e) => {
        console.log(generateLogMessageString(`onUpdateClick||id:${props.item.id}`, CLASS_NAME));

        //validate form
        if (!validateForm()) {
            //alert("validation failed");
            return;
        }

        //convert min, max to number
        if (_editSettings.useMinMax) {
            _editItem.minValue = convertToNumeric(_editItem.dataType, _editItem.minValue);
            _editItem.maxValue = convertToNumeric(_editItem.dataType, _editItem.maxValue);
        }

        //bubble up to parent
        props.onUpdate(_editItem);

        props.onClosePanel(false, null);
    };

    //onchange data type
    const onChangeDataType = (e) => {
        //console.log(generateLogMessageString(`onAttributeAddChangeDataType||e:${e.target}`, CLASS_NAME));
        var isVariableType = false;
        var useMinMax = false;
        var useEngUnit = false;
        if (e.target.value == null || e.target.value.toString() === "-1") {
            useMinMax = true;
            useEngUnit = true;
        }
        else {
            var lookupItem = props.lookupDataTypes.find(dt => { return dt.val === e.target.value; });
            isVariableType = lookupItem != null && lookupItem.isVariableType;
            useMinMax = lookupItem != null && lookupItem.useMinMax;
            useEngUnit = lookupItem != null && lookupItem.useEngUnit;
        }

        //update state
        setEditSettings({ ..._editSettings, useMinMax: useMinMax, useEngUnit: useEngUnit, isVariableType: isVariableType });

        //clear out vals based on data type
        _editItem.variableType = isVariableType ? { id: parseInt(e.target.value), name: e.target.options[e.target.selectedIndex].text } : null;
        _editItem.variableTypeId = isVariableType ? parseInt(e.target.value) : null;
        _editItem.minValue = useMinMax ? _editItem.minValue : null;
        _editItem.maxValue = useMinMax ? _editItem.maxValue : null;
        _editItem.engUnit = useEngUnit ? _editItem.engUnit : null;

        //call common change method
        onChange(e);
    }

    //onchange numeric field
    const onChangeMinMax = (e) => {
        // Perform a numeric only check for numeric fields.
        if (!onChangeNumericKeysOnly(e)) {
            e.preventDefault();
            return;
        }

        //convert to int - this will convert '10.' to '10' to int
        if (_editItem.dataType === 'integer' || _editItem.dataType === 'long') {
            e.target.value = toInt(e.target.value);
        }

        //call commonn change method
        onChange(e);
    }

    //onchange - update state on change
    const onCheckChange = (e) => {
        _editItem[e.target.id] = e.target.checked;
        setEditItem(JSON.parse(JSON.stringify(_editItem)));
    }

    //onchange - update state on change
    const onChange = (e) => {
        //TBD - remove this check for now because the model is evolving during dev
        ////check existence of field
        //if (e.target.id in _editItem === false) {
        //    console.warn(generateLogMessageString(`onChange||Unknown column:${e.target.id}. Id value should match a valid property value.`, CLASS_NAME));
        //    return;
        //}
        _editItem[e.target.id] = e.target.value;
        setEditItem(JSON.parse(JSON.stringify(_editItem)));
    }

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderIcon = () => {
        //set up color properly
        //var iconColor = (currentUserId == null || currentUserId !== item.author.id) ? color.silver : color.shark;
        var iconColor = props.item._itemType == null || props.item._itemType === "profile" ? color.shark : color.silver;

        //set up icon properly
        var iconName = props.item._itemType == null || props.item._itemType === "profile" ? "account-circle" : "group";

        if (props.item.dataType === "composition") iconName = "profile";

        //variable type - special icon type
        if (props.item.variableType != null) iconName = "variabletype";

        if (props.item.interface != null) iconName = "key";

        return (
            <span className="mr-2">
                <SVGIcon name={iconName} size="24" fill={iconColor} />
            </span>
        );
    }

    //render the attribute name, append some stuff for certain types of attributes
    const renderNameUI = () => {
        return (
            <Form.Group className="flex-grow-1 align-self-center">
                {renderIcon()}
                <Form.Label className="mb-0" >Name</Form.Label>
                {!_isValid.name &&
                    <span className="invalid-field-message inline">
                        Required
                    </span>
                }
                {!_isValid.nameDuplicate &&
                    <span className="invalid-field-message inline">
                        Duplicate
                    </span>
                }
                <Form.Control id="name" type="" placeholder="Enter a name" value={_editItem.name}
                    onChange={onChange} onBlur={validateForm_name}
                    className={(!_isValid.name || !_isValid.nameDuplicate ? 'invalid-field' : '')} />
            </Form.Group>
        );
    };

    //render the attribute name, append some stuff for certain types of attributes
    const renderName = () => {
        if (props.readOnly || props.item._itemType == null || props.item._itemType === "extended" || props.item.interface != null) {
            if (props.item.interface != null) {
                return (
                    <>
                        {renderIcon()}
                        {props.item.name} [<a href={`/profile/${props.item.interface.id}`} >{props.item.interface.name}</a>]
                    </>
                );
            }
            //simple scenario
            return (
                <>
                    {renderIcon()}
                    {props.item.name}
                </>
            );
        }
        //edit mode
        else {
            return renderNameUI();
        }
    };
    

    //render the description ui
    const renderDescription = () => {
        var isReadOnly = (props.readOnly || props.item._itemType == null || props.item._itemType === "extended" || props.item.interface != null);
        return (
            <Form.Group className="flex-grow-1 align-self-center">
                <Form.Label className="mb-0" >Description</Form.Label>
                <Form.Control id="description" type="" placeholder="Enter a description" value={_editItem.description} readOnly={isReadOnly}
                    onChange={onChange} />
            </Form.Group>
        );
    };

    //render the displayName ui
    const renderDisplayName = () => {
        var isReadOnly = (props.readOnly || props.item._itemType == null || props.item._itemType === "extended" || props.item.interface != null);

        return (
            <Form.Group className="flex-grow-1 align-self-center">
                <Form.Label className="mb-0" >Display Name</Form.Label>
                <Form.Control id="displayName" type="" placeholder="Enter a display name" value={_editItem.displayName} readOnly={isReadOnly}
                    onChange={onChange} />
            </Form.Group>
        );
    };

    //render data type ui
    const renderDataType = () => {
        if (props.lookupDataTypes == null || props.lookupDataTypes.length === 0) return;

        var isReadOnly = props.readOnly || props.item._itemType == null || props.item._itemType === "extended" || props.item.interface != null ||
            !_editSettings.changeDataType;

        const options = props.lookupDataTypes.map((item) => {
            //skip interface, composition types in edit mode
            if (item.val === 'composition' || item.val === 'interface') {
                return null;
            }
            else {
                return (<option key={item.val} value={item.val} >{item.caption}</option>)
            }
        });

        //grab the associated caption when showing in read only mode
        var selItem = (props.lookupDataTypes == null || props.lookupDataTypes.length === 0) ? null :
            props.lookupDataTypes.find(x => { return x.val === _editItem.dataType });
        var selectedText = "";
        if (props.item.dataType == null || props.item.dataType.toString() === "-1") selectedText = "";
        else
            selectedText = selItem == null ? props.item.dataType : selItem.caption;

        return (
            <Form.Group className="flex-grow-1 align-self-center" >
                <Form.Label className="mb-0" >Data Type</Form.Label>
                {
                    !_isValid.dataType &&
                    <span className="invalid-field-message inline">
                        Required
                    </span>
                }

                {isReadOnly ?
                    <Form.Control id="dataType" value={selectedText} readOnly={isReadOnly} />
                    :
                    <Form.Control id="dataType" as="select" value={_editItem.dataType}
                        onChange={onChangeDataType} onBlur={validateForm_dataType}
                        className={(!_isValid.dataType ? 'invalid-field minimal pr-5' : 'minimal pr-5')} >
                        <option key="-1|Select One" value="-1" >Select</option>
                        {options}
                    </Form.Control>
                }
            </Form.Group>
        );
    };

    //render the historizing ui
    const renderIsArray = () => {
        var isReadOnly = (props.readOnly || props.item._itemType == null || props.item._itemType === "extended" || props.item.interface != null);

        return (
            <Form.Group className="flex-grow-1 align-self-center">
                <Form.Check type="checkbox" id="isArray" label="Is Array" checked={_editItem.isArray} onChange={onCheckChange}
                    disabled={isReadOnly ? "disabled" : ""} />
            </Form.Group>
        );
    };

    //render the historizing ui
    const renderHistorizing = () => {
        var isReadOnly = (props.readOnly || props.item._itemType == null || props.item._itemType === "extended" || props.item.interface != null);

        return (
            <Form.Group className="flex-grow-1 align-self-center">
                <Form.Check type="checkbox" id="historizing" label="Historizing" checked={_editItem.historizing} onChange={onCheckChange}
                    disabled={isReadOnly ? "disabled" : ""} />
            </Form.Group>
        );
    };

    //render the attribute min, append some stuff for certain types of attributes
    const renderMin = () => {
        var isReadOnly = (props.readOnly || props.item._itemType == null || props.item._itemType === "extended"
            || (props.item.variableType != null && props.item.dataType === props.item.variableType.id.toString()) //extra logic allows for user to change data type in inline edit
            || props.item._itemType === "composition" || props.item.interface != null ||
            !_editSettings.useMinMax)

        var tip = !_isValid.minMax ? 'Min > Max.' : '';
        tip = !_isValid.minIsNumeric ? tip + ' Invalid (ie. ####).' : tip;
        return (
            <Form.Group className="flex-grow-1">
                <Form.Label className="mb-0" >EU Min</Form.Label>
                {!_isValid.minMax &&
                    <span className="invalid-field-message inline">
                        Min &gt; Max
                        </span>
                }
                {!_isValid.minIsNumeric &&
                    <span className="invalid-field-message inline">
                        Invalid (ie. ####)
                        </span>
                }
                <Form.Control id="minValue" type="" value={_editItem.minValue} readOnly={isReadOnly}
                    onChange={onChangeMinMax} onBlur={validateForm_minMax} title={tip}
                    className={(!_isValid.minMax || !_isValid.minIsNumeric ? 'invalid-field' : '')} />
            </Form.Group>
        );
    };

    //render the attribute max, append some stuff for certain types of attributes
    const renderMax = () => {
        var isReadOnly = (props.readOnly || props.item._itemType == null || props.item._itemType === "extended"
            || (props.item.variableType != null && props.item.dataType === props.item.variableType.id.toString()) //extra logic allows for user to change data type in inline edit
            || props.item._itemType === "composition" || props.item.interface != null ||
            !_editSettings.useMinMax);

        var tip = !_isValid.minMax ? 'Min > Max.' : '';
        tip = !_isValid.maxIsNumeric ? tip + ' Invalid (ie. ####).' : tip;

        return (
            <Form.Group className="flex-grow-1">
                <Form.Label className="mb-0" >EU Max</Form.Label>
                {!_isValid.maxIsNumeric &&
                    <span className="invalid-field-message inline">
                        Invalid (ie. ####)
                        </span>
                }
                <Form.Control id="maxValue" type="" value={_editItem.maxValue} readOnly={isReadOnly}
                    onChange={onChangeMinMax} onBlur={validateForm_minMax} title={tip}
                    className={(!_isValid.minMax || !_isValid.maxIsNumeric ? 'invalid-field' : '')} />
            </Form.Group>
        );
    };

    //instrument Min
    const renderInstrumentMin = () => {
        var isReadOnly = (props.readOnly || props.item._itemType == null || props.item._itemType === "extended"
            || (props.item.variableType != null && props.item.dataType === props.item.variableType.id.toString()) //extra logic allows for user to change data type in inline edit
            || props.item._itemType === "composition" || props.item.interface != null ||
            !_editSettings.useMinMax)

        var tip = !_isValid.instrumentMinMax ? 'Min > Max.' : '';
        tip = !_isValid.instrumentMinIsNumeric ? tip + ' Invalid (ie. ####).' : tip;
        return (
            <Form.Group className="flex-grow-1">
                <Form.Label className="mb-0" >Instrument Min</Form.Label>
                {!_isValid.instrumentMinMax &&
                    <span className="invalid-field-message inline">
                        Min &gt; Max
                    </span>
                }
                {!_isValid.instrumentMinIsNumeric &&
                    <span className="invalid-field-message inline">
                        Invalid (ie. ####)
                    </span>
                }
                <Form.Control id="instrumentMinValue" type="" value={_editItem.instrumentMinValue} readOnly={isReadOnly}
                    onChange={onChangeMinMax} onBlur={validateForm_instrumentMinMax} title={tip}
                    className={(!_isValid.instrumentMinMax || !_isValid.instrumentMinIsNumeric ? 'invalid-field' : '')} />
            </Form.Group>
        );
    };

    //instrument max
    const renderInstrumentMax = () => {
        var isReadOnly = (props.readOnly || props.item._itemType == null || props.item._itemType === "extended"
            || (props.item.variableType != null && props.item.dataType === props.item.variableType.id.toString()) //extra logic allows for user to change data type in inline edit
            || props.item._itemType === "composition" || props.item.interface != null ||
            !_editSettings.useMinMax);

        var tip = !_isValid.instrumentMinMax ? 'Min > Max.' : '';
        tip = !_isValid.instrumentMaxIsNumeric ? tip + ' Invalid (ie. ####).' : tip;

        return (
            <Form.Group className="flex-grow-1">
                <Form.Label className="mb-0" >Instrument Max</Form.Label>
                {!_isValid.instrumentMaxIsNumeric &&
                    <span className="invalid-field-message inline">
                        Invalid (ie. ####)
                        </span>
                }
                <Form.Control id="instrumentMaxValue" type="" value={_editItem.instrumentMaxValue} readOnly={isReadOnly}
                    onChange={onChangeMinMax} onBlur={validateForm_instrumentMinMax} title={tip}
                    className={(!_isValid.instrumentMinMax || !_isValid.instrumentMaxIsNumeric ? 'invalid-field' : '')} />
            </Form.Group>
        );
    };

    //render the eng unit input
    const renderEngUnit = () => {
        var isReadOnly = (props.readOnly || props.item._itemType == null || props.item._itemType === "extended"
            || (props.item.variableType != null && props.item.dataType === props.item.variableType.id.toString()) //extra logic allows for user to change data type in inline edit
            || props.item._itemType === "composition" || props.item.interface != null ||
            !_editSettings.useEngUnit);
        const options = LookupData.engUnits.map((item) => {
            return (<option key={item.val} value={item.val} >{item.caption}</option>)
        });
        
        //grab the associated caption when showing in read only mode
        var selItem = LookupData.engUnits.find(x => { return x.val === _editItem.engUnit });
        var selectedText = _editItem.engUnit == null || _editItem.engUnit.toString() === "-1" || selItem == null ? '' :
            selItem.caption;

        return (
            <Form.Group className="flex-grow-1" >
                <Form.Label className="mb-0" >Eng unit</Form.Label>
                {!_isValid.engUnit &&
                    <span className="invalid-field-message inline">
                        Required
                    </span>
                }
                {isReadOnly ?
                    <Form.Control id="engUnit" value={selectedText} readOnly={isReadOnly} />
                    :
                    <Form.Control id="engUnit" as="select" value={_editItem.engUnit} readOnly={isReadOnly}
                        onChange={onChange} onBlur={validateForm_engUnit}
                        className={(!_isValid.engUnit ? 'invalid-field minimal pr-5' : 'minimal pr-5')} >
                        <option key="-1|Select One" value="-1" >Select</option>
                        {options}
                    </Form.Control>
                }
            </Form.Group>
        );
    };

    //render the actions col. in edit mode, we swap out the icons
    const renderButtons = () => {

        if (props.readOnly || props.item._itemType == null || props.item._itemType === "extended" ) return;

        //return edit/delete for editable grid
        return (
            <>
                <Button variant="text-solo" className="mx-1" onClick={() => { props.onClosePanel(false, null); } } >Cancel</Button>
                <Button variant="secondary" type="button" className="mx-3" onClick={onUpdateClick} >Update</Button>
            </>
        );
    };

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    //only do this for non header rows
    if (props.item === null || props.item === {}) return null;
    if (props.item.name == null) return null;

    //grid row
    return (
        <>
            <Form noValidate>
                <div class="row mb-3" >
                    <div class="col-sm-12" >
                        {renderName()}
                    </div>
                </div>
                <div class="row mb-3" >
                    <div class="col-sm-12" >
                        {renderDataType()}
                    </div>
                </div>
                <div class="row mb-3" >
                    <div class="col-sm-12" >
                        {renderDisplayName()}
                    </div>
                </div>
                <div class="row mb-3" >
                    <div class="col-sm-12" >
                        {renderDescription()}
                    </div>
                </div>
                <div class="row mb-3" >
                    <div class="col-sm-6" >
                        {renderIsArray()}
                    </div>
                    <div class="col-sm-6" >
                        {renderHistorizing()}
                    </div>
                </div>
                <div class="row mb-3" >
                    <div class="col-sm-6" >
                        {renderInstrumentMin()}
                    </div>
                    <div class="col-sm-6" >
                        {renderInstrumentMax()}
                    </div>
                </div>
                <div class="row mb-3" >
                    <div class="col-sm-6" >
                        {renderMin()}
                    </div>
                    <div class="col-sm-6" >
                        {renderMax()}
                    </div>
                </div>
                <div class="row mb-3" >
                    <div class="col-sm-12" >
                        {renderEngUnit()}
                    </div>
                </div>
                <div className="d-flex mt-4 align-items-center justify-content-end">
                    {renderButtons()}
                </div>
            </Form>
        </>
    );
};

export default AttributeEntity;