import React, { useState, useEffect, useRef } from 'react'
import { Helmet } from "react-helmet"
import { axiosInstance } from "../../services/AxiosService";
import { useMsal } from "@azure/msal-react";

import { AppSettings } from '../../utils/appsettings'
import { generateLogMessageString } from '../../utils/UtilityService'
import GridPager from '../../components/GridPager'
import { useLoadingContext } from "../../components/contexts/LoadingContext";

import HeaderSearch from '../../components/HeaderSearch';
//import { getLookupPreferences, setLookupPreferencesPageSize } from '../../services/LookupService';
import AdminUserRow from './AdminUserRow';
import ConfirmationModal from '../../components/ConfirmationModal';
import color from '../../components/Constants';

const CLASS_NAME = "AdminUserList";

function AdminUserList() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { instance } = useMsal();
    const _activeAccount = instance.getActiveAccount();
    const _scrollToRef = useRef(null);
    const [_dataRows, setDataRows] = useState({
        all: [], itemCount: 0, listView: true
    });
    const _userPreferences = { pageSize: 25};
    const [_pager, setPager] = useState({ currentPage: 1, pageSize: _userPreferences.pageSize, searchVal: null });
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_deleteModal, setDeleteModal] = useState({ show: false, items: null });
    const [_error, setError] = useState({ show: false, message: null, caption: null });

    const caption = 'Admin';

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const handleOnSearchChange = (val) => {
        //raised from header nav
        //console.log(generateLogMessageString('handleOnSearchChange||Search value: ' + val, CLASS_NAME));

        //this will trigger a fetch from the API to pull the data for the filtered criteria
        setPager({ ..._pager, currentPage: 1, searchVal: val });
    };

    const onChangePage = (currentPage, pageSize) => {
        console.log(generateLogMessageString(`onChangePage||Current Page: ${currentPage}, Page Size: ${pageSize}`, CLASS_NAME));

        //this will trigger a fetch from the API to pull the data for the filtered criteria
        setPager({ ..._pager, currentPage: currentPage, pageSize: pageSize });

        //scroll screen to top of grid on page change
        ////scroll a bit higher than the top edge so we get some of the header in the view
        window.scrollTo({ top: (_scrollToRef.current.offsetTop - 120), behavior: 'smooth' });
        //scrollToRef.current.scrollIntoView();

        //preserve choice in local storage
        //setUserPreferencesPageSize(pageSize);
    };


    //-------------------------------------------------------------------
    // Region: Get data 
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchData() {
            //show a spinner
            setLoadingProps({ isLoading: true, message: null });

            var url = `user/search`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

            var data = { Query: _pager.searchVal, Skip: (_pager.currentPage - 1) * _pager.pageSize, Take: _pager.pageSize };
            await axiosInstance.post(url, data).then(result => {
                if (result.status === 200) {

                    //set state on fetch of data
                    setDataRows({
                        ..._dataRows,
                        all: result.data.data, itemCount: result.data.count
                    });

                    //hide a spinner
                    setLoadingProps({ isLoading: false, message: null });
                } else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving these items.', isTimed: true }]
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
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving these items.', isTimed: true }]
                    });
                }
            });
        }

        fetchData();

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
        //type passed so that any change to this triggers useEffect to be called again
        //_setMarketplacePageSizePreferences.pageSize - needs to be passed so that useEffects dependency warning is avoided.
    }, [_pager]);

    //-------------------------------------------------------------------
    // Region: Event Handling - delete item
    //-------------------------------------------------------------------
    const onDeleteItem = (img) => {
        console.log(generateLogMessageString('onDeleteItem', CLASS_NAME));
        setDeleteModal({ show: true, item: img });
    };

    const onDeleteConfirm = () => {
        console.log(generateLogMessageString('onDeleteConfirm', CLASS_NAME));

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform delete call
        var data = { id: _deleteModal.item.id };
        var url = `user/delete`;
        axiosInstance.post(url, data)  //api allows one or many
            .then(result => {

                if (result.data.isSuccess) {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            {
                                id: new Date().getTime(), severity: "success", body: `Item was deleted`, isTimed: true
                            }
                        ],
                    });
                    //update the item active status. 
                    var i = _dataRows.all.findIndex(x => x.id === _deleteModal.item.id);
                    if (i >= 0) {
                        _dataRows.all[i].isActive = false; 
                        setDataRows({
                            ..._dataRows, all: _dataRows.all, itemCount: _dataRows.itemCount - 1
                        });
                    }
                    //var i = _dataRows.all.findIndex(x => x.id === _deleteModal.item.id);
                    //if (i >= 0) {
                    //    _dataRows.all.splice(i, 1)
                    //    setDataRows({
                    //        ..._dataRows, all: _dataRows.all, itemCount: _dataRows.itemCount - 1
                    //    });
                    //}

                    setDeleteModal({ show: false, item: null });
                }
                else {
                    //update spinner, messages
                    setError({ show: true, caption: 'Delete Item Error', message: `An error occurred deleting this item: ${result.data.message}` });
                    setLoadingProps({ isLoading: false, message: null });
                    setDeleteModal({ show: false, item: null });
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
                setDeleteModal({ show: false, item: null });
            });
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderNoDataRow = () => {
        return (
            <div className="alert alert-info-custom mt-2 mb-2">
                <div className="text-center" >There are no matching items.</div>
            </div>
        );
    }

    //render pagination ui
    const renderPagination = () => {
        if (_dataRows == null || _dataRows.all.length === 0) return;
        return <GridPager currentPage={_pager.currentPage} pageSize={_pager.pageSize} itemCount={_dataRows.itemCount} onChangePage={onChangePage} />
    }

    const renderItemsGridHeader = () => {
        if ((_dataRows.all == null || _dataRows.all.length === 0)) return;
        return (
            <thead>
                <AdminUserRow key="header" item={null} isHeader={true} cssClass="admin-item-row" />
            </thead>
        )
    }

    //render the main grid
    const renderItemsGrid = () => {
        if (!loadingProps.isLoading && (_dataRows.all == null || _dataRows.all.length === 0)) {
            return (
                <tbody>
                    <tr>
                        <td className="no-data">
                            {renderNoDataRow()}
                        </td>
                    </tr>
                </tbody>
            )
        }
        if ((_dataRows.all == null || _dataRows.all.length === 0)) return;

        const mainBody = _dataRows.all.map((item) => {
            return (
                <AdminUserRow key={item.id} item={item} cssClass={`admin-item-row`} onDeleteItem={onDeleteItem} />
            );
        });

        return (
            <tbody>
                {mainBody}
            </tbody>
        )
    }

    //

    //
    //const renderGridActions = () => {
    //    return (
    //        <>
    //            Sort by: tbd - drop down
    //            <Button variant="icon-solo" onClick={onListViewToggle} className={_dataRows.listView ? "ml-2" : "ml-2 inactive"} ><i className="material-icons">format_list_bulleted</i></Button>
    //            <Button variant="icon-solo" onClick={onTileViewToggle} className={!_dataRows.listView ? "ml-2" : "ml-2 inactive"}  ><i className="material-icons">grid_view</i></Button>
    //        </>
    //    );
    //}

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

    //render the delete modal when show flag is set to true
    //callbacks are tied to each button click to proceed or cancel
    const renderDeleteConfirmation = () => {

        if (!_deleteModal.show) return;

        var message = `You are about to delete '${_deleteModal.item.userName}'. This action cannot be undone. Are you sure?`;
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
            <Helmet>
                <title>{AppSettings.Titles.Main + " Admin | " + caption}</title>
            </Helmet>
            <div className="row py-2 pb-4">
                <div className="col-sm-8">
                    <h1>Admin | Manage Users</h1>
                </div>
                <div className="col-sm-4 d-flex align-items-center justify-content-end" >
                    <HeaderSearch showAdvancedSearch={false} filterVal={_pager.searchVal == null ? null : _pager.searchVal} onSearch={handleOnSearchChange} searchMode="standard" activeAccount={_activeAccount} />
                </div>
            </div>

            <div className="row pb-2" >
                <div className="col-sm-12 d-flex align-items-center" >
                    {(_dataRows.itemCount != null && _dataRows.itemCount > 0) ?
                        <>
                            <span className="px-2 ml-auto font-weight-bold">{_dataRows.itemCount}{_dataRows.itemCount === 1 ? ' item' : ' items'}</span>
                            <a className="btn btn-icon-outline circle primary" href={`/admin/user/new`} ><i className="material-icons">add</i></a>
                        </>
                        :
                        <a className="btn btn-icon-outline circle ml-auto hl-blue" href={`/admin/user/new`} ><i className="material-icons">add</i></a>
                    }
                </div>
            </div>

            <div className="row" >
                <div ref={_scrollToRef} className="col-sm-12 mb-4" >
                    <table className="flex-grid w-100" >
                        {renderItemsGridHeader()}
                        {renderItemsGrid()}
                    </table>
                    {renderPagination()}
                </div>
            </div>
            {renderDeleteConfirmation()}
            {renderErrorMessage()}
        </>
    )
}

export default AdminUserList;