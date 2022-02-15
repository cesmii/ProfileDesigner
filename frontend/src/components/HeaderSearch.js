import React, { useState, useEffect } from 'react'
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

const CLASS_NAME = "HeaderSearch";

function HeaderSearch(props) { //(caption, iconName, showSearch, searchValue, onSearch)
    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [_filterVal, setFilterVal] = useState(props.filterVal); //props.searchValue
    const history = useHistory();

    //-------------------------------------------------------------------
    // Region: useEffect
    //-------------------------------------------------------------------
    useEffect(() => {

        if (_filterVal !== props.filterVal) {
            setFilterVal(props.filterVal);
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [props.filterVal]);

    ////-------------------------------------------------------------------
    //// Region: Event Handling of child component events
    ////-------------------------------------------------------------------
    //update search state so that form submit has value
    const onSearchChange = (e) => {
        //when using predictive search mode, we don't execute the search on the parent grid. we display it in a drop down
        if (props.searchMode === 'predictive') return;

        setFilterVal(e.target.value);
    }

    //trigger search after x chars entered or search button click
    const onSearchClick = (e) => {
        console.log(generateLogMessageString(`onSearchClick||Search value: ${_filterVal}`, CLASS_NAME));
        e.preventDefault();
        // call change page function in parent component
        if (props.onSearch) props.onSearch(_filterVal);
    }

    const onAdvancedSearchClick = (e) => {
        console.log(generateLogMessageString(`onAdvancedSearchClick||`, CLASS_NAME));
        e.preventDefault();
        history.push(`/advancedsearch`);
    }

    ////-------------------------------------------------------------------
    //// Region: Render helpers
    ////-------------------------------------------------------------------
    const renderSearchUI = () => {
        //Some components won't show the search ui.
        if (props.showSearch != null && !props.showSearch) return;
        return (
            <>
                {(props.itemCount != null && props.itemCount > 0) &&
                    <span className="text-right text-nowrap">{props.itemCount}{props.itemCount === 1 ? ' item' : ' items'}</span>
                }
                <Form onSubmit={onSearchClick} className={`header-search-block ${props.className == null ? "mx-3" : props.className}`}>
                    <Form.Row>
                        <InputGroup className="global-search">
                            <FormControl
                                type="text"
                                placeholder="Search here"
                                aria-label="Search here"
                                value={_filterVal == null ? '' : _filterVal}
                                onChange={onSearchChange}
                            />
                            <InputGroup.Append>
                                {props.searchMode == null || props.searchMode === "standard" ? (
                                    <Button variant="search" className="p-0 pl-2 pr-2 border-left-0" onClick={onSearchClick} type="submit" title="Run Search">
                                        <SVGIcon name="search" size="24" fill={color.shark} />
                                    </Button>
                                ) : ""}
                                {props.searchMode === "predictive" ? (
                                    <ProfilePredictiveSearch filterVal={_filterVal} currentUserId={props.currentUserId} />
                                ) : ""}
                            </InputGroup.Append>
                        </InputGroup>
                    </Form.Row>
                </Form>
                {(props.showAdvancedSearch) &&
                    <Button variant="secondary" className="auto-width" onClick={onAdvancedSearchClick} >
                        Advanced
                    </Button>
                }
            </>
        );
    }


    //-------------------------------------------------------------------
    // Region: Render 
    //-------------------------------------------------------------------
    return (
        <>
            {renderSearchUI()}
        </>
    )

}

export default HeaderSearch