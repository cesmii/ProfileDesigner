///--------------------------------------------------------------------------
/// Global constants - purely static settings that remain unchanged during the lifecycle
///--------------------------------------------------------------------------
export const AppSettings = {
    BASE_API_URL: process.env.REACT_APP_BASE_API_URL  //api server url - environment specific
    , Titles: { Anonymous: 'CESMII | SM Profile Designer', Main: 'CESMII | SM Profile Designer', Caption: 'SM Profile™ Designer' }
    , PageSize: 25
    , PageSizeOptions: [10, 25, 50, 100]
    , DateSettings: {
        DateFormat: 'M/d/yyyy'
        , DateFormat_Grid: 'MM/dd/yyyy'
        , DateTimeFormat_Grid: 'M/dd/yyyy h:mm a'
        , DateTimeZeroDate_Grid: '1900-01-01T00:00:00'
    }
    , SelectOneCaption: '--Select One--'
    , Messages: {
        SessionTimeoutMessage: "Your session has timed out. Please log in to continue."
        , RouteForbiddenMessage: "You are not permitted to enter this area. You have been redirected to the home page."
        , InvalidPasswordMessage: {
            MinLength: "Password must be at least 8 characters"
            , UpperCase: "Password must include an uppercase character"
            , LowerCase: "Password must include a lowercase character"
            , Number: "Password must include a number"
            , SpecialCharacter: "Password must contain a special character (ie: $@!%*?&)"
            , Zipper: "Invalid password. "
        }
    }
    , ValidatorPatterns: {
        //Email: '^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,4}$',
        Password: '(?=[^a-z]*[a-z])'
        , HasUpperLowerCase: '^(?=.*[a-z])(?=[A-Z])$' // (?=.*[A-Za-z])'
        , HasNumber: '^[0-9]$'
        , HasSpecialCharacter: '^[a-z]$' //'^[$@!%*?&]$'
        , MinLength: '{8,}'
    }
    , ProfileTypeDefaults: {
        InterfaceId: 1,
        ClassId: 2,
        VariableTypeId: 12,
        CustomDataTypeId: 3,
        StructureId: 18,
        EnumerationId: 19,
        ObjectId: 11
    }
    , DataTypeDefaults: {
        CompositionId: 1
        , InterfaceId: 2
        , VariableTypeId: 10
    }
    , AttributeTypeDefaults: {
        EnumerationId: 8
        , CompositionId: 9
        , InterfaceId: 10
        , StructureId: 7
    }
    , SearchCriteriaCategory: {
        Author: 1,
        Popular: 2,
        TypeDefinitionType: 3,
        Profile: 4
    }
    , ImportLogStatus: {
        NotStarted: 13,
        InProgress: 14,
        Completed: 15,
        Failed: 16,
        Cancelled: 17
    }
}

export const LookupData = {
    dataTypes: [
          { caption: "Boolean", val: "boolean", useMinMax: false, useEngUnit: false }
        , { caption: "Char", val: "char", useMinMax: false, useEngUnit: false }
        , { caption: "Composition", val: "composition", useMinMax: false, useEngUnit: false }
        , { caption: "Double", val: "double", useMinMax: true, useEngUnit: true }
        , { caption: "Float", val: "float", useMinMax: true, useEngUnit: true }
        , { caption: "Integer", val: "integer", useMinMax: true, useEngUnit: true }
        , { caption: "Long (int)", val: "long", useMinMax: true, useEngUnit: true }
        , { caption: "String", val: "string", useMinMax: false, useEngUnit: false }
        , { caption: "Interface", val: "interface", useMinMax: false, useEngUnit: false }
    ],
    //engUnits: [
    //      { caption: "Other", val: "other" }
    //    , { caption: "Duration (hr)", val: "hour" }
    //    , { caption: "Duration (sec)", val: "second" }
    //    , { caption: "Duration (ms)", val: "millisecond" }
    //    , { caption: "Duration (tick)", val: "tick" }
    //    , { caption: "Length (m)", val: "meter" }
    //    , { caption: "Length (cm)", val: "centimeter" }
    //    , { caption: "Length (mm)", val: "millimeter" }
    //    , { caption: "Length (inch)", val: "inch" }
    //    , { caption: "Length (ft)", val: "foot" }
    //    , { caption: "Mass (Kg)", val: "kilogram" }
    //    , { caption: "Mass (g)", val: "gram" }
    //    , { caption: "Temperature (C)", val: "centigrade" }
    //    , { caption: "Termperature (F)", val: "farenheit" }
    //    , { caption: "Termperature (K)", val: "kelvin" }
    //    , { caption: "Volume (liter)", val: "liter" }
    //    , { caption: "Volume (gallon)", val: "gallon" }
    //    , { caption: "Volume (cc)", val: "cubic centimeter" }
    //    , { caption: "Weight (lbs)", val: "pound" }
    //    , { caption: "Weight (ozs)", val: "ounce" }
    //],
    //profileTypes: [
    //     { id: 1, name: "Interface" }
    //    ,{ id: 2, name: "Class" }
    //    ,{ id: 3, name: "VariableType" }
    //],
    searchFields: [
          { caption: "Author", val: "author.fullName", dataType: "string" }
        , { caption: "Description", val: "description", dataType: "string" }
        , { caption: "Id", val: "id", dataType: "numeric" }
        , { caption: "Interface", val: "interface.name", dataType: "string" }
        , { caption: "Meta Tags", val: "metaTagsConcatenated", dataType: "string" }
        , { caption: "Name", val: "name", dataType: "string" }
        , { caption: "Type", val: "type.name", dataType: "string" }
    ],
    searchOperators: [
        { caption: "Contains", val: "contain", dataType: "string" }
        , { caption: "Equals", val: "equal", dataType: "string" }
        , { caption: "Does not contain", val: "!contain", dataType: "string" }
        , { caption: "=<", val: "lte", dataType: "numeric" }
        , { caption: "<", val: "lt", dataType: "numeric" }
        , { caption: "=", val: "=", dataType: "numeric" }
        , { caption: ">", val: "gt", dataType: "numeric" }
        , { caption: ">=", val: "gte", dataType: "numeric" }
        , { caption: "<>", val: "!equal", dataType: "numeric" }
    ]
}

