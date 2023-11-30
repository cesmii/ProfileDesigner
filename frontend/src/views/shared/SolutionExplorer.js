import React, { useState, useEffect} from 'react'
import Form from 'react-bootstrap/Form'
import FormControl from 'react-bootstrap/FormControl'
import InputGroup from 'react-bootstrap/InputGroup'
import Button from 'react-bootstrap/Button'
import axios from 'axios'

import { AppSettings } from '../../utils/appsettings'
import { generateLogMessageString } from '../../utils/UtilityService'
import { filterSolutionExplorer, filterProfiles } from '../../services/ProfileService';
import { renderTypeIcon, renderLinkedName } from './ProfileRenderHelpers';

import { SVGIcon } from '../../components/SVGicon'
import '../styles/ProfileExplorer.scss';

const CLASS_NAME = "SolutionExplorer";

function SolutionExplorer(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [_items, setItems] = useState({all:[], filtered:[], explorer:[]});
    const [_filterVal, setFilterVal] = useState('');

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    ////trigger search after x chars entered or search button click
    const onSearchBlur = (e) => {
        console.log(generateLogMessageString(`onSearchBlur||Search value: ${e.target.value}`, CLASS_NAME));
        var matches = filterProfiles(_items.all, e.target.value);
        //set state on filter of data
        setItems({ all: _items.all, filtered: matches, explorer: _items.explorer });
        setFilterVal(e.target.value);
    }

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchData() {

            const url = `${AppSettings.BASE_API_URL}/profile`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));
            const result = await axios(url);

            var explorer = filterSolutionExplorer(result.data, null);
            //set state on fetch of data
            setItems({ all: result.data, filtered: result.data, explorer: explorer });
        }
        fetchData();
        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [props.profile]);

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    //render child items
    const renderChildren = (items, level, key) => {
        if (items == null || items.length === 0) return;
        //recursively build out children
        const childItems = items.map((p) => {
            return renderItem(p, level + 1);
        });

        return (
            <ul id={key} key={key} className={"profile-explorer children"} >
                {childItems}
            </ul>
        );
    }

    //render item
    const renderItem = (p, level) => {
        var key = `li_${level.toString()}_${p.id.toString()}`;
        var cssClass = `small-size${props.currentProfileId == null || props.currentProfileId !== p.id ? '' : ' current'}`;
        //dynamically increase padding for each level, the built in pl-n only worked till pl-5.
        // var padding = (level * 3).toString() + '%';
        var padding = (level * 4).toString() + 'px';
        return (
            <>
                <li id={key} key={key} className={cssClass} >
                    <div style={{ paddingLeft: padding}}>
                        {renderTypeIcon(p, props.activeAccount)}
                        {renderLinkedName(p)}
                    </div>
                    {/*recursively build out children*/}
                    {renderChildren(p.children, level, `ul_${level.toString()}_${p.id.toString()}`)}
                </li>
            </>
        );
    }

    const renderSearchUI = () => {
        return (
            <Form className="header-search-block">
                <div className="row m-0" >
                    <InputGroup>
                        <FormControl
                            type="text"
                            placeholder="Search profiles"
                            aria-label="Enter text to filter by"
                            val={_filterVal}
                            onBlur={onSearchBlur}
                        />
                        {/*Button is just visual cue, search is happening on blur*/}
                        <Button variant="icon-outline" title="Filter explorer view">
                            <SVGIcon name="search" />
                        </Button>
                    </InputGroup>
                </div>
            </Form>
        );
    }

    //render the main grid
    const renderExplorer = () => {
        if (_items == null || _items.explorer == null || _items.explorer.length === 0) {
            return (
                <div className="alert alert-info-custom mt-2 mb-2">
                    <div className="text-center" >There are no matching profiles.</div>
                </div>
            )
        }
        const mainBody = _items.explorer.map((p) => {
            return renderItem(p, 1);
        });

        return (
            <ul id="explorer" key="explorer" className="profile-explorer root">
                {mainBody}
            </ul>
        );
    }

    const renderFiltered = () => {
        if (_items == null || _items.filtered == null || _items.filtered.length === 0) {
            return (
                <div className="alert alert-info-custom mt-2 mb-2">
                    <div className="text-center" >There are no matching profiles.</div>
                </div>
            )
        }
        const mainBody = _items.filtered.map((p) => {
            return renderItem(p, 1);
        });

        return (
            <ul id="filtered" key="filtered" className="profile-explorer root">
                {mainBody}
            </ul>
        );
    }
    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <div className="profile-explorer-container"> 
            <h2 className="m-2" >Profile Explorer</h2>
            {renderSearchUI()}
            { _filterVal == null || _filterVal === '' ?
                renderExplorer() : renderFiltered()
            }
        </div>
    )
}

export default SolutionExplorer;