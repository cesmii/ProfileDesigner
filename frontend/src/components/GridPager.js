import React from 'react';
//import PropTypes from 'prop-types';
import { Pagination } from 'react-bootstrap'
import { AppSettings } from '../utils/appsettings';
import { generateLogMessageString } from '../utils/UtilityService'
//import { First } from 'react-bootstrap/esm/PageItem';
import { Dropdown } from 'react-bootstrap'

const CLASS_NAME = "GridPager";

function GridPager(props) { //(currentPage, pageSize, itemCount, onChangePage)
    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const firstCaption = "|<";
    const lastCaption = ">|";
    const previousCaption = "<";
    const nextCaption = ">";
    const minimuPageSize = 10;

    const raiseOnChangePage = props.onChangePage;

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    //change page event handler
    const onPageClick = (pageIndex) => {
        console.log(generateLogMessageString(`onPageClick||Val: ${pageIndex}`, CLASS_NAME));
        //setCurrentPage(e.target.value);
        // call change page function in parent component
        raiseOnChangePage(pageIndex, props.pageSize);
    }

    //on page size change which will trigger a change page event 
    const onPageSizeSelect = (pageSize) => {
        console.log(generateLogMessageString(`onPageSizeSelect||Val: ${pageSize}`, CLASS_NAME));
        //setPager({ currentPage: _pager.currentPage, pageSize: pageSize, itemCount: _pager.itemCount });
        // call change page function in parent component - back to first page with increased page size
        raiseOnChangePage(1, pageSize);
    }

    const getPagerData = (currentPage, pageSize, itemCount) => {

        // default to first page if null
        currentPage = currentPage || 1;
        // default page size if null
        pageSize = pageSize || AppSettings.PageSize;
        // calculate total pages
        var totalPages = Math.ceil(itemCount / pageSize);
        //TBD - do we need this?
        if (currentPage < 1 || currentPage > totalPages) {
            return;
        }

        var startPage, endPage;
        if (totalPages <= 10) {
            // less than 10 total pages so show all
            startPage = 1;
            endPage = totalPages;
        } else {
            // more than 10 total pages so calculate start and end pages
            if (currentPage <= 6) {
                startPage = 1;
                endPage = 10;
            } else if (currentPage + 4 >= totalPages) {
                startPage = totalPages - 9;
                endPage = totalPages;
            } else {
                startPage = currentPage - 5;
                endPage = currentPage + 4;
            }
        }

        // calculate start and end item indexes
        var startIndex = (currentPage - 1) * pageSize;
        var endIndex = Math.min(startIndex + pageSize - 1, itemCount - 1);
        //console.log(generateLogMessageString(`getPagerData||Start index: ${startIndex}, End index: ${endIndex}, Current Page: ${currentPage}`, CLASS_NAME));

        // create an array of pages to ng-repeat in the pager control
        var pages = [...Array((endPage + 1) - startPage).keys()].map(i => startPage + i);

        // return object with all pager properties required by the view
        return {
            itemCount: itemCount,
            currentPage: currentPage,
            pageSize: pageSize,
            totalPages: totalPages,
            startPage: startPage,
            endPage: endPage,
            startIndex: startIndex,
            endIndex: endIndex,
            pages: pages
        };

    }

    const renderPageItems = () => {

        if (props.itemCount <= props.pageSize) {
            // don't display pager if there is only 1 page
            return null;
        }

        //var pager = setPage(_currentPage, _pageSize, _itemCount);
        var pager = getPagerData(props.currentPage, props.pageSize, props.itemCount);
        //it will be null while data is being fetched
        if (pager == null) return;

        var result = [];
        if (pager.totalPages > minimuPageSize) {
            //add first, previous item
            result.push(<Pagination.Item key="first" active={pager.currentPage === 1} onClick={() => onPageClick(1)}>{firstCaption}</Pagination.Item>);
            result.push(<Pagination.Item key="previous" active={pager.currentPage === 1} onClick={() => onPageClick(pager.currentPage - 1)}>{previousCaption}</Pagination.Item>);
        }
        //add individual pages
        for (let pageNum = 1; pageNum <= pager.totalPages; pageNum++) {
            result.push(
                <Pagination.Item key={pageNum} active={pageNum === pager.currentPage} onClick={() => onPageClick(pageNum)}>
                    {pageNum}
                </Pagination.Item>
            );
        }
        //add next, last item
        if (pager.totalPages > minimuPageSize) {
            result.push(<Pagination.Item key="next" active={pager.currentPage === pager.totalPages} onClick={() => onPageClick(pager.currentPage + 1)}>{nextCaption}</Pagination.Item>);
            result.push(<Pagination.Item key="last" active={pager.currentPage === pager.totalPages} onClick={() => onPageClick(pager.totalPages)}>{lastCaption}</Pagination.Item>);
        }
        return (<div className="mr-auto"><Pagination >{result}</Pagination></div>);
    }

    const renderPageSizeOptions = (pageSize) => {

        //console.log(generateLogMessageString(`renderPageSizeOptions||Val: ${pageSize}`, CLASS_NAME));
        //make copy of options. If current page size is not represented, add it to options
        var pageSizeOptions = JSON.parse(JSON.stringify(AppSettings.PageSizeOptions));
        if (pageSizeOptions.find(item => { return item === pageSize; }) == null) {
            pageSizeOptions.push(pageSize);
            pageSizeOptions.sort((a, b) => { return parseFloat(a) - parseFloat(b) }); //force numeric sort
        }

        //set current page in drop down toggle
        var toggleHTML = (
            <Dropdown.Toggle key={pageSize} variant="primary" id="dropdown-basic">
                {pageSize} per page
            </Dropdown.Toggle>
        );
        const pageOptionsHTML = pageSizeOptions.map((item) => {
            if (item === pageSize) {
            }
            return (<Dropdown.Item key={item} onSelect={() => onPageSizeSelect(item)} >{item} per page</Dropdown.Item>);
        });

        return (
            <Dropdown className="pagination-dropdown" onClick={(e) => e.stopPropagation()} >
                <span className="label-left">Show</span>
                {toggleHTML}
                <Dropdown.Menu>
                    {pageOptionsHTML}
                    <Dropdown.Divider />
                    <Dropdown.Item key="all" onSelect={() => onPageSizeSelect(null)} >Show all</Dropdown.Item>
                </Dropdown.Menu>
            </Dropdown>
        )
    }

    //-------------------------------------------------------------------
    // Region: Render 
    //-------------------------------------------------------------------
    return (
        <div className="pagination-wrapper">
            {renderPageItems()}
            {renderPageSizeOptions(props.pageSize)}
        </div>
    )
}

export default GridPager;

