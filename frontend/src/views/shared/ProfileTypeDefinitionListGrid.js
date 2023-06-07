import React, { useState, useEffect, useRef } from 'react'
import { useMsal } from "@azure/msal-react";
import axiosInstance from "../../services/AxiosService";

import { getTypeDefPreferences, setProfileTypeDisplayMode, setProfileTypePageSize } from '../../services/ProfileService';
import { generateLogMessageString } from '../../utils/UtilityService'
import ProfileTypeDefinitionFilter from './ProfileTypeDefinitionFilter'
import ProfileTypeDefinitionRow from './ProfileTypeDefinitionRow';

import ProfileItemRow from './ProfileItemRow';
import GridPager from '../../components/GridPager'
import ConfirmationModal from '../../components/ConfirmationModal';
import { useLoadingContext } from "../../components/contexts/LoadingContext";

import color from '../../components/Constants'

const CLASS_NAME = "ProfileTypeDefinitionListGrid";
const entityInfo = {
    name: "Type Definition",
    namePlural: "Types",
    entityUrl: "/type/:id",
    listUrl: "/types/library"
}

function ProfileTypeDefinitionListGrid(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { instance } = useMsal();
    const _activeAccount = instance.getActiveAccount();
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const _profileTypeDefPreferences = getTypeDefPreferences();
    const _scrollToRef = useRef(null);
    const [_dataRows, setDataRows] = useState({
        all: [], itemCount: 0, profile: null
    });
    const [_pager, setPager] = useState({ currentPage: 1, pageSize: _profileTypeDefPreferences.pageSize, searchVal: null});
    const [_displayMode, setDisplayMode] = useState(_profileTypeDefPreferences.displayMode == null ? "list" : _profileTypeDefPreferences.displayMode);
    const [_error, setError] = useState({ show: false, message: null, caption: null });
    const [_refreshData, setRefreshData] = useState(0);
    const [_itemCount, setItemCount] = useState(0);
    const [_isLoading, setIsLoading] = useState(null);

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onChangePage = (currentPage, pageSize) => {
        console.log(generateLogMessageString(`onChangePage||Current Page: ${currentPage}, Page Size: ${pageSize}`, CLASS_NAME));

        var criteria = JSON.parse(JSON.stringify(props.searchCriteria));
        criteria.skip = (currentPage - 1) * pageSize;
        criteria.take = pageSize;

        //for api call
        //setLoadingProps({ searchCriteria: criteria });
        //bubble up to parent component and it will save state
        if (props.onSearchCriteriaChanged != null) props.onSearchCriteriaChanged(criteria);
        //for grid pager component
        setPager({ ..._pager, currentPage: currentPage, pageSize: pageSize });

        //scroll screen to top of grid on page change
        ////scroll a bit higher than the top edge so we get some of the header in the view
        window.scrollTo({ top: (_scrollToRef.current.offsetTop-120), behavior: 'smooth' }); 

        //preserve choice in local storage
        setProfileTypePageSize(pageSize);

        //this will trigger a fetch from the API to pull the data for the filtered criteria
        //setRefreshData(_refreshData + 1);
    };

    const onSearchCriteriaChanged = (criteria) => {
        //raised from filter component
        //console.log(generateLogMessageString('handleOnSearchChange||Search value: ' + val, CLASS_NAME));
        //setLoadingProps({ searchCriteria: criteria });
        //setRefreshData(_refreshData + 1);
        setPager({ ..._pager, currentPage: 1 });
        //bubble up to parent component and it will save state
        if (props.onSearchCriteriaChanged != null) props.onSearchCriteriaChanged(criteria);
    };

    const toggleDisplayMode = (val) => {
        if (_displayMode === val) return;
        setDisplayMode(val);
        setProfileTypeDisplayMode(val);
    }

    const onRowSelect = (item) => {
        console.log(generateLogMessageString(`onRowSelect`, CLASS_NAME));

        if (props.selectMode == null) return;

        //copy collection, update is selected
        var copy = JSON.parse(JSON.stringify(_dataRows.all));

        //if single select mode, then loop over all others and mark selected to false
        if (item.selected && props.selectMode === "single") {
            copy.forEach(r => {
                if (r.id !== item.id) r.selected = false;
            });
        }
        setDataRows({ ..._dataRows, all: copy });
        //bubble up to parent
        if (props.onGridRowSelect) props.onGridRowSelect(item);
    };

    const onDeleteCallback = (isSuccess) => {
        console.log(generateLogMessageString(`onDeleteCallback`, CLASS_NAME));
        if (isSuccess) {
            setRefreshData(_refreshData + 1);
        }
    };

    //-------------------------------------------------------------------
    // Region: Parent state update
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------
    useEffect(() => {
        //TBD - enhance the mock api to return profiles by user id
        async function fetchData() {
            //show a spinner
            setLoadingProps({ isLoading: true, message: null });
            setIsLoading(true);

            const url = `profiletypedefinition/library`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

            //apply the page size info from this page
            props.searchCriteria.skip = (_pager.currentPage - 1) * _pager.pageSize;
            props.searchCriteria.take = _pager.pageSize;
            //call search
            await axiosInstance.post(url, props.searchCriteria).then(result => {
                if (result.status === 200) {

                    //set state on fetch of data
                    //if profile id filter was passed in, then grab the profile info from the 1st returned item for display
                    setDataRows({
                        all: result.data.data, itemCount: result.data.count,
                        profileFilters: result.data.profiles
                    });

                    //hide a spinner
                    setLoadingProps({ isLoading: false, message: null });

                    //preserve and display item count  
                    setItemCount(result.data.count);

                } else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving these types.', isTimed: true }]
                    });
                    //preserve and display item count  
                    setItemCount(null);
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
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving these types.', isTimed: true }]
                    });
                    //preserve and display item count  
                    setItemCount(null);
                }
                setIsLoading(null);
            });
        }

        //don't fetch data if this is null, other components will trigger refresh
        if (props.searchCriteria == null) return;

        //only trigger change on certain searchcriteria updates
        if (_refreshData > 0 || props.searchCriteriaChanged) {
            fetchData();
        }

    }, [_refreshData, props.searchCriteria, props.searchCriteriaChanged]);

    //-------------------------------------------------------------------
    // Region: Delete event handlers
    //-------------------------------------------------------------------
    //render error message as a modal to force user to say ok.
    const renderErrorMessage = () => {

        if (!_error.show) return;

        return (
            <>
                <ConfirmationModal showModal={_error.show} caption={_error.caption} message={_error.message}
                    icon={{ name: "warning", color: color.trinidad }}
                    confirm={null}
                    cancel={{
                        caption: "OK",
                        callback: () => {
                            setError({ show: false, caption: null, message: null });
                        },
                        buttonVariant: 'danger'
                    }} />
            </>
        );
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderProfileFilters = () => {
        if (_dataRows.profileFilters == null || _dataRows.profileFilters.length === 0 || !props.showProfileFilter ) return;

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
        if (_isLoading) return null; //don't show no data message if we are trying to load data

        //_isLoading = null until first load happens or we encounter error, true while loading and false when finished loading
        if (_isLoading != null && !loadingProps.isLoading && (_dataRows.all == null || _dataRows.all.length === 0)) {
            return (
                <div className="flex-grid no-data">
                    {renderNoDataRow()}
                </div>
            )
        }
        const mainBody = _dataRows.all.map((item) => {
            return (<ProfileTypeDefinitionRow key={item.id} item={item} activeAccount={_activeAccount} showActions={true}
                cssClass={`profile-list-item ${props.rowCssClass ?? ''}`} onDeleteCallback={onDeleteCallback} displayMode={_displayMode}
                selectMode={props.selectMode} onRowSelect={onRowSelect} selectedItems={props.selectedItems} />)
        });
        if (_displayMode === "tile") {
            return (
                <div className="mt-1">
                    <div className="row">
                        {mainBody}
                    </div>
                </div>
            );
        }
        else {
            return (
                <div className="mt-1">
                    {mainBody}
                </div>
            );
        }
    }

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            {renderProfileFilters()}
            <ProfileTypeDefinitionFilter onSearchCriteriaChanged={onSearchCriteriaChanged} displayMode={_displayMode}
                toggleDisplayMode={toggleDisplayMode} itemCount={_itemCount} cssClass={props.rowCssClass} searchCriteria={props.searchCriteria} />
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
            {renderErrorMessage()}
        </>
    )
}

export default ProfileTypeDefinitionListGrid;