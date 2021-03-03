import { LookupData } from '../utils/appsettings'

import { getUserPreferences, setUserPreferences, generateLogMessageString, concatenateField } from '../utils/UtilityService';

const CLASS_NAME = "ProfileService";

//-------------------------------------------------------------------
// Region: Common / Helper Profile Methods
//-------------------------------------------------------------------
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
// getProfileEntityLink - get a profile link based on current user id
//-------------------------------------------------------------------
export function getProfileEntityLink(item) {
    if (item == null) return;
    return `/profile/${item.id}`;
    //if (currentUserId == null || currentUserId !== item.author.id)
    //    return `/profile/${item.id}/view`;
    //if (currentUserId != null && currentUserId === item.author.id)
    //    return `/profile/${item.id}/edit`;
};

//-------------------------------------------------------------------
// getProfilePreferences, setProfilePreferences - get/set commonly shared user preferences for a profile (ie page size)
//-------------------------------------------------------------------
export function getProfilePreferences() {
    var item = getUserPreferences();
    return item.profilePreferences;
}

export function setProfilePageSize(val) {
    var item = getUserPreferences();
    item.profilePreferences.pageSize = val;
    setUserPreferences(item);
}

//-------------------------------------------------------------------
// Region: Solution Explorer - All profiles
//-------------------------------------------------------------------
//TBD - move this into a context paradigm
export function buildSolutionExplorer(items) {
    console.log(generateLogMessageString(`buildSolutionExplorer`, CLASS_NAME));
    //filter out items with no parent - root level, sort by name
    var result = items.filter((p) => {
        return p.parentProfile == null;
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
    var remainingItems = items.filter((p) => {
        return p.parentProfile != null;
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
        return c.parentProfile != null && c.parentProfile.id === p.id;
    });
    //keep only remaining items that were not just identified as children
    remainingItems = remainingItems.filter((c) => {
        return c.parentProfile != null && c.parentProfile.id !== p.id;
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
// Region: Profile Explorer Methods (individual profile)
//-------------------------------------------------------------------
//-------------------------------------------------------------------
// getProfileInheritanceTree - hierarchical representation of items
//-------------------------------------------------------------------
export function getProfileInheritanceTree(items, item, includeSiblings = false) {
    //TBD - in phase II, the API find and return the list of items
    var hierarchy = [];
    //loop to put ancestors in, start with item and then parent
    var level = 0;
    var profileId = item.id;
    var itemCur = JSON.parse(JSON.stringify(item));
    while (itemCur != null) {
        itemCur.level = level;
        //add the parent to result and then position current result items as children
        //make copies so we don't get circular dependencies and a tangled mess
        if (hierarchy !== []) {
            itemCur.children = [];
            itemCur.children = JSON.parse(JSON.stringify(hierarchy));
            //re-initialize the current level and continue upward
            hierarchy = [];
        }

        //set the current item as the root and then work upward
        hierarchy.push(JSON.parse(JSON.stringify(itemCur)));

        //find parent of curItem
        var parentId = (itemCur.parentProfile == null ? null : itemCur.parentProfile.id);
        itemCur = itemCur.parentProfile == null ? null : items.find(x => x.id === parentId);

        //on first pass through, set the siblings on the same level as the root profile
        //optional - put siblings (exclude self) in then sort by level then by name
        if (includeSiblings && level === 0 && itemCur != null) {
            var siblings = items.filter((p) => {
                var isSibling = (p.parentProfile != null && p.parentProfile.id === itemCur.id && p.id !== profileId);
                if (isSibling) p.level = level;
                return (isSibling);
            });
            //concat siblings w/ original first item
            hierarchy = hierarchy.concat(siblings);
        }
        //decrement level
        level -= 1;
    }
/*
    //sort by level then by name
    result.sort((a, b) => {
        if (a.level < b.level) {
            return -1;
        }
        if (a.level > b.level) {
            return 1;
        }
        //secondary sort by name
        if (a.name.toLowerCase() < b.name.toLowerCase()) {
            return -1;
        }
        if (a.name.toLowerCase() > b.name.toLowerCase()) {
            return 1;
        }
        return 0;
    }); //sort by level then name
*/
    return hierarchy;
}

//-------------------------------------------------------------------
// getProfileDependencies - get the profiles that depend on this profile
//-------------------------------------------------------------------
export function getProfileDependencies(id, items) {
    //TBD - handle this server side in final system
    //filter out only items where props.profile.id is a parent profile or in the attributes or extended attributes
    var result = items.filter(function (p) {
        //if a parent id dependency
        if (p.parentProfile != null && p.parentProfile.id === id) return true;
        //if an attribute has a composition dependency or a variable type dependency 
        if (p.profileAttributes != null) {
            var x = p.profileAttributes.findIndex(attr => {
                return (attr.composition != null && attr.composition.id === id) || (attr.variableType != null && attr.variableType.id === id);
            });
            if (x > -1) return true;
        }
        //if a base attribute has a composition dependency or a variable type dependency
        if (p.extendedProfileAttributes != null) {
            var y = p.extendedProfileAttributes.findIndex(attr => {
                return (attr.composition != null && attr.composition.id === id) || (attr.variableType != null && attr.variableType.id === id);
            });
            if (y > -1) return true;
        }
        //interfaces - this item has interfaces and one of those interfaces is item.id
        if (p.interfaces != null) {
            var z = p.interfaces.findIndex(i => {return (i.id === id);});
            if (z > -1) return true;
        } 
        //if we get here, false
        return false;
    });

    return result;
}

//-------------------------------------------------------------------
// getProfileCompositions - get the profiles that are used as compositions in attributes or extended attributes
//-------------------------------------------------------------------
export function getProfileCompositions(item, items) {
    //TBD - handle this server side in final system
    //search for profiles that are used in attributes, extended attributes of this profile
    var result = [];

    var attrList = item.profileAttributes == null ? [] : item.profileAttributes.filter(function (a) {
        return (a.dataType.toLowerCase() === 'composition' && a.composition != null);
    });
    var extendedAttrList = item.extendedProfileAttributes == null ? [] : item.extendedProfileAttributes.filter(function (a) {
        return (a.dataType.toLowerCase() === 'composition' && a.composition != null);
    });

    //instead of returning profiles, return attributes so we can show user the attr. name, etc. 
    //and show how a profile may have multiple pointers to the same type of profile (ie front axle, rear axle both point to axle.)
    //loop over concatenated list, find associated profiles
    result = attrList.concat(extendedAttrList);
    //for each composition, get the full profile record so we can use that downstream
    result.forEach(function (a) {
        var match = items.find(p => { return p.id === a.composition.id; });
        if (match != null) {
            a.composition = JSON.parse(JSON.stringify(match));
        }
    });

    //sort by attr name then by composition name
    result.sort((a, b) => {
        if (a.name.toLowerCase() < b.name.toLowerCase()) {
            return -1;
        }
        if (a.name.toLowerCase() > b.name.toLowerCase()) {
            return 1;
        }
        //secondary sort by name
        if (a.composition.name < b.composition.name) {
            return -1;
        }
        if (a.composition.name > b.composition.name) {
            return 1;
        }
        return 0;
    }); //sort by level then name

    return result;
}

//-------------------------------------------------------------------
// getProfileInterfaces - get the profiles that are used as interfaces for this item
//-------------------------------------------------------------------
export function getProfileInterfaces(item, items) {
    //TBD - handle this server side in final system
    //search for profiles that are used as an interface of this profile
    if (item.interfaces == null || item.interfaces.length === 0) return [];
    var result = [];
    //for each interface, get the full profile record so we can use that downstream
    item.interfaces.forEach(function (i) {
        var match = items.find(p => { return p.id === i.id; });
        if (match != null) {
            result.push(JSON.parse(JSON.stringify(match)));
        }
    });

    //sort by attr name then by composition name
    result.sort((a, b) => {
        if (a.name.toLowerCase() < b.name.toLowerCase()) {
            return -1;
        }
        if (a.name.toLowerCase() > b.name.toLowerCase()) {
            return 1;
        }
        return 0;
    }); //sort by level then name

    return result;
}

//-------------------------------------------------------------------
// getProfileCompositionsLookup - get the profiles that can be potentially used 
//-------------------------------------------------------------------
export function getProfileCompositionsLookup(id, items) {
    //TBD - handle this server side in final system
    //search for profiles that are used in attributes, extended attributes of this profile
    var result = [];

    //get related items so we know what to trim out 
    var item = items.find(p => { return p.id === id; });
    var tree = item == null ? [] : getProfileInheritanceTree(items, item, true);
    var dependencies = getProfileDependencies(id, items);

    //remove items of a different type, remove self
    result = items.filter(function (p) {
        return (item == null || (p.type != null && p.type.name === item.type.name && p.id !== item.id));
    });

    //trim inherited items - trims param by ref
    trimInheritedItems(tree, result);

    //trim dependencies - if no dependency found, keep it
    result = result.filter(function (p) {
        //if item is a dependency, return false
        return dependencies.find(d => { return d.id === p.id; }) == null;
    });

    //sort by name 
    result.sort((a, b) => {
        if (a.name.toLowerCase() < b.name.toLowerCase()) {
            return -1;
        }
        if (a.name.toLowerCase() > b.name.toLowerCase()) {
            return 1;
        }
        return 0;
    }); //sort by name

    return result;
}

//-------------------------------------------------------------------
// getProfileInterfacesLookup - get the profiles that can be potentially used 
//-------------------------------------------------------------------
export function getProfileInterfacesLookup(id, items) {
    //TBD - handle this server side in final system
    //search for profiles that are used in attributes, extended attributes of this profile
    var result = [];

    //get related items so we know what to trim out 
    var item = items.find(p => { return p.id === id; });

    //remove items of a different type, remove self
    result = items.filter(function (p) {
        return (p.type != null && p.type.name.toLowerCase() === "interface") && (item == null || p.id !== item.id);
    });

    //remove interfaces already used by profile. 
    if (item != null && item.interfaces != null) {
        result = result.filter(function (p) {
            return (item.interfaces.find((i) => { return i.id === p.id; }) == null);
        });
    }

    //sort by name 
    result.sort((a, b) => {
        if (a.name.toLowerCase() < b.name.toLowerCase()) {
            return -1;
        }
        if (a.name.toLowerCase() > b.name.toLowerCase()) {
            return 1;
        }
        return 0;
    }); //sort by name

    return result;
}

//-------------------------------------------------------------------
// getProfileVariableTypesLookup - get the profiles that can be potentially used
//-------------------------------------------------------------------
export function getProfileDataTypesLookup(id, items) {
    //TBD - handle this server side in final system
    var result = [];

    //get related items so we know what to trim out 
    var item = items.find(p => { return p.id === id; });

    //remove items of a different type, remove self, add special value field to indicate variable type and this id
    result = items.map(function (p) {
        if ((p.type != null && p.type.name.toLowerCase() === "variabletype") && (item == null || p.id !== item.id)) {
            return { caption: p.name, val: p.id.toString(), useMinMax: false, useEngUnit: false, isVariableType: true};
        }
        else {
            return null;
        }
        //return (p.type != null && p.type.name.toLowerCase() === "variabletype") && (item == null || p.id !== item.id);
    }).filter(p => { return p != null});

    //combine list with static look up data types list
    result = result.concat(LookupData.dataTypes);

    //sort by name 
    result.sort((a, b) => {
        if (a.caption.toLowerCase() < b.caption.toLowerCase()) {
            return -1;
        }
        if (a.caption.toLowerCase() > b.caption.toLowerCase()) {
            return 1;
        }
        return 0;
    }); //sort by name

    return result;
}

//-------------------------------------------------------------------
// Scour the inheritance tree and trim out items in the lookup list that exist in the tree.
//-------------------------------------------------------------------
function trimInheritedItems (tree, items) {
    tree.forEach(t => {
        //remove the item from the array that matches this tree item
        var z = items.findIndex(p => { return (p.id === t.id); });
        if (z > -1) items.splice(z, 1);
        //if this node has children, recursively call this
        if (t.children != null) trimInheritedItems(t.children, items);
    });
};

