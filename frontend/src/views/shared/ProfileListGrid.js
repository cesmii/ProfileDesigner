import React, { useState, useEffect, useRef } from 'react'
import { useMsal } from "@azure/msal-react";
import axiosInstance from "../../services/AxiosService";

import { getProfilePreferences, setProfilePageSize } from '../../services/ProfileService';
import { generateLogMessageString } from '../../utils/UtilityService'
import GridPager from '../../components/GridPager'
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
    // Region: Get data 
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchData() {
            //show a spinner
            setLoadingProps({ isLoading: true, message: null });

            var url = `profile/${props.isMine ? 'mine' : 'library'}`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

            var data = { Query: _pager.searchVal, Skip: (_pager.currentPage - 1) * _pager.pageSize, Take: _pager.pageSize };
            await axiosInstance.post(url, data).then(result => {
                if (result.status === 200) {

                    //set state on fetch of data
                    setDataRows({
                        all: result.data.data, itemCount: result.data.count
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
    }, [_pager, _forceReload, props.isMine]);


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
            setLoadingProps({refreshProfileList: null, refreshSearchCriteria: true});
        }
        
        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||importingItemsChange||Cleanup', CLASS_NAME));
        };
    }, [loadingProps.refreshProfileList]);


    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
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