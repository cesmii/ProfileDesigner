import { Form } from 'react-bootstrap';
import Select from 'react-select';

import { SVGIcon } from '../components/SVGIcon'
import color from '../components/Constants'
import { AppSettings } from '../utils/appsettings';
import { getUserPreferences, setUserPreferences, convertToNumeric, validateNumeric, validate_NoSpecialCharacters, getPermittedDataTypesForVariableTypeById } from '../utils/UtilityService';
import { getProfileCaption } from './ProfileService';

//const CLASS_NAME = "AttributeService";

const _dataTypeInit = { id: -1, name: '', customTypeId: null, customType: null };

export const attributeNew = {
    id: null, name: '', dataType: { id: -1, name: '', customTypeId: null, customType: null }
            , attributeType: { id: -1, name: '', customTypeId: null, customType: null }
            , minValue: null, maxValue: null, engUnit: null, compositionId: -1, composition: null,
    interfaceId: -1, interface: null, description: '', displayName: '', typeDefinitionId: null,
    isArray: false, isRequired: false
}

export function getAttributesPreferences() {
    var item = getUserPreferences();
    return item.attributePreferences;
}

export function setAttributesPageSize(val) {
    var item = getUserPreferences();
    item.attributePreferences.pageSize = val;
    setUserPreferences(item);
}

//-------------------------------------------------------------------
// Region: Validation
//-------------------------------------------------------------------
export const validate_name = (val, item) => {
    return (val != null && val.trim().length > 0) || item.interface != null;
};

export const validate_nameDuplicate = (val, item, allAttributes) => {
    //dup check
    return (val == null || val.trim().length === 0) || item.interface != null ||
        (allAttributes.find((a) => { return a.id !== item.id && a.name.toLowerCase() === val.toLowerCase() }) == null);
};

export const validate_dataType = (val, permittedDataTypes) => {
    if (val == null || val.id.toString() === "-1")
        return false;
    const match = permittedDataTypes.find(dt => dt.id.toString() === val.id.toString());
    return match != null;
};
export const validate_variableType = (val) => {
    return true; // val != null && val.id.toString() !== "-1";
};

export const validate_attributeType = (dataType, val) => {
    //return val != null && val.id.toString() !== "-1";
    return val != null && val.toString() !== "-1";
};

export const validate_engUnit = (val, editSettings) => {
    return !editSettings.useEngUnit || (val != null && val.toString() !== "-1"); 
};

export const validate_minMax = (min, max, dataType, editSettings) => {
    var minNum = convertToNumeric(dataType, min);
    var maxNum = convertToNumeric(dataType, max);
    return !editSettings.useMinMax ||
        (min == null || min === '' || max == null || max === '') ||
        (minNum <= maxNum);
};

export const validate_enumValueDuplicate = (val, item, allAttributes) => {
    if (item.attributeType.id !== AppSettings.AttributeTypeDefaults.EnumerationId) return true;
    //dup check
    return (val == null || val.toString().trim().length === 0) || item.interface != null ||
        (allAttributes.find((a) => { return a.id !== item.id && parseInt(a.enumValue) === parseInt(val) }) == null);
};

export const validate_enumValueNumeric = (val, item) => {
    if (item.attributeType.id !== AppSettings.AttributeTypeDefaults.EnumerationId) return true;
    if (val == null || val === '' || val === '-' || val === '.' || val === '-.') return true;
    if (val.toString().indexOf('.') > 0) return false;
    if (isNaN(parseInt(val))) return false;
    return parseInt(val) >= 0;
};

export const validate_symbolicName = (val) => {
    return (val == null || val.toString().trim().length === 0) || validate_NoSpecialCharacters(val);
};


//validate all - call from button click
export const validate_All = (item, editSettings, allAttributes, permittedDataTypes) => {
    const isValidDataType = validate_dataType(item.dataType, permittedDataTypes);
    var result = {
        name: validate_name(item.name, item),
        nameDuplicate: validate_nameDuplicate(item.name, item, allAttributes),
        //certain attr types do not require data type and therefore are true
        dataType: item.attributeType?.id === AppSettings.AttributeTypeDefaults.InterfaceId ||
            item.attributeType?.id === AppSettings.AttributeTypeDefaults.CompositionId ||
            item.attributeType?.id === AppSettings.AttributeTypeDefaults.EnumerationId || isValidDataType,
        composition: item.attributeType?.id !== AppSettings.AttributeTypeDefaults.CompositionId || 
            (item.composition != null && item.compositionId > 0),
        variableType: validate_variableType(item.variableTypeDefinition),
        structure: item.attributeType?.id !== AppSettings.AttributeTypeDefaults.StructureId || isValidDataType,
        attributeType: validate_attributeType(item.dataType, item.attributeType == null ? null : item.attributeType.id), //can be null if composition
        symbolicName: validate_symbolicName(item.symbolicName),
        minMax: validate_minMax(item.minValue, item.maxValue, item.dataType, editSettings),
        minIsNumeric: validateNumeric(item.dataType, item.minValue),
        maxIsNumeric: validateNumeric(item.dataType, item.maxValue),
        instrumentMinMax: validate_minMax(item.instrumentMinValue, item.instrumentMaxValue, item.dataType, editSettings),
        instrumentMinIsNumeric: validateNumeric(item.dataType, item.instrumentMminValue),
        instrumentMaxIsNumeric: validateNumeric(item.dataType, item.instrumentMaxValue),
        engUnit: item.attributeType?.id !== AppSettings.AttributeTypeDefaults.InterfaceId || 
            item.engUnit == null || validate_engUnit(item.engUnit.id, editSettings),
        enumValue: validate_enumValueNumeric(item.enumValue, item),
        enumValueDuplicate: validate_enumValueDuplicate(item.enumValue, item, allAttributes),
    };

    return result;
}

//-------------------------------------------------------------------
// Region: Shared Attribute Methods
//-------------------------------------------------------------------
//-------------------------------------------------------------------
// onChangeDataType: on change of data type, update item and settings values. 
//      Shared by attributeList.add, attributeItemRow inline edit and
//      attributeEntity edit 
//-------------------------------------------------------------------
export const onChangeDataTypeShared = (val, item, settings, lookupDataTypes) => {
    //e can be e.target.value or e.value - two different controls

    var isCustom = false;
    //var isComposition = false;
    //var isInterface = false;
    var useMinMax = false;
    var useEngUnit = false;

    var lookupItem = lookupDataTypes.find(dt => { return dt.id.toString() === val.toString(); });
    if (val == null || val.toString() === "-1") {
        useMinMax = true;
        useEngUnit = true;
    }
    else {
        isCustom = lookupItem != null && lookupItem.isCustom;
        useMinMax = lookupItem != null && lookupItem.useMinMax;
        useEngUnit = lookupItem != null && lookupItem.useEngUnit;
        //isComposition = lookupItem != null && lookupItem.id === AppSettings.DataTypeDefaults.CompositionId;
        //isInterface = lookupItem != null && lookupItem.id === AppSettings.DataTypeDefaults.InterfaceId;
    }

    //reset composition, interface object anytime this changes
    //item.composition = null;
    //item.compositionId = -1;
    //item.interface = null;
    //item.interfaceId = -1;

    //clear out vals based on data type
    item.minValue = useMinMax ? item.minValue : null;
    item.maxValue = useMinMax ? item.maxValue : null;
    item.engUnit = useEngUnit ? item.engUnit : null;

    item.minValue = useMinMax ? item.minValue : null;
    item.maxValue = useMinMax ? item.maxValue : null;
    item.engUnit = useEngUnit ? item.engUnit : null;

    //set data type and update state
    item.dataType = lookupItem != null ? lookupItem :
        { id: -1, name: '', customTypeId: null, customType: null };
    if (settings != null) {
        settings = {
            ...settings,
            useMinMax: useMinMax,
            useEngUnit: useEngUnit,
            //showComposition: isComposition,
            //showInterface: isInterface,
            isCustomType: isCustom,
            //showDescription: !isInterface
        }
    }
    return { item: item, settings: settings };
};

//-------------------------------------------------------------------
// onChangeDataType: on change of data type, update item and settings values. 
//      Shared by attributeList.add, attributeItemRow inline edit and
//      attributeEntity edit 
//-------------------------------------------------------------------
export const onChangeVariableTypeShared = (val, item, lookupVariableTypes, lookupDataTypes) => {
    //e can be e.target.value or e.value - two different controls

    const match = lookupVariableTypes.find(vt => { return vt.id.toString() === val?.toString(); });

    //set variable type and update state
    //change item ref variable
    setVariableType(item, match);
    //reset data type value to force new selection
    item.dataType = _dataTypeInit;
};

const setVariableType = (item, val) => {
    //set variable type, item ref variable
    if (val == null) {
        item.variableTypeDefinition = null;
        item.variableTypeDefinitionId = null;
        return null;
    }
    else {
        item.variableTypeDefinition = {};
        item.variableTypeDefinitionId = val.id;
        item.variableTypeDefinition.id = val.id;
        item.variableTypeDefinition.name = val.name;
        item.variableTypeDefinition.browseName = val.browseName; //this becomes critical for adding on server side in new scenario
        return null;
    }
};


export const getPermittedDataTypesForAttribute = (attributeItem, lookupDataTypes, lookupVariableTypes) => {
    if (attributeItem.variableTypeDefinition != null) {
        var variableType = lookupVariableTypes.find(vt => { return vt.id.toString() === attributeItem.variableTypeDefinition.id?.toString(); });
        if (variableType != null) {
            return getPermittedDataTypesForVariableTypeById(variableType?.variableDataTypeId, lookupDataTypes);
        }
        return null;
    }
}

//-------------------------------------------------------------------
// onChangeAttributeTypeShared
//      Shared by attributeList.add, attributeItemRow inline edit and
//      attributeEntity edit 
//-------------------------------------------------------------------
export const onChangeAttributeTypeShared = (e, item, settings, lookupAttributeTypes, lookupDataTypes, lookupVariableTypes) => {

    var isComposition = false;
    var isStructure = false;
    var isInterface = false;
    var isEnumeration = false;
    var isDataVariable = false;
    var isProperty = false;

    var lookupItem = lookupAttributeTypes.find(dt => { return dt.id.toString() === e.target.value; });
    if (e.target.value == null || e.target.value.toString() === "-1") {
    }
    else {
        isComposition = lookupItem != null && lookupItem.id === AppSettings.AttributeTypeDefaults.CompositionId;
        isStructure = lookupItem != null && lookupItem.id === AppSettings.AttributeTypeDefaults.StructureId;
        isInterface = lookupItem != null && lookupItem.id === AppSettings.AttributeTypeDefaults.InterfaceId;
        isEnumeration = lookupItem != null && lookupItem.id === AppSettings.AttributeTypeDefaults.EnumerationId;
        isDataVariable = lookupItem != null && lookupItem.id === AppSettings.AttributeTypeDefaults.DataVariableId;
        isProperty = lookupItem != null && lookupItem.id === AppSettings.AttributeTypeDefaults.PropertyId;
    }

    //reset composition, interface object anytime this changes
    item.enumValue = null;
    item.composition = null;
    item.compositionId = -1;
    item.interface = null;
    item.interfaceId = -1;

    //set attribute type and update state
    item.attributeType = lookupItem != null ? lookupItem : { id: -1, name: ''};

    //if composition or interface selected, then set data type accordingly
    if (lookupItem != null && isComposition) {
        item.dataType = { id: AppSettings.DataTypeDefaults.CompositionId, name: 'Composition', customTypeId: null, customType: null };
    }
    else if (lookupItem != null && isInterface) {
        item.dataType = { id: -1, name: '', customTypeId: null, customType: null };
    }
    //set to Int64 or fallback of Int32 for enumeration field
    else if (lookupItem != null && isEnumeration) {
        var dataTypeInt = lookupDataTypes.find(dt => { return dt.name.toLowerCase().indexOf("int64") === 0 });
        dataTypeInt = dataTypeInt == null ? lookupDataTypes.find(dt => { return dt.name.toLowerCase().indexOf("int32") === 0 }) : dataTypeInt;
        item.dataType = dataTypeInt;
    }
    //set variable data type field default to BaseDataVariableType
    else if (lookupItem != null && isDataVariable) {
        const varTypeDefault = lookupVariableTypes.find(x => { return x.name?.toLowerCase().indexOf("basedatavariabletype") === 0 });
        setVariableType(item, varTypeDefault);
        //init data type
        item.dataType = _dataTypeInit;
    }
    else { //reset to select one...
        item.dataType = _dataTypeInit;
    }

    settings = {
        ...settings,
        showComposition: isComposition,
        showStructure: isStructure,
        showInterface: isInterface,
        showDescription: !isInterface,
        showEnumeration: isEnumeration,
        showVariableType: isDataVariable,
        showProperty: isProperty
    }

    return { item: item, settings: settings };
};

//-------------------------------------------------------------------
// onChangeDataType: on change of data type, update item and settings values. 
//      Shared by attributeList.add, attributeItemRow inline edit and
//      attributeEntity edit 
//-------------------------------------------------------------------
export const onChangeEngUnitShared = (val, item, lookupEngUnits) => {

    var match = lookupEngUnits.find(dt => { return dt.id.toString() === val.toString(); });

    //reset engunit
    item.engUnit = null;
    item.engUnitId = -1;

    //set engunit - can be null (if they select "-1" value)
    item.engUnit = match;
    item.engUnitId = match.id;

    return item;
};

//-------------------------------------------------------------------
// onChangeInterfaceShared
//      Shared by attributeList.add, attributeItemRow inline edit and
//      attributeEntity edit 
//-------------------------------------------------------------------
export const onChangeInterfaceShared = (e, item) => {
    //change item ref variable
    item.interfaceId = parseInt(e.value);
    if (e.value.toString() === "-1") {
        item.interface = null;
        item.interfaceId = null;
    }
    else {
        item.interface = {};
        item.interface.id = parseInt(e.value);
        item.interface.name = e.label;
    }
}

//-------------------------------------------------------------------
// onChangeCompositionShared
//      Shared by attributeList.add, attributeItemRow inline edit and
//      attributeEntity edit 
//-------------------------------------------------------------------
export const onChangeCompositionShared = (match, item) => {
    //change item ref variable
    if (match == null) {
        item.composition = null;
        item.compositionId = null;
        item.browseName = null;
        item.symbolicName = null;
    }
    else {
        // Preserve the intermediate object so we can find and update it in the backend
        const intermediateObjectId = item.composition?.intermediateObjectId;
        const intermediateObjectName = item.composition?.intermediateObjectName;

        item.composition = {};
        item.compositionId = match.id;
        item.composition.id = match.id;
        item.composition.name = match.name;
        item.composition.description = match.description;
        item.composition.browseName = match.browseName; 
        //this is what is used downstream. 
        item.composition.relatedProfileTypeDefinitionId = item.composition.id;
        item.composition.relatedName = item.composition.name;
        item.composition.intermediateObjectId = intermediateObjectId;
        item.composition.intermediateObjectName = intermediateObjectName;
        //copy some values into the profile_composition data record
    }
}


//-------------------------------------------------------------------
// Region: Shared render methods
//-------------------------------------------------------------------
export const renderAttributeIcon = (item, readOnly, className = '') => {
    //simplify icons
    //set up color properly
    const iconColor = readOnly ? color.nevada : color.mine;

    //set up icon properly
    let iconName = AppSettings.IconMapper.Attribute;

    if (item.dataType.id === AppSettings.DataTypeDefaults.CompositionId) iconName = AppSettings.IconMapper.TypeDefinition;

    if (item.interface != null) iconName = AppSettings.IconMapper.Interface;

    return (
        <span className={`me-2 ${className}`}>
            <SVGIcon name={iconName} size="20" fill={iconColor} />
        </span>
    );
}

//-------------------------------------------------------------------
// Region: Render Common variable type drop down list
//-------------------------------------------------------------------
export const renderVariableTypeUIShared = (editItem, lookupVariableTypes, settings, isValid, showLabel, onChangeCallback, onBlurCallback) => {
    if (lookupVariableTypes == null || lookupVariableTypes.length === 0 || !settings.showVariableType) return;
    const options = buildSelectOptionsByVariabletype(lookupVariableTypes);

    //map value bind to structure the control accepts
    const selValue = {
        label: editItem === {} || editItem.variableTypeDefinitionId == null || editItem.variableTypeDefinitionId.toString() === "-1" ?
            "Select" : editItem.variableTypeDefinition?.name, value : editItem.variableTypeDefinitionId?.id
    };

    return renderSelectGroupByUI(
        options,
        selValue,
        'typeDefinitionId',
        `Data Variable Type`,
        isValid,
        showLabel,
        onChangeCallback,
        onBlurCallback,
        true
    );
};

const buildSelectOptionsByVariabletype = (lookupItems) => {
    if (lookupItems == null || lookupItems.length === 0) return null;

    //this data is ordered by popularity - a combo of usage count and a manual rank count. Everytime
    //we hit a new popularity level, add a grouping row separator
    return buildSelectOptionsByPopularity(lookupItems, "name");
}

//-------------------------------------------------------------------
// Region: Render Common data type drop down list
//-------------------------------------------------------------------
export const renderDataTypeUIShared = (dataType, lookupDataTypes, typeDef, isValid, showLabel, labelOverride, onChangeCallback, onBlurCallback) => {
    if (lookupDataTypes == null || lookupDataTypes.length === 0) return;
    const options = buildSelectOptionsByDatatype(lookupDataTypes, typeDef?.type);

    //map value bind to structure the control accepts
    const selValue = {
        label: dataType?.id == null || dataType.id.toString() === "-1" ?
            "Select" : dataType.name, value: dataType?.id
    };

    return renderSelectGroupByUI(
        options,
        selValue,
        'ddlDatatype',
        labelOverride != null ? labelOverride : `Data Type`,
        isValid,
        showLabel,
        onChangeCallback,
        onBlurCallback
    );
};

const buildSelectOptionsByDatatype = (lookupItems, type, skipInterfaceAndCompositionType = false) => {
    if (lookupItems == null || lookupItems.length === 0) return null;

    //filter out certain items
    const lookupItemsFiltered = lookupItems.filter((item) => {
        if (type != null && type.name.toLowerCase() === 'interface' && item.code === 'interface') {
            //skip this one if we are on a profile of type interface. Interface profile can't add interfaces. It extends interfaces.
            return null;
        }
        if (skipInterfaceAndCompositionType &&
            (item.id === AppSettings.DataTypeDefaults.CompositionId || item.id === AppSettings.DataTypeDefaults.InterfaceId)) {
            return null;
        }
        return item;
    });

    //this data is ordered by popularity - a combo of usage count and a manual rank count. Everytime
    //we hit a new popularity level, add a grouping row separator
    return buildSelectOptionsByPopularity(lookupItemsFiltered, "name");
}

//-------------------------------------------------------------------
// Region: Render composition drop down list
//-------------------------------------------------------------------
export const renderCompositionSelectUIShared = (selItem, lookupItems, isValid, showLabel, onChangeCallback, onBlurCallback) => {
    if (lookupItems == null || lookupItems.length === 0) return;
    const options = buildSelectOptionsByProfile(lookupItems);

    //map value bind to structure the control accepts
    const selValue = {
        label: selItem === {} || selItem.compositionId == null || selItem.compositionId.toString() === "-1" ?
            "Select" : selItem.composition?.relatedName, value: selItem.composition?.id
    };

    return renderSelectGroupByUI(
        options,
        selValue,
        'compositionId',
        `Composition`,
        isValid,
        showLabel,
        onChangeCallback,
        onBlurCallback
    );
};

//-------------------------------------------------------------------
// Region: Render Interface drop down list
//-------------------------------------------------------------------
export const renderInterfaceSelectUIShared = (selItem, lookupItems, isValid, showLabel, onChangeCallback, onBlurCallback) => {
    if (lookupItems == null || lookupItems.length === 0) return;
    const options = buildSelectOptionsByProfile(lookupItems);

    //map value bind to structure the control accepts
    const selValue = {
        label: selItem === {} || selItem.interfaceId == null || selItem.interfaceId.toString() === "-1" ?
            "Select" : selItem.interface?.name, value: selItem.interface?.id
    };

    return renderSelectGroupByUI(
        options,
        selValue,
        'interfaceId',
        `Interface`,
        isValid,
        showLabel,
        onChangeCallback,
        onBlurCallback
    );
};

//-------------------------------------------------------------------
// Region: Shared multi-level select box render methods
//-------------------------------------------------------------------
// buildSelectOptionsByProfile: build a multi-level array grouped by profile then level 2 is type definition
const buildSelectOptionsByProfile = (lookupItems) => {
    if (lookupItems == null || lookupItems.length === 0) return null;

    //if we only have a single loading... item, then handle differently
    if (lookupItems.length === 1 && lookupItems[0].id === -1) {
        return [{ value: lookupItems[0].id, label: lookupItems[0].name }];
    }

    //this data is ordered by profileid then by type def name. Everytime
    //we hit a new profile id, add a grouping row separator
    let profileId = null;
    let result = [];
    let curGroup = {};
    lookupItems.forEach((item) => {
        //note - for custom types - api handles setting name properly
        //same popularity level, nothing special
        if (profileId == null || profileId !== item.profile?.id) {
            curGroup = { label: getProfileCaption(item.profile), options: [] };
            curGroup.options.push({ value: item.id, label: item.name });
            result.push(curGroup);
            profileId = item.profile.id;
        }
        else if (profileId === item.profile?.id) {
            curGroup.options.push({ value: item.id, label: item.name });
        }
    });

    return result;
}

//-------------------------------------------------------------------
// buildSelectOptionsByPopularity: build a multi-level array grouped by popularity indicator then level 2 is type definition
//-------------------------------------------------------------------
export const buildSelectOptionsByPopularity = (lookupItems, fldName = "name") => {
    if (lookupItems == null || lookupItems.length === 0) return null;

    //this data is ordered by popularity - a combo of usage count and a manual rank count. Everytime
    //we hit a new popularity level, add a grouping row separator
    let popularityLevel = null;
    let result = [];
    let curGroup = {};
    lookupItems.forEach((item) => {
        //same popularity level, nothing special
        if (popularityLevel == null || popularityLevel !== item.popularityLevel) {
            curGroup = { label: mapPopularityLevelToName(item.popularityLevel), options: [] };
            curGroup.options.push({ value: item.id, label: item[fldName] });
            result.push(curGroup);
            popularityLevel = item.popularityLevel;
        }
        else if (popularityLevel === item.popularityLevel) {
            curGroup.options.push({ value: item.id, label: item[fldName] });
        }
    });

    return result;
}

//-------------------------------------------------------------------
// renderSelectGroupByUI: build multi-level select box, label
//-------------------------------------------------------------------
const renderSelectGroupByUI = (
    options,
    selValue,
    id,
    lbl,
    isValid,
    showLabel,
    onChangeCallback,
    onBlurCallback,
    isClearable
) => {

    const styleCustom = {
        groupHeading: (provided, state) => ({
            ...provided,
            backgroundColor: "rgba(204, 204, 204, 0.3)",
            margin: 0,
            paddingTop: 10,
            paddingBottom: 10
        }),
        control: (provided, state) => ({
            ...provided,
            borderColor: !isValid ? "#dc3545" : "#ced4da"
        })
    }

    return (
        <Form.Group>
            {showLabel &&
                <Form.Label>{lbl}</Form.Label>
            }
            {(showLabel && isValid != null && !isValid) &&
                <span className="invalid-field-message inline">
                    Required
                </span>
            }
            <Select
                id={id}
                styles={styleCustom}
                value={selValue}
                defaultValue={{ label: "Select", value: "-1" }}
                onChange={onChangeCallback}
                onBlur={onBlurCallback}
                options={options}
                isClearable={isClearable}
            />
        </Form.Group>
    )
}


//-------------------------------------------------------------------
// Region: Render Common eng unit drop down list
//-------------------------------------------------------------------
export const renderEngUnitUIShared = (editItem, lookupEngUnits, isValid, onChangeCallback, onBlurCallback) => {
    if (lookupEngUnits == null || lookupEngUnits.length === 0) return;

    const options = buildSelectOptionsByPopularity(lookupEngUnits, "displayName");

    //map value bind to structure the control accepts
    const selValue = {
        label: editItem.engUnit?.id == null || editItem.engUnit.id.toString() === "-1" ?
            "Select" : editItem.engUnit.displayName,
        value: editItem.engUnit?.id == null ? -1 : editItem.engUnit.id
    };

    return renderSelectGroupByUI(
        options,
        selValue,
        'engUnit',
        `Eng Unit`,
        isValid,
        true,
        onChangeCallback,
        onBlurCallback
    );
};



//map a popularity level to a name in a multi-category drop down list - used by data type and eng unit
const mapPopularityLevelToName = (popularityLevel) => {
    switch (popularityLevel) {
        case 3:
            return 'Most Popular & Frequently Used';
        case 2:
            return 'Common';
        case 1:
            return 'Seldomly Used';
        default:
            return 'Other';
    }
}


