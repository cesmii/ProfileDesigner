import React, { useEffect } from 'react'
import Button from 'react-bootstrap/Button'
import axiosInstance from "../services/AxiosService";

import { useLoadingContext } from "./contexts/LoadingContext";
import { generateLogMessageString } from '../utils/UtilityService'
import { LoadingIcon } from "./SVGIcon";

const CLASS_NAME = "DownloadMessage";

function DownloadMessage() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    //Note: this is information about items being downloaded. This will be populated at download api call
    const { loadingProps, setLoadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Region: Launch export - execute
    //-------------------------------------------------------------------
    useEffect(() => {
        async function downloadProfile(item) {
            const url = `profile/export`;
            console.log(generateLogMessageString(`downloadProfile||${url}`));

            const data = { id: item.profileId, format: item.downloadFormat };
            await axiosInstance.post(url, data).then(async result => {
                console.log(generateLogMessageString(`downloadProfile||${result.data.isSuccess ? 'success' : 'fail'}`));
                if (result.status === 200 && result.data.isSuccess) {
                    //check for success message OR check if some validation failed

                    //if success show w/ link for download
                    //update item status to show success
                    var extension = "xml";
                    if (item.downloadFormat === "AASX") {
                        extension = "aasx";
                    }
                    else if (item.downloadFormat === "SmipJson") {
                        extension = "json";
                    }
                    item = {...item, 
                        show: true,
                        statusName: result.data.warnings == null || result.data.warnings.length === 0 ? 'completed' : "warning",
                        message: result.data.message,
                        data: result.data.data,
                        download: `${item.fileName}.${extension}`,
                        caption: `${item.fileName}.${extension}`,
                        warnings: result.data.warnings
                    };

                    //FIX - because some nodesets are large, they cause us to exceed a max storage limit in local storage. 
                    //  so, open the file immediately and then don't save item w/ file contents
                    //  below the state will be updated w/o the large file so user will see a confirmation of the completion
                    openFile(item);
                    item.data = null;
                }
                else {
                    item = {
                        ...item,
                        show: true,
                        statusName: 'failed',
                        message: result.data.message,
                        blob: null,
                        download: null,
                        warnings: null,
                        caption: null
                    };
                    console.log(generateLogMessageString('downloadProfile||error||' + result.data.message, CLASS_NAME, 'error'));
                }

                //update state
                var x = loadingProps.downloadItems.findIndex(msg => { return msg.id.toString() === item.id.toString(); });
                //no item found
                if (x < 0) {
                    console.warn(generateLogMessageString(`downloadProfile||no item found to dismiss with this id: ${item.id}`, CLASS_NAME));
                    return;
                }
                loadingProps.downloadItems[x] = JSON.parse(JSON.stringify(item));
                setLoadingProps({ downloadItems: JSON.parse(JSON.stringify(loadingProps.downloadItems)) });

            }).catch(e => {
                item = {
                    ...item,
                    show: true,
                    statusName: 'failed',
                    message: 'An error occurred downloading this profile.',
                    blob: null,
                    download: null,
                    caption: '',
                    warnings: null
                };
                console.log(generateLogMessageString('downloadProfile||error||' + JSON.stringify(e), CLASS_NAME, 'error'));
                //update state
                setLoadingProps({ downloadItems: JSON.parse(JSON.stringify(loadingProps.downloadItems)) });
            })
        } //end downloadProfile

        //-------------------------------------------------------------------------------
        //kick off download - if any messages are in the queue that are not started
        //in most cases, this will be one item in queue
        //-------------------------------------------------------------------------------
        if (loadingProps.downloadItems != null && loadingProps.downloadItems.length > 0) {

            const itemsNotStarted = loadingProps.downloadItems.filter((x) => { return x.statusName == null; });

            //only export the new ones
            if (itemsNotStarted.length > 0) {
                //update state of not started items so we can see the progress
                loadingProps.downloadItems.forEach((x) => {
                    if (x.statusName == null) {
                        x.id = new Date().getTime();
                        x.statusName = "inprogress";
                        return;
                    }
                });
                setLoadingProps({ downloadItems: JSON.parse(JSON.stringify(loadingProps.downloadItems)) });

                //now loop over items tagged as not started (after we set state) and kick off export for each
                itemsNotStarted.forEach((item) => {
                    downloadProfile(item);
                });
            }
        }
    }, [loadingProps.downloadItems]);

    //-------------------------------------------------------------------
    // Region: event
    //-------------------------------------------------------------------
    const dismissMessage = (msgId) => {
        var x = loadingProps.downloadItems.findIndex(msg => { return msg.id.toString() === msgId.toString(); });
        //no item found
        if (x < 0) {
            console.warn(generateLogMessageString(`dismissMessage||no item found to dismiss with this id: ${msgId}`, CLASS_NAME));
            return;
        }

        //delete the message locally
        loadingProps.downloadItems.splice(x, 1);

        //update state
        setLoadingProps({ downloadItems: JSON.parse(JSON.stringify(loadingProps.downloadItems)) });
    }

    const onDismiss = (e) => {
        console.log(generateLogMessageString('onDismiss||', CLASS_NAME));
        const id = e.currentTarget.getAttribute("data-id");
        dismissMessage(id);
    }

    const openFile = (msg) => {
        console.log(generateLogMessageString(`openFile`, CLASS_NAME));
        var blobType = 'application/xml';
        var blobData = msg.data;
        if (msg.downloadFormat === "AASX") {
            blobType = 'application/octet-stream';
            blobData = Buffer.from(msg.data, "base64");
        }
        else if (msg.downloadFormat === "SmipJson") {
            blobType = 'application/json';
        }

        const blob = new Blob([blobData], { type: blobType });
        const link = document.createElement('a');
        link.href = URL.createObjectURL(blob);
        link.download = msg.download;
        document.body.appendChild(link);
        link.click();
        document.body.removeChild(link);
    }

    const onOpenFile = (e) => {
        console.log(generateLogMessageString(`onOpenFile||start`, CLASS_NAME));

        e.preventDefault();

        var id = e.currentTarget.getAttribute("data-id");
        var x = loadingProps.downloadItems.findIndex(msg => { return msg.id.toString() === id.toString(); });
        //no item found
        if (x < 0) {
            console.warn(generateLogMessageString(`onOpenFile||no item found to open with this id: ${id}`, CLASS_NAME));
            return;
        }

        openFile(loadingProps.downloadItems[x]);
    }

    const onToggleWarnings = (e) => {
        var id = e.currentTarget.getAttribute("data-id");
        var x = loadingProps.downloadItems.findIndex(msg => { return msg.id.toString() === id.toString(); });
        //no item found
        if (x < 0) {
            console.warn(generateLogMessageString(`onToggleWarnings||no item found to toggle this id: ${id}`, CLASS_NAME));
            return;
        }

        loadingProps.downloadItems[x].showWarnings = !loadingProps.downloadItems[x].showWarnings;

        //update state
        setLoadingProps({ downloadItems: JSON.parse(JSON.stringify(loadingProps.downloadItems)) });
    };
    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderFileLink = (msg) => {
        if (msg.data == null) return;

        return (
            <>
                Click 
                <button type="button" className="btn btn-link d-inline mx-1" data-id={msg.id} onClick={onOpenFile} >{msg.caption}</button>
                to open file.
            </>
        );
    };

    const getSeverity = (msg) => {
        switch (msg.statusName?.toLowerCase()) {
            case "failed":
            case "cancelled":
                return "danger";
            case "warning":
                return "warning";
            case "completed":
                return "success";
            case "inprogress":
            default:
                return "info-custom";
        }
    };

    const getMessage = (msg) => {
        switch (msg.statusName?.toLowerCase()) {
            case "failed":
                return `${msg.fileName} download failed. ${msg.message}`;
            case "cancelled":
                return `${msg.fileName} download was cancelled. ${msg.message}`;
            case "completed":
                return `${msg.fileName} download completed. `;
            case "warning":
                return `${msg.fileName} download completed with warnings. `;
            case "inprogress":
            default:
                return `${msg.fileName} download is in progress...`;
        }
    };

    const renderWarnings = (msg) => {
        if (msg == null || msg.warnings == null || msg.warnings.length === 0) return;

        const warnings = msg.warnings.map((w, i) => {
            return (
                <li key={`warning-${i}`}>{w}</li>
            );
        });

        return (
            <div className="mt-2" >
                <div className="d-flex align-content-center" >
                    <Button variant="" data-id={msg.id} onClick={onToggleWarnings} className="btn-link p-0 d-flex" title={msg.showWarnings ? "Hide warnings" : "Show warnings"} >
                        <span className="d-inline" >{msg.showWarnings ? "Hide warnings" : "Show warnings"}</span>
                        <span key="toggle" className="ml-2">
                                <i className="material-icons">{msg.showWarnings ? "expand_less" : "expand_more"}</i>
                        </span>
                    </Button>
                </div>
                <ul className={`section-items m-0 px-3 ${msg.showWarnings ? "" : "d-none"}`} style={{ maxHeight: '300px', overflowY: 'auto'}} >
                    {warnings}
                </ul>
            </div>
        );
    };

    const renderMessage = (msg) => {
        //apply special handling for sev="processing"
        var isProcessing = msg.statusName?.toLowerCase() === "inprogress";
        var sev = getSeverity(msg);
        var caption = getMessage(msg);

        return (
            <div key={`inline-msg-${msg.id}`} className="row mb-1" >
                <div className={"col-sm-12 alert alert-" + sev + ""} >
                    <div className="dismiss-btn">
                        <Button id={`btn-inline-msg-dismiss-${msg.id}`} variant="icon-solo square small" data-id={msg.id} onClick={onDismiss} className="align-items-center" ><i className="material-icons">close</i></Button>
                    </div>
                    <div className="d-flex align-items-center" >
                        {isProcessing &&
                            <span className={`processing spin`} >
                                <LoadingIcon size="20" />
                            </span>
                        }
                        <span className={isProcessing ? 'ml-1' : ''} dangerouslySetInnerHTML={{ __html: caption }} />
                        {renderFileLink(msg)}
                    </div>
                    {renderWarnings(msg)}
                </div>
            </div >
        )
    };

    const renderMessages = loadingProps.downloadItems?.map((msg) => {
        msg.id = msg.id == null ? (-1) * (new Date().getTime()) : msg.id;  //put this in for uniqueness before the useEffect sets it. 
        return (renderMessage(msg));
    });

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    if (loadingProps.downloadItems == null || loadingProps.downloadItems.length === 0) return null;

    //return final ui
    return (renderMessages);
}

export default DownloadMessage;
