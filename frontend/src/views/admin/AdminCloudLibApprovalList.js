import React, { useState, useEffect, useRef } from 'react'
import { Helmet } from "react-helmet"
import { axiosInstance } from "../../services/AxiosService";
import { useMsal } from "@azure/msal-react";

import ConfirmationModal from "../../components/ConfirmationModal";

import { AppSettings } from '../../utils/appsettings'
import { generateLogMessageString } from '../../utils/UtilityService'
import GridPager from '../../components/GridPager'
import { useLoadingContext } from "../../components/contexts/LoadingContext";

import HeaderSearch from '../../components/HeaderSearch';
import AdminCloudLibApprovalRow from './AdminCloudLibApprovalRow';
import AdminCloudLibApprovalModal from './AdminCloudLibApprovalModal';
import color from '../../components/Constants';

const CLASS_NAME = "AdminCloudLibApprovalList";

function AdminCloudLibApprovalList() {

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
    const [_refreshData, setRefreshData] = useState(0);
    const [_changeApprovalModal, setChangeApprovalModal] = useState({ show: false, item: null });
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

            var url = `cloudlibrary/pendingapprovals`;
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
    }, [_pager, _refreshData]);

    //-------------------------------------------------------------------
    // Region: Event Handling - delete item
    //-------------------------------------------------------------------
    const onChangeApprovalStatus = (item) => {
        console.log(generateLogMessageString('onChangeApprovalStatus', CLASS_NAME));
        setChangeApprovalModal({ show: true, item: item });
    };

    const onChangeApprovalConfirm = (approvalData) => {
        console.log(generateLogMessageString('onChangeApprovalConfirm', CLASS_NAME));

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform delete call
        var data = { id: _changeApprovalModal.item.cloudLibraryId, approvalStatus: approvalData.approvalStatus, approvalDescription: approvalData.description };
        var url = `cloudlibrary/approve`;
        axiosInstance.post(url, data)  //api allows one or many
            .then(result => {

                if (result.data != null && result.data.cloudLibraryId == _changeApprovalModal.item.cloudLibraryId) {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            {
                                id: new Date().getTime(), severity: "success", body: `Approval status was updated`, isTimed: true
                            }
                        ],
                    });

                    setChangeApprovalModal({ show: false, item: null });
                }
                else {
                    //update spinner, messages
                    setError({ show: true, caption: 'Delete Item Error', message: `An error occurred updating approval status${result.data.message}` });
                    setLoadingProps({ isLoading: false, message: null });
                    setChangeApprovalModal({ show: false, item: null });
                }
                setRefreshData(_refreshData + 1);
            })
            .catch(error => {
                //hide a spinner, show a message
                setError({ show: true, caption: 'Update approval status Error', message: `An error occurred updating the approval status.` });
                setLoadingProps({ isLoading: false, message: null });

                console.log(generateLogMessageString('changeApprovalStatus||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
                console.log(error);
                //scroll back to top
                window.scroll({
                    top: 0,
                    left: 0,
                    behavior: 'smooth',
                });
                setChangeApprovalModal({ show: false, item: null });
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
                <AdminCloudLibApprovalRow key="header" item={null} isHeader={true} cssClass="admin-item-row" />
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
                <AdminCloudLibApprovalRow key={item.id} item={item} cssClass={`admin-item-row`} onChangeApprovalStatus={onChangeApprovalStatus} />
            );
        });

        return (
            <tbody>
                {mainBody}
            </tbody>
        )
    }

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
    const renderChangeApprovalStatusConfirmation = () => {

        if (!_changeApprovalModal.show) return;

        var message = `You are about change status for '${_changeApprovalModal.item.title}'. This action cannot be undone. Are you sure?`;
        var caption = `Change Approval Status`;

        return (
            <>
                <AdminCloudLibApprovalModal item={_changeApprovalModal.item} showModal={_changeApprovalModal.show} caption={caption} message={message}
                    icon={{ name: "warning", color: color.trinidad }}
                    confirm={{ caption: "Change", callback: onChangeApprovalConfirm, buttonVariant: "danger" }}
                    cancel={{
                        caption: "Cancel",
                        callback: () => {
                            console.log(generateLogMessageString(`onChangeApprovalCancel`, CLASS_NAME));
                            setChangeApprovalModal({ show: false, item: null });
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
                    <h1>Admin | View Pending Approvals for Cloud Library</h1>
                </div>
                <div className="col-sm-4 d-flex align-items-center justify-content-end" >
                    <HeaderSearch showAdvancedSearch={false} filterVal={_pager.searchVal == null ? null : _pager.searchVal} onSearch={handleOnSearchChange} searchMode="standard" activeAccount={_activeAccount} />
                </div>
            </div>

            <div className="row pb-2" >
                <div className="col-sm-12 d-flex align-items-center" >
                    {(_dataRows.itemCount != null && _dataRows.itemCount > 0) && 
                        <>
                            <span className="px-2 ml-auto font-weight-bold">{_dataRows.itemCount}{_dataRows.itemCount === 1 ? ' item' : ' items'}</span>
                        </>
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
            {renderChangeApprovalStatusConfirmation()}
            {renderErrorMessage()}
        </>
    )
}

export default AdminCloudLibApprovalList;