import React, { useEffect, useState }from 'react'
import { Button, Form, FormControl, InputGroup } from 'react-bootstrap';

import { generateLogMessageString } from '../../utils/UtilityService';
import { clearSearchCriteria, toggleSearchFilterSelected } from '../../services/ProfileService';
import { SVGIcon } from '../../components/SVGIcon';

import '../../components/styles/InfoPanel.scss';

const CLASS_NAME = "ProfileTypeDefinitionFilter";

function ProfileTypeDefinitionFilter(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const [_filterVal, setFilterVal] = useState(null); //props.searchValue
    const _sortByOptions = [
        { val: "1", caption: "My Types" },
        { val: "2", caption: "Popular" },
        { val: "3", caption: "Name" }
    ];

    //-------------------------------------------------------------------
    // Region: useEffect
    //-------------------------------------------------------------------
    useEffect(() => {
        if (props.searchCriteria == null || props.searchCriteria.filters == null) return;

        if (props.searchCriteria.query !== _filterVal) {
            setFilterVal(props.searchCriteria.query);
        }

    }, [props.searchCriteria]);

    //-------------------------------------------------------------------
    // Region: Helper Methods
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    //update search state so that form submit has value
    const onSearchChange = (e) => {
        //update state for other components to see
        setFilterVal(e.target.value);
    }

    //trigger search after enter or search button click
    const onSearchClick = (e) => {
        console.log(generateLogMessageString(`onSearchClick||Search value: ${props.searchCriteria.query}`, CLASS_NAME));
        e.preventDefault();

        var criteria = JSON.parse(JSON.stringify(props.searchCriteria));
        criteria.query = _filterVal;

        //bubble up to parent component and it will save state
        if (props.onSearchCriteriaChanged != null) props.onSearchCriteriaChanged(criteria);
    }

    //called when an item is selected in the filter panel
    const onItemClick = (e) => {

        var criteria = JSON.parse(JSON.stringify(props.searchCriteria));
        criteria.query = _filterVal;

        //loop through filters and their items and find the id. Note ids are not unique across groups 
        const parentId = e.currentTarget.getAttribute('data-parentid');
        const id = e.currentTarget.getAttribute('data-id');
        toggleSearchFilterSelected(criteria, parentId, id);

        //bubble up to parent component and it will save state
        if (props.onSearchCriteriaChanged != null) props.onSearchCriteriaChanged(criteria);
    }

    const onClearAll = () => {
        console.log(generateLogMessageString('onClearAll', CLASS_NAME));

        //clear out the selected, the query val
        var criteria = clearSearchCriteria(props.searchCriteria);
        setFilterVal(criteria.query);

        //bubble up to parent component and it will save state
        if (props.onSearchCriteriaChanged != null) props.onSearchCriteriaChanged(criteria);
    }

    const onChangeSortBy = (e) => {
        console.log(generateLogMessageString('onChangeSortBy', CLASS_NAME));

        //clear out the selected, the query val
        var criteria = JSON.parse(JSON.stringify(props.searchCriteria));
        criteria.sortByEnum = parseInt(e.target.value);

        //bubble up to parent component and it will save state
        if (props.onSearchCriteriaChanged != null) props.onSearchCriteriaChanged(criteria);
    }

    const onTileViewToggle = () => {
        console.log(generateLogMessageString('onTileViewToggle', CLASS_NAME));
        if (props.toggleDisplayMode) props.toggleDisplayMode("tile");
    }

    const onListViewToggle = () => {
        console.log(generateLogMessageString('onListViewToggle', CLASS_NAME));
        if (props.toggleDisplayMode) props.toggleDisplayMode("list");
    }


    //-------------------------------------------------------------------
    // Region: Render helpders
    //-------------------------------------------------------------------
    const renderSearchUI = () => {
        return (
            <Form onSubmit={onSearchClick} className={`header-search-block`}>
                <Form.Row>
                    <InputGroup className="global-search">
                        <FormControl
                            type="text"
                            placeholder="Search here"
                            aria-label="Filter type definitions containing this text."
                            value={_filterVal != null ? _filterVal : ""}
                            onChange={onSearchChange}
                        />
                        <InputGroup.Append>
                            <Button variant="search" className="p-0 pl-2 pr-2 border-left-0" onClick={onSearchClick} type="submit" title="Run Search." >
                                <SVGIcon name="search" />
                            </Button>
                        </InputGroup.Append>
                    </InputGroup>
                </Form.Row>
            </Form>
        );
    }

    //const renderSection = (section) => {
    //    const choices = section.items.map((item) => {
    //        if (!item.visible) return null;
    //        return (
    //            <li id={`${section.id}-${item.id}`} key={`${section.id}-${item.id}`} className="m-1 d-inline-block"
    //                onClick={onItemClick} data-parentid={section.id} data-id={item.id} >
    //                <span className={`${item.selected ? "selected" : "not-selected"} py-1 px-2 d-flex`} >{item.name}</span>
    //            </li>
    //        )
    //    }).filter(x => x != null);
    //    return (
    //        <>
    //            {choices}
    //        </>
    //    );
    //}

    const renderSections = () => {
        if (props.searchCriteria == null || props.searchCriteria.filters == null ) {
            return;
        }
        
        const choices = props.searchCriteria.filters.map((section) => {
            //return renderSection(section);
            return section.items.map((item) => {
                if (!item.visible) return null;
                return (
                    <li id={`${section.id}-${item.id}`} key={`${section.id}-${item.id}`} className="m-1 d-inline-block"
                        onClick={onItemClick} data-parentid={section.id} data-id={item.id} >
                        <span className={`${item.selected ? "selected" : "not-selected"} py-1 px-2 d-flex`} >{item.name}</span>
                    </li>
                )
            }).filter(x => x != null);
        });

        return (
            <ul className="m-0 p-0 d-inline" >
                {choices}
            </ul>
        );
    }

    const renderSortBy = () => {
        if (props.searchCriteria == null || props.searchCriteria.filters == null) {
            return;
        }
        const options = _sortByOptions.map((item) => {
            return (<option key={item.val} value={item.val} >{item.caption}</option>)
        });

        var selValue = props.searchCriteria.sortByEnum;

        return (
            <>
                <Form.Label htmlFor="sortBy" className="d-inline mx-1" >Sort by:</Form.Label>
                <Form.Control id="sortByEnum" as="select" className="input-rounded minimal pr-5" value={selValue == null ? "3" : selValue}
                    onChange={onChangeSortBy} >
                    {options}
                </Form.Control>
            </>
        )
    }

    const renderDisplayMode = () => {
        return (
            <>
                <Button variant="icon-solo" onClick={onListViewToggle} className={props.displayMode !== "list" ? "mr-2" : "mr-2 inactive"} ><i className="material-icons"><span className="material-icons">
                    view_headline
                </span></i></Button>
                <Button variant="icon-solo" onClick={onTileViewToggle} className={props.displayMode !== "tile" ? "mr-2" : "mr-2 inactive"}  ><i className="material-icons">grid_view</i></Button>
            </>
        );
    }

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    //if (!hasSelected()) return null;
    return (
        <>
            <div className={`row selected-panel px-3 py-1 mb-1 rounded d-flex ${props.cssClass ?? ''}`} >
                <div className="col-sm-12 px-0 align-items-start d-block d-lg-flex align-items-center" >
                    <div className="d-flex mr-lg-3 mb-2 mb-lg-0" >
                        {renderSearchUI()}
                    </div>
                    <div className="d-block d-lg-inline mb-2 mb-lg-0" >
                        {renderSections()}
                    </div>
                    <div className="ml-auto justify-content-end text-nowrap d-flex align-items-center" >
                        <button onClick={onClearAll} className="ml-2 px-2 btn-auto btn btn-text-solo d-flex align-items-center" >Clear All<i className="pl-1 material-icons">update</i></button>
                    </div>
                </div>
            </div>
            <div className={`row d-flex ${props.cssClass ?? ''}`} >
                <div className="col-12 align-items-center d-flex mb-2" >
                    {(props.itemCount != null && props.itemCount > 0) &&
                        <span className="font-weight-bold text-nowrap mr-3">{props.itemCount}{props.itemCount === 1 ? ' item' : ' items'}</span>
                    }
                    <div className="ml-auto justify-content-end text-nowrap d-flex align-items-center" >
                        {renderDisplayMode()}
                        {renderSortBy()}
                    </div>
                </div>
            </div>
        </>
    )

}

export default ProfileTypeDefinitionFilter