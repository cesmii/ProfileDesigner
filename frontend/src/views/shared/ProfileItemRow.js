import React from 'react'
import { useHistory } from 'react-router-dom'
import { Dropdown } from 'react-bootstrap'

//import { generateLogMessageString } from '../../utils/UtilityService'
import { SVGIcon, SVGDownloadIcon } from '../../components/SVGIcon'
import color from '../../components/Constants'
import { renderIcon } from './ProfileRenderHelpers';
import { downloadFileJSON } from '../../utils/UtilityService';

//const CLASS_NAME = "ProfileItemRow";

function ProfileItemRow(props) { //props are item, showActions

    const history = useHistory();

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const navigateToProfile = (e) => {
        console.log('rowclick');
        history.push({
            pathname: `/profile/${props.item.id}`,
            state: { id: `${props.item.id}`}
        });
    };

    const onProfileExtendClick = (e) => {
        history.push({
            pathname:`/profile/extend/${props.item.id}/`,
            state: {id: `${props.item.id}`}
        });
    };

    const downloadMe = async () => {
        downloadFileJSON(props.item, `${props.item.name.trim().replace(" ", "_")}`);
    }

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderMetaTagItem = (item) => {
        return (
            item.metaTags.map((tag) => {
                return (
                    <span key={tag.name} className="metatag badge meta">
                        {tag.name}
                    </span>
                )
            })
        )
    }

    const renderActionsColumn = (item, showActions) => {

        if (!showActions) return;

        return (
            <div className="col col-icon center" >
                <Dropdown className="action-menu icon-dropdown" onClick={(e) => e.stopPropagation()} >
                    <Dropdown.Toggle drop="left">
                        <SVGIcon name="more-vert" size="24" fill={color.shark}/>
                    </Dropdown.Toggle>
                    <Dropdown.Menu>
                        {(props.currentUserId == null || props.currentUserId !== item.author.id) &&
                            <Dropdown.Item key="moreVert1" onClick={navigateToProfile} ><span className="mr-3" alt="view"><SVGIcon name="visibility" size="24" fill={color.shark}/></span>View</Dropdown.Item>
                        }
                        {(props.currentUserId != null && props.currentUserId === item.author.id) &&
                            <Dropdown.Item key="moreVert2" onClick={navigateToProfile} ><span className="mr-3" alt="edit"><SVGIcon name="edit" size="24" fill={color.shark}/></span>Edit</Dropdown.Item>
                        }
                        <Dropdown.Item key="moreVert3" onClick={onProfileExtendClick} ><span className="mr-3" alt="extend"><SVGIcon name="extend" size="24" fill={color.shark} /></span>Extend</Dropdown.Item>
                        <Dropdown.Item key="moreVert4" onClick={downloadMe} ><span className="mr-3" alt="arrow-drop-down"><SVGDownloadIcon name="download" size="24" fill={color.shark} /></span>Download</Dropdown.Item>
                    </Dropdown.Menu>
                </Dropdown>
            </div>
        );
    }

    //build the row
    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    //TBD - improve this check
    if (props.item === null || props.item === {}) return null;
    if (props.item.name == null) return null;

    var cssClass = "row " + props.cssClass;
    var avatarColor = (props.currentUserId == null || props.currentUserId !== props.item.author.id) ? "col col-avatar rounded-circle avatar-locked elevated clickable" : "col col-avatar rounded-circle avatar-unlocked elevated clickable";
    
    return (
        <>
            <div className={cssClass}>
                <div className={avatarColor} onClick={navigateToProfile} >{renderIcon(props.item, props.currentUserId, 32, false)}</div>
                <div className="col left item-text-block clickable" onClick={navigateToProfile} >
                    <p>{props.item.name}</p>
                    <p><small>{props.item.description}</small></p>
                    <p><small><b>{props.item.dateCreated}</b></small></p>
                </div>
                <div className="col col-small center badge children clickable" onClick={navigateToProfile} >{props.item.childCount}</div>
                <div className="col d-inline-flex flex-wrap metatags-col clickable" onClick={navigateToProfile} >{renderMetaTagItem(props.item)}</div>
                {renderActionsColumn(props.item, props.showActions)}
            </div>
        </>
    );
}

export default ProfileItemRow;