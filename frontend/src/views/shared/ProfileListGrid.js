import React, { useState, useEffect, useRef } from 'react'
import { useMsal } from "@azure/msal-react";
import axiosInstance from "../../services/AxiosService";

import { getProfilePreferences, setProfilePageSize } from '../../services/ProfileService';
import { generateLogMessageString } from '../../utils/UtilityService'
import GridPager from '../../components/GridPager'
import ProfileFilter from './ProfileFilter'

import ProfileItemRow from './ProfileItemRow';
import { useLoadingContext } from "../../components/contexts/LoadingContext";

import '../styles/ProfileList.scss';
import { AppSettings } from '../../utils/appsettings';

const CLASS_NAME = "ProfileListGrid";
const entityInfo = {
    name: "Profile",
    namePlural: "Profiles",
    entityUrl: "/profile/:id",
    listUrl: "/profiles/library"
}

function ProfileListGrid(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { instance } = useMsal();
    const _activeAccount = instance.getActiveAccount();
    const _profilePreferences = getProfilePreferences();
    const _scrollToRef = useRef(null);
    const [_dataRows, setDataRows] = useState({
        all: [], itemCount: 0
    });
    const [_pager, setPager] = useState({ currentPage: 1, pageSize: _profilePreferences.pageSize, searchVal: null});
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_isLoading, setIsLoading] = useState(null);

    //importer
    const [_forceReload, setForceReload] = useState(0);

    const [_searchCriteria, setSearchCriteria] = useState(props.searchCriteria);
    const [_searchCriteriaChanged, setSearchCriteriaChanged] = useState(0);

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onChangePage = (currentPage, pageSize) => {
        console.log(generateLogMessageString(`onChangePage||Current Page: ${currentPage}, Page Size: ${pageSize}`, CLASS_NAME));

        //this will trigger a fetch from the API to pull the data for the filtered criteria
        setPager({ ..._pager, currentPage: currentPage, pageSize: pageSize });

        //scroll screen to top of grid on page change
        ////scroll a bit higher than the top edge so we get some of the header in the view
        window.scrollTo({ top: (_scrollToRef.current.offsetTop-120), behavior: 'smooth' }); 
        //scrollToRef.current.scrollIntoView();

        //preserve choice in local storage
        setProfilePageSize(pageSize);
    };

    // Delete ONE - from row
    const onDeleteItemClick = (item) => {
        console.log(generateLogMessageString(`onDeleteItemClick`, CLASS_NAME));
        if (props.onDeleteItemClick) props.onDeleteItemClick(item);
    };

    const onEdit = (item) => {
        console.log(generateLogMessageString(`onEdit`, CLASS_NAME));
        //bubble up to parent
        if (props.onEdit) props.onEdit(item);
    };
    const onImport = (item) => {
        console.log(generateLogMessageString(`onImport`, CLASS_NAME));
        //bubble up to parent
        if (props.onImport) props.onImport(item);
    };

    const onRowSelect = (item) => {
        console.log(generateLogMessageString(`onRowSelect`, CLASS_NAME));

        if (props.selectMode == null) return;

        //copy collection, update is selected
        var copy = JSON.parse(JSON.stringify(_dataRows.all));

        //if single select mode, then loop over all others and mark selected to false
        if (item.selected && props.selectMode === "single")
            copy.forEach(r => {
                if (r.id !== item.id) r.selected = false;
            });
        setDataRows({ ..._dataRows, all: copy });
        //bubble up to parent
        if (props.onGridRowSelect) props.onGridRowSelect(item);
    };

    const onRowChanged = (item) => {
        console.log(generateLogMessageString(`onRowChanged`, CLASS_NAME));
        setForceReload(_forceReload + 1)
    }

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------

    //bubble up search criteria changed so the parent page can control the search criteria
    const onProfileSearchCriteriaChanged = (criteria) => {
        console.log(generateLogMessageString(`onProfileSearchCriteriaChanged`, CLASS_NAME));
        //update state
        setSearchCriteria(criteria);
        //trigger api to get data
        setSearchCriteriaChanged(_searchCriteriaChanged + 1);
        if (props.onSearchCriteriaChanged != null) props.onSearchCriteriaChanged(criteria);
    };

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchDataProfile() {
            //show a spinner
            setLoadingProps({ isLoading: true, message: null });
            setIsLoading(true);

            const url = `profile/library`;
            console.log(generateLogMessageString(`useEffect||fetchDataProfile||${url}`, CLASS_NAME));

            //apply the page size info from this page
            _searchCriteria.skip = (_pager.currentPage - 1) * _pager.pageSize;
            _searchCriteria.take = _pager.pageSize;
            //call search
            await axiosInstance.post(url, _searchCriteria).then(result => {
                if (result.status === 200) {

                    //set state on fetch of data
                    //if profile id filter was passed in, then grab the profile info from the 1st returned item for display
                    setDataRows({
                        all: result.data.data, itemCount: result.data.count
                    });

                    //hide a spinner
                    setLoadingProps({ isLoading: false, message: null });

                    //preserve and display item count  
                    //setItemCount(result.data.count);

                } else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving these types.', isTimed: true }]
                    });
                    //setItemCount(null);
                }
                //hide a spinner
                setLoadingProps({ isLoading: false, message: null });
                setIsLoading(false);

            }).catch(e => {
                if ((e.response && e.response.status === 401) || e.toString().indexOf('Network Error') > -1) {
                    //do nothing, this is handled in routes.js using common interceptor
                    //setAuthTicket(null); //the call of this will clear the current user and the token
                }
                else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving these profiles.', isTimed: true }]
                    });
                    //setItemCount(null);
                }
                setIsLoading(false);
            });
        }

        async function fetchDataCloudLib() {

            const url = 'profile/cloudlibrary';
            
            console.log(generateLogMessageString(`useEffect||fetchDataCloudLib||${url}`, CLASS_NAME));

            // Cursor pagination can only move one page at a time
            let cursor;
            let pageBackwards = false;
            if (_pager.currentPage === 1) {
                cursor = null;
            }
            else if (_pager.currentPage === _dataRows?.pageNumber) {
                return;
            }
            else if (_pager.currentPage > _dataRows?.pageNumber + 1 && _pager.currentPage === Math.ceil(_dataRows.itemCount / _pager.pageSize)) {
                // Jump to last page
                cursor = null;
                pageBackwards = true;
            }
            else if (_pager.currentPage > _dataRows?.pageNumber) {
                cursor = _dataRows?.endCursor;
                _pager.currentPage = _dataRows?.pageNumber + 1;
            }
            else if (_pager.currentPage < _dataRows?.pageNumber) {
                cursor = _dataRows?.startCursor;
                pageBackwards = true;
                _pager.currentPage = _dataRows?.pageNumber - 1;
            }

            //apply the page size info from this page
            _searchCriteria.skip = (_pager.currentPage - 1) * _pager.pageSize;
            _searchCriteria.take = _pager.pageSize;
            
            //dynamically append CloudLib specific params to common filtering model
            // Cursor pagination for CloudLib
            _searchCriteria.cursor= cursor;
            _searchCriteria.pageBackwards = pageBackwards;

            //show a spinner
            setLoadingProps({ isLoading: true, message: null });

            await axiosInstance.post(url, _searchCriteria).then(result => {
                if (result.status === 200) {

                    let itemCount = result.data.count;
                    if (result.data.hasNextPage != null && !result.data.hasNextPage
                        && !(_searchCriteria.pageBackwards && _searchCriteria.cursor != null)
                        && _pager.currentPage < Math.ceil(itemCount / _pager.pageSize)) {
                        // There were more items reported than actually available (backend filtering or other bug): adjust total items
                        itemCount = _pager.pageSize * (_pager.currentPage - 1) + result.data.data.length;
                    }
                    if (result.data.hasPreviousPage != null && !result.data.hasPreviousPage && _searchCriteria.pageBackwards && _pager.currentPage !== 1) {
                        // We are at the first page but the pager is out of sync - there were more items reported than actually available (backend filtering or other bug): adjust total items
                        _pager.currentPage = 1;
                    }
                    //set state on fetch of data
                    setDataRows({
                        all: result.data.data,
                        itemCount: itemCount,
                        startCursor: result.data.startCursor,
                        endCursor: result.data.endCursor,
                        pageNumber: _pager.currentPage,
                    });

                    //hide a spinner
                    setLoadingProps({ isLoading: false, message: null });

                } else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving these nodesets.', isTimed: true }]
                    });
                }
                //hide a spinner
                setLoadingProps({ isLoading: false, message: null });

            }).catch(e => {
                if ((e.response && e.response.status === 401) || e.toString().indexOf('Network Error') > -1) {
                    //do nothing, this is handled in routes.js using common interceptor
                    //setAuthTicket(null); //the call of this will clear the current user and the token
                }
                else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving these nodesets.', isTimed: true }]
                    });
                }
            });
        }

        //don't fetch data if this is null
        if (_searchCriteria == null) return;

        //this component is shared by profile list and cloud lib importer. Get the proper data based
        //on component mode
        if (!props.mode || props.mode === AppSettings.ProfileListMode.Profile) fetchDataProfile();
        else if (props.mode === AppSettings.ProfileListMode.CloudLib) fetchDataCloudLib();

        //type passed so that any change to this triggers useEffect to be called again
        //_nodesetPreferences.pageSize - needs to be passed so that useEffects dependency warning is avoided.
    }, [_pager, _forceReload, props.mode, _searchCriteria]);


    useEffect(() => {
        //check for searchcriteria - if there, update state and then that triggers retrieval of grid data
        if (props.searchCriteria == null) return;
        setSearchCriteria(JSON.parse(JSON.stringify(props.searchCriteria)));
    }, [props.searchCriteria]);

    //-------------------------------------------------------------------
    // useEffect - Importing items
    // if the importing items list changes, it is triggered from here or from import messages component.
    // if triggered from import messages component, then it means something has completed. If so, trigger 
    // a refresh of the profile list
    //-------------------------------------------------------------------
    useEffect(() => {

        //if the importing message component has triggered a refresh, handle it here. 
        if (loadingProps.refreshProfileList === true) {
            setForceReload(_forceReload + 1);
            setLoadingProps({ refreshProfileList: null, refreshProfileSearchCriteria: true, refreshCloudLibImporterSearchCriteria: true});
        }
    }, [loadingProps.refreshProfileList]);


    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderProfileFilters = () => {
        if (_dataRows.profileFilters == null || _dataRows.profileFilters.length === 0) return;

        const mainBody = _dataRows.profileFilters.map((item) => {
            return (
                <ProfileItemRow key={item.id} mode="simple" item={item} activeAccount={_activeAccount}
                    cssClass={`profile-list-item shaded rounded ${_dataRows.profileFilters.length > 1 ? 'mb-1' : ''} ${props.rowCssClass ?? ''}`} />)
        });

        return (
            <div className="mb-2">
                {mainBody}
            </div>
        );
    };
    const renderNoDataRow = () => {
        if (_isLoading) return null; //don't show no data message if we are trying to load data

        return (
            <div className="alert alert-info-custom mt-2 mb-2">
                <div className="text-center" >There are no {entityInfo.name.toLowerCase()} records.</div>
            </div>
        );
    }

    //render pagination ui
    const renderPagination = () => {
        if (_dataRows == null || _dataRows.all.length === 0) return;
        return <GridPager currentPage={_pager.currentPage} pageSize={_pager.pageSize} itemCount={_dataRows.itemCount} onChangePage={onChangePage} />
    }

    //render the main grid
    const renderItemsGrid = () => {
        if (!loadingProps.isLoading && (_dataRows.all == null || _dataRows.all.length === 0)) {
            return (
                <div className="flex-grid no-data">
                    {renderNoDataRow()}
                </div>
            )
        }
        const mainBody = _dataRows.all.map((item) => {
            return (<ProfileItemRow key={item.id} item={item} activeAccount={_activeAccount}
                showActions={true} cssClass={`profile-list-item ${props.rowCssClass ?? ''}`} selectMode={props.selectMode}
                onEditCallback={onEdit} onDeleteCallback={onDeleteItemClick} onRowSelect={onRowSelect} onRowChanged={onRowChanged}
                onImportCallback={onImport}
                selectedItems={props.selectedItems} navigateModal={props.navigateModal}
            />)
        });

        return (
            <>
                <div className="">
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
            {renderProfileFilters()}
            {!props.hideFilter &&
                <ProfileFilter onSearchCriteriaChanged={onProfileSearchCriteriaChanged} noSortOptions="true"
                    //displayMode={_displayMode}
                    //toggleDisplayMode={toggleDisplayMode} itemCount={_itemCount}
                cssClass={props.rowCssClass} searchCriteria={_searchCriteria} hideSearchBox={props.hideSearchBox} hideClearAll={true} />
            }
            <div className="">
                <div ref={_scrollToRef} className="row">
                    <div className="col-12">
                        {renderItemsGrid()}
                    </div>
                </div>
                <div className="row">
                    <div className="col-12">
                        {renderPagination()}
                    </div>
                </div>
            </div>
        </>
    )
}

export default ProfileListGrid;