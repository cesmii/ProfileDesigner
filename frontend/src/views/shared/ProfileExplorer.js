import React, { useState, useEffect, Fragment } from 'react'
import Form from 'react-bootstrap/Form'
import FormControl from 'react-bootstrap/FormControl'
import InputGroup from 'react-bootstrap/InputGroup'
import Button from 'react-bootstrap/Button'
import axios from 'axios'

import { AppSettings } from '../../utils/appsettings'
import { generateLogMessageString } from '../../utils/UtilityService'
//import { useAuthContext } from "../../components/authentication/AuthContext";
import { filterProfiles, getProfileDependencies, getProfileInheritanceTree, getProfileInterfaces, getProfileCompositions, getProfileEntityLink } from '../../services/ProfileService';
import { renderIcon, renderLinkedName } from './ProfileRenderHelpers';

import { SVGIcon } from '../../components/SVGIcon'
import color from '../../components/Constants'
import '../styles/ProfileExplorer.scss';

const CLASS_NAME = "ProfileExplorer";
var childCount = 0;

function ProfileExplorer(props) {

    //-------------------------------------------------------------------
    // Region: Todo list
    //-------------------------------------------------------------------
    //make the profile inheritance nested
    //all ui treatment - all stuff right now is placeholder
    //search ui is under construction

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    //const { authTicket } = useAuthContext();
    const [_items, setItems] = useState({
        item: {},
        all: { inheritanceTree: [], compositions: [], dependencies: [], interfaces: [] },
        filtered: { inheritanceTree: [], compositions: [], dependencies: [], interfaces: [] }
    });
    const [_toggleStates, setToggleStates] = useState({inheritanceTree: true, compositions: false, dependencies: false, interfaces: false});
    const [_filterVal, setFilterVal] = useState('');

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    ////trigger search after x chars entered or search button click
    const onSearchBlur = (e) => {
        console.log(generateLogMessageString(`onSearchBlur||Search value: ${e.target.value}`, CLASS_NAME));

        //return if the user clears the search. we just show the all data
        if (e.target.value === '') {
            setFilterVal(e.target.value);
            return;
        }

        //filter out the list of items per category
        var tree = filterProfiles(_items.all.inheritanceTree, e.target.value);
        var dependencies = filterProfiles(_items.all.dependencies, e.target.value);
        var compositions = filterCompositions(_items.all.compositions, e.target.value);
        var interfaces = filterProfiles(_items.all.interfaces, e.target.value);

        //set state on filter of data
        setItems({
            item: _items.item,
            all: { inheritanceTree: _items.inheritanceTree, compositions: _items.compositions, dependencies: _items.dependencies, interfaces: _items.interfaces },
            filtered: { inheritanceTree: tree, compositions: compositions, dependencies: dependencies, interfaces: interfaces }
        });
        setFilterVal(e.target.value);
        //open up all sections
        setToggleStates({ inheritanceTree: true, compositions: true, dependencies: true, interfaces: true });

    }

    ////expand/collapse child section
    const toggleChildren = (e) => {
        console.log(generateLogMessageString(`toggleChildren||${e.target.id}`, CLASS_NAME));
        switch (e.currentTarget.id.toLowerCase()) {
            case "inheritancetree":
                _toggleStates.inheritanceTree = !_toggleStates.inheritanceTree;
                break;
            case "compositions":
                _toggleStates.compositions = !_toggleStates.compositions;
                break;
            case "dependencies":
                _toggleStates.dependencies = !_toggleStates.dependencies;
                break;
            case "interfaces":
                _toggleStates.interfaces = !_toggleStates.interfaces;
                break;
            default:
                return;
        }
        setToggleStates(JSON.parse(JSON.stringify(_toggleStates)));
    }

    // Apply filter on data starting with all rows
    const filterCompositions = (items, val) => {
        const delimiter = ":::";

        if (items == null) return null;

        var filteredCopy = JSON.parse(JSON.stringify(items));

        if (val == null || val === '') {
            return filteredCopy;
        }

        // Filter data - match up against a number of fields
        return filteredCopy.filter((item, i) => {
            var concatenatedSearch = delimiter + item.name.toLowerCase() + delimiter
                + item.composition.name.toLowerCase() + delimiter
            return (concatenatedSearch.indexOf(val.toLowerCase()) !== -1);
        });
    }


    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchData() {

            //hardcode for now
            var url = `${AppSettings.BASE_API_URL}/profile`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));
            const result = await axios(url);

            var item = result.data.find(p => { return p.id === props.currentProfileId; });
            if (item == null) {
                console.log(generateLogMessageString(`useEffect||fetchData||error - Profile (id: ${props.currentProfileId}) not found`, CLASS_NAME, 'error'));
                return;
            }
            var tree = getProfileInheritanceTree(result.data, item, true);
            var dependencies = getProfileDependencies(props.currentProfileId, result.data);
            var compositions = getProfileCompositions(item, result.data);
            var interfaces = getProfileInterfaces(item, result.data);

            //set state on fetch of data
            setItems({
                item: item,
                all: { inheritanceTree: tree, compositions: compositions, dependencies: dependencies, interfaces: interfaces },
                filtered: { inheritanceTree: tree, compositions: compositions, dependencies: dependencies, interfaces: interfaces }
            });

        }
        fetchData();
        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [props.currentProfileId, props.currentUserId]);

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    //render an icon per section
    const renderSectionHeaderIcon = (sectionId) => {
        var iconName = "profile";
        switch (sectionId.toLowerCase()) {
            case "inheritancetree":
                iconName = 'account-tree';
                break;
            case "compositions":
                iconName = 'profile';
                break;
            case "dependencies":
                iconName = 'folder-profile';
                break;
            case "interfaces":
                iconName = 'key';
                break;
            default:
                iconName = 'profile';
                break;
        }

        var svg = (<SVGIcon name={iconName} size="18" fill={color.shark} alt={iconName} />);

        return (<span>{svg}</span>)

    };

    //render child items
    const renderChildren = (items, level, key) => {
        if (items == null || items.length === 0) return;
        //recursively build out children
        const childItems = items.map((p) => {
            return renderProfileItem(p, level + 1);
        });

        return (
            <ul id={key} key={key} className={"profile-explorer children"} >
                {childItems}
            </ul>
        );
    }

    //render a profile item - supports a nested view
    const renderProfileItem = (p, level) => {
        var key = `li_${level.toString()}_${p.id.toString()}`;
        var cssClass = `small-size${props.currentProfileId == null || props.currentProfileId !== p.id ? '' : ' current'}`;
        // dynamically increase padding for each level
        var padding = (level * 16).toString() + 'px';
               
        //::::::::::::::::::::::
        // Increment the child count
        childCount++;

        return (
            <li id={key} key={key} className={cssClass} >
                <div style={{ paddingLeft: padding }} className="hierarchy-link d-flex pr-3">
                    {renderIcon(p, props.currentUserId, 18)}
                    <span className="hierarchy-item ml-2 flex-grow-1">{renderLinkedName(p)}</span>
                    {/* Affordance for "go-to / view" */}
                    <SVGIcon name="chevron-right" size="24" fill={color.silver} className="view-affordance-icon float-right" />
                </div>
                {/*recursively build out children*/}
                {renderChildren(p.children, level, `ul_${level.toString()}_${p.id.toString()}`)}
            </li>
        );
    }

    // render a profile item - supports a nested view
    const renderCompositionItem = (a) => {
        var key = `li_attr_${a.id.toString()}`;
        var cssClass = `small-size`;

        //::::::::::::::::::::::
        // Increment the child count
        childCount++;

        return (
            <li id={key} key={key} className={cssClass} >
                <div className="composition-link d-flex pr-3">
                    {renderIcon(a.composition, props.currentUserId, 18, color.nevada)}
                    <span className="composition-item ml-2 flex-grow-1">{renderLinkedCompositionName(a, props.currentUserId)}</span>
                    {/* Affordance for "go-to / view" */}
                    <SVGIcon name="chevron-right" size="24" fill={color.silver} className="view-affordance-icon float-right"/>
                </div>
            </li>
        );
    }

    //
    const renderLinkedCompositionName = (item, currentUserId) => {
        if (item == null) return;
        var href = getProfileEntityLink(item.composition);
        return (
            <a href={href} >{`${item.name} (${item.composition.name})`}</a>
        );
    };

    //
    const renderSearchUI = () => {
        return (
            <Form className="header-search-block">
                <Form.Row className="m-0" >
                    
                    <InputGroup className="quick-search">
                        <FormControl
                            type="text"
                            placeholder="Filter"
                            aria-label="Filter"
                            aria-describedby="basic-addon2"
                            val={_filterVal}
                            onBlur={onSearchBlur}
                            className= "border-right-0"
                        />
                        <InputGroup.Append>
                            <Button variant="search" className="p-0 pl-2 pr-2 border-left-0" >
                                <SVGIcon name="search" size="24" fill={color.shark}/>
                            </Button>
                        </InputGroup.Append>
                    </InputGroup>
                    
                    
                </Form.Row>
            </Form>
        );
    }

    const renderSectionHeader = (items, caption, toggleState, sectionId) => {
        var toggleCss = toggleState ? "expanded d-flex align-items-center action-menu" : "d-flex align-items-center action-menu";
        var toggleIcon = toggleState ? "arrow-drop-up" : "arrow-drop-down";
        var myCount = childCount;
        childCount = 0;
        return (
            <>
                <div id={sectionId} key={sectionId} className={toggleCss} onClick={toggleChildren} >
                    {renderSectionHeaderIcon(sectionId)}
                    <span key="caption" className="caption">
                        {/* INCLUDES CHILD COUNT */}
                        {caption} <span className="small-size ml-2"> ({myCount})</span>
                    </span>
                    {(items != null && items.length > 0) ?
                        <span key="toggle" className="ml-auto">
                            <Button variant="accordion" className="btn" >
                                <span>
                                    <SVGIcon name={toggleIcon} size="24" fill={color.shark} alt={caption} className="toggle-icon"/>
                                </span>
                            </Button>
                        </span> :
                        <span key="toggle" className="ml-auto">
                            <Button variant="accordion" className="btn" >
                                <span>
                                    <SVGIcon name={toggleIcon} size="24" fill={color.transparent} alt={caption} className="toggle-icon empty"/>
                                </span>
                            </Button>
                        </span> 
                    }
                </div>
            </>
        )
    };

    //render the main area - this is called for each section (inheritance tree, dependencies, compositions, interfaces)
    const renderSection = (items, caption, toggleState, sectionId) => {
        var toggleCss = toggleState ? "expanded" : "collapsed";

        //has children scenario
        const mainBody = items.map((p) => {
            if (sectionId === "compositions") return renderCompositionItem(p);
            else return renderProfileItem(p, 2);
        });

        return (
            <Fragment key={sectionId}>
                {renderSectionHeader(items, caption, toggleState, sectionId)}
                <ul className={`profile-explorer children ${toggleCss}`}>
                    {mainBody}
                </ul>
            </Fragment>
        );
    }

    //render the main area
    const renderExplorer = () => {
        if (_items == null) {
            return (
                <div className="alert alert-info-custom mt-2 mb-2">
                    {/* <div className="text-center small" >There are no matching items.</div> */}
                </div>
            )
        }
        return (
            <ul id="explorer" key="explorer" className="profile-explorer root">
                <li id="tree" key="tree" >
                    {renderSection(_items.all.inheritanceTree, 'Profile Hierarchy', _toggleStates.inheritanceTree, 'inheritanceTree')}
                </li>
                <li id="interfaces" key="interfaces" >
                    {renderSection(_items.all.interfaces, 'Interfaces', _toggleStates.interfaces, 'interfaces')}
                </li>
                <li id="compositions" key="compositions" >
                    {renderSection(_items.all.compositions, 'Compositions', _toggleStates.compositions, 'compositions')}
                </li>
                <li id="dependencies" key="dependencies" >
                    {renderSection(_items.all.dependencies, 'Dependencies', _toggleStates.dependencies, 'dependencies')}
                </li>
            </ul>
        );
    }

    const renderFiltered = () => {
        if (_items == null) {
            return (
                <div className="alert alert-info-custom mt-2 mb-2">
                    <div className="text-center small" >There are no matching items.</div>
                </div>
            )
        }
        return (
            <ul id="explorer" key="explorer" className="profile-explorer root">
                <li id="tree" key="tree" >
                    {renderSection(_items.filtered.inheritanceTree, 'Profiles', _toggleStates.inheritanceTree, 'inheritanceTree')}
                </li>
                <li id="interfaces" key="interfaces" >
                    {renderSection(_items.filtered.interfaces, 'Interfaces', _toggleStates.interfaces, 'interfaces')}
                </li>
                <li id="compositions" key="compositions" >
                    {renderSection(_items.filtered.compositions, 'Compositions', _toggleStates.compositions, 'compositions')}
                </li>
                <li id="dependencies" key="dependencies" >
                    {renderSection(_items.filtered.dependencies, 'Dependencies', _toggleStates.dependencies, 'dependencies')}
                </li>
            </ul>
        );
    }

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <div className="profile-explorer-container"> 
            <p className="header-name small-size text-uppercase ml-4" >Profile Explorer</p>
            {renderSearchUI()}
            { _filterVal == null || _filterVal === '' ?
                renderExplorer() : renderFiltered()
            }
        </div>
    )
}

export default ProfileExplorer;