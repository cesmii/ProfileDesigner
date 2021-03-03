import { getUserPreferences, setUserPreferences, convertToNumeric, validateNumeric } from '../utils/UtilityService';

//const CLASS_NAME = "AttributeService";

export const attributeNew = {
    parentId: null, id: null, name: '', dataType: -1, minValue: '', maxValue: '', engUnit: -1, compositionId: -1, composition: null,
    interfaceId: -1, interface: null, variableTypeId: -1, variableType: null, description: null, displayName: null
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

//validate all - call from button click
export const validate_All = (item, editSettings, allAttributes) => {
    var result = {
        name: validate_name(item.name, item),
        nameDuplicate: validate_nameDuplicate(item.name, item, allAttributes),
        dataType: validate_dataType(item.dataType),
        minMax: validate_minMax(item.minValue, item.maxValue, item.dataType, editSettings),
        minIsNumeric: validateNumeric(item.dataType, item.minValue),
        maxIsNumeric: validateNumeric(item.dataType, item.maxValue),
        instrumentMinMax: validate_minMax(item.instrumentMinValue, item.instrumentMaxValue, item.dataType, editSettings),
        instrumentMinIsNumeric: validateNumeric(item.dataType, item.instrumentMminValue),
        instrumentMaxIsNumeric: validateNumeric(item.dataType, item.instrumentMaxValue),
        engUnit: validate_engUnit(item.engUnit, editSettings)
    };

    return result;
}





