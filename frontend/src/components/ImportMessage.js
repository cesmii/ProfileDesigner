import React, { useState, useEffect } from 'react'
import { useLoadingContext } from "./contexts/LoadingContext";
import axiosInstance from "../services/AxiosService";

import { generateLogMessageString } from "../utils/UtilityService";
import Button from 'react-bootstrap/Button'
import { LoadingIcon } from "./SVGIcon";
import { AppSettings } from '../utils/appsettings';

const CLASS_NAME = "ImportMessage";

//-------------------------------------------------------------------
// use Hook: delete an import message and trigger a refresh of the display
//  trigger from some other component to kick off an import log refresh and start tracking import status
//-------------------------------------------------------------------
export function useDeleteImportMessage(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { loadingProps, setLoadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Region: hooks
    //  trigger a delete when the id passed in changes
    //-------------------------------------------------------------------
    useEffect(() => {

        if (props.id == null) return;

        console.log(generateLogMessageString(`deleteImportMessage || ${props.id}`, CLASS_NAME));

        //clear import message - local
        var importingLogs = loadingProps.importingLogs == null || loadingProps.importingLogs.length === 0 ? [] :
            JSON.parse(JSON.stringify(loadingProps.importingLogs));
        importingLogs = importingLogs.filter(x => x.id !== props.id);
        setLoadingProps({ importingLogs: importingLogs });

        //perform delete call - api
        var data = { id: props.id };
        var url = `importlog/delete`;
        axiosInstance.post(url, data)
            .then(result => {

                if (result.data.isSuccess) {
                    setLoadingProps({ activateImportLog: true });
                }
                else {
                    console.log(generateLogMessageString(`deleteImportMessage||error||${result.data.message}`, CLASS_NAME, 'error'));
                }
            })
            .catch(error => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: `An error occurred dismissing this message.`, isTimed: true }
                    ]
                });
                console.log(generateLogMessageString('deleteImportMessage||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
                console.log(error);
                //scroll back to top
                window.scroll({
                    top: 0,
                    left: 0,
                    behavior: 'smooth',
                });
            });

    }, [props.id]);

    //nothing to render
    return null;
};



function ImportMessage() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_dataRows, setDataRows] = useState(null);
    const [_forceReload, setForceReload] = useState(0); //increment this value to cause a re-get of the latest data.
    const [_processingCss, setProcessingCss] = useState(""); //used to create animation effect
    const [_deleteId, setDeleteId] = useState(null); 

    //-------------------------------------------------------------------
    // Region: hooks
    //  trigger from some other component to kick off an import log refresh and start tracking import status
    //-------------------------------------------------------------------
    useEffect(() => {

        if (loadingProps.activateImportLog === true) {
            console.log(generateLogMessageString('useEffect||activateImportLog||Trigger fetch', CLASS_NAME));
            setForceReload(_forceReload + 1);
            //once it is activated, it will then know when to stop itself.
            setLoadingProps({ activateImportLog: null });
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||importingLogs||Cleanup', CLASS_NAME));
            //setFilterValOnChild('');
        };
        //type passed so that any change to this triggers useEffect to be called again
        //_nodesetPreferences.pageSize - needs to be passed so that useEffects dependency warning is avoided.
    }, [loadingProps.activateImportLog]);

    //-------------------------------------------------------------------
    // Region: hooks
    //  track progress of import by periodically querying the API for updated status
    //-------------------------------------------------------------------
    useEffect(() => {

        async function fetchImportLogData() {
            const data = { Query: null, Skip: 0, Take: 999999 };
            //var data = { id: _importLogIdCurrent };
            const url = `importlog/mine`;
            console.log(generateLogMessageString(`useEffect||fetchImportLogData||${url}`, CLASS_NAME));

            await axiosInstance.post(url, data).then(result => {
                if (result.status === 200) {

                    //find num not completed. If any not completed, kick off timer to re-check again in 6 seconds
                    const numIncomplete = result.data.data.filter((x) => { return x.completed == null; });
                    if (numIncomplete.length > 0) {
                        setTimeout(() => {
                            setForceReload(_forceReload + 1);
                        }, 6000);
                        console.log(generateLogMessageString(`useEffect||fetchImportLogData||${numIncomplete.length} imports in progress.`, CLASS_NAME));
                        setProcessingCss("spin");
                    }
                    else {
                        console.log(generateLogMessageString(`useEffect||fetchImportLogData||All imports complete`, CLASS_NAME));
                        setProcessingCss("");
                        //update lookup data and search criteria on complete.
                        setLoadingProps({
                            refreshProfileList: true,
                            refreshLookupData: true,
                            refreshSearchCriteria: true
                        });
                    }

                    setDataRows(result.data.data);

                    //compare a new import log list to previous list. If it has changed, then trigger a profile list refresh
                    //var importingLogs = numIncomplete.length === 0 ? [] : numIncomplete.map((x) => { return { id: x.id, status: x.status, message: x.message }; });
                    //keep completed bu failed around so other components can use this info for their needs, only get most recent msg
                    const importingLogs = result.data.data
                        .map((x) => { return { id: x.id, status: x.status, message: x.messages != null && x.messages.length > 0 ? x.messages[0].message : null }; })
                        .filter((x) => { return x.completed == null || x.status !== AppSettings.ImportLogStatus.Completed; });

                    var diff1 = loadingProps.importingLogs.filter(x => !importingLogs.includes(x));
                    var diff2 = importingLogs.filter(x => !loadingProps.importingLogs.includes(x));

                    //update list of in progress item ids. Set refresh trigger, update lookup data to get latest data types
                    setLoadingProps({
                        importingLogs: importingLogs,
                        //refreshProfileList: (diff1.length > 0 || diff2.length > 0),
                        //refreshLookupData: (diff1.length > 0 || diff2.length > 0) ? true : loadingProps.refreshLookupData,
                        //refreshSearchCriteria: (diff1.length > 0 || diff2.length > 0) ? true : loadingProps.refreshSearchCriteria
                    });

                } else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving import status.', isTimed: true }]
                    });
                }

            }).catch(e => {
                if ((e.response && e.response.status === 401) || e.toString().indexOf('Network Error') > -1) {
                    //do nothing, this is handled in routes.js using common interceptor
                    //setAuthTicket(null); //the call of this will clear the current user and the token
                }
                else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving import status.', isTimed: true }]
                    });
                }
            });
        }

        if (_forceReload > 0) {
            fetchImportLogData();
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||fetchImportLogData||Cleanup', CLASS_NAME));
        };
    }, [_forceReload]);

    //-------------------------------------------------------------------
    // Region: Events
    //-------------------------------------------------------------------
    const dismissMessage = (msgId, warnOnNotFound = false) => {
        setDeleteId(msgId);
    }

    const onDismiss = (e) => {
        console.log(generateLogMessageString('onDismiss||', CLASS_NAME));
        const id = e.currentTarget.getAttribute("data-id");
        dismissMessage(id);
    }


    useDeleteImportMessage({id:_deleteId});

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const getSeverity = (msg) => {
        switch (msg.status) {
            case AppSettings.ImportLogStatus.Failed:
            case AppSettings.ImportLogStatus.Cancelled:
                return "danger";
            case AppSettings.ImportLogStatus.Completed:
                return "success";
            case AppSettings.ImportLogStatus.InProgress:
            default:
                return "info-custom";
        }
    };

    const getMessage = (msg) => {
        //messages sorted by date descending
        let msgAppend = '';
        if (msg.messages != null && msg.messages.length > 0) {
            msgAppend = msg.messages[0].message;
        }
        else {
        }


        switch (msg.status) {
            case AppSettings.ImportLogStatus.Failed:
                return `The import failed. ${msgAppend}`;
            case AppSettings.ImportLogStatus.Cancelled:
                return `The import was cancelled. ${msgAppend}`;
            case AppSettings.ImportLogStatus.Completed:
                return `The import completed. ${msgAppend}`;
            case AppSettings.ImportLogStatus.InProgress:
            default:
                return `The import is processing. ${msgAppend}`;
        }
    };

    const renderMessage = (msg) => {
        //apply special handling for sev="processing"
        const isProcessing = msg.status === AppSettings.ImportLogStatus.InProgress;
        const sev = getSeverity(msg);
        const caption = getMessage(msg);

        return (
            <div key={`inline-msg-${msg.id}`} className="row mb-1" >
                <div className={"col-sm-12 alert alert-" + sev + ""} >
                    {!isProcessing &&
                        <div className="dismiss-btn">
                            <Button id={`btn-inline-msg-dismiss-${msg.id}`} variant="icon-solo square small" data-id={msg.id} onClick={onDismiss} className="align-items-center" ><i className="material-icons">close</i></Button>
                        </div>
                    }
                    <div className="text-center" >
                        {isProcessing &&
                            <span className={`processing ${_processingCss}`} >
                                <LoadingIcon size="20" />
                            </span>
                        }
                        <span className={isProcessing ? 'ml-1' : ''} dangerouslySetInnerHTML={{ __html: caption }} />
                    </div>
                </div>
            </div >
        )    };

    //TBD - check for dup messages and don't show.
    const renderMessages = _dataRows?.map((msg) => {
        //if (msg.completed == null)
        //    dismissMessageTimed(msg.id);  //dismiss the message on a timed basis
        return (renderMessage(msg));
    });

    //-------------------------------------------------------------------
    // Region: Render 
    //-------------------------------------------------------------------
    if (_dataRows == null || _dataRows.length === 0) return null;

    return (
        renderMessages
    );
}

export { ImportMessage };

