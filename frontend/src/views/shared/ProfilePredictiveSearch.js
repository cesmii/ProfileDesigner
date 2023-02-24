import React, { useState, useEffect } from 'react'
import axios from 'axios'
import { Dropdown } from 'react-bootstrap'

import { generateLogMessageString, concatenateField } from '../../utils/UtilityService'
import { AppSettings } from '../../utils/appsettings';
import { renderTypeIcon } from './ProfileRenderHelpers';
import { SVGIcon } from '../../components/SVGIcon'

const CLASS_NAME = "ProfilePredictiveSearch";

function ProfilePredictiveSearch(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const _entityUrlProfile = "/type/:id";
    const [_items, setItems] = useState([]);

    //-------------------------------------------------------------------
    // Region: Get the ancestors for this item and all of its siblings in a sorted array
    //-------------------------------------------------------------------
    useEffect(() => {
        if (props.filterVal == null || props.filterVal === '') {
            setItems([]);
        }

        //TBD - in final system, the API would handle finding all matching namespaces
        //function processNamespaces(data) {

        //    if (data == null || data.length === 0) return [];

        //    var result = [];
        //    data = data.filter(function (p, i) {
        //        return (p.namespace.toLowerCase().indexOf(props.filterVal) > -1);
        //    });
        //    //only add the namespace once
        //    data.forEach(function (p, i) {
        //        var j = result.findIndex(x => x.name === p.namespace);
        //        //new one found, add
        //        if (j <= -1) {
        //            result.push({
        //                id: i, name: p.namespace, childCount: 1, metaTags: p.metaTags, type: { id: -1, name: "namespace" },
        //                url: _entityUrlNamespace.replace(':namespace', encodeURIComponent(p.namespace))
        //            });
        //        }
        //        //existing one found, append
        //        else {
        //            result[j].childCount += 1;
        //            //namespaces[j].metaTags = p.metaTags; //tbd - add unique metatags
        //        }

        //    });

        //    return result;
        //}

        //TBD - in final system, the API would handle finding all matching profiles
        function processProfiles(data) {

            if (data == null || data.length === 0) return [];

            //TBD - in final system, the API would go find the list of items
            const delimiter = ":::";
            var result = []; //do foreach so we get a unified array with common object
            data.forEach(function (p, i) {
                var metaTagsConcatenated = concatenateField(p.metaTags, "name", delimiter);
                var concatenatedSearch = delimiter + p.name + delimiter
                    + p.description.toLowerCase() + delimiter
                    + (p.author != null ? p.author.firstName + delimiter : "")
                    + (p.author != null ? p.author.lastName : "") + delimiter
                    + metaTagsConcatenated + delimiter;
                if (concatenatedSearch.toLowerCase().indexOf(props.filterVal.toLowerCase()) !== -1) {
                    result.push({
                        //id: p.id, name: `${p.name} [${p.namespace}]`, childCount: 1, metaTags: p.metaTags, type: { id: p.type.id, name: p.type.name },
                        id: p.id, name: p.name, childCount: 1, metaTags: p.metaTags, type: { id: p.type.id, name: p.type.name }, author: p.author,
                        url: _entityUrlProfile.replace(':id', p.id)
                    });
                }
            });

            return result;
        }

        //TBD - move the fetches into a common area so they can be used by multiple components
        async function fetchData() {
            console.log(generateLogMessageString('useEffect||fetchProfileData||async', CLASS_NAME));

            if (props.filterVal == null || props.filterVal === '') return;

            //TBD - in final system, the API would handle finding all namespaces in profiles
            //const result = await axios(`${AppSettings.BASE_API_URL}/profile`);
            //var namespaces = processNamespaces(result.data);

            //TBD - infinal system, the API would go find the list of items
            const result = await axios(`${AppSettings.BASE_API_URL}/profile`);
            var profiles = processProfiles(result.data);

            //concatenate the data, sort the data
            //var combinedData = namespaces.concat(profilesz);

            profiles.sort((a, b) => {
                if (a.name.toLowerCase() < b.name.toLowerCase()) {
                    return -1;
                }
                if (a.name.toLowerCase() > b.name.toLowerCase()) {
                    return 1;
                }
                //secondary sort by object type
                if (a.type.name.toLowerCase() < b.type.name.toLowerCase()) {
                    return -1;
                }
                if (a.type.name.toLowerCase() > b.type.name.toLowerCase()) {
                    return 1;
                }
                return 0;
            }); //sort by name then type

            //TBD - add in support for paging

            //set state on fetch of data
            setItems(JSON.parse(JSON.stringify(profiles)));
        }

        fetchData();

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [props.filterVal]);

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderSearchResults = () => {
        //no results
        if (_items == null || props.filterVal == null || props.filterVal === '') {
            return (
                <Dropdown.Item>
                    Enter all or partial profile name, namespace, metatags, author to search
                </Dropdown.Item>
            );
        }

        if (_items.length === 0) {
            return (
                <Dropdown.Item>
                    There are no profiles or namespaces matching your criteria
                </Dropdown.Item>
            );
        }

        //assume array ordered properly
        const result = _items.map((p, i) => {
            //show a type icon and the name or namespace
            return (
                <Dropdown.Item key={i} href={p.url} >
                    {renderTypeIcon(p, props.activeAccount, 24)}
                    {p.name}
                </Dropdown.Item>
            );
        });

        return (
            <>
            {result}
            </>
        );
    };

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------

    //if (props.filterVal == null) return;
    // onClick={onToggleClick} 
    //return final ui
    return (
        <>
            {/*<Button variant="secondary" onClick={onToggleClick} type="submit" >
                <span>
                        <SVGIcon name="search" />
                </span>
            </Button>*/}
            <Dropdown className="m-0 p-0 action-menu" onClick={(e) => e.stopPropagation()} >
                <Dropdown.Toggle drop="left" className="btn-search p-0 px-2 border border-left-0">
                    {/*
                    <Button variant="search" className="p-0 pl-2 pr-2 border-left-0">
                        <SVGIcon name="search" />
                    </Button>
                    */}
                    <span className="p-0 px-2 border border-left-0">
                        <SVGIcon name="search" />
                    </span>
                </Dropdown.Toggle>
                
                <Dropdown.Menu>
                    {renderSearchResults()}
                </Dropdown.Menu>
                    
        
            </Dropdown>
        </>
    )
}

export default ProfilePredictiveSearch;
