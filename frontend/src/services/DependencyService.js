import { getUserPreferences, setUserPreferences } from '../utils/UtilityService';

//const CLASS_NAME = "DependencyService";

export function getDependencyPreferences() {
    var item = getUserPreferences();
    return item.dependencyPreferences;
}

export function setDependencyPageSize(val) {
    var item = getUserPreferences();
    item.dependencyPreferences.pageSize = val;
    setUserPreferences(item);
}
