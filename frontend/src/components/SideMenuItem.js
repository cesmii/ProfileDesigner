import React from 'react'
import { Link, useHistory } from "react-router-dom"
import { Dropdown } from 'react-bootstrap'
import Fab from './Fab'
import { SVGIcon } from './SVGIcon'
import color from './Constants'

//import { generateLogMessageString } from '../utils/UtilityService'

//const CLASS_NAME = "SideMenuItem";

function SideMenuItem(props) { //props are subMenuItems, bgColor, iconName, navUrl
    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();

    var currentStyle = {
        borderLeftColor: props.bgColor
    }
    //this is the off-white, couldn't get the color("cornflower") to work code-wise
    var defaultStyle = {
        borderLeftColor: color.almostWhite
    }
    const opacity = "33";

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderDropdownItems = () => {
        if (props.subMenuItems == null || props.subMenuItems.length === 0) return null;
        return props.subMenuItems.map((link,i) => {
            return (
                <Dropdown.Item key={i} href={link.url}>
                    {link.iconName == null ? "" : 
                        <span className="mr-2">
                            <SVGIcon name={link.iconName} size="24" fill={color.shark} />
                        </span>
                    }
                    {link.caption}
                </Dropdown.Item>
            )
        });
    }

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    //console.log(generateLogMessageString('render', CLASS_NAME));

    return (
        <li className="sidemenu-item" style={(history.location.pathname === props.navUrl) ? currentStyle : defaultStyle} >
            <Link className="sidemenu-item-link" to={props.navUrl} style={{ color: 'inherit', textDecoration: 'inherit' }}>
                <Fab color={props.bgColor} bgColor={props.bgColor} opacity={opacity} iconName={props.iconName} size='48px' />
                <div className="d-none d-lg-block">
                    {props.caption}<br />
                    <small>{props.subText}</small>
                </div>
            </Link>
            {/*some side menu items may have stuff, others may not*/}
            {(props.subMenuItems == null || props.subMenuItems.length === 0) ?
                ('') :
                (    
                    <Dropdown className="action-menu icon-dropdown ml-auto" onClick={(e) => e.stopPropagation()} >
                    <Dropdown.Toggle drop="left">
                        {/* <MaterialIcon icon="more_vert" /> */}
                        <SVGIcon name="more-vert" size="24" fill={color.shark} />
                    </Dropdown.Toggle>
                    <Dropdown.Menu>
                        {renderDropdownItems()}
                    </Dropdown.Menu>
                </Dropdown>
                )
            }
        </li>
    );
}

export default SideMenuItem;
