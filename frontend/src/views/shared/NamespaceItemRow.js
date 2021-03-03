import React from 'react'
import { useHistory } from 'react-router-dom'
import Button from 'react-bootstrap/Button'

//import { generateLogMessageString } from '../../utils/UtilityService'
import { SVGIcon } from '../../components/SVGIcon'
import color from '../../components/Constants'
import '../styles/NamespaceList.scss';

//const CLASS_NAME = "NamespaceItemRow";

function NamespaceItemRow(props) { //props are item, hasActions

    const history = useHistory();

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onItemClick = () => {
        if (!props.hasActions) return;
        history.push(`/profiles/library/namespace/${encodeURIComponent(props.item.namespace)}`);
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderIcon = () => {
        if (props.item == null) return;
        return (
            <span className="mr-2">
                <SVGIcon name="folder-profile" size="24" fill={color.shark}/>
            </span>
        )
    };

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

    const renderActionsColumn = (hasActions) => {

        if (!hasActions) return;

        return (
            <Button variant="icon-solo" className="align-items-center">
                <span>
                    <SVGIcon name="visibility" size="24" fill={color.shark} />
                </span>
            </Button>
        );
    }

    //build the row
    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    //TBD - improve this check
    if (props.item === null || props.item === {}) return null;
    if (props.item.namespace == null) return null;

    var cssClass = "row " + props.cssClass;

    return (
        <>
            <div className={cssClass} onClick={onItemClick} >
                <div className="col col-avatar center" >{props.item.namespace.substr(0, 1)}</div>
                <div className="col left item-text-block" >
                    <p>{renderIcon()}{props.item.namespace}</p>
                </div>
                <div className="col col-small center badge children" >{props.item.childCount}</div>
                <div className="col d-inline-flex flex-wrap metatags-col" >{renderMetaTagItem(props.item)}</div>
                <div className="col col-icon center" >
                    {renderActionsColumn(props.hasActions)}
                </div>
            </div>
        </>
    );
}

export default NamespaceItemRow;