import React from 'react'
import Dropdown from 'react-bootstrap/Dropdown'
import Form from 'react-bootstrap/Form'
import Button from 'react-bootstrap/Button'

import { SVGIcon } from './SVGIcon'
import color from '../components/Constants'

import { generateLogMessageString } from '../utils/UtilityService'

const CLASS_NAME = "HeaderNavEntity";

function HeaderNavEntity(props) { //(caption, onSave)
    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------

    ////-------------------------------------------------------------------
    //// Region: Event Handling of child component events
    ////-------------------------------------------------------------------

    //trigger save after x chars entered or save button click
    const onSaveClick = (e) => {
        console.log(generateLogMessageString(`onSaveClick`, CLASS_NAME));
        // call function in parent component
        props.onSave();
    }
    const toggleFavorite = (e) => {
        console.log(generateLogMessageString(`toggleFavorite`, CLASS_NAME));
        // call function in parent component
        props.onToggleFavorite();
    }

    ////-------------------------------------------------------------------
    //// Region: Render helpers
    ////-------------------------------------------------------------------
    const renderTitleBlock = () => {
        var svg = (<SVGIcon name={props.iconName} size="32" fill={color.shark} alt={props.caption} />);

        return (
            <div className="header-title-block mr-auto">
                <span className="mr-3">
                    {svg}
                </span>
                <p className="h2">{props.caption}</p>
                {(props.item == null) ? "" :
                    <Button variant="square" size="lg" aria-label="Toggle favorite" onClick={toggleFavorite} className="ml-2 favorite d-flex align-items-center justify-content-center">
                        <span>
                            <SVGIcon name={props.isFavorite ? "favorite" : "favorite-border"} size="32" fill={props.isFavorite ? color.citron : color.silver} />
                        </span>
                    </Button>
                }
            </div>
        );
    }

    const renderControlsUI = () => {
        //Some components won't show the search ui.
        return (
            <Form className="header-search-block mr-5">
                {/* cancel button needs event */}
                <Button variant="text-solo">
                    Cancel
                </Button>

                <Button variant="secondary" type="button" onClick={onSaveClick}>
                    Save
                </Button>
            </Form>
        );
    }

    const renderMoreDropDown = () => {
        if (props.item == null) return;
        //TBD - add support to pass in list of actions.

        return (
            <Dropdown className="action-menu icon-dropdown">
                <Dropdown.Toggle drop="left">
                    <SVGIcon name="more-vert" size="24" fill={color.shark}/>
                </Dropdown.Toggle>
                <Dropdown.Menu>
                    {props.item == null ? "" :
                        <Dropdown.Item href={`/profile/${props.item.id}/extend`}>Extend '{props.item.name}'</Dropdown.Item>
                    }
                </Dropdown.Menu>
            </Dropdown>
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
                    {renderControlsUI()}
                    {renderMoreDropDown()}
                </div>
            </div>
        </div>
    )

}

export default HeaderNavEntity