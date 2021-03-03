import React, { useState, useEffect, Fragment } from 'react'
import axios from 'axios'

import { generateLogMessageString } from '../../utils/UtilityService'
import { AppSettings } from '../../utils/appsettings';
import { renderLinkedName } from './ProfileRenderHelpers';

import { SVGIcon } from '../../components/SVGIcon'
import color from '../../components/Constants'
import '../styles/ProfileBreadcrumbs.scss';

const CLASS_NAME = "ProfileBreadcrumbs";

function ProfileBreadcrumbs(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [_items, setItems] = useState([]);

    //-------------------------------------------------------------------
    // Region: Get the ancestors for this item and all of its siblings in a sorted array
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchData() {
            //console.log(generateLogMessageString('useEffect||fetchData||async', CLASS_NAME));

            //TBD - in phase II, the API would go find the list of items
            const result = await axios(`${AppSettings.BASE_API_URL}/profile`);

            //TBD - in phase II, the API would go find the list of items
            var breadCrumbs = [];
            //loop to put ancestors in, start with item and then parent
            var curItem = props.item;
            //var curItem = props.item.parentProfile == null ? null : result.data.find(x => x.id === props.item.parentProfile.id);
            var level = 0;
            while (curItem != null) {
                curItem.level = level;
                breadCrumbs.push(curItem);
                //find parent of curItem
                var parentId = (curItem.parentProfile == null ? null : curItem.parentProfile.id);
                curItem = curItem.parentProfile == null ? null : result.data.find(x => x.id === parentId);
                level -= 1;
            }
            //put siblings (exclude self) in then sort by level then by name
            //var siblings = result.data.filter((p) => {
            //    var isSibling = (p.parentProfile != null && p.parentProfile.id === props.item.parentProfile.id && p.id !== props.item.id);
            //    if (isSibling) p.level = 1;
            //    return (isSibling);
            //});
            ////join ancestors and siblings
            //inheritanceTree = inheritanceTree.concat(siblings);

            //sort by level then by name
            breadCrumbs.sort((a, b) => {
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

            //set item state value
            setItems(breadCrumbs);
        }

        fetchData();

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [props.item]);

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------

    if (props.item == null) return;

    if (_items == null || _items === []) return;

    //return final ui
    //assume array ordered properly by inheritance
    const result = _items.map((p, i) => {
        var delimiter = i < _items.length - 1 ? (<span className="mr-2" >/</span>) : "";
        return (
            <Fragment key={`crumb_${p.id}`} >
                {i < _items.length - 1 ? renderLinkedName(p, 'mr-2') : p.name }
                { delimiter }
            </Fragment>
        );
    });

    return (
        <div key="breadcrumbs" className="row breadcrumbs m-0">
            <div className="col-sm-12 m-0 p-0">
                <span className="mr-2" ><SVGIcon name="schema" size="18" fill={color.shark} alt="breadcrumbs" /></span>
                {result}
            </div>
        </div>
    );
}

export default ProfileBreadcrumbs;
