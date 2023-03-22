
import { getUserPreferences, setUserPreferences, generateLogMessageString, concatenateField, validate_Required, validate_namespaceFormat } from '../utils/UtilityService';
import axiosInstance from './AxiosService';

const CLASS_NAME = "ProfileService";

//-------------------------------------------------------------------
// Region: Common / Helper Profile Methods
//-------------------------------------------------------------------
export const profileNew  = { id: 0, namespace: '', version: null, publishDate: null, authorId: null }

//-------------------------------------------------------------------
// filterProfiles - filter profiles by searching in multiple fields
// TBD - phase II, the API would handle finding all matching profiles
//-------------------------------------------------------------------
export function filterProfiles(items, filterVal) {

    if (items == null || items.length === 0) return [];

    if (filterVal == null || filterVal === '') return JSON.parse(JSON.stringify(items));

    //TBD - in phase II, the API would find and return the list of items
    const delimiter = ":::";
    var result = []; //do foreach so we get a unified array with common object
    items.forEach(function (p, i) {
        var metaTagsConcatenated = concatenateField(p.metaTags, "name", delimiter);
        var concatenatedSearch = delimiter + p.name + delimiter
            + p.description.toLowerCase() + delimiter
            + (p.author != null ? p.author.firstName + delimiter : "")
            + (p.author != null ? p.author.lastName : "") + delimiter
            + metaTagsConcatenated + delimiter;
        if (concatenatedSearch.toLowerCase().indexOf(filterVal.toLowerCase()) !== -1) {
            result.push(JSON.parse(JSON.stringify(p)));
        }
    });

    return result;
}

//-------------------------------------------------------------------
// getProfileCaption - Get a consistently formatted profile caption for use in various ui elements
//-------------------------------------------------------------------
export function getProfileCaption(item) {
    const namespace = `${item.namespace}${item.version == null ? '' : ` (v ${item.version})`}`;
    return item.title == null || item.title === '' ? namespace : `${item.title} (${namespace})`;
}


//-------------------------------------------------------------------
// getProfileEntityLink - get a profile link based on current user id
//-------------------------------------------------------------------
export function getTypeDefEntityLink(item) {
    if (item == null) return;
    if (item.relatedProfileTypeDefinitionId != null)
    {
        return `/type/${item.relatedProfileTypeDefinitionId}`;
    }
    return `/type/${item.id}`;
    //if (currentUserId == null || currentUserId !== item.author.id)
    //    return `/type/${item.id}/view`;
    //if (currentUserId != null && currentUserId === item.author.id)
    //    return `/type/${item.id}/edit`;
};

//-------------------------------------------------------------------
// getProfileTypePreferences, setProfileTypePageSize - get/set commonly shared user preferences for a profile type def (ie page size)
//-------------------------------------------------------------------
export function getTypeDefPreferences() {
    const item = getUserPreferences();
    return item.typeDefPreferences;
}

export function setProfileTypePageSize(val) {
    const item = getUserPreferences();
    item.typeDefPreferences.pageSize = val;
    setUserPreferences(item);
}

export function setProfileTypeDisplayMode(val) {
    const item = getUserPreferences();
    item.typeDefPreferences.displayMode = val;
    setUserPreferences(item);
}

//-------------------------------------------------------------------
// getProfilePreferences, setProfilePageSize - get/set commonly shared user preferences for a profile (ie page size)
//-------------------------------------------------------------------
export function getProfilePreferences() {
    const item = getUserPreferences();
    return item.profilePreferences;
}

export function setProfilePageSize(val) {
    const item = getUserPreferences();
    item.profilePreferences.pageSize = val;
    setUserPreferences(item);
}

//-------------------------------------------------------------------
// Region: Common search criteria functions
//-------------------------------------------------------------------
//Clear out all search criteria values
export function clearSearchCriteria (criteria) {

    var result = JSON.parse(JSON.stringify(criteria));

    //loop over parents then over children and set selected to false
    if (result.filters != null)
    {
        result.filters.forEach(parent => {
            if (parent != null)
            {
            parent.items.forEach(item => {
                item.selected = false;
            });
            }
        });
    }

    result.query = null;
    result.skip = 0;
    return result;
}

//-------------------------------------------------------------------
//Find and toggle (set selected) a search filter item. If not present, put warning in console but do not cause exception
export function toggleSearchFilterSelected (criteria, parentId, id) {

    let item = findSearchFilter(criteria, parentId, id);
    if (item == null) return;

    //toggle the selection or set for initial scenario
    item.selected = !item.selected;
}

//-------------------------------------------------------------------
//Check if a search filter exists. If not present, return null, else return filter.
export function findSearchFilter(criteria, parentId, id) {

    //loop through filters and their items and find the id
    //note it won't stop the foreach loop even if it finds it. Account for that.
    const parent = criteria.filters.find(x => { return x.id.toString() === parentId.toString(); });
    if (parent == null) {
        console.warn(generateLogMessageString(`toggleSearchFilterValue||Could not find parent with id: ${parentId.toString()} in lookup data`, CLASS_NAME));
        return null;
    }
    const item = parent.items.find(x => { return x.id.toString() === id.toString(); });
    if (item == null) {
        console.warn(generateLogMessageString(`toggleSearchFilterValue||Could not find item with id: ${id.toString()} in lookup data`, CLASS_NAME));
        return null;
    }

    return item;
}


//-------------------------------------------------------------------
// Region: Solution Explorer - All profiles
//-------------------------------------------------------------------
//TBD - move this into a context paradigm
export function buildSolutionExplorer(items) {
    console.log(generateLogMessageString(`buildSolutionExplorer`, CLASS_NAME));
    //filter out items with no parent - root level, sort by name
    let result = items.filter((p) => {
        return p.parent == null;
    });
    result.sort((a, b) => {
        if (a.name.toLowerCase() < b.name.toLowerCase()) {
            return -1;
        }
        if (a.name.toLowerCase() > b.name.toLowerCase()) {
            return 1;
        }
        return 0;
    }); //sort by name
    //maintain a remaining items and dwindle this down till nothing is left
    let remainingItems = items.filter((p) => {
        return p.parent != null;
    });
    //go through current level and pull out items from source that have this item as a parent
    result.forEach((p) => {
        p = buildSolutionExplorerRecursive(p, remainingItems);
    });
    //TBD - cache result. hook up to a context so that it is automatically updated
    return result;
}

//-------------------------------------------------------------------
// buildSolutionExplorerRecursive - build a nested representation of profiles
//-------------------------------------------------------------------
function buildSolutionExplorerRecursive(p, remainingItems) {
    //go through current level and pull out items from source that have this item as a parent
    var children = remainingItems.filter((c) => {
        return c.parent != null && c.parent.id === p.id;
    });
    //keep only remaining items that were not just identified as children
    remainingItems = remainingItems.filter((c) => {
        return c.parent != null && c.parent.id !== p.id;
    });
    //sort children by name
    children.sort((a, b) => {
        if (a.name.toLowerCase() < b.name.toLowerCase()) {
            return -1;
        }
        if (a.name.toLowerCase() > b.name.toLowerCase()) {
            return 1;
        }
        return 0;
    });
    //add children to items, on each add, recursively call this
    p.children = children;
    p.children.forEach((c) => {
        c = buildSolutionExplorerRecursive(c, remainingItems);
    });
    return p;
}

//-------------------------------------------------------------------
// filterSolutionExplorer - filter out the profiles and show as a flat list
//-------------------------------------------------------------------
export function filterSolutionExplorer(items, filterVal) {
    var matches = filterProfiles(items, filterVal);
    return buildSolutionExplorer(matches);
}



//-------------------------------------------------------------------
// Shared validation - multiple components call these
//-------------------------------------------------------------------
//validate all - call from button click
export const validate_All = (item) => {
    var result = {
        namespace: validate_Required(item.namespace),
        namespaceFormat: validate_namespaceFormat(item.namespace)
    };

    return result;
}

//separate method does the validation, this is the logic to determine 
//what constitutes validity
export const isProfileValid = (isValid) => {
    return (isValid.namespace && isValid.namespaceFormat);
}


export const saveProfile = (item, onSaveCallback, onSaveCallbackError) => {
    //perform insert/update call
    console.log(generateLogMessageString(`handleOnSave||${item.id == null || item.id === 0 ? `add` : `update`}`, CLASS_NAME));
    const url = item.id == null || item.id === 0 ? `profile/add` : `profile/update`;
    axiosInstance.post(url, item)
        .then(resp => {

            if (resp.data.isSuccess) {
                //callback to parent
                item.id = resp.data.data;
                if (onSaveCallback != null) onSaveCallback(item);
            }
            else {
                //callback to parent w/ error
                console.warn(generateLogMessageString('handleOnSave||error||' + resp.data.message, CLASS_NAME, 'error'));
                if (onSaveCallbackError != null) onSaveCallbackError(resp.data.message);
            }

        })
        .catch(error => {
            //hide a spinner, show a message
            if (onSaveCallbackError != null) onSaveCallbackError(JSON.stringify(error));
            console.error(generateLogMessageString('handleOnSave||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
        });
};
