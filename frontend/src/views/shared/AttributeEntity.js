import React, { useState } from 'react'
import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'

//import color from './Constants'
import { generateLogMessageString, onChangeNumericKeysOnly, validateNumeric, convertToNumeric, toInt } from '../../utils/UtilityService'
import { AppSettings } from '../../utils/appsettings';
import {
    validate_name, validate_nameDuplicate, validate_dataType, validate_minMax, validate_engUnit, validate_All,
    onChangeDataTypeShared, renderAttributeIcon, validate_attributeType, onChangeAttributeTypeShared, validate_enumValueDuplicate, validate_enumValueNumeric, onChangeInterfaceShared, onChangeCompositionShared, onChangeEngUnitShared, validate_symbolicName, renderDataTypeUIShared, renderEngUnitUIShared
} from '../../services/AttributesService';

const CLASS_NAME = "AttributeEntity";

function AttributeEntity(props) { //props are item, showActions
    
    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const initEditSettings = () => {
        if (props.isHeader) return null;

        //var changeDataType = 
        //    (_editItem.attributeType?.id !== AppSettings.AttributeTypeDefaults.CompositionId &&
        //        _editItem.attributeType?.id !== AppSettings.AttributeTypeDefaults.InterfaceId &&
        //        _editItem.attributeType?.id !== AppSettings.AttributeTypeDefaults.EnumerationId);

        var changeAttributeType = true;

        var showComposition = _editItem.attributeType?.id === AppSettings.AttributeTypeDefaults.CompositionId;
        var showInterface = _editItem.attributeType?.id === AppSettings.AttributeTypeDefaults.InterfaceId;
        var showEnumeration = _editItem.attributeType?.id === AppSettings.AttributeTypeDefaults.EnumerationId;

        if (props.lookupDataTypes == null || props.lookupDataTypes.length === 0) {
            return {
                useMinMax: !(showComposition | showInterface | showEnumeration),
                useEngUnit: !(showComposition | showInterface | showEnumeration ),
                //changeDataType: changeDataType,
                changeAttributeType: changeAttributeType,
                showComposition: showComposition,
                showInterface: showInterface,
                showEnumeration: showEnumeration
            };
        }
        var lookupItem = props.lookupDataTypes.find(dt => { return dt.id === _editItem.dataType.id; });

        return {
            useMinMax: lookupItem != null && lookupItem.useMinMax && !(showComposition | showInterface | showEnumeration),
            useEngUnit: lookupItem != null && lookupItem.useEngUnit && !(showComposition | showInterface | showEnumeration),
            //changeDataType: changeDataType,
            changeAttributeType: changeAttributeType,
            showComposition: showComposition,
            showInterface: showInterface,
            showEnumeration: showEnumeration
        };
    };

    var [_editItem, setEditItem] = useState(JSON.parse(JSON.stringify(props.item)));
    const [_isValid, setIsValid] = useState({
        name: true,
        nameDuplicate: true,
        dataType: true,
        attributeType: true,
        composition: true,
        interface: true,
        symbolicName: true,
        minMax: true,
        minIsNumeric: true,
        maxIsNumeric: true,
        instrumentMinMax: true,
        instrumentMinIsNumeric: true,
        instrumentMaxIsNumeric: true,
        engUnit: true,
        enumValue: true,
        enumValueDuplicate: true
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
        var dataType = props.lookupDataTypes.find(dt => { return dt.id === parseInt(e.target.value); });
        setIsValid({ ..._isValid, dataType: validate_dataType(dataType) });
    };

    const validateForm_attributeType = (e) => {
        setIsValid({ ..._isValid, attributeType: validate_attributeType(_editItem.dataType, e.target.value) });
    };

    const validateForm_composition = (e) => {
        var isValid = e.target.value.toString() !== "-1" || parseInt(_editItem.attributeType.id) !== AppSettings.AttributeTypeDefaults.CompositionId;
        setIsValid({ ..._isValid, composition: isValid });
    };

    const validateForm_interface = (e) => {
        var isValid = e.target.value.toString() !== "-1" || parseInt(_editItem.attributeType.id) !== AppSettings.AttributeTypeDefaults.InterfaceId;
        setIsValid({ ..._isValid, interface: isValid });
    };

    const validateForm_symbolicName = (e) => {
        setIsValid({ ..._isValid, symbolicName: validate_symbolicName(e.target.value) });
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

    const validateForm_enumValue = (e) => {
        //dup check
        var isValidDup = validate_enumValueDuplicate(e.target.value, _editItem, props.allAttributes);
        //check for valid integer - is numeric and is positive
        var isValidValue = validate_enumValueNumeric(e.target.value, _editItem);
        setIsValid({ ..._isValid, enumValue: isValidValue, enumValueDuplicate: isValidDup });
    }

    //validate all - call from button click
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        var isValid = validate_All(_editItem, _editSettings, props.allAttributes);

        setIsValid(JSON.parse(JSON.stringify(isValid)));
        return (isValid.name && isValid.nameDuplicate && isValid.minMax && isValid.dataType && isValid.attributeType && isValid.engUnit
            && isValid.minIsNumeric && isValid.maxIsNumeric && isValid.enumValue && isValid.enumValueDuplicate);
    }

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onUpdate = (e) => {
        console.log(generateLogMessageString(`onUpdate||id:${_editItem.id}`, CLASS_NAME));

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

    //attribute add ui - change composition ddl
    const onChangeComposition = (e) => {
        //_addItem changed by ref in shared method
        onChangeCompositionShared(e, _editItem);

        //call commonn change method
        onChange(e);
    }

    const onChangeInterface = (e) => {
        //_addItem changed by ref in shared method
        onChangeInterfaceShared(e, _editItem);

        //call commonn change method
        onChange(e);
    }

    //onchange data type
    const onChangeDataType = (e) => {
        var data = onChangeDataTypeShared(e, _editItem, _editSettings, props.lookupDataTypes);

        //replace add settings (updated in shared method)
        setEditSettings(JSON.parse(JSON.stringify(data.settings)));
        //update state - after changes made in shared method
        setEditItem(JSON.parse(JSON.stringify(data.item)));
        return;

    }

    //onchange attribute type
    const onChangeAttributeType = (e) => {
        var data = onChangeAttributeTypeShared(e, _editItem, _editSettings, props.lookupAttributeTypes, props.lookupDataTypes);

        //replace settings (updated in shared method)
        setEditSettings(JSON.parse(JSON.stringify(data.settings)));
        //update state - after changes made in shared method
        setEditItem(JSON.parse(JSON.stringify(data.item)));
        return;
    }

    //onchange eng unit
    const onChangeEngUnit = (e) => {
        //var item = onChangeEngUnitShared(e.target.value, _editItem, props.lookupEngUnits);
        var item = onChangeEngUnitShared(e.value, _editItem, props.lookupEngUnits);

        //update state - after changes made in shared method
        setEditItem(JSON.parse(JSON.stringify(item)));
        return;
    }

    //onchange numeric field
    const onChangeMinMax = (e) => {
        // Perform a numeric only check for numeric fields.
        if (!onChangeNumericKeysOnly(e)) {
            e.preventDefault();
            return;
        }

        //convert to int - this will convert '10.' to '10' to int
        if (_editItem.dataType.name.toLowerCase().indexOf('integer') > -1
            || _editItem.dataType.name.toLowerCase().indexOf('int64') > -1
            || _editItem.dataType.name.toLowerCase().indexOf('int32') > -1
            || _editItem.dataType.name.toLowerCase().indexOf('int16') > -1
            || _editItem.dataType.name.toLowerCase().indexOf('long') > -1) {
            e.target.value = toInt(e.target.value);
        }

        //call commonn change method
        onChange(e);
    }

    //onchange numeric field
    const onChangeEnumValue = (e) => {
        // Perform a numeric only check for numeric fields.
        if (!onChangeNumericKeysOnly(e)) {
            e.preventDefault();
            return;
        }

        //convert to int - this will convert '10.' to '10' to int
        var val = toInt(e.target.value);

        _editItem[e.target.id] = val;
        setEditItem(JSON.parse(JSON.stringify(_editItem)));
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
    //-------------------------------------------------------------------
    // render_CheckReadOnly: Before rendering a UI component or a label, check conditions
    //      to see if display element should be readonly. Note the caller will have additional
    //      specific conditions on top of this.
    //-------------------------------------------------------------------
    const render_CheckReadOnly = () => {
        return props.readOnly ||
            _editItem._itemType == null ||
            _editItem._itemType === "extended" ||
            (_editItem.variableType != null && _editItem.dataType.id === _editItem.variableType.id.toString()) || //extra logic allows for user to change data type in inline edit
            _editItem.dataType.id === AppSettings.DataTypeDefaults.CompositionId ||
            _editItem.interface != null;
    };


    //render the attribute name, append some stuff for certain types of attributes
    const renderNameUI = () => {
        return (
            <Form.Group className="flex-grow-1 align-self-center">
                {renderAttributeIcon(_editItem)}
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
        if (props.readOnly || _editItem._itemType == null || _editItem._itemType === "extended" || _editItem.interface != null) {

            if (_editItem.interface != null) {
                return (
                    <>
                        {renderAttributeIcon(_editItem)}
                        {_editItem.name} [<a href={`/type/${_editItem.interface.id}`} >{_editItem.interface.name}</a>]
                    </>
                );
            }
            //simple scenario
            return (
                <>
                    {renderAttributeIcon(_editItem)}
                    {_editItem.name}
                </>
            );
        }
        //edit mode
        else {
            return renderNameUI();
        }
    };
    

    //render for enumeration attr type
    const renderEnumValue = () => {

        var isReadOnly = (render_CheckReadOnly());

        if (_editItem.attributeType.id !== AppSettings.AttributeTypeDefaults.EnumerationId) return;

        var tip = !_isValid.enumValue ? 'Integer > 0 required.' : '';
        tip = !_isValid.enumValueIsNumeric ? tip + ' Integer required.' : tip;
        return (
            <Form.Group>
                <Form.Label className="mb-0" >Enum Value</Form.Label>
                {!_isValid.enumValue &&
                    <span className="invalid-field-message inline">
                        Integer &gt; 0 required
                    </span>
                }
                {!_isValid.enumValueDuplicate &&
                    <span className="invalid-field-message inline">
                        Duplicate value
                    </span>
                }
                {isReadOnly ?
                    <Form.Control id="enumValue" value={_editItem.enumValue == null ? '' : _editItem.enumValue} readOnly={isReadOnly} />
                    :
                    <Form.Control id="enumValue" type="" value={_editItem.enumValue == null ? '' : _editItem.enumValue} readOnly={isReadOnly}
                        onChange={onChangeEnumValue} onBlur={validateForm_enumValue} title={tip}
                        className={(!_isValid.enumValue || !_isValid.enumValueDuplicate ? 'invalid-field' : '')} />
                }
            </Form.Group>
        );
    };

    //only show this for one data type
    const renderComposition = () => {
        if (!_editSettings.showComposition) return;

        var isReadOnly = props.readOnly;

        const options = props.lookupCompositions.map((item) => {
            return (<option key={item.id} value={item.id} >{item.name}</option>)
        });

        return (
            <Form.Group>
                <Form.Label>Composition</Form.Label>
                {!_isValid.composition &&
                    <span className="invalid-field-message inline">
                        Required
                    </span>
                }
                {isReadOnly ?
                    <Form.Control id="compositionId" value={_editItem.composition == null ? '' : _editItem.composition.name} readOnly={isReadOnly} />
                    :
                    <Form.Control id="compositionId" as="select" value={_editItem.compositionId} onBlur={validateForm_composition} readOnly={isReadOnly}
                        onChange={onChangeComposition} className={(!_isValid.composition ? 'invalid-field minimal pr-5' : 'minimal pr-5')} >
                        <option key="-1|Select One" value="-1" >Select</option>
                        {options}
                    </Form.Control>
                }
            </Form.Group>
        )
    };

    //only show this for one data type
    const renderInterface = () => {
        if (!_editSettings.showInterface) return;

        const options = props.lookupInterfaces.current.map((item) => {
            return (<option key={item.id} value={item.id} >{item.name}</option>)
        });

        return (
            <Form.Group>
                <Form.Label>Interface</Form.Label>
                {!_isValid.interface &&
                    <span className="invalid-field-message inline">
                        Required
                    </span>
                }
                <Form.Control id="interfaceId" as="select" value={_editItem.interfaceId} onBlur={validateForm_interface}
                    onChange={onChangeInterface} className={(!_isValid.interface ? 'invalid-field minimal pr-5' : 'minimal pr-5')} >
                    <option key="-1|Select One" value="-1" >Select</option>
                    {options}
                </Form.Control>
            </Form.Group>
        )
    };

    //render the description ui
    const renderDescription = () => {
        var isReadOnly = (props.readOnly || _editItem._itemType == null || _editItem._itemType === "extended" || _editItem.interface != null);
        return (
            <Form.Group>
                <Form.Label className="mb-0" >Description</Form.Label>
                <Form.Control id="description" type="" placeholder="Enter a description" value={_editItem.description == null ? '' : _editItem.description} readOnly={isReadOnly}
                    onChange={onChange} />
            </Form.Group>
        );
    };

    //render the browseName ui
    const renderBrowseName = () => {
        var isReadOnly = (props.readOnly || _editItem._itemType == null || _editItem._itemType === "extended" || _editItem.interface != null);

        return (
            <Form.Group>
                <Form.Label className="mb-0" >Browse Name</Form.Label>
                <Form.Control id="browseName" type="" placeholder="Enter a browse name" value={_editItem.browseName == null ? '' :_editItem.browseName} readOnly={isReadOnly}
                    onChange={onChange} />
            </Form.Group>
        );
    };

    //render the symbolicName ui
    const renderSymbolicName = () => {
        var isReadOnly = (props.readOnly || _editItem._itemType == null || _editItem._itemType === "extended" || _editItem.interface != null);

        return (
            <Form.Group>
                <Form.Label className="mb-0" >Symbolic Name</Form.Label>
                {!_isValid.symbolicName &&
                    <span className="invalid-field-message inline">
                        No numbers, spaces or special characters permitted
                    </span>
                }
                <Form.Control id="symbolicName" className={(!_isValid.symbolicName ? `invalid-field` : ``)} type="" placeholder="Enter a symbolic name" value={_editItem.symbolicName != null ? _editItem.symbolicName : ""} readOnly={isReadOnly}
                    onChange={onChange} onBlur={validateForm_symbolicName} />
            </Form.Group>
        );
    };

    //render data type ui
    const renderDataType = () => {
        return renderDataTypeUIShared(_editItem, props.lookupDataTypes, null, _isValid.dataType, true, onChangeDataType, validateForm_dataType);
        //if (props.lookupDataTypes == null || props.lookupDataTypes.length === 0) return;

        //var isReadOnly = props.readOnly || _editItem.interface != null;

        //const options = renderDataTypeSelectOptions(props.lookupDataTypes, null, false);

        ////grab the associated caption when showing in read only mode
        //var selectedText = "";
        //if (_editItem.dataType == null || _editItem.dataType.id.toString() === "-1") selectedText = "";
        //else {
        //    var selItem = (props.lookupDataTypes == null || props.lookupDataTypes.length === 0) ? null :
        //        props.lookupDataTypes.find(x => { return x.id === _editItem.dataType.id });
        //    selectedText = selItem == null ? _editItem.dataType.name : selItem.name;
        //}

        //return (
        //    <Form.Group>
        //        <Form.Label className="mb-0" >Data Type</Form.Label>
        //        {
        //            !_isValid.dataType &&
        //            <span className="invalid-field-message inline">
        //                Required
        //            </span>
        //        }

        //        {isReadOnly ?
        //            <Form.Control id="dataType" value={selectedText} readOnly={isReadOnly} />
        //            :
        //            <Form.Control id="dataType" as="select" value={_editItem.dataType.id}
        //                onChange={onChangeDataType} onBlur={validateForm_dataType}
        //                className={(!_isValid.dataType ? 'invalid-field minimal pr-5' : 'minimal pr-5')} >
        //                <option key="-1|Select One" value="-1" >Select</option>
        //                {options}
        //            </Form.Control>
        //        }
        //    </Form.Group>
        //);
    };

    //render attr type ui
    const renderAttributeType = () => {
        if (props.lookupAttributeTypes == null || props.lookupAttributeTypes.length === 0) return;

        var isReadOnly = props.readOnly || _editItem._itemType == null || _editItem._itemType === "extended" || !_editSettings.changeAttributeType;

        const options = props.lookupAttributeTypes.map((item) => {
            return (<option key={item.id} value={item.id} >{item.name}</option>)
        });

        //grab the associated caption when showing in read only mode
        var selectedText = "";
        if (_editItem.attributeType == null || _editItem.attributeType.id.toString() === "-1") selectedText = "";
        else {
            var selItem = (props.lookupAttributeTypes == null || props.lookupAttributeTypes.length === 0) ? null :
                props.lookupAttributeTypes.find(x => { return x.id === _editItem.attributeType.id });
            selectedText = selItem == null ? _editItem.attributeType.name : selItem.name;
        }

        return (
            <Form.Group>
                <Form.Label className="mb-0" >Attribute Type</Form.Label>
                {
                    !_isValid.attributeType &&
                    <span className="invalid-field-message inline">
                        Required
                    </span>
                }

                {isReadOnly ?
                    <Form.Control id="attributeType" value={selectedText} readOnly={isReadOnly} />
                    :
                    <Form.Control id="attributeType" as="select" value={_editItem.attributeType.id}
                        onChange={onChangeAttributeType} onBlur={validateForm_attributeType}
                        className={(!_isValid.attributeType ? 'invalid-field minimal pr-5' : 'minimal pr-5')} >
                        <option key="-1|Select One" value="-1" >Select</option>
                        {options}
                    </Form.Control>
                }
            </Form.Group>
        );
    };

    //render the is array ui
    const renderIsArray = () => {
        var isReadOnly = (props.readOnly || _editItem._itemType == null || _editItem._itemType === "extended" || _editItem.interface != null);

        return (
            <Form.Group className="flex-grow-1 align-self-center">
                <Form.Check type="checkbox" id="isArray" label="Is Array" checked={_editItem.isArray} onChange={onCheckChange}
                    disabled={isReadOnly ? "disabled" : ""} />
            </Form.Group>
        );
    };

    //render the is required ui
    const renderIsRequired = () => {
        var isReadOnly = (props.readOnly || _editItem._itemType == null || _editItem._itemType === "extended" || _editItem.interface != null);

        return (
            <Form.Group className="flex-grow-1 align-self-center">
                <Form.Check type="checkbox" id="isRequired" label="Is Required" checked={_editItem.isRequired} onChange={onCheckChange}
                    disabled={isReadOnly ? "disabled" : ""} />
            </Form.Group>
        );
    };

    //render the modelling rule - always read only for now
    const rendermodelingRule = () => {
        //var isReadOnly = (props.readOnly || _editItem._itemType == null || _editItem._itemType === "extended" || _editItem.interface != null);
        var isReadOnly = true;
        //don't show if null or empty - read only ui for now
        if (_editItem.modelingRule == null || _editItem.modelingRule === '') return;

        return (
            <Form.Group>
                <Form.Label className="mb-0" >Modelling Rule</Form.Label>
                <Form.Control id="modelingRule" type="" placeholder="" value={_editItem.modelingRule == null ? '' : _editItem.modelingRule} readOnly={isReadOnly}
                    onChange={onChange} />
            </Form.Group>
        );
    };

    //render the attribute min, append some stuff for certain types of attributes
    const renderMin = () => {
        var isReadOnly = (render_CheckReadOnly() || !_editSettings.useMinMax);

        var tip = !_isValid.minMax ? 'Min > Max.' : '';
        tip = !_isValid.minIsNumeric ? tip + ' Invalid (ie. ####).' : tip;
        return (
            <Form.Group>
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
                <Form.Control id="minValue" type="" value={_editItem.minValue == null ? '' : _editItem.minValue} readOnly={isReadOnly}
                    onChange={onChangeMinMax} onBlur={validateForm_minMax} title={tip}
                    className={(!_isValid.minMax || !_isValid.minIsNumeric ? 'invalid-field' : '')} />
            </Form.Group>
        );
    };

    //render the attribute max, append some stuff for certain types of attributes
    const renderMax = () => {
        var isReadOnly = (render_CheckReadOnly() || !_editSettings.useMinMax);

        var tip = !_isValid.minMax ? 'Min > Max.' : '';
        tip = !_isValid.maxIsNumeric ? tip + ' Invalid (ie. ####).' : tip;

        return (
            <Form.Group>
                <Form.Label className="mb-0" >EU Max</Form.Label>
                {!_isValid.maxIsNumeric &&
                    <span className="invalid-field-message inline">
                        Invalid (ie. ####)
                        </span>
                }
                <Form.Control id="maxValue" type="" value={_editItem.maxValue == null ? '' : _editItem.maxValue} readOnly={isReadOnly}
                    onChange={onChangeMinMax} onBlur={validateForm_minMax} title={tip}
                    className={(!_isValid.minMax || !_isValid.maxIsNumeric ? 'invalid-field' : '')} />
            </Form.Group>
        );
    };

    //instrument Min
    const renderInstrumentMin = () => {
        var isReadOnly = (render_CheckReadOnly() || !_editSettings.useMinMax);

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
                <Form.Control id="instrumentMinValue" type="" value={_editItem.instrumentMinValue == null ? '' : _editItem.instrumentMinValue} readOnly={isReadOnly}
                    onChange={onChangeMinMax} onBlur={validateForm_instrumentMinMax} title={tip}
                    className={(!_isValid.instrumentMinMax || !_isValid.instrumentMinIsNumeric ? 'invalid-field' : '')} />
            </Form.Group>
        );
    };

    //instrument max
    const renderInstrumentMax = () => {
        var isReadOnly = (render_CheckReadOnly() || !_editSettings.useMinMax);

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
                <Form.Control id="instrumentMaxValue" type="" value={_editItem.instrumentMaxValue == null ? '' : _editItem.instrumentMaxValue} readOnly={isReadOnly}
                    onChange={onChangeMinMax} onBlur={validateForm_instrumentMinMax} title={tip}
                    className={(!_isValid.instrumentMinMax || !_isValid.instrumentMaxIsNumeric ? 'invalid-field' : '')} />
            </Form.Group>
        );
    };

    //render the eng unit input
    const renderEngUnit = () => {
        if (props.lookupEngUnits == null || props.lookupEngUnits.length === 0 || !_editSettings.useEngUnit) return;

        var isReadOnly = (render_CheckReadOnly());

        if (!isReadOnly) {
            return renderEngUnitUIShared(_editItem, props.lookupEngUnits, onChangeEngUnit, validateForm_engUnit);
        }
        else {
            //grab the associated caption when showing in read only mode
            var selectedText = "";
            var tip = "";
            if (_editItem.engUnit == null || _editItem.engUnit.id.toString() === "-1") selectedText = "";
            else {
                var selItem = (props.lookupEngUnits == null || props.lookupEngUnits.length === 0) ? null :
                    props.lookupEngUnits.find(x => { return x.id === _editItem.engUnit.id });
                selectedText = selItem == null ? _editItem.engUnit.displayName : selItem.displayName;
                tip = selItem == null ? _editItem.engUnit.description : selItem.description;
            }

            return (
                <Form.Group>
                    <Form.Label className="mb-0" >Eng Unit</Form.Label>
                        <Form.Control id="engUnit" value={selectedText} readOnly={isReadOnly} title={tip} />
                </Form.Group>
            );
        }

        //const options = props.lookupEngUnits.map((item) => {
        //    return (<option key={item.id} value={item.id} title={item.description} >{item.displayName}</option>)
        //});

        ////grab the associated caption when showing in read only mode
        //var selectedText = "";
        //var tip = "";
        //if (_editItem.engUnit == null || _editItem.engUnit.id.toString() === "-1") selectedText = "";
        //else {
        //    var selItem = (props.lookupEngUnits == null || props.lookupEngUnits.length === 0) ? null :
        //        props.lookupEngUnits.find(x => { return x.id === _editItem.engUnit.id });
        //    selectedText = selItem == null ? _editItem.engUnit.displayName : selItem.displayName;
        //    tip = selItem == null ? _editItem.engUnit.description : selItem.description;
        //}

        //return (
        //    <Form.Group className="flex-grow-1" >
        //        <Form.Label className="mb-0" >Eng Unit</Form.Label>
        //        {isReadOnly ?
        //            <Form.Control id="engUnit" value={selectedText} readOnly={isReadOnly} title={tip} />
        //            :
        //            <Form.Control id="engUnit" as="select" value={_editItem.engUnit == null ? "-1" : _editItem.engUnit.id} readOnly={isReadOnly}
        //                onChange={onChangeEngUnit} onBlur={validateForm_engUnit}
        //                className="minimal pr-5" >
        //                <option key="-1|Select One" value="-1" >Select</option>
        //                {options}
        //            </Form.Control>
        //        }
        //    </Form.Group>
        //);
    };

    //render the actions col. in edit mode, we swap out the icons
    const renderButtons = () => {

        if (props.readOnly || _editItem._itemType == null || _editItem._itemType === "extended") {
            //return edit/delete for editable grid
            return (
                <>
                    <Button variant="secondary" className="mx-1" onClick={() => { props.onClosePanel(false, null); }} >Close</Button>
                </>
            );
        }

        //return edit/delete for editable grid
        return (
            <>
                <Button variant="text-solo" className="mx-1" onClick={() => { props.onClosePanel(false, null); } } >Cancel</Button>
                <Button variant="secondary" type="button" className="mx-3" onClick={onUpdate} >Update</Button>
            </>
        );
    };

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    //only do this for non header rows
    if (_editItem === null || _editItem === {}) return null;
    if (_editItem.name == null) return null;

    //grid row
    return (
        <>
            <div className="row mb-3" >
                <div className="col-sm-12" >
                    {renderName()}
                </div>
            </div>
            <div className="row mb-3" >
                <div className="col-sm-12 col-md-6" >
                    {renderAttributeType()}
                </div>
                {(!_editSettings.showInterface && !_editSettings.showComposition && !_editSettings.showEnumeration) &&
                    <div className="col-sm-12 col-md-6" >
                        {renderDataType()}
                    </div>
                }
                {_editSettings.showEnumeration &&
                    <div className="col-6" >{renderEnumValue()}</div>
                }
                {_editSettings.showComposition &&
                    <div className="col-sm-12 col-md-6" >{renderComposition()}</div>
                }
                {_editSettings.showInterface &&
                    <div className="col-sm-12 col-md-6" >{renderInterface()}</div>
                }
            </div>
            {_editSettings.useEngUnit &&
                <div className="row mb-3" >
                    <div className="col-sm-6" >
                        {renderEngUnit()}
                    </div>
                </div>
            }
            <div className="row mb-3" >
                <div className="col-sm-12" >
                    {renderBrowseName()}
                </div>
            </div>
            <div className="row mb-3" >
                <div className="col-sm-12" >
                    {renderSymbolicName()}
                </div>
            </div>
            <div className="row mb-3" >
                <div className="col-sm-12" >
                    {renderDescription()}
                </div>
            </div>
            <div className="row mb-3" >
                <div className="col-sm-6" >
                    {renderIsRequired()}
                </div>
                <div className="col-sm-6" >
                    {renderIsArray()}
                </div>
            </div>
            {(_editItem.modelingRule != null && _editItem.modelingRule !== '') &&
                <div className="row mb-3" >
                    <div className="col-sm-6" >
                        {rendermodelingRule()}
                    </div>
                    <div className="col-sm-6" >
                    </div>
                </div>
            }
            {_editSettings.useMinMax &&
                <div className="row mb-3" >
                    <div className="col-sm-6" >
                        {renderInstrumentMin()}
                    </div>
                    <div className="col-sm-6" >
                        {renderInstrumentMax()}
                    </div>
                </div>
            }
            {_editSettings.useMinMax &&
                <div className="row mb-3" >
                    <div className="col-sm-6" >
                        {renderMin()}
                    </div>
                    <div className="col-sm-6" >
                        {renderMax()}
                    </div>
                </div>
            }
            <div className="d-flex mt-4 align-items-center justify-content-end">
                {renderButtons()}
            </div>
        </>
    );
};

export default AttributeEntity;