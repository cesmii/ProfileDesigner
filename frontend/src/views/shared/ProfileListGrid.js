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
    //importer
    const [_forceReload, setForceReload] = useState(0);

    const [_profileSearchCriteria, setProfileSearchCriteria] = useState(props.searchCriteria);
    const [_profileSearchCriteriaChanged, setProfileSearchCriteriaChanged] = useState(0);

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

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------

    //bubble up search criteria changed so the parent page can control the search criteria
    const onProfileSearchCriteriaChanged = (criteria) => {
        console.log(generateLogMessageString(`onProfileSearchCriteriaChanged`, CLASS_NAME));
        //update state
        setProfileSearchCriteria(criteria);
        //trigger api to get data
        setProfileSearchCriteriaChanged(_profileSearchCriteriaChanged + 1);
        if (props.onSearchCriteriaChanged != null) props.onSearchCriteriaChanged(criteria);
    };

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchData() {
            //show a spinner
            setLoadingProps({ isLoading: true, message: null });

            const localProfileSelected = _profileSearchCriteria?.filters?.find(x => x.id === 1)?.items[0]?.selected;
            const baseProfileSelected = _profileSearchCriteria?.filters?.find(x => x.id === 2)?.items[0]?.selected;
            const cloudLibSelected = _profileSearchCriteria?.filters?.find(x => x.id === 3)?.items[0]?.selected;

            let url;
            if (props.isMine || (!baseProfileSelected && !cloudLibSelected)) {
                url = 'profile/mine';
            }
            else if (!cloudLibSelected) {
                url = 'profile/library';
            }
            else {
                url = 'profile/cloudlibrary';
            }

            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

            const keywords = _profileSearchCriteria?.query == null ? null 
                : [_profileSearchCriteria?.query?.toString()];

            // Cursor pagination can only move one page at a time
            // TODO Adjust the pager UI?
            let cursor = null;
            let beforeCursor = false;
            if (_pager.currentPage === 1) {
                cursor = null;
            }
            else if (_dataRows?.pageNumber > _pager.currentPage + 1 &&  _pager.currentPage === Math.ceil(_dataRows.itemCount / _pager.pageSize))
            {
                cursor = null;
                beforeCursor = true;
            }
            else if (_pager.currentPage > _dataRows?.pageNumber) {
                cursor = _dataRows?.lastCursor;
            }
            else if (_pager.currentPage < _dataRows?.pageNumber) {
                cursor = _dataRows?.firstCursor;
                beforeCursor = true;
            }
            const data = {
                Query: _pager.searchVal,

                // Offset pagination for local profiles
                Skip: (_pager.currentPage - 1) * _pager.pageSize,
                Take: _pager.pageSize,

                // Cursor pagination for CloudLib
                Cursor: cursor,
                BeforeCursor: beforeCursor,

                // CloudLib filters
                AddLocalLibrary: (localProfileSelected),
                ExcludeLocalLibrary: (!baseProfileSelected && !localProfileSelected),
                Keywords: keywords
            };

            await axiosInstance.post(url, data).then(result => {
                if (result.status === 200) {

                    //set state on fetch of data
                    setDataRows({
                        all: result.data.data,
                        itemCount: result.data.count,
                        firstCursor: result.data.firstCursor,
                        lastCursor: result.data.lastCursor,
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
        fetchData();
        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
            //setFilterValOnChild('');
        };
    //type passed so that any change to this triggers useEffect to be called again
        //_nodesetPreferences.pageSize - needs to be passed so that useEffects dependency warning is avoided.
    }, [_pager, _forceReload, props.isMine, _profileSearchCriteria]);


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
            setLoadingProps({refreshProfileList: null, refreshProfileSearchCriteria: true});
        }
        
        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||importingItemsChange||Cleanup', CLASS_NAME));
        };
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
                onEditCallback={onEdit} onDeleteCallback={onDeleteItemClick} onRowSelect={onRowSelect}
                onImportCallback={onImport}
                selectedItems={props.selectedItems} 
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
            <ProfileFilter onSearchCriteriaChanged={onProfileSearchCriteriaChanged} noSortOptions="true"
                //displayMode={_displayMode}
                //toggleDisplayMode={toggleDisplayMode} itemCount={_itemCount}
                cssClass={props.rowCssClass} searchCriteria={props.searchCriteria} noSearch={props.noSearch} noClearAll="true" />
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