import React, { useState, useEffect, Fragment, useRef } from 'react'
import { useParams, useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import axios from 'axios'

import { getProfilePreferences, setProfilePageSize } from '../services/ProfileService';
import { AppSettings } from '../utils/appsettings'
import { generateLogMessageString, pageDataRows } from '../utils/UtilityService'
import HeaderNav from '../components/HeaderNav'
import GridPager from '../components/GridPager'
import ProfileItemRow from './shared/ProfileItemRow';
import NamespaceItemRow from './shared/NamespaceItemRow';
import { useAuthContext } from "../components/authentication/AuthContext";
import { useLoadingContext, UpdateRecentFileList } from "../components/contexts/LoadingContext";

const CLASS_NAME = "NamespaceEntity";
const entityInfo = {
    name: "Namespace",
    namePlural: "Namespaces",
    entityUrl: "/namespace/:name",
    listUrl: "/namespaces/all"
}

function NamespaceEntity() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();
    const { namespace } = useParams();
    const { authTicket } = useAuthContext();
    const _profilePreferences = getProfilePreferences();
    const _scrollToRef = useRef(null);
    const [_item, setItem] = useState({});
    const [_dataRows, setDataRows] = useState({
        all: [], filtered: [], paged: [],
        pager: { currentPage: 1, pageSize: _profilePreferences.pageSize, itemCount: 0 }
    });
    const filterVal = '';
    const caption = entityInfo.name ;
    const iconName = "folder-profile";
    const { loadingProps, setLoadingProps } = useLoadingContext();

    const initComponent = () => {
        console.log(generateLogMessageString(`initComponent`, CLASS_NAME));
    };

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const handleOnSearchChange = (val) => {
        //raised from header nav
        console.log(generateLogMessageString('handleOnSearchChange||Search value: ' + val, CLASS_NAME));
        //if (filterVal === val) return;

        //filter the data, update the state
        var filteredData = filterDataRows(_dataRows.all, val);
        //setFilterVal(val); //update state
        //setDataRowsFiltered(filteredData); //filter rows state updated to matches

        //page the filtered data and update the paged data state
        var pagedData = pageDataRows(filteredData, 1, _dataRows.pager.pageSize);
        //updatePagerState(pagedData, 1, _pager.pageSize, filteredData == null ? 0 : filteredData.length);
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

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------
    useEffect(() => {
        //TBD - enhance the mock api to return profiles by user id
        async function fetchData() {
            //show a spinner
            setLoadingProps({ isLoading: true, message: null });

            //TBD - in phase II system, the API would handle finding all profiles for this namespace
            const result = await axios(`${AppSettings.BASE_API_URL}/profile?namespace=${namespace}`);

            //TBD - assign the item using data from the first profile. in phase II, we would get this from API
            if (result.data.length > 0) {
                setItem({ id: 1, namespace: result.data[0].namespace, childCount: result.data.length, metaTags: result.data[0].metaTags });
            }
            result.data.sort((a, b) => {
                if (a.name.toLowerCase() < b.name.toLowerCase()) {
                    return -1;
                }
                if (a.name.toLowerCase() > b.name.toLowerCase()) {
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
            if (result.data.length > 0) {
                var revisedList = UpdateRecentFileList(loadingProps.recentFileList, { url: history.location.pathname, caption: caption + ' - ' + result.data[0].namespace, iconName: "folder-profile" });
                setLoadingProps({ recentFileList: revisedList });
            }
        }
        fetchData();
        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
            //setFilterValOnChild('');
        };
    //type passed so that any change to this triggers useEffect to be called again
        //_profilePreferences.pageSize - needs to be passed so that useEffects dependency warning is avoided.
    }, [namespace, _profilePreferences.pageSize, authTicket]); 

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

    const renderNoDataRow = () => {
        return (
            <div className="row">
                <div className="col no-data center" >There are no profile records for this namespace.</div>
            </div>
        );
    }

    //render pagination ui
    const renderPagination = () => {
        return <GridPager currentPage={_dataRows.pager.currentPage} pageSize={_dataRows.pager.pageSize} itemCount={_dataRows.pager.itemCount} onChangePage={onChangePage}  />
    }

    //render the namespace grid
    const renderNamespaceHeader = () => {
        if (_item == null || _item === {}) return;
        return (
            <div className="flex-grid mb-5">
                <NamespaceItemRow key={_item.id} item={_item} currentUserId={authTicket.user.id} hasActions={false} cssClass="namespace-list-item" />
            </div>
        );
    };

    //render the main grid
    const renderItemsGrid = () => {
        if (_dataRows.paged == null || _dataRows.paged.length === 0) {
            return (
                <div className="flex-grid no-data">
                    {renderNoDataRow()}
                </div>
            )
        }
        const mainBody = _dataRows.paged.map((profile) => {
            return (<ProfileItemRow key={profile.id} item={profile} currentUserId={authTicket.user.id} showActions={true} cssClass="profile-list-item" />)
        });

        return (
            <div className="flex-grid">
                {mainBody}
            </div>
        );
    }

    //call init function 
    initComponent(); 

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <Fragment>
            <Helmet>
                <title>{AppSettings.Titles.Main + " | " + caption}</title>
            </Helmet>
            {/* <HeaderNav caption={caption} iconName={"folder-shared"} showSearch={true} searchValue={filterVal} onSearch={handleOnSearchChange} /> */}
            <HeaderNav caption={caption} iconName={iconName} showSearch={true} searchValue={filterVal} onSearch={handleOnSearchChange} />
            <div ref={_scrollToRef} id="--cesmii-main-content">
                <div id="--cesmii-left-content">
                    {renderNamespaceHeader()}
                    {renderItemsGrid()}
                    {renderPagination()}
                </div>
            </div>
        </Fragment>
    )
}

export default NamespaceEntity;