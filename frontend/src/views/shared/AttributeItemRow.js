import React, { useState } from 'react'
import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'
import Alert from 'react-bootstrap/Alert'

import { SVGIcon, SVGCheckIcon } from '../../components/SVGIcon'
import color from '../../components/Constants'
//import color from './Constants'
import { generateLogMessageString, convertToNumeric} from '../../utils/UtilityService'
import ConfirmationModal from '../../components/ConfirmationModal';
import { validate_name, validate_nameDuplicate, validate_dataType, validate_All } from '../../services/AttributesService';

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

    var [_isEditMode, setIsEditMode] = useState(false);
    var [_editItem, setEditItem] = useState(JSON.parse(JSON.stringify(props.item)));
    const [_isValid, setIsValid] = useState({
        name: true,
        nameDuplicate: true,
        dataType: true,
        minMax: true,
        minIsNumeric: true,
        maxIsNumeric: true,
        engUnit: true
    });
    const [_editSettings, setEditSettings] = useState(initEditSettings());

    // Confirm Modals
    const [showDeleteModal, setShowDeleteModal] = useState(false);
    const [showDeleteInterfaceModal, setShowDeleteInterfaceModal] = useState(false);

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

    //validate all - call from button click
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        var isValid = validate_All(_editItem, _editSettings, props.allAttributes);

        setIsValid(JSON.parse(JSON.stringify(isValid)));
        return (isValid.name && isValid.nameDuplicate && isValid.dataType);
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
        console.log(generateLogMessageString(`onDeleteConfirm||id:${props.item.id}`, CLASS_NAME));
        setShowDeleteModal(false);
        props.onDelete(props.item.id);
    };

    //modal dismiss
    const onDeleteCancel = (e) => {
        console.log(generateLogMessageString(`onDeleteCancel||id:${props.item.id}`, CLASS_NAME));
        setShowDeleteModal(false);
    };

    //modal confirm
    const onDeleteInterfaceConfirm = (e) => {
        //should always be there, check just in case.
        if (props.item.interface == null) {
            console.log(generateLogMessageString(`onDeleteInterfaceConfirm||interface id not found:${props.item.id}`, CLASS_NAME, "error"));
        }
        console.log(generateLogMessageString(`onDeleteInterfaceConfirm||interface id:${props.item.interface.id}`, CLASS_NAME));
        props.onDeleteInterface(props.item.interface.id);
    };

    //modal dismiss
    const onDeleteInterfaceCancel = (e) => {
        console.log(generateLogMessageString(`onDeleteInterfaceCancel||id:${props.item.id}`, CLASS_NAME));
        setShowDeleteInterfaceModal(false);
    };

    const onEditClick = (e) => {
        console.log(generateLogMessageString(`onEditClick||id:${props.item.id}`, CLASS_NAME));
        setEditItem(JSON.parse(JSON.stringify(props.item)));
        setIsEditMode(true);
    };

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

        setIsEditMode(false);
        //bubble up to parent
        props.onUpdate(_editItem);
    };

    const onCancelUpdateClick = (e) => {
        console.log(generateLogMessageString(`onCancelUpdateClick||id:${props.item.id}`, CLASS_NAME));
        setEditItem(JSON.parse(JSON.stringify(props.item)));
        setIsEditMode(false);
    };

    //call from item row click, bubble up
    const onShowSlideOutVariableType = (e) => {
        props.toggleSlideOutVariableType(true, props.item.variableType.id);
    };

    const onShowSlideOutDetail = (e) => {
        props.toggleSlideOutDetail(true, props.item.id, props.readOnly);
    };

    //attribute edit ui - change data type
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

    //attribute edit ui - update state on change
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
        var iconColor = (props.item._itemType == null || props.item._itemType === "profile") && !props.readOnly
            ? color.shark : color.silver;

        //set up icon properly
        var iconName = props.item._itemType == null || props.item._itemType === "profile" ? "account-circle" : "group";

        if (props.item.dataType === "composition") iconName = "profile";

        //variable type - special icon type
        if (props.item.variableType != null) iconName = "variabletype";

        if (props.item.interface != null) iconName = "key";

        return (
            <span>
                <SVGIcon name={iconName} size="24" fill={iconColor} />
            </span>
        );
    }

    //render the attribute name, append some stuff for certain types of attributes
    const renderNameUI = () => {
        return (
            <Form.Group className="flex-grow-1 align-self-center">
                <Form.Control id="name" type="" placeholder="Enter a name" value={_editItem.name}
                    onChange={onChange} onBlur={validateForm_name}
                    className={(!_isValid.name || !_isValid.nameDuplicate ? 'invalid-field' : '')} />
            </Form.Group>
        );
    };

    //render the attribute name, append some stuff for certain types of attributes
    const renderName = () => {
        if (props.readOnly || props.item._itemType == null || props.item._itemType === "extended" || props.item.interface != null || !_isEditMode) {
            if (props.item.interface != null) {
                return (
                    <>
                        {props.item.name} [<a href={`/profile/${props.item.interface.id}`} >{props.item.interface.name}</a>]
                    </>
                );
            }
            //simple scenario
            return props.item.name;
        }
        //edit mode
        else {
            return renderNameUI();
        }
    };
    
    
    //render editable input for data type
    const renderDataTypeUI = () => {
        if (props.lookupDataTypes == null || props.lookupDataTypes.length === 0) return;
        const options = props.lookupDataTypes.map((item) => {
            //skip interface, composition types in edit mode
            if (item.val === 'composition' || item.val === 'interface') {
                return null;
            }
            else {
                return (<option key={item.val} value={item.val} >{item.caption}</option>)
            }
        });

        return (
            <Form.Group className="flex-grow-1 align-self-center" >
                <Form.Control id="dataType" as="select" value={_editItem.dataType}
                    onChange={onChangeDataType} onBlur={validateForm_dataType}
                    className={(!_isValid.dataType ? 'invalid-field minimal pr-5' : 'minimal pr-5')} >
                    <option key="-1|Select One" value="-1" >Select</option>
                    {options}
                </Form.Control>
            </Form.Group>
        );
    };

    //render data type col
    const renderDataType = () => {
        if (props.readOnly || props.item._itemType == null || props.item._itemType === "extended" || props.item.interface != null || !_isEditMode ||
            !_editSettings.changeDataType) {
            if (props.item.dataType === "composition") {
                return (
                    <a href={`/profile/${props.item.composition.id}`} >{props.item.composition.name} [{props.item.dataType}]</a>
                );
            }
            //if variable type, show the variable type name
            if (props.item.variableType != null) {
                return (
                    <>
                    <a href={`/profile/${props.item.variableType.id}`} >{props.item.variableType.name} [variable type]</a>
                    </>
                );
            }

            //simple scenario
            //grab the associated caption when showing in read only mode
            var selItem = (props.lookupDataTypes == null || props.lookupDataTypes.length === 0) ? null :
                props.lookupDataTypes.find(x => { return x.val === props.item.dataType });
            if (props.item.dataType == null || props.item.dataType.toString() === "-1") return "";
            return selItem == null ? props.item.dataType : selItem.caption;
        }
        //edit mode
        else {
            return renderDataTypeUI();
        }
    };

    //render the description ui
    const renderDescriptionUI = () => {
        return (
            <Form.Group className="flex-grow-1 align-self-center">
                <Form.Control id="description" type="" placeholder="Enter a description" value={_editItem.description}
                    onChange={onChange} />
            </Form.Group>
        );
    };

    //render the description
    const renderDescription = () => {
        if (props.readOnly || props.item._itemType == null || props.item._itemType === "extended" || props.item.interface != null || !_isEditMode) {
            //simple scenario
            return props.item.description;
        }
        //edit mode
        else {
            return renderDescriptionUI();
        }
    };

    //render the actions col. in edit mode, we swap out the icons
    const renderActionIcons = () => {

        if (props.readOnly || props.item._itemType == null || props.item._itemType === "extended") {
            return (
                <>
                    {/*Detail slideout */}
                    <Button variant="icon-solo" onClick={props.item.variableType == null ? onShowSlideOutDetail : onShowSlideOutVariableType} className="align-items-center" >
                    <span>
                        <SVGIcon name="vertical-split" size="24" fill={color.shark} />
                    </span>
                </Button>
                </>
            );
        }

        //allow delete for an interface attribute unless it is an extended attrib. This would trigger delete of all attributes of said interface
        if (props.item.interface != null && props.item._itemType !== "extended") {
            return (
                <>
                {/*Detail slideout */}
                <Button variant="icon-solo" onClick={onShowSlideOutDetail} className="align-items-center" >
                    <span>
                        <SVGIcon name="vertical-split" size="24" fill={color.shark} />
                    </span>
                </Button>
                {/* <Button variant="icon-solo" onClick={onDeleteInterfaceClick} className="align-items-center" > */}
                <Button variant="icon-solo" onClick={onDeleteInterfaceModal} className="align-items-center" >
                    <span>
                        <SVGIcon name="trash" size="24" fill={color.shark} />
                    </span>
                </Button>
                </>
            );
        }
        //type of variableType
        else if (!_isEditMode && props.item.variableType != null) {
            return (
                <>
                    <Button variant="icon-solo" onClick={onEditClick} className="align-items-center" >
                        <span>
                            <SVGIcon name="edit" size="24" fill={color.shark} />
                        </span>
                    </Button>
                    {/*variable type slideout */}
                    <Button variant="icon-solo" onClick={onShowSlideOutVariableType} className="align-items-center" >
                        <span>
                            <SVGIcon name="vertical-split" size="24" fill={color.shark} />
                        </span>
                    </Button>
                    {/* Confirmation modal */}
                    {/* <Button variant="icon-solo" onClick={onDeleteClick} className="align-items-center" > */}
                    <Button variant="icon-solo" onClick={onDeleteModal} className="align-items-center" >
                        <span>
                            <SVGIcon name="trash" size="24" fill={color.shark} />
                        </span>
                    </Button>
                </>
            );
        }
        //return edit/delete for editable grid
        else if (!_isEditMode) {
            return (
                <>
                    <Button variant="icon-solo" onClick={onEditClick} className="align-items-center" >
                        <span>
                            <SVGIcon name="edit" size="24" fill={color.shark} />
                        </span>
                    </Button>
                    {/*Detail slideout */}
                    <Button variant="icon-solo" onClick={onShowSlideOutDetail} className="align-items-center" >
                        <span>
                            <SVGIcon name="vertical-split" size="24" fill={color.shark} />
                        </span>
                    </Button>
                    {/* <Button variant="icon-solo" onClick={onDeleteClick} className="align-items-center" > */}
                    <Button variant="icon-solo" onClick={onDeleteModal} className="align-items-center" >
                        <span>
                            <SVGIcon name="trash" size="24" fill={color.shark} />
                        </span>
                    </Button>
                </>
            );
        }
        else {
            //return edit/delete for editable grid
            return (
                <>
                    <Button variant="icon-solo" onClick={onUpdateClick} className="align-items-center" >
                        <span>
                            <SVGCheckIcon name="check" size="24" fill={color.shark} />
                        </span>
                    </Button>
                    <Button variant="icon-solo" onClick={onCancelUpdateClick} className="align-items-center" >
                        <span>
                            <SVGIcon name="close" size="24" fill={color.shark} />
                        </span>
                    </Button>
                </>
            );
        }
    };

    const renderInterfaceStyle = () => {
        if (props.item.interfaceGroupId != null) {
            //use the group id to get a color from the color constants file - this keeps a group of items color coded together
            var colorCounter = 0;
            for (const c in colorLookups) {

                if (props.item.interfaceGroupId === colorCounter) {
                    return { borderLeftColor: colorLookups[c],
                                borderLeftWidth: "4px",
                                borderLeftStyle: "solid"
                            };
                }

                colorCounter++;
            }
        }
    }

    //render the delete modals
    const renderDeleteConfirmation = (e) => {

        if (!showDeleteModal) return;

        var message = (<> <strong>WARNING: </strong>You are about to delete '{props.item.name}'. </>);

        return (
            <>
                <Alert variant="danger">
                    {/* <Alert.Heading>Preparing to delete</Alert.Heading> */}
                    <p className="mb-0" >Preparing to delete  '{props.item.name}'</p>
                </Alert>

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

        var message = (<> <strong>WARNING: </strong>You are about to delete all attributes associated with interface '{props.item.interface.name}'. </>);

        return (
            <>
                <Alert variant="danger">
                    {/* <Alert.Heading>Preparing to delete</Alert.Heading> */}
                    <p className="mb-0" >Preparing to delete attributes associated with '{props.item.interface.name}'</p>
                </Alert>

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
    var cssClass = "row " + props.cssClass + (props.isHeader ? " bottom header" : " center") ;
    
        //header row - controlled by props flag
    if (props.isHeader) {
        return (
            <div className={cssClass}>
                <div className="col col-x-small left pl-3" >&nbsp;</div>
                <div className="col col-20 left" >Name</div>
                <div className="col col-25 left" >Data type</div>
                <div className="col auto-size left" >Description</div>
                <div className="col auto-size right nowrap" ></div>
            </div>
        );
    }

    //only do this for non header rows
    if (props.item === null || props.item === {}) return null;
    if (props.item.name == null) return null;

    //grid row
    return (
        <>
            <div style={renderInterfaceStyle()} className={cssClass}>
                <div className="col col-x-small left pl-3 h-100 bg-light-trans d-flex flex-col align-items-center" >{renderIcon()}</div>
                <div className="col col-20 left h-100 bg-light-trans d-flex flex-col align-items-center" >{renderName()}</div>
                <div className="col col-25 left h-100 bg-light-trans d-flex flex-col align-items-center" >{renderDataType()}</div>
                <div className="col auto-size left" >{renderDescription()}</div>
                <div className="col auto-size right nowrap" >
                    {renderActionIcons()}
                </div>
            </div>

            {renderDeleteConfirmation()}
            {renderDeleteInterfaceConfirmation()}
        </>
    );
};

export default AttributeItemRow;