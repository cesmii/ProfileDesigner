import React, { useState, useEffect} from 'react'

import { getDependencyPreferences, setDependencyPageSize } from '../../services/DependencyService';
import { generateLogMessageString, pageDataRows } from '../../utils/UtilityService'
import GridPager from '../../components/GridPager'
import DependencyItemRow from './DependencyItemRow';

const CLASS_NAME = "DependencyList";

function DependencyList(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const _dependencyPreferences = getDependencyPreferences();
    const [_dataRows, setDataRows] = useState({
        all: [], filtered: [], paged: [],
        pager: { currentPage: 1, pageSize: _dependencyPreferences.pageSize, itemCount: 0 }
    });
    //const iconName = "profile";

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onChangePage = (currentPage, pageSize) => {
        console.log(generateLogMessageString(`onChangePage||Current Page: ${currentPage}, Page Size: ${pageSize}`, CLASS_NAME));
        var pagedData = pageDataRows(_dataRows.filtered, currentPage, pageSize);
        //update state - several items w/in keep their existing vals
        setDataRows({
            all: _dataRows.all, filtered: _dataRows.filtered, paged: pagedData,
            pager: { currentPage: currentPage, pageSize: pageSize, itemCount: _dataRows.filtered == null ? 0 : _dataRows.filtered.length }
        });

        //preserve choice in local storage
        setDependencyPageSize(pageSize);
    };

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------
    useEffect(() => {
        async function bindData() {

            var dependencies = props.typeDefinition.dependencies;

            //update state with data returned
            var pagedData = pageDataRows(dependencies, 1, _dependencyPreferences.pageSize); //also updates state
            //set state on fetch of data
            setDataRows({
                all: dependencies, filtered: dependencies, paged: pagedData,
                pager: { currentPage: 1, pageSize: _dependencyPreferences.pageSize, itemCount: dependencies == null ? 0 : dependencies.length }
            });
        }
        bindData();
        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [props.typeDefinition.id, _dependencyPreferences.pageSize, props.typeDefinition.dependencies]);

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderHeaderRow = () => {
        return (<DependencyItemRow key="header" isHeader="true" item={null} activeAccount={props.activeAccount} cssClass="attribute-list-header pb-2" />)
    }
    //render pagination ui
    const renderPagination = () => {
        if (_dataRows == null || _dataRows.all.length === 0) return;
        return <GridPager currentPage={_dataRows.pager.currentPage} pageSize={_dataRows.pager.pageSize} itemCount={_dataRows.pager.itemCount} onChangePage={onChangePage} />
    }

    //render the main grid
    const renderItemsGrid = () => {
        if (_dataRows.paged == null || _dataRows.paged.length === 0) {
            return (
                <div className="alert alert-info-custom mt-2 mb-2">
                    <div className="text-center" >There are no dependencies.</div>
                </div>
            )
        }
        const mainBody = _dataRows.paged.map((item) => {
            return (<DependencyItemRow key={item.id} item={item} activeAccount={props.activeAccount} cssClass="attribute-list-item" />)
        });

        return (
            <>
                <div className="flex-grid attribute-list px-2">
                    {renderHeaderRow()}
                    {mainBody}
                </div>
            </>
        );
    }

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            {renderItemsGrid()}
            {renderPagination()}
        </>
    )
}

export default DependencyList;