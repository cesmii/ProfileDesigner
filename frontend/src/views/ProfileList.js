import React, { useState, useEffect, Fragment, useRef } from 'react'
import { useParams, useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import axios from 'axios'
import {Button} from 'react-bootstrap'

import { getProfilePreferences, setProfilePageSize } from '../services/ProfileService';
import { AppSettings } from '../utils/appsettings'
import { generateLogMessageString, pageDataRows } from '../utils/UtilityService'
import HeaderNav from '../components/HeaderNav'
import GridPager from '../components/GridPager'
import ProfileItemRow from './shared/ProfileItemRow';
import { useAuthContext } from "../components/authentication/AuthContext";
import { useLoadingContext, UpdateRecentFileList } from "../components/contexts/LoadingContext";

import { SVGIcon } from '../components/SVGIcon'
import color from '../components/Constants'

const CLASS_NAME = "ProfileList";
const entityInfo = {
    name: "Profile",
    namePlural: "Profiles",
    entityUrl: "/profile/:id",
    listUrl: "/profile/all"
}

function ProfileList() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();
    const { authTicket } = useAuthContext();
    const _profilePreferences = getProfilePreferences();
    const _scrollToRef = useRef(null);
    const { type } = useParams()
    const [_dataRows, setDataRows] = useState({
        all: [], filtered: [], paged: [],
        pager: { currentPage: 1, pageSize: _profilePreferences.pageSize, itemCount: 0 }
    });
    const filterVal = '';
    const caption = (type == null || type.toLowerCase() === 'all' ? 'All ' + entityInfo.namePlural : 'My ' + entityInfo.namePlural);
    const iconName = (type == null || type.toLowerCase() === 'all' ? 'folder-profile' : 'folder-shared');
    const { loadingProps, setLoadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const handleOnSearchChange = (val) => {
        //raised from header nav
        //console.log(generateLogMessageString('handleOnSearchChange||Search value: ' + val, CLASS_NAME));

        //filter the data, update the state
        var filteredData = filterDataRows(_dataRows.all, val);
        //setFilterVal(val); //update state
        //setDataRowsFiltered(filteredData); //filter rows state updated to matches

        //page the filtered data and update the paged data state
        var pagedData = pageDataRows(filteredData, 1, _dataRows.pager.pageSize);

        //update state - several items w/in keep their existing vals
        setDataRows({
            all: _dataRows.all, filtered: filteredData, paged: pagedData,
            pager: { currentPage: 1, pageSize: _dataRows.pager.pageSize, itemCount: filteredData == null ? 0 : filteredData.length }
        });
    };

    const onChangePage = (currentPage, pageSize) => {
        console.log(generateLogMessageString(`onChangePage||Current Page: ${currentPage}, Page Size: ${pageSize}`, CLASS_NAME));
        var pagedData = pageDataRows(_dataRows.filtered, currentPage, pageSize);
        //update state - several items w/in keep their existing vals
        setDataRows({
            all: _dataRows.all, filtered: _dataRows.filtered, paged: pagedData,
            pager: { currentPage: currentPage, pageSize: pageSize, itemCount: _dataRows.filtered == null ? 0 : _dataRows.filtered.length }
        });

        //scroll screen to top of grid on page change
        ////scroll a bit higher than the top edge so we get some of the header in the view
        window.scrollTo({ top: (_scrollToRef.current.offsetTop-120), behavior: 'smooth' }); 
        //scrollToRef.current.scrollIntoView();

        //preserve choice in local storage
        setProfilePageSize(pageSize);
    };

    const onImportClick = () => {
        console.log(generateLogMessageString(`onImportClick`, CLASS_NAME));
        history.push('/');
    };

   // run this function from an event handler or an effect to execute scroll 

    //const updatePagerState = (pagedItems, currentPage, pageSize, itemCount) => {
    //    //update state of pager - if necessary
    //    if (_pager.currentPage !== currentPage || _pager.pageSize !== pageSize || _pager.itemCount !== itemCount) {
    //        setPager({ currentPage: currentPage, pageSize: pageSize, itemCount: itemCount });
    //    }
    //    //update paged data
    //    setDataRowsPaged(pagedItems);
    //};

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------
    useEffect(() => {
        //TBD - enhance the mock api to return profiles by user id
        async function fetchData() {
            //show a spinner
            setLoadingProps({ isLoading: true, message: null });

            var url = `${AppSettings.BASE_API_URL}/profile${type === "all" ? "" : "?author.id=" + authTicket.user.id}`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));
            const result = await axios(url);

            result.data.sort((a, b) => {
                if (a.name.toLowerCase() < b.name.toLowerCase()) {
                    return -1;
                }
                if (a.name.toLowerCase() > b.name.toLowerCase()) {
                    return 1;
                }
                //secondary sort by object type
                if (a.type.name.toLowerCase() < b.type.name.toLowerCase()) {
                    return -1;
                }
                if (a.type.name.toLowerCase() > b.type.name.toLowerCase()) {
                    return 1;
                }
                return 0;
            }); //sort by name

            //update state with data returned
            var pagedData = pageDataRows(result.data, 1, _profilePreferences.pageSize); //also updates state
            //set state on fetch of data
            setDataRows({
                all: result.data, filtered: result.data, paged: pagedData,
                pager: { currentPage: 1, pageSize: _profilePreferences.pageSize, itemCount: result.data == null ? 0 : result.data.length }
            });

            //hide a spinner
            setLoadingProps({ isLoading: false, message: null });

            //add to recently visited page list
            var revisedList = UpdateRecentFileList(loadingProps.recentFileList, { url: history.location.pathname, caption: caption, iconName: "folder-profile"  });
            setLoadingProps({ recentFileList: revisedList });
        }
        fetchData();
        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
            //setFilterValOnChild('');
        };
    //type passed so that any change to this triggers useEffect to be called again
        //_profilePreferences.pageSize - needs to be passed so that useEffects dependency warning is avoided.
    }, [type, _profilePreferences.pageSize, authTicket]); 

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    // Apply filter on data starting with all rows
    const filterDataRows = (dataRows, val) => {
        const delimiter = ":::";

        //const [dataRows, setDataRows] = useState({ all: [], filtered: [], paged: [], pager: {} });
        if (dataRows == null) return null;

        var filteredCopy = JSON.parse(JSON.stringify(dataRows));

        if (val == null || val === '') {
            return filteredCopy;
        }

        // Filter data - match up against a number of fields
        return filteredCopy.filter((item, i) => {
            var concatenatedSearch = delimiter + item.name.toLowerCase() + delimiter
                + item.description.toLowerCase() + delimiter
                + (item.author != null ? item.author.firstName.toLowerCase() + delimiter : "")
                + (item.author != null ? item.author.lastName.toLowerCase() : "") + delimiter;
            return (concatenatedSearch.indexOf(val.toLowerCase()) !== -1);
        });
    }

    const renderHeaderActionsRow = () => {
        return (
            <div className="header-actions-row">
                <div id="--import-fab" className="d-flex align-items-center cursor-pointer" onClick={onImportClick} >
                    <span className="mr-3">Import</span>
                    <Button variant="fab" className="elevated">
                        <SVGIcon name="add" size="32" fill={color.white} alt="Add profile"/>
                    </Button>
                </div>
            </div>
        );
    }

    const renderNoDataRow = () => {
        return (
            <div className="alert alert-info-custom mt-2 mb-2">
                <div className="text-center" >There are no {entityInfo.name.toLowerCase()} records.</div>
            </div>
        );
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
                <div className="flex-grid no-data">
                    {renderNoDataRow()}
                </div>
            )
        }
        const mainBody = _dataRows.paged.map((item) => {
            return (<ProfileItemRow key={item.id} item={item} currentUserId={authTicket.user.id} showActions={true} cssClass="profile-list-item" />)
        });

        return (
            <>
                { renderHeaderActionsRow() }
                <div className="flex-grid">
                    {mainBody}
                </div>
            </>
        );
    }

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <Fragment>
            <Helmet>
                <title>{AppSettings.Titles.Main + " | " + caption}</title>
            </Helmet>
            <HeaderNav caption={caption} iconName={iconName} showSearch={true} searchValue={filterVal} onSearch={handleOnSearchChange} searchMode="standard" currentUserId={authTicket.user.id}  />
            <div ref={_scrollToRef} id="--cesmii-main-content">
                <div id="--cesmii-left-content">
                    {renderItemsGrid()}
                    {renderPagination()}
                </div>
            </div>
        </Fragment>
    )
}

export default ProfileList;