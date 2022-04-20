import React, { useState, useEffect, useRef } from 'react'
import axiosInstance from "../../services/AxiosService";

import { getTypeDefPreferences, setProfileTypeDisplayMode, setProfileTypePageSize } from '../../services/ProfileService';
import { generateLogMessageString } from '../../utils/UtilityService'
import ProfileTypeDefinitionFilter from './ProfileTypeDefinitionFilter'
import ProfileTypeDefinitionRow from './ProfileTypeDefinitionRow';

import ProfileItemRow from './ProfileItemRow';
import GridPager from '../../components/GridPager'
import ConfirmationModal from '../../components/ConfirmationModal';
import { useAuthState } from "../../components/authentication/AuthContext";
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
    const authTicket = useAuthState();
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const _profilePreferences = getTypeDefPreferences();
    const _scrollToRef = useRef(null);
    const [_dataRows, setDataRows] = useState({
        all: [], itemCount: 0, profile: null
    });
    const [_pager, setPager] = useState({ currentPage: 1, pageSize: _profilePreferences.pageSize, searchVal: null});
    const [_displayMode, setDisplayMode] = useState(_profilePreferences.displayMode == null ? "list" : _profilePreferences.displayMode);
    const [_deleteModal, setDeleteModal] = useState({ show: false, item: null });
    const [_error, setError] = useState({ show: false, message: null, caption: null });
    const [_refreshData, setRefreshData] = useState(0);
    const [_itemCount, setItemCount] = useState(0);

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
        //scrollToRef.current.scrollIntoView();

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
        //bubble up to parent component and it will save state
        if (props.onSearchCriteriaChanged != null) props.onSearchCriteriaChanged(criteria);
    };

    const toggleDisplayMode = (val) => {
        //console.log(generateLogMessageString('toggleDisplayMode', CLASS_NAME));
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
        if (item.selected && props.selectMode === "single")
        copy.forEach(r => {
            if (r.id !== item.id) r.selected = false;
        });
        setDataRows({ ..._dataRows, all: copy });
        //bubble up to parent
        if (props.onGridRowSelect) props.onGridRowSelect(item);
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

            var url = `profiletypedefinition/library`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

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
                //setRefreshData(false);

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
            });
        }

        //only trigger change on certain searchcriteria updates
        if (_refreshData > 0 || props.searchCriteriaChanged) {
            fetchData();
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
            //setFilterValOnChild('');
        };
    }, [_refreshData, props.searchCriteriaChanged]);

    //-------------------------------------------------------------------
    // Region: Update criteria if profileId(s) passed in
    // //if profiles are passed in the props, update the search criteria to filter on the passed in properties
    //-------------------------------------------------------------------
/*
    useEffect(() => {

        //Scenarios
        //Typical - search criteria cache populated on login - all dependent data in place
        //Less common - search criteria missing as we go from older version to newer version.
        //      In this case, if user is trying to ViewTypeDefinitions, we need the search criteria
        //      so we can assign the profileId in the proper place. If this happens, set flag to go get 
        //      data and then tell user to try loading page again.
        //Note - the filter component will also be checking for existence of the searchCriteria - it will set the
        //      refresh flag

        if (props.searchCriteria == null || props.searchCriteria.filters == null) {
            //update state to trigger retrieval of search criteria data
            if (!loadingProps.refreshSearchCriteria) setLoadingProps({ refreshSearchCriteria: true });

            //Scenario 1 - no search criteria data and profileId == null 
            //Scenario 2 - no search criteria data and profileId != null 
            //Inform user to reload page.By then, we will have data they need.
            setLoadingProps({
                inlineMessages: [
                    { id: new Date().getTime(), severity: "danger", body: 'An error occurred loading the type library. Please try again.', isTimed: true }]
            });
            console.error(generateLogMessageString('useEffect||InitGrid||Search criteria data was not loaded yet. System is trying to reload data', CLASS_NAME));
            return;
        }
        //Scenario 3 - typical 
        else {
            //update search criteria - if profile id passed in
            var criteria = JSON.parse(JSON.stringify(props.searchCriteria));
            if (props.filterProfiles != null && props.filterProfiles.length > 0) {
                criteria = clearSearchCriteria(criteria);
                //loop over each filter profile and update filter property
                props.filterProfiles.forEach(id => {
                    toggleSearchFilterSelected(criteria, AppSettings.SearchCriteriaCategory.Profile, parseInt(id));
                });
            }
            //update state for other components to see
            setLoadingProps({ searchCriteria: criteria });
            //trigger API call
            setRefreshData(_refreshData + 1);
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
            //setFilterValOnChild('');
        };
    }, [props.filterProfiles]);
*/

    //-------------------------------------------------------------------
    // Region: Delete event handlers
    //-------------------------------------------------------------------
    // Delete ONE - from row
    const onDeleteItemClick = (item) => {
        console.log(generateLogMessageString(`onDeleteItemClick`, CLASS_NAME));
        setDeleteModal({ show: true, item: item });
    };

    //on confirm click within the modal, this callback will then trigger the next step (ie call the API)
    const onDeleteConfirm = () => {
        console.log(generateLogMessageString(`onDeleteConfirm`, CLASS_NAME));
        deleteItem(_deleteModal.item);
        setDeleteModal({ show: false, item: null });
    };

    const deleteItem = (item) => {
        console.log(generateLogMessageString(`deleteItem||Id:${item.id}`, CLASS_NAME));

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform delete call
        var data = { id: item.id };
        var url = `profiletypedefinition/delete`;
        axiosInstance.post(url, data)  //api allows one or many
            .then(result => {

                if (result.data.isSuccess) {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            {
                                id: new Date().getTime(), severity: "success", body: `Type definition was deleted`, isTimed: true
                            }
                        ],
                        //get count from server...this will trigger that call on the side menu
                        refreshTypeCount: true
                    });
                    //force re-load to show the imported nodesets in table
                    setRefreshData(_refreshData + 1);
                }
                else {
                    //update spinner, messages
                    setError({ show: true, caption: 'Delete Item Error', message: `An error occurred deleting this item: ${result.data.message}` });
                    setLoadingProps({isLoading: false, message: null});
                }

            })
            .catch(error => {
                //hide a spinner, show a message
                setError({ show: true, caption: 'Delete Item Error', message: `An error occurred deleting this item.` });
                setLoadingProps({ isLoading: false, message: null });

                console.log(generateLogMessageString('deleteItem||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
                console.log(error);
                //scroll back to top
                window.scroll({
                    top: 0,
                    left: 0,
                    behavior: 'smooth',
                });
            });

    };

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
                            //console.log(generateLogMessageString(`onErrorMessageOK`, CLASS_NAME));
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
        if (_dataRows.profileFilters == null || _dataRows.profileFilters.length === 0 ) return;

        const mainBody = _dataRows.profileFilters.map((item) => {
            return (
                <ProfileItemRow key={item.id} mode="simple" item={item} currentUserId={authTicket.user.id}
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
            return (<ProfileTypeDefinitionRow key={item.id} item={item} currentUserId={authTicket.user.id} showActions={true}
                cssClass={`profile-list-item ${props.rowCssClass ?? ''}`} onDeleteCallback={onDeleteItemClick} displayMode={_displayMode}
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

    //render the delete modal when show flag is set to true
    //callbacks are tied to each button click to proceed or cancel
    const renderDeleteConfirmation = () => {

        if (!_deleteModal.show) return;

        var message = `You are about to delete your type definition '${_deleteModal.item.name}'. This action cannot be undone. Are you sure?`;
        var caption = `Delete Item`;

        return (
            <>
                <ConfirmationModal showModal={_deleteModal.show} caption={caption} message={message}
                    icon={{ name: "warning", color: color.trinidad }}
                    confirm={{ caption: "Delete", callback: onDeleteConfirm, buttonVariant: "danger" }}
                    cancel={{
                        caption: "Cancel",
                        callback: () => {
                            console.log(generateLogMessageString(`onDeleteCancel`, CLASS_NAME));
                            setDeleteModal({ show: false, item: null });
                        },
                        buttonVariant: null
                    }} />
            </>
        );
    };

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
            {renderDeleteConfirmation()}
            {renderErrorMessage()}
        </>
    )
}

export default ProfileTypeDefinitionListGrid;