import { Form } from 'react-bootstrap';
import Select from 'react-select';

import { SVGIcon } from '../components/SVGIcon'
import color from '../components/Constants'
import { AppSettings } from '../utils/appsettings';
import { getUserPreferences, setUserPreferences, convertToNumeric, validateNumeric, validate_NoSpecialCharacters } from '../utils/UtilityService';

//const CLASS_NAME = "AttributeService";

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

export const validate_dataType = (val) => {
    return val != null && val.id.toString() !== "-1";
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
export const validate_All = (item, editSettings, allAttributes) => {
    var result = {
        name: validate_name(item.name, item),
        nameDuplicate: validate_nameDuplicate(item.name, item, allAttributes),
        dataType: item.attributeType?.id === AppSettings.AttributeTypeDefaults.InterfaceId ||
            item.attributeType?.id === AppSettings.AttributeTypeDefaults.EnumerationId || validate_dataType(item.dataType),
        composition: item.attributeType?.id !== AppSettings.AttributeTypeDefaults.CompositionId || 
            (item.composition != null && item.compositionId > 0),
        structure: item.attributeType?.id !== AppSettings.AttributeTypeDefaults.StructureId || validate_dataType(item.dataType),
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

    settings = {
        ...settings,
        useMinMax: useMinMax,
        useEngUnit: useEngUnit,
        //showComposition: isComposition,
        //showInterface: isInterface,
        isCustomType: isCustom,
        //showDescription: !isInterface
    }

    return { item: item, settings: settings };
};

//-------------------------------------------------------------------
// onChangeAttributeTypeShared
//      Shared by attributeList.add, attributeItemRow inline edit and
//      attributeEntity edit 
//-------------------------------------------------------------------
export const onChangeAttributeTypeShared = (e, item, settings, lookupAttributeTypes, lookupDataTypes) => {

    var isComposition = false;
    var isStructure = false;
    var isInterface = false;
    var isEnumeration = false;

    var lookupItem = lookupAttributeTypes.find(dt => { return dt.id.toString() === e.target.value; });
    if (e.target.value == null || e.target.value.toString() === "-1") {
    }
    else {
        isComposition = lookupItem != null && lookupItem.id === AppSettings.AttributeTypeDefaults.CompositionId;
        isStructure = lookupItem != null && lookupItem.id === AppSettings.AttributeTypeDefaults.StructureId;
        isInterface = lookupItem != null && lookupItem.id === AppSettings.AttributeTypeDefaults.InterfaceId;
        isEnumeration = lookupItem != null && lookupItem.id === AppSettings.AttributeTypeDefaults.EnumerationId;
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
    else { //reset to select one...
        item.dataType = { id: -1, name: '', customTypeId: null, customType: null };
    }

    settings = {
        ...settings,
        showComposition: isComposition,
        showStructure: isStructure,
        showInterface: isInterface,
        showDescription: !isInterface,
        showEnumeration: isEnumeration
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

    return item;
};

//-------------------------------------------------------------------
// onChangeInterfaceShared
//      Shared by attributeList.add, attributeItemRow inline edit and
//      attributeEntity edit 
//-------------------------------------------------------------------
export const onChangeInterfaceShared = (e, item) => {
    //change item ref variable
    item.interfaceId = parseInt(e.target.value);
    if (e.target.value.toString() === "-1") {
        item.interface = null;
        item.interfaceId = null;
    }
    else {
        item.interface = {};
        item.interface.id = parseInt(e.target.value);
        item.interface.name = e.target.options[e.target.selectedIndex].text;
    }
}

//-------------------------------------------------------------------
// onChangeCompositionShared
//      Shared by attributeList.add, attributeItemRow inline edit and
//      attributeEntity edit 
//-------------------------------------------------------------------
export const onChangeCompositionShared = (e, item) => {
    //change item ref variable
    item.compositionId = parseInt(e.target.value);
    if (e.target.value.toString() === "-1") {
        item.composition = null;
        item.compositionId = null;
    }
    else {
        item.composition = {};
        item.composition.id = parseInt(e.target.value);
        item.composition.name = e.target.options[e.target.selectedIndex].text;
        //this is what is used downstream. 
        item.composition.relatedProfileTypeDefinitionId = item.composition.id;
        item.composition.relatedName = item.composition.name;
    }
}


//-------------------------------------------------------------------
// Region: Shared render methods
//-------------------------------------------------------------------
export const renderAttributeIcon = (item) => {
    //set up color properly
    //var iconColor = (currentUserId == null || currentUserId !== item.author.id) ? color.silver : color.shark;
    var iconColor = item._itemType == null || item._itemType === "profile" ? color.shark : color.silver;

    //set up icon properly
    var iconName = item._itemType == null || item._itemType === "profile" ? "account-circle" : "group";

    if (item.dataType.id === AppSettings.DataTypeDefaults.CompositionId) iconName = "profile";

    //custom type - special icon type
    if (item.dataType.customType != null) iconName = "customdatatype";

    if (item.interface != null) iconName = "key";

    return (
        <span className="mr-2">
            <SVGIcon name={iconName} size="24" fill={iconColor} />
        </span>
    );
}

//-------------------------------------------------------------------
// Region: Render Common data type drop down list
//-------------------------------------------------------------------
export const renderDataTypeUIShared = (editItem, lookupDataTypes, typeDef, isValid, showLabel, onChangeCallback, onBlurCallback) => {
    if (lookupDataTypes == null || lookupDataTypes.length === 0) return;
    const options = renderDataTypeSelectOptions(lookupDataTypes, typeDef?.type);

    //map value bind to structure the control accepts
    var selValue = {
        label: editItem.dataType.id == null || editItem.dataType.id.toString() === "-1" ?
            "Select" : editItem.dataType.name, value: editItem.dataType.id
    };

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
                <Form.Label>Data Type</Form.Label>
            }
            {(showLabel && !isValid) &&
                <span className="invalid-field-message inline">
                    Required
                </span>
            }
            <Select
                id="dataType"
                styles={styleCustom}
                value={selValue}
                defaultValue={{ label: "Select", value: "-1" }}
                onChange={onChangeCallback}
                onBlur={onBlurCallback}
                options={options}
            />
        </Form.Group>
    )
};

const renderDataTypeSelectOptions = (lookupDataTypes, type, skipInterfaceAndCompositionType = false) => {
    if (lookupDataTypes == null || lookupDataTypes.length === 0) return null;

    //this data is ordered by popularity - a combo of usage count and a manual rank count. Everytime
    //we hit a new popularity level, add a grouping row separator
    var popularityLevel = null;
    var result = [];
    var curGroup = {};
    lookupDataTypes.forEach((item) => {
        if (type != null && type.name.toLowerCase() === 'interface' && item.code === 'interface') {
            //skip this one if we are on a profile of type interface. Interface profile can't add interfaces. It extends interfaces.
            return null;
        }
        if (skipInterfaceAndCompositionType &&
            (item.id === AppSettings.DataTypeDefaults.CompositionId || item.id === AppSettings.DataTypeDefaults.InterfaceId)) {
            return null;
        }

        //note - for custom types - api handles setting name properly
        //same popularity level, nothing special
        if (popularityLevel == null || popularityLevel !== item.popularityLevel) {
            curGroup = { label: mapPopularityLevelToName(item.popularityLevel), options: [] };
            curGroup.options.push({ value: item.id, label: item.name });
            result.push(curGroup);
            popularityLevel = item.popularityLevel;
        }
        else if (popularityLevel === item.popularityLevel) {
            curGroup.options.push({ value: item.id, label: item.name });
        }
    });

    return result;
}


//-------------------------------------------------------------------
// Region: Render Common eng unit drop down list
//-------------------------------------------------------------------
export const renderEngUnitUIShared = (editItem, lookupEngUnits, onChangeCallback, onBlurCallback) => {
    if (lookupEngUnits == null || lookupEngUnits.length === 0) return;

    const options = renderEngUnitSelectOptions(lookupEngUnits);

    //map value bind to structure the control accepts
    var selValue = {
        label: editItem.engUnit?.id == null || editItem.engUnit.id.toString() === "-1" ?
            "Select" : editItem.engUnit.name,
        value: editItem.engUnit?.id == null ? -1 : editItem.engUnit.id
    };

    const styleCustom = {
        groupHeading: (provided, state) => ({
            ...provided,
            backgroundColor: "rgba(204, 204, 204, 0.3)",
            margin: 0,
            paddingTop: 10,
            paddingBottom: 10
        })
    }

    return (
        <Form.Group>
            <Form.Label>Eng Unit</Form.Label>
            <Select
                id="engUnit"
                styles={styleCustom}
                value={selValue}
                defaultValue={{ label: "Select", value: "-1" }}
                onChange={onChangeCallback}
                onBlur={onBlurCallback}
                options={options}
            />
        </Form.Group>
    )
};

export const renderEngUnitSelectOptions = (lookupEngUnits) => {
    if (lookupEngUnits == null || lookupEngUnits.length === 0) return null;

    //this data is ordered by popularity - a combo of usage count and a manual rank count. Everytime
    //we hit a new popularity level, add a grouping row separator
    var popularityLevel = null;
    var result = [];
    var curGroup = {};
    lookupEngUnits.forEach((item) => {
        //same popularity level, nothing special
        if (popularityLevel == null || popularityLevel !== item.popularityLevel) {
            curGroup = { label: mapPopularityLevelToName(item.popularityLevel), options: [] };
            curGroup.options.push({ value: item.id, label: item.displayName });
            result.push(curGroup);
            popularityLevel = item.popularityLevel;
        }
        else if (popularityLevel === item.popularityLevel) {
            curGroup.options.push({ value: item.id, label: item.displayName });
        }
    });

    return result;
}


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

