import React, { useState } from 'react'
//import PropTypes from 'prop-types';
import { useHistory } from 'react-router-dom'
import Form from 'react-bootstrap/Form'
import FormControl from 'react-bootstrap/FormControl'
import InputGroup from 'react-bootstrap/InputGroup'
import Button from 'react-bootstrap/Button'

import { SVGIcon } from './SVGIcon'
import color from '../components/Constants'
import ProfilePredictiveSearch from '../views/shared/ProfilePredictiveSearch'

//import Fab from './Fab'

import { generateLogMessageString } from '../utils/UtilityService'

const CLASS_NAME = "HeaderNav";

function HeaderNav(props) { //(caption, iconName, showSearch, searchValue, onSearch)
    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const iconName = props.iconName;
    const [_filterVal, setFilterVal] = useState(null); //props.searchValue
    const raiseOnSearch = props.onSearch;
    const history = useHistory();
    ////-------------------------------------------------------------------
    //// Region: Event Handling of child component events
    ////-------------------------------------------------------------------
    ////trigger search after x chars entered or search button click
    const onSearchBlur = (e) => {
        //console.log(generateLogMessageString(`onSearchChange||Search value: ${e.target.value}`, CLASS_NAME));
        setFilterVal(e.target.value);
    }

    const onSearchChange = (e) => {
        //when using predictive search mode, we don't execute the search on the parent grid. we display it in a drop down
        if (props.searchMode === 'predictive') return;

        setFilterVal(e.target.value);
        if (e.target.value.length > 1) {
            raiseOnSearch(_filterVal);
        }
    }

    //trigger search after x chars entered or search button click
    const onSearchClick = (e) => {
        console.log(generateLogMessageString(`onSearchClick||Search value: ${_filterVal}`, CLASS_NAME));
        // call change page function in parent component
        raiseOnSearch(_filterVal);
    }

    const onAdvancedSearchClick = (e) => {
        console.log(generateLogMessageString(`onAdvancedSearchClick||`, CLASS_NAME));
        e.preventDefault();
        history.push(`/advancedsearch`);
    }

    ////-------------------------------------------------------------------
    //// Region: Render helpers
    ////-------------------------------------------------------------------
    const renderTitleBlock = () => {
        return (
            <div className="header-title-block mr-auto">
                <span className="mr-3">
                    <SVGIcon name={iconName} size="48" fill={color.shark} alt={props.caption} />
                </span>
                <p className="h2 mb-0">{props.caption}</p>
            </div>
        );
    }

    const renderSearchUI = () => {
        //Some components won't show the search ui.
        if (props.showSearch != null && !props.showSearch) return;
        return (
            <Form className="header-search-block mr-5">
                <Form.Row>
                    <InputGroup className="global-search mr-4">
                        <FormControl
                            type="text"
                            placeholder="Search here"
                            aria-label="Search here"
                            aria-describedby="basic-addon2"
                            val={_filterVal}
                            onBlur={onSearchBlur}
                            onChange={onSearchChange}
                        />
                        <InputGroup.Append>
                            {props.searchMode == null || props.searchMode === "standard" ? (
                                <Button variant="search" className="p-0 pl-2 pr-2 border-left-0" onClick={onSearchClick} >
                                    <SVGIcon name="search" size="24" fill={color.shark} />
                                </Button>
                            ) : ""}
                            {props.searchMode === "predictive" ? (
                                <ProfilePredictiveSearch filterVal={_filterVal} currentUserId={props.currentUserId} />
                            ) : ""}
                        </InputGroup.Append>
                    </InputGroup>
                </Form.Row>
                <Button variant="secondary" onClick={onAdvancedSearchClick} type="submit" >
                    Advanced search
                </Button>
            </Form>
        );
    }

    const renderMoreDropDown = () => {
        //TBD - profile list only uses this. import is already separate button so render nothing here
        return;
        /*
        //TBD - add support to pass in list of actions.
        //TBD - add option to show "close" icon for advanced search screen
        return (
            <Dropdown className="action-menu icon-dropdown">
                <Dropdown.Toggle drop="left">
                    <SVGIcon name="more-vert" size="24" fill={color.shark}/>
                </Dropdown.Toggle>
                <Dropdown.Menu>
                    <Dropdown.Item href="#/action-1">Import profile</Dropdown.Item>
                </Dropdown.Menu>
            </Dropdown>
        );
        */
    }
    //-------------------------------------------------------------------
    // Region: Render 
    //-------------------------------------------------------------------
    return (
        <div id="--cesmii-header-nav">
            <div className="header-content-wrapper">
                <div className="header-title-row">
                    {renderTitleBlock()}
                    {renderSearchUI()}
                    {renderMoreDropDown()}
                </div>
            </div>
        </div>
    )

}

//HeaderNav.propTypes = propTypes;
//HeaderNav.defaultProps = defaultProps;
export default HeaderNav