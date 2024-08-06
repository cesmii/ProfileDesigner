import React, { useState, useEffect } from 'react'
import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'

import { SVGIcon, SVGCheckIcon } from '../../components/SVGIcon'
import color from '../../components/Constants'
import { generateLogMessageString, convertToNumeric, toInt, onChangeNumericKeysOnly} from '../../utils/UtilityService'
import ConfirmationModal from '../../components/ConfirmationModal';
import {
    validate_name, validate_nameDuplicate, validate_dataType, validate_All, onChangeDataTypeShared, renderAttributeIcon, onChangeAttributeTypeShared, validate_attributeType, validate_enumValueDuplicate, validate_enumValueNumeric, onChangeCompositionShared, renderDataTypeUIShared, renderCompositionSelectUIShared,
    onChangeVariableTypeShared, renderVariableTypeUIShared, getPermittedDataTypesForAttribute,
} from '../../services/AttributesService';
import { AppSettings } from '../../utils/appsettings'

const CLASS_NAME = "AttributeItemRow";

const colorLookups = {
    cornflower: "#4084EF",
    cerulean: "#00AEEF",
    apple: "#6AA342",
    citron: "#9FB522",
    blazeOrange: "#FF6404",
    trinidad: "#E22C09",
    alabaster: "#F7F7F7",
    cararra: "#E7E6E2",
    alto: "#DADADA",
    shark: "#1F262A",
    outerSpace: "#2A3439",
    nevada: "#5B676D",
    osloGray: "#848689",
    spunPearl: "#AAA9AD",
    cardinal: "#D2222D",
    amber: "#FFBF00",
    forestGreen: "#238823",
    japaneseLaurel: "#007000",
    sauvignon: "#FFF6F4",
    camarone: "#006127",
    hintOfGreen: "#FAFFFC",
    peppermint: "#DEF2D5",
    tomThumb: "#485C3F",
    blue8: "#0AB4FF",
    info: "#d1ecf1",
    hlBlue: "#E5F6FD",
    hlTan: "#fdf7e5",
    coolGray: "444444",
    gris: "#394347"
};


function AttributeItemRow(props) { //props are item, showActions
    
    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const initEditSettings = () => {
        if (props.isHeader) return null;

        const isDataVariable = _editItem.attributeType?.id === AppSettings.AttributeTypeDefaults.DataVariableId;

        if (props.lookupDataTypes == null || props.lookupDataTypes.length === 0) {
            return {
                useMinMax: true,
                useEngUnit: true,
                isCustomDataType: _editItem.dataType.isCustom,
                showVariableType: isDataVariable
            };
        }
        const lookupItem = props.lookupDataTypes.find(dt => { return dt.val === _editItem.dataType.id; });
        return {
            useMinMax: lookupItem != null && lookupItem.useMinMax,
            useEngUnit: lookupItem != null && lookupItem.useEngUnit,
            isCustomDataType: _editItem.dataType.isCustom,
            showVariableType: isDataVariable
        };
    };

    const [_isEditMode, setIsEditMode] = useState(false);
    const [_editItem, setEditItem] = useState(JSON.parse(JSON.stringify(props.item)));
    const [_originalItem, setOriginalItem] = useState(null);
    const [_isValid, setIsValid] = useState({
        name: true,
        nameDuplicate: true,
        dataType: true,
        variableType: true,
        attributeType: true,
        composition: true,
        structure: true,
        variableType: true,
        minMax: true,
        minIsNumeric: true,
        maxIsNumeric: true,
        engUnit: true,
        enumValue: true,
        enumValueDuplicate: true
    });
    const [_permittedDataTypes, setPermittedDataTypes] = useState(props.lookupDataTypes);
    const [_editSettings, setEditSettings] = useState(initEditSettings());

    // Confirm Modals
    const [showDeleteModal, setShowDeleteModal] = useState(false);
    const [showDeleteInterfaceModal, setShowDeleteInterfaceModal] = useState(false);

    //-------------------------------------------------------------------
    // Region: useEffect - update _editItem when parent changes it
    //-------------------------------------------------------------------
    useEffect(() => {

        setEditItem(props.item);

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [props.item]);

    useEffect(() => {
        setPermittedDataTypes(props.lookupDataTypes);
    }, [props.lookupDataTypes]);

    useEffect(() => {
        if (_isEditMode) {
            const newPermittedDataTypes = getPermittedDataTypesForAttribute(_editItem, props.lookupDataTypes, props.lookupVariableTypes);
            if (newPermittedDataTypes != null) {
                setPermittedDataTypes(newPermittedDataTypes)
            }
            else {
                setPermittedDataTypes(props.lookupDataTypes);
            }
        }
    }, [_editItem, _isEditMode]);

    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------
    const validateForm_name = (e) => {
        const isValid = validate_name(e.target.value, _editItem);
        //dup check
        const isValidDup = validate_nameDuplicate(e.target.value, _editItem, props.allAttributes);
        setIsValid({ ..._isValid, name: isValid, nameDuplicate: isValidDup });
    };

    const validateForm_dataType = (e) => {
        const dataType = _permittedDataTypes.find(dt => { return dt.id === _editItem.dataType.id; });
        setIsValid({ ..._isValid, dataType: validate_dataType(dataType, _permittedDataTypes) });
    };
    const validateForm_variableType = (e) => {
        // OnBlur does not provide the value of the selected item
        //const variableType = props.lookupVariableTypes.find(vt => { return vt.id === parseInt(e.target.value); });
        //setIsValid({ ..._isValid, variableType: validate_variableType(variableType) });
    };

    const validateForm_attributeType = (e) => {
        setIsValid({ ..._isValid, attributeType: validate_attributeType(_editItem.dataType, e.target.value) });
    };

    const validateForm_composition = (e) => {
        const isValid = e.target.value.toString() !== "-1" || parseInt(_editItem.attributeType.id) !== AppSettings.AttributeTypeDefaults.CompositionId;
        setIsValid({ ..._isValid, composition: isValid });
    };

    //const validateForm_structure = (e) => {
    //    var isValid = e.target.value.toString() !== "-1" || parseInt(_editItem.attributeType.id) !== AppSettings.AttributeTypeDefaults.StructureId;
    //    setIsValid({ ..._isValid, structure: isValid });
    //};

    const validateForm_enumValue = (e) => {
        //dup check
        const isValidDup = validate_enumValueDuplicate(e.target.value, _editItem, props.allAttributes);
        //check for valid integer - is numeric and is positive
        const isValidValue = validate_enumValueNumeric(e.target.value, _editItem);
        setIsValid({ ..._isValid, enumValue: isValidValue, enumValueDuplicate: isValidDup });
    }

    //validate all - call from button click
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        const isValid = validate_All(_editItem, _editSettings, props.allAttributes, _permittedDataTypes);

        setIsValid(JSON.parse(JSON.stringify(isValid)));
        return (isValid.name && isValid.nameDuplicate && isValid.dataType && isValid.attributeType
            && isValid.composition && isValid.structure && isValid.enumValue && isValid.enumValueDuplicate);
    }

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    //modal show
    const onDeleteModal = (e) => {
        setShowDeleteModal(true);
    };

    //modal show
    const onDeleteInterfaceModal = (e) => {
        setShowDeleteInterfaceModal(true);
    };

    //modal confirm
    const onDeleteConfirm = (e) => {
        console.log(generateLogMessageString(`onDeleteConfirm||id:${_editItem.id}`, CLASS_NAME));
        setShowDeleteModal(false);
        props.onDelete(_editItem.id);
    };

    //modal dismiss
    const onDeleteCancel = (e) => {
        console.log(generateLogMessageString(`onDeleteCancel||id:${_editItem.id}`, CLASS_NAME));
        setShowDeleteModal(false);
    };

    //modal confirm
    const onDeleteInterfaceConfirm = (e) => {
        //should always be there, check just in case.
        if (_editItem.interface == null) {
            console.log(generateLogMessageString(`onDeleteInterfaceConfirm||interface id not found:${_editItem.id}`, CLASS_NAME, "error"));
            setShowDeleteInterfaceModal(false);
            return;
        }
        console.log(generateLogMessageString(`onDeleteInterfaceConfirm||interface id:${_editItem.interface.id}`, CLASS_NAME));
        props.onDeleteInterface(_editItem.interface.id);
        setShowDeleteInterfaceModal(false);
    };

    //modal dismiss
    const onDeleteInterfaceCancel = (e) => {
        console.log(generateLogMessageString(`onDeleteInterfaceCancel||id:${_editItem.id}`, CLASS_NAME));
        setShowDeleteInterfaceModal(false);
    };

    const onEditClick = (e) => {
        console.log(generateLogMessageString(`onEditClick||id:${_editItem.id}`, CLASS_NAME));
        setEditItem(JSON.parse(JSON.stringify(_editItem)));
        setOriginalItem(JSON.parse(JSON.stringify(_editItem)));
        setIsEditMode(true);
    };

    const onUpdateClick = (e) => {
        console.log(generateLogMessageString(`onUpdateClick||id:${_editItem.id}`, CLASS_NAME));

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

        setIsEditMode(false);
        //bubble up to parent
        props.onUpdate(_editItem);
    };

    const onCancelUpdateClick = (e) => {
        console.log(generateLogMessageString(`onCancelUpdateClick||id:${_editItem.id}`, CLASS_NAME));
        if (_originalItem != null) {
            setEditItem(JSON.parse(JSON.stringify(_originalItem)));
        }
        setIsEditMode(false);
    };

    //call from item row click, bubble up - a structure field would fall into this category.
    //This will show the associated profile's list of attr in read only grid.
    //Note many of the data types are considered custom but w/ no child attr. The parent component
    //will handle that scenario and open the slide out in detail view. 
    const onShowSlideOutCustomType = () => {
        props.toggleSlideOutCustomType(true, _editItem.dataType.customType.id, _editItem.id, _editItem.typeDefinitionId, props.readOnly);
    };

    //call from item row click, bubble up - a composition would fall into this category.
    //This will show the associated profile's list of attr in read only grid.
    const onShowSlideOutComposition = () => {
        props.toggleSlideOutCustomType(true, _editItem.composition.id, _editItem.id, _editItem.typeDefinitionId, props.readOnly);
    };

    //this will open a view showing a single attribute in an edit type form. It may be read only depending on the scenario. 
    const onShowSlideOutDetail = (e) => {
        props.toggleSlideOutDetail(true, _editItem.typeDefinitionId, _editItem.id, props.readOnly);
    };

    //attribute edit ui - change data type
    const onChangeDataType = (e) => {
        const data = onChangeDataTypeShared(e.value, _editItem, _editSettings, _permittedDataTypes);

        //replace add settings (updated in shared method)
        setEditSettings(JSON.parse(JSON.stringify(data.settings)));
        //update state - after changes made in shared method
        setEditItem(JSON.parse(JSON.stringify(data.item)));
    }

    const onChangeAttributeType = (e) => {
        const data = onChangeAttributeTypeShared(e, _editItem, _editSettings, props.lookupAttributeTypes, props.lookupDataTypes, props.lookupVariableTypes);

        //replace settings (updated in shared method)
        setEditSettings(JSON.parse(JSON.stringify(data.settings)));
        //update state - after changes made in shared method
        setEditItem(JSON.parse(JSON.stringify(data.item)));

        onChangeVariableTypeShared(_editItem.variableTypeDefinition?.id, _editItem, props.lookupVariableTypes, props.lookupDataTypes);
    }

    const onChangeVariableType = (e) => {
        onChangeVariableTypeShared(e?.value, _editItem, props.lookupVariableTypes, props.lookupDataTypes);

        //update state - after changes made in shared method
        setEditItem(JSON.parse(JSON.stringify(_editItem)));
    }

    //attribute add ui - change composition ddl
    const onChangeComposition = (e) => {
        //find the full composition item associated with selection. We need to 
        //populate more than just id in shared method
        const match = props.lookupCompositions.find(x => x.id === e.value);
        //_Item changed by ref in shared method
        onChangeCompositionShared(match, _editItem);

        //update state
        setEditItem(JSON.parse(JSON.stringify(_editItem)));
    }

    //attribute edit ui - change structure 
    //const onChangeStructure = (e) => {
    //    //Note - still sets data type val
    //    var data = onChangeDataTypeShared(e, _editItem, _editSettings, props.lookupStructures);

    //    //replace add settings (updated in shared method)
    //    setEditSettings(JSON.parse(JSON.stringify(data.settings)));
    //    //update state - after changes made in shared method
    //    setEditItem(JSON.parse(JSON.stringify(data.item)));
    //    return;
    //}

    //onchange numeric field
    const onChangeEnumValue = (e) => {
        // Perform a numeric only check for numeric fields.
        if (!onChangeNumericKeysOnly(e)) {
            e.preventDefault();
            return;
        }

        //convert to int - this will convert '10.' to '10' to int
        const val = toInt(e.target.value);

        _editItem[e.target.id] = val;
        setEditItem(JSON.parse(JSON.stringify(_editItem)));
    }

    //attribute edit ui - update state on change (used by multiple controls except onChangeDataType)
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

    const onOverrideProperty = (e) => {
        console.log(generateLogMessageString(`onOverrideProperty||${_editItem.name}`, CLASS_NAME));
        if (props.onOverrideProperty) props.onOverrideProperty(JSON.parse(JSON.stringify(_editItem)), _editItem.id);
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    // render_CheckReadOnly: Before rendering a UI component or a label, check conditions
    //      to see if display element should be readonly. Note the caller will have additional
    //      specific conditions on top of this.
    const render_CheckReadOnly = () => {
        return props.readOnly ||
            _editItem._itemType == null ||
            _editItem._itemType === "extended" ||
            _editItem.interface != null || !_isEditMode;
    };

    //render enum value ui
    const renderEnumValue = () => {

        if (_editItem.attributeType?.id !== AppSettings.AttributeTypeDefaults.EnumerationId) return;

        if (render_CheckReadOnly() || !_isEditMode) {
            return `Enum: ${_editItem.enumValue}`;
        }
        //edit mode
        else {
            return renderEnumValueUI();
        }
    };

    //render for enumeration attr type
    const renderEnumValueUI = () => {

        if (_editItem.attributeType.id !== AppSettings.AttributeTypeDefaults.EnumerationId) return;

        const isReadOnly = (render_CheckReadOnly());

        let tip = !_isValid.enumValue ? 'Integer > 0 required.' : '';
        tip = !_isValid.enumValueIsNumeric ? tip + ' Integer required.' : tip;
        return (
            <Form.Group className="form-inline">
                <Form.Label className="mr-2" htmlFor="enumValue" >Enum:</Form.Label>
                <Form.Control id="enumValue" type="" value={_editItem.enumValue == null ? '' : _editItem.enumValue} readOnly={isReadOnly}
                    onChange={onChangeEnumValue} onBlur={validateForm_enumValue} title={tip}
                    className={(!_isValid.enumValue || !_isValid.enumValueDuplicate ? 'invalid-field' : '')} />
                {!_isValid.enumValue &&
                    <span className="invalid-field-message">
                        Integer &gt; 0 required
                    </span>
                }
                {!_isValid.enumValueDuplicate &&
                    <span className="invalid-field-message">
                        Duplicate value
                    </span>
                }
            </Form.Group>
        );
    };

    //render composition
    const renderComposition = () => {

        if (_editItem.attributeType?.id !== AppSettings.AttributeTypeDefaults.CompositionId) return;

        if (render_CheckReadOnly() || !_isEditMode) {

            //simple scenario
            if (_editItem.composition.relatedProfileTypeDefinitionId != null) {
                return (
                    <div>
                        <a href={`/type/${_editItem.composition.relatedProfileTypeDefinitionId}`} >{_editItem.composition.relatedName}</a>
                    </div>
                );
            }
        }
        //edit mode
        else {
            return renderCompositionUI();
        }
    };

    //only show this for one attr type
    const renderCompositionUI = () => {

        return renderCompositionSelectUIShared(_editItem,
            props.lookupCompositions,
            _isValid.composition,
            true,
            onChangeComposition,
            validateForm_composition);
    };

    //render structure
    //const renderStructure = () => {

    //    if (_editItem.attributeType?.id !== AppSettings.AttributeTypeDefaults.StructureId) return;

    //    if (props.lookupStructures == null || props.lookupStructures.length === 0) return;

    //    if (render_CheckReadOnly() || !_isEditMode) {

    //        //grab the associated caption when showing in read only mode
    //        //find sel item in the data types lookup
    //        var selItem = (props.lookupStructures == null || props.lookupStructures.length === 0) ? null :
    //            props.lookupStructures.find(x => { return x.id === _editItem.dataType.id });
    //        var caption = selItem == null ? _editItem.dataType.name : selItem.name;

    //        //if custom type, show the custom type name
    //        if (selItem != null) {
    //            return (
    //                <div>
    //                    <a href={`/type/${selItem.customTypeId}`} >{caption}</a>
    //                </div>
    //            );
    //        }

    //        //simple scenario
    //        return caption;
    //    }
    //    //edit mode
    //    else {
    //        return renderStructureUI();
    //    }
    //};

    //only show this for one attr type
    //const renderStructureUI = () => {
    //    const options = props.lookupStructures.map((item) => {
    //        return (<option key={item.id} value={item.id} >{item.name}</option>)
    //    });

    //    return (
    //        <Form.Group>
    //            <Form.Control id="structureId" as="select" value={_editItem.dataType.id} onBlur={validateForm_structure}
    //                onChange={onChangeStructure} className={(!_isValid.structure ? 'invalid-field minimal pr-5' : 'minimal pr-5')} >
    //                <option key="-1|Select One" value="-1" >Select</option>
    //                {options}
    //            </Form.Control>
    //            {!_isValid.structure &&
    //                <span className="invalid-field-message inline">
    //                    Required
    //                </span>
    //            }
    //        </Form.Group>
    //    )
    //};

    //render the attribute name, append some stuff for certain types of attributes
    const renderNameUI = () => {
        //adding empty label for alignment purposes
        return (
            <>
                <Form.Group className="flex-grow-1">
                    {(_editItem.attributeType?.id !== AppSettings.AttributeTypeDefaults.EnumerationId &&
                        _editItem.attributeType?.id !== AppSettings.AttributeTypeDefaults.PropertyId &&
                        _editItem.attributeType?.id !== AppSettings.AttributeTypeDefaults.StructureId) &&
                        <label></label>
                    }
                    <Form.Control id="name" type="" placeholder="Enter a name" value={_editItem.name} aria-label="Name"
                    onChange={onChange} onBlur={validateForm_name}
                    className={(!_isValid.name || !_isValid.nameDuplicate ? 'invalid-field' : '')} />
                </Form.Group>
            </>
        );
    };

    //render the attribute name, append some stuff for certain types of attributes
    const renderName = () => {

        if (render_CheckReadOnly() || !_isEditMode || _editItem.overrideType === AppSettings.AttributeOverrideTypeEnum.Overriding) {

            if (_editItem.interface != null) {
                return (
                    <div>
                        {_nameCaption}<br />[<a href={`/type/${_editItem.interface.id}`} >{_editItem.interface.name}</a>]
                    </div>
                );
            }
            if (_editItem.composition?.intermediateObjectId != null) {
                return (
                    <a href={`/type/${_editItem.composition.intermediateObjectId}`} >{_editItem.name}</a>
                );
            }

            //simple scenario
            return _nameCaption;
        }
        //edit mode
        else {
            return renderNameUI();
        }
    };
    
    const _nameCaption = _editItem == null ? '' : `${_editItem.name}${_editItem.overrideType === AppSettings.AttributeOverrideTypeEnum.Overriding ? ' (override)' : ''}`;

    //render editable input for data type
    const renderDataTypeUI = () => {
        return renderDataTypeUIShared(_editItem.dataType, _permittedDataTypes, null, _isValid.dataType, false, null, onChangeDataType, validateForm_dataType);
    };

    ////render editable input for data type
    //const renderDataTypeUI = () => {
    //    if (props.lookupDataTypes == null || props.lookupDataTypes.length === 0) return;

    //    const options = renderDataTypeSelectOptions(props.lookupDataTypes, null, true);

    //    //typical scenario is just data type id
    //    var selectedId = _editItem.dataType == null ? "-1" : _editItem.dataType.id;

    //    if (_editItem.attributeType?.id === AppSettings.AttributeTypeDefaults.InterfaceId) {
    //        return;
    //    }

    //    //else, normal scenario
    //    return (
    //        <Form.Group className="align-self-center" >
    //            <Form.Control id="dataType" as="select" value={selectedId} aria-label="Data Type"
    //                onChange={onChangeDataType} onBlur={validateForm_dataType}
    //                className={(!_isValid.dataType ? 'invalid-field minimal pr-5' : 'minimal pr-5')} >
    //                <option key="-1|Select One" value="-1" >Select</option>
    //                {options}
    //            </Form.Control>
    //        </Form.Group>
    //    );
    //};

    //render data type col
    const renderDataType = () => {

        if (props.lookupDataTypes == null || props.lookupDataTypes.length === 0) return;

        if (render_CheckReadOnly() || !_isEditMode || _editItem.overrideType === AppSettings.AttributeOverrideTypeEnum.Overriding) {

            //find sel item in the data types lookup
            var selItem = (props.lookupDataTypes == null || props.lookupDataTypes.length === 0) ? null :
                props.lookupDataTypes.find(x => { return x.id === _editItem.dataType.id });
            var caption = selItem == null ? _editItem.dataType.name : selItem.name;
            caption = _editItem.isArray ? `${caption} []` : caption;

            //if custom type, show the custom type name
            if (selItem != null) {
                return (
                    <div>
                        <a href={`/type/${selItem.customTypeId}`} >{caption}</a>
                    </div>
                );
            }

            //simple scenario
            return caption;
        }
        //edit mode
        else {
            return (
                <>
                    {renderVariableTypeUI()}
                    {renderDataTypeUI()}
                </>
            )
        }
    };

    //render editable input for attr type
    const renderAttributeTypeUI = () => {
        if (props.lookupAttributeTypes == null || props.lookupAttributeTypes.length === 0) return;
        const options = props.lookupAttributeTypes.map((item) => {
            return (<option key={item.id} value={item.id} >{item.name}</option>)
        });

        //typical scenario is just data type id
        const selectedId = _editItem.attributeType == null ? "-1" : _editItem.attributeType.id;

        return (
            <div>
                <Form.Group className="flex-grow-1" >
                {(_editItem.attributeType?.id !== AppSettings.AttributeTypeDefaults.EnumerationId &&
                        _editItem.attributeType?.id !== AppSettings.AttributeTypeDefaults.PropertyId &&
                        _editItem.attributeType?.id !== AppSettings.AttributeTypeDefaults.StructureId) &&
                    <label></label>
                }
                <Form.Control id="attributeType" as="select" value={selectedId} aria-label="Attribute Type"
                    onChange={onChangeAttributeType} onBlur={validateForm_attributeType}
                    className={(!_isValid.attributeType ? 'invalid-field minimal pr-5' : 'minimal pr-5')} >
                    <option key="-1|Select One" value="-1" >Select</option>
                    {options}
                </Form.Control>
                </Form.Group>
            </div>
        );
    };

    //render attr type col
    const renderAttributeType = () => {
        if (render_CheckReadOnly() || !_isEditMode || _editItem.overrideType === AppSettings.AttributeOverrideTypeEnum.Overriding) {
            //grab the associated caption when showing in read only mode
            if (_editItem.attributeType == null || _editItem.attributeType.id.toString() === "-1") return "";
            const selItem = (props.lookupAttributeTypes == null || props.lookupAttributeTypes.length === 0) ? null :
                props.lookupAttributeTypes.find(x => { return x.id === _editItem.attributeType.id });

            var attributeType = selItem == null ? _editItem.attributeType.name : selItem.name;

            if (_editSettings.showVariableType && _editItem.variableTypeDefinition != null) {
                return (<div>{attributeType}: <a href={`/type/${_editItem.variableTypeDefinition.id}`}>{_editItem.variableTypeDefinition.name}</a></div>);
            }
            return attributeType;
        }
        //edit mode
        else {
            return renderAttributeTypeUI();
        }
};

    const renderVariableTypeUI = () => {
        return renderVariableTypeUIShared(_editItem, props.lookupVariableTypes, _editSettings, _isValid.variableType, true, onChangeVariableType, validateForm_variableType);
    }

    //render the description ui
    const renderDescriptionUI = () => {
        return (
            <Form.Group className="flex-grow-1 align-self-center">
                <Form.Control id="description" type="" placeholder="Enter a description" aria-label="Description"
                    value={_editItem.description == null ? '' : _editItem.description}
                    onChange={onChange} />
            </Form.Group>
        );
    };

    //render the description
    const renderDescription = () => {
        if (render_CheckReadOnly() || !_isEditMode) {
            //simple scenario
            return _editItem.description;
        }
        //edit mode
        else {
            return renderDescriptionUI();
        }
    };

    //render the icon which initiates an inline edit (ie a pencil icon)
    const renderActionIconStartInlineEdit = () => {
        if (!props.readOnly && _editItem.interface == null) {
            return (
                <>
                    <Button variant="icon-solo" onClick={onEditClick} className="align-items-center" title="Edit item inline" >
                        <span>
                            <SVGIcon name="edit" />
                        </span>
                    </Button>
                </>
            );
        }
    };

    //render slide out icon. No restrictions on showing this unless in inline edit mode.
    const renderActionIconSlideOut = () => {

        let onClickFn = onShowSlideOutDetail;
        if (_editItem.dataType.customType != null) {
            onClickFn = onShowSlideOutCustomType;
        }
        else if (_editItem.composition != null) {
            onClickFn = onShowSlideOutComposition;
        }

        return (
            <>
                {/*Detail slideout */}
                <Button variant="icon-solo" onClick={onClickFn} className="align-items-center" title="Open item detail (slide out)" >
                    <span>
                        <SVGIcon name="vertical-split" />
                    </span>
                </Button>
            </>
        );
    };

    //render delete icon
    //Allow delete if attribute belongs to this profile type def. 
    //Allow delete if attribute is an interface and interface is in the colleciton of profile type def's interfaces. 
    const renderActionIconDelete = () => {

        //default behavior
        let showDeleteBtn = props.onDelete != null;
        let deleteCallback = onDeleteModal;

        //if interface and allowed to delete interface, override
        if (_editItem.interface != null && props.onDeleteInterface == null) {
            showDeleteBtn = false;
            deleteCallback = null;
        }
        //if interface and allowed to delete interface, override
        else if (_editItem.interface != null && props.onDeleteInterface != null) {
            showDeleteBtn = true;
            deleteCallback = onDeleteInterfaceModal;
        }

        if (showDeleteBtn) {
            return (
                <>
                    {/* Confirmation modal */}
                    <Button variant="icon-solo" onClick={deleteCallback} className="align-items-center" title="Delete item" >
                        <span>
                            <SVGIcon name="trash" />
                        </span>
                    </Button>
                </>
            );
        }
    };

    //render the icons associated while inline editing (check, cancel)
    const renderActionIconInlineEditing = () => {

        return (
            <>
                <Button variant="icon-solo" onClick={onUpdateClick} className="align-items-center mr-1" title="Apply inline edits" >
                    <span>
                        <SVGCheckIcon name="check" />
                    </span>
                </Button>
                <Button variant="icon-solo" onClick={onCancelUpdateClick} className="align-items-center" title="Cancel inline edits" >
                    <span>
                        <SVGIcon name="close" />
                    </span>
                </Button>
            </>
        );
    };

    //for properties, allow the ability to override a parent attribute
    const renderActionOverride = () => {

        if (props.readOnly
            && _editItem.attributeType?.id === AppSettings.AttributeTypeDefaults.PropertyId
            && _editItem.overrideType === AppSettings.AttributeOverrideTypeEnum.None)
        {
            return (
                <>
                    <Button variant="icon-solo" onClick={onOverrideProperty} className="align-items-center" title="Override Property" >
                        <i className="material-icons">model_training</i>
                    </Button>
                </>
            );
        }
    };

    //render the actions col. Based on data conditions, we show or hide certain icons
    const renderActionIcons = () => {
        if (!_isEditMode) {
            return (
                <>
                    {renderActionOverride()}
                    {renderActionIconStartInlineEdit()}
                    {renderActionIconSlideOut()}
                    {renderActionIconDelete()}
                </>
            );
        }
        else
        {
            return (
                <>
                    {renderActionIconInlineEditing()}
                </>
            );
        }
    };


    const renderInterfaceStyle = () => {
        if (_editItem.interfaceGroupId != null) {
            //use the group id to get a color from the color constants file - this keeps a group of items color coded together
            let colorCounter = 0;
            for (const c in colorLookups) {

                if (_editItem.interfaceGroupId === colorCounter) {
                    return { borderLeftColor: colorLookups[c],
                                borderLeftWidth: "4px",
                                borderLeftStyle: "solid"
                            };
                }

                colorCounter++;
            }
        }
        //non - interfaces - keep alignment consistent
        return {
            borderLeftColor: "Transparent",
            borderLeftWidth: "4px",
            borderLeftStyle: "solid"
        };
    }

    //render the delete modals
    const renderDeleteConfirmation = (e) => {

        if (!showDeleteModal) return;

        const message = (<> <strong>WARNING: </strong>You are about to delete '{_nameCaption}'. </>);

        return (
            <>
                <ConfirmationModal showModal={showDeleteModal} caption="Confirm Deletion" message={message}
                    icon={{ name: "warning", color: color.trinidad }}
                    confirm={{ caption: "Delete", callback: onDeleteConfirm, buttonVariant: "danger" }}
                    cancel={{ caption: "Cancel", callback: onDeleteCancel, buttonVariant: null }} />
            </>
            );
    };

    // Modal is working
    const renderDeleteInterfaceConfirmation = (e) => {

        if (!showDeleteInterfaceModal) return;

        const message = (<> <strong>WARNING: </strong>You are about to delete all attributes associated with interface '{_editItem.interface.name}'. </>);

        return (
            <>
                <ConfirmationModal showModal={showDeleteInterfaceModal} caption="Confirm Deletion" message={message}
                    icon={{ name: "warning", color: color.trinidad }}
                    confirm={{ caption: "Delete", callback: onDeleteInterfaceConfirm, buttonVariant: "danger" }}
                    cancel={{ caption: "Cancel", callback: onDeleteInterfaceCancel, buttonVariant: null }} />
            </>
        );
    };

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    if (props.isHeader) {
        return (
            <tr className="" >
                <td className="pl-1">&nbsp;</td>
                <th className="px-2" >
                    <div className="row" >
                        <div className="d-none d-sm-block col-sm-5" >Name</div>
                        <div className="d-none d-sm-block col-sm-4" >Data Type</div>
                        <div className="d-none d-sm-block col-sm-3" >Attribute Type</div>
                        <div className="d-block d-sm-none" >Name, Data Type, Attribute Type</div>
                        </div>
                    </th>
                <td>&nbsp;</td>
            </tr>
        );
    }

    //only do this for non header rows
    if (_editItem === null || _editItem === {}) return null;
    if (_editItem.name == null) return null;

    //grid row
    return (
        <>
            <tr style={renderInterfaceStyle()} className="" >
                <td className="pl-1 col-icon" >{renderAttributeIcon(_editItem, props.readOnly)}</td>
                <td className="px-2" >
                    <div className={`row ${_isEditMode ? "mt-2" : ""}`} >
                        <div className={`col-sm-5 align-self-${_isEditMode ? 'start' : 'center'}`} >{renderName()}</div>

                        <div className={`col-sm-4 align-self-${_isEditMode ? 'start' : 'center'}`} >
                            {(!_editSettings.showInterface &&
                                _editItem.attributeType?.id !== AppSettings.AttributeTypeDefaults.EnumerationId &&
                                _editItem.attributeType?.id !== AppSettings.AttributeTypeDefaults.CompositionId ) &&
                                renderDataType()
                            }
                            {_editItem.attributeType?.id === AppSettings.AttributeTypeDefaults.EnumerationId &&
                                renderEnumValue()
                            }
                            {_editItem.attributeType?.id === AppSettings.AttributeTypeDefaults.CompositionId &&
                                renderComposition()
                            }
                        </div>
                        <div className={`col-sm-3 align-self-${_isEditMode ? 'start' : 'center'}`} >{renderAttributeType()}</div>
                        {(!props.isPopout) &&
                            <div className={`col-sm-12 d-none d-md-block text-muted ${_editItem.description != null && _editItem.description !== '' ? "mt-1" : ""}`} >{renderDescription()}</div>
                        }
                    </div>
                </td>
                <td className="text-right" >
                    <div className="d-flex justify-content-end">{renderActionIcons()}</div>
                    {renderDeleteConfirmation()}
                    {renderDeleteInterfaceConfirmation()}
                </td>
            </tr>
        </>
    );
};

export default AttributeItemRow;