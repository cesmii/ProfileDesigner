import React, { useState, Fragment } from 'react'
import Button from 'react-bootstrap/Button'

import { SVGIcon } from './SVGIcon'
import color from './Constants'
import './styles/SideMenuLinkList.scss';

//import { generateLogMessageString } from '../utils/UtilityService'

//const CLASS_NAME = "SideMenuLinkList";

function SideMenuLinkList(props) { //props are subMenuItems, bgColor, iconName, navUrl
    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [_toggleState, setToggleState] = useState(false);

    //-------------------------------------------------------------------
    // Region: Events
    //-------------------------------------------------------------------
    ////expand/collapse child section
    const toggleChildren = (e) => {
        setToggleState(!_toggleState);
    }

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderSectionHeader = (caption, toggleState, itemCount) => {
        var toggleCss = toggleState ? "expanded d-flex align-items-center action-menu" : "d-flex align-items-center action-menu";
        var toggleIcon = toggleState ? "expand-less" : "expand-more";
        // sectionKey = caption;
        return (
            <div className={toggleCss} onClick={toggleChildren} >
                <span key="caption" className="caption small-size text-uppercase">
                    {caption}
                </span>
                {(itemCount > 0) &&
                    <span key="toggle" className="ml-auto">
                        <Button variant="accordion" className="btn" >
                            <span>
                                <SVGIcon name={toggleIcon} size="20" fill={color.gris} alt={caption} />
                            </span>
                        </Button>
                    </span>
                }
            </div>                
        )
    };


    const renderListItem = (link, index) => {
        var iconColor = (link.authorId == null || props.currentUserId == null || props.currentUserId !== link.authorId) ? color.gris : color.cornflower;
        var key = `li_${index.toString()}`;
        return (
            <Fragment key={key}>
                <li id={key} key={key} className="body-size">
                    <a href={link.url} >
                        {link.iconName == null ? "" :
                            <span className="mr-2">
                                <SVGIcon name={link.iconName} size="18" fill={iconColor} />
                            </span>
                        }
                        {link.caption}
                    </a>
                </li>
            </Fragment>
        );
    }

    //render the main area
    const renderList = () => {
        if (props.items == null || props.items.length === 0) {
            return (
                <>
                    {renderSectionHeader(props.caption, _toggleState, 0)}
                    <div className="text-center small" >There are no {props.caption.toLowerCase()}.</div>
                </>
            )
        }

        //has children scenario
        const mainBody = props.items.map((l, i) => {
            // set childCount for section header
            // childCount = props.items.length;
            // console.log(childCount);
            return renderListItem(l, i);
        });

        var toggleCss = _toggleState ? "expanded" : "collapsed";

        return (
            <>
                <ul className="link-list">
                    {renderSectionHeader(props.caption, _toggleState, 1)}
                    <ul className={`link-list children ${toggleCss}`}>
                        {mainBody}
                    </ul>
                </ul>

                {/* <Card>
                    
                    {renderAccordionSectionHeader(props.caption, _toggleState, 1)}
                    
                    <Accordion.Collapse eventKey="1">
                        <Card.Body>
                            {mainBody}
                        </Card.Body>
                    </Accordion.Collapse>
                </Card> */}
            </>
        );
    }

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------

    return (
        <div className="side-menu-list-container">
        {/* <Accordion defaultActiveKey="0" className="side-menu-list-container"> */}
            {renderList()}
        {/* </Accordion> */}
        </div>
    )
}

export default SideMenuLinkList;
