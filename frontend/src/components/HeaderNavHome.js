import React, { useState } from 'react'
//import PropTypes from 'prop-types';
import { useHistory } from 'react-router-dom'
import Form from 'react-bootstrap/Form'
import FormControl from 'react-bootstrap/FormControl'
import InputGroup from 'react-bootstrap/InputGroup'
import Button from 'react-bootstrap/Button'

import { SVGIcon } from './SVGIcon'
import color from '../components/Constants'

//import Fab from './Fab'

import { generateLogMessageString } from '../utils/UtilityService'

const CLASS_NAME = "HeaderNavHome";

function HeaderNavHome(props) { //(caption, showSearch, searchValue, onSearch)
    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
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
                    <SVGIcon name="new-file-filled" size="48" fill={color.shark} alt={props.caption} />
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
                <InputGroup className="mr-3">
                        <FormControl
                            type="text"
                            placeholder="Search here"
                            aria-label="Search here"
                            aria-describedby="basic-addon2"
                            val={_filterVal}
                            onBlur={onSearchBlur}
                        />
                        <InputGroup.Append>
                            <Button variant="icon-outline p-0 pl-2 pr-2 border-left-0" onClick={onSearchClick} >
                                <SVGIcon name="search" size="24" fill={color.shark}/>
                            </Button>
                        </InputGroup.Append>
                    </InputGroup>
                </Form.Row>
                <Button variant="secondary" onClick={onAdvancedSearchClick} type="submit" >
                    Advanced search
                </Button>
            </Form>
        );
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
                </div>
            </div>
        </div>
    )

}

//HeaderNav.propTypes = propTypes;
//HeaderNav.defaultProps = defaultProps;
export default HeaderNavHome