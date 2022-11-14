import React, { useState, useEffect } from 'react'
import { Breadcrumb } from 'react-bootstrap';

import { generateLogMessageString } from '../../utils/UtilityService'
import { getTypeDefEntityLink } from '../../services/ProfileService';

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

        async function bindData() {
            //console.log(generateLogMessageString('useEffect||bindData||async', CLASS_NAME));
            setItems(props.item.ancestory);
        }
        bindData();

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [props.item]);

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    if (props.item == null) return;

    if (_items == null || _items === []) return;

    //assume array ordered properly by inheritance
    const mainContent = _items.map((p, i) => {
        if (i < _items.length - 1) {
            return (
                <Breadcrumb.Item key={`crumb_${p.id}`} href={getTypeDefEntityLink(p)}>{p.name}</Breadcrumb.Item>
            );
        }
        else {
            return (
                <Breadcrumb.Item key={`crumb_${p.id}`} active>{p.name}</Breadcrumb.Item>
            );
        }
    });

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    //final render
    return (
        <Breadcrumb>
            <i className="material-icons mr-1 mr-md-2">schema</i>
            {mainContent}
        </Breadcrumb>
    );
}

export default ProfileBreadcrumbs;
