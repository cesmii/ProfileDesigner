import { AppSettings } from "./appsettings";

const _logMessageDelimiter = " || ";

///#region: Logging Helper Methods
///--------------------------------------------------------------------------
/// Generate Log Message String. This allows components to use this common message
/// format and log the message directly to the console so they can have line number and
/// file where message is generated.
/// Sample:
///  console.log(this.svcUtility.getlogMessageString("ngOnInit", CLASS_NAME, this.svcUtility.LogMessageSeverity.Info));
///--------------------------------------------------------------------------
export function generateLogMessageString(message, category = '', severity= "info") {
    var msg = { Code: '', Message: message, Category: category, Severity: severity, DateCreated: new Date() };
    return generateLogMessageStringModel(msg);
}

function generateLogMessageStringModel(msgModel) {
    return getTimeStamp(msgModel.DateCreated)
        + (msgModel.Category !== '' ? _logMessageDelimiter : '') + msgModel.Category
        + _logMessageDelimiter + msgModel.Severity.toString()
        + (msgModel.Code !== '' ? _logMessageDelimiter : '') + msgModel.Code
        + (msgModel.Message !== '' ? _logMessageDelimiter : '') + msgModel.Message;
}

///--------------------------------------------------------------------------
/// Log a formatted message
/// Sample:
///  this.svcUtility.logMessage("ngOnInit", CLASS_NAME, this.svcUtility.LogMessageSeverity.Info);
///--------------------------------------------------------------------------
export function logMessage(message, category = '', severity = "info") {
    //if (!isDevMode) return;
    //convert error object to string
    if (typeof message === "string") {//do nothing
    }
    else { message = JSON.stringify(message); }

    var msg = { Code: '', Message: message, Category: category, Severity: severity, DateCreated: new Date() };
    logMessageByModel(msg);
}

///--------------------------------------------------------------------------
/// Log a formatted message using the logMessage model as param
///--------------------------------------------------------------------------
function logMessageByModel(msgModel) {
    //if (!isDevMode) return;
    var formattedMsg = generateLogMessageStringModel(msgModel);
    switch (msgModel.Severity.toLowerCase()) {
        case "error":
        case "exception":
        case "critical":
            console.error(formattedMsg);
            break;
        case "warn":
        case "warning":
            console.warn(formattedMsg);
            break;
        case "debug":
            console.debug(formattedMsg);
            break;
        default:
            console.log(formattedMsg);
    }
}

///--------------------------------------------------------------------------
/// Log an elapsed time message. This is useful when trying to get elapsed time
/// for an action between two times. 
/// Sample:
///    this.svcUtility.logMessageElapsedTime("ngOnInit", CLASS_NAME, new Date('2018-01-21'), new Date());
///--------------------------------------------------------------------------
export function logMessageElapsedTime(message, category, startDate, endDate) {
    //if (!isDevMode) return;
    var msg = getlogMessageElapsedTimeModel(message, category, startDate, endDate);
    logMessageByModel(msg);
}

///--------------------------------------------------------------------------
/// Log an elapsed time message. This is useful when trying to get elapsed time
/// for an action between two times. 
/// Sample:
///    this.svcUtility.logMessageElapsedTime("ngOnInit", CLASS_NAME, new Date('2018-01-21'), new Date());
///--------------------------------------------------------------------------
export function getlogMessageElapsedTimeString(message, category, startDate, endDate) {
    var msg = getlogMessageElapsedTimeModel(message, category, startDate, endDate);
    var formattedMsg = generateLogMessageStringModel(msg);
    return formattedMsg;
}

///--------------------------------------------------------------------------
/// Shared function to get an elapsed time message model. This is useful when trying to get elapsed time
/// for an action between two times. 
/// Sample:
///    this.svcUtility.logMessageElapsedTime("ngOnInit", CLASS_NAME, new Date('2018-01-21'), new Date());
///--------------------------------------------------------------------------
function getlogMessageElapsedTimeModel(message, category, startDate, endDate) {
    var elapsed = endDate.getTime() - startDate.getTime();
    var elapsedMessage = 'Elasped Time: ' + elapsed + ' ms (Start: ' + getTimeStamp(startDate) +
        ', End: ' + getTimeStamp(endDate) + ')';
    var msg = {
        Code: '', Message: message == null || message === '' ? elapsedMessage : message + _logMessageDelimiter + elapsedMessage,
        Category: category, Severity: "info", DateCreated: endDate
    };
    return msg;
}

///--------------------------------------------------------------------------
/// Create a timestamp value in a consistent manner
///--------------------------------------------------------------------------
function getTimeStamp(d) {
    var result = (d.getHours() < 10 ? '0' : '') + d.getHours() + ':' +
        (d.getMinutes() < 10 ? '0' : '') + d.getMinutes() + ':' +
        (d.getSeconds() < 10 ? '0' : '') + d.getSeconds() + ':' +
        (d.getMilliseconds() < 10 ? '00' : (d.getMilliseconds() < 100 ? '0' : '')) + d.getMilliseconds();
    return result;
}
  ///#endregion: Logging Helper Methods

///--------------------------------------------------------------------------
/// Paging - Take an incoming datarows array and page it based on a start index, page size and array length
/// Client side paging - this assumes we have all the data pulled down. Only do this for smaller data sets. 
///--------------------------------------------------------------------------
export function pageDataRows(items, currentPage, pageSize) {
    if (items == null) return null;
    //null means show all
    if (pageSize == null) pageSize = items.length;

    var result = JSON.parse(JSON.stringify(items));
    //if item count < pageSize set paged == filtered
    if (items.length <= pageSize) {
        return result;
    }

    //calculate start and endindex and then slice pagedCopy 
    var startIndex = (currentPage - 1) * pageSize;
    var endIndex = startIndex + pageSize - 1;
    return result.slice(startIndex, endIndex + 1);
}

export function getUserPreferences() {
    var result = localStorage.getItem('userPreferences');
    if (result == null) return {
        profilePreferences: { pageSize: AppSettings.PageSize },
        attributePreferences: { pageSize: 5 },
        dependencyPreferences: { pageSize: 25 }
    };

    var item = JSON.parse(result);
    //handle null of individual preference items
    var needsUpdate = (item.profilePreferences == null || item.attributePreferences == null);
    if (item.profilePreferences == null) item.profilePreferences = { pageSize: AppSettings.PageSize };
    if (item.attributePreferences == null) item.attributePreferences = { pageSize: 5 };
    if (item.dependencyPreferences == null) item.dependencyPreferences = { pageSize: 25 };
    if (needsUpdate) localStorage.setItem('userPreferences', JSON.stringify(item));

    return item;
}

export function setUserPreferences(item) {
    localStorage.setItem('userPreferences', JSON.stringify(item));
}

export function concatenateField(items, fieldName, delimiter = ',') {
    if (items == null || items.length === 0) return "";
    var result = items.map((item) => {
        return item[fieldName];
    });
    return result.join(delimiter);
};

//TBD - move to profile service file
export function getProfileCaption(item) {
    if (item == null || item.type == null) return 'Profile';
    switch (item.type.name.toLowerCase()) {
        case 'interface':
            return 'Interface';
        case 'variabletype':
            return 'Variable Type';
        case 'abstract':
        case 'structure':
        case 'class':
        default:
            return 'Profile';
    }
}

//TBD - move to profile service file
export function getProfileIconName(item) {
    if (item == null || item.type == null) return 'profile';
    //TBD - eventually get icons specific for each type
    switch (item.type.name.toLowerCase()) {
        case "namespace":
            return "folder-profile";
        case 'interface':
            return 'key';
        case 'variabletype':
            return 'variabletype';
        case 'abstract':
        case 'structure':
        case 'class':
        default:
            return 'profile';
    }
}

///--------------------------------------------------------------------------
// ConvertToNumeric - use a data type to help guide how to convert a value to number
//--------------------------------------------------------------------------
//attribute min max, convert to numeric
export function convertToNumeric(dataType, val) {

    //don't attempt if any of these are the sole values
    if (val === '' || val === '-' || val === '.' || val === '-.') return val;

    //convert to numeric - if data type is numeric
    switch (dataType) {
        case "integer":
        case "long":
            return parseInt(val);
        case "double":
        case "float":
        case "-1":
        case -1:
        case null:
            return parseFloat(val);
        default:
            return val;
    }
}

///--------------------------------------------------------------------------
// take a number and trim off the '.'. Make a '10.' to 
//--------------------------------------------------------------------------
export function toInt(val) {
    //don't attempt if any of these are the sole values
    if (val === '' || val === '-' || val === '.' || val === '-.') return val;

    //handle the -.# scenario
    if (isNaN(parseInt(val))) {
        return 0;
    }
    return parseInt(val);
}

///--------------------------------------------------------------------------
// validateNumeric - test whether an input value is numeric based on a data type 
//--------------------------------------------------------------------------
export function validateNumeric(dataType, val) {
    //var result = null;

    //don't attempt if any of these are the sole values
    if (val == null || val === '' || val === '-' || val === '.' || val === '-.') return true;

    return !isNaN(val);

    ////convert to numeric - if data type is numeric
    //switch (dataType) {
    //    case "integer":
    //    case "long":
    //        result = parseInt(val);
    //        break;
    //    case "double":
    //    case "float":
    //    case "-1":
    //    case -1:
    //    default:
    //        result = parseFloat(val);
    //        break;
    //}
    //return !isNaN(result);

}

///--------------------------------------------------------------------------
// Perform a numeric only check for numeric fields. Call this from onchange 
// to prevent keying in alpha chars. 
//--------------------------------------------------------------------------
export function onChangeNumericKeysOnly(e) {

    return !(e.target.value !== ''
        && e.target.value !== '-'
        && e.target.value !== '.'
        && e.target.value !== '-.'
        && isNaN(e.target.value))
}

///--------------------------------------------------------------------------
// Prepare a properly formatted download link to download the profile json (or any other json)
//--------------------------------------------------------------------------
export async function downloadFileJSON(data, fileName) {
    const json = JSON.stringify(data);
    const blob = new Blob([json], { type: 'application/json' });
    const href = await URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = href;
    link.download = fileName + ".json";
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}
