import React, { useState } from 'react'
import axiosInstance from "../../services/AxiosService";

import { useLoadingContext } from '../../components/contexts/LoadingContext';
import { generateLogMessageString, renderMenuIcon } from '../../utils/UtilityService'
import { ErrorModal } from '../../services/CommonUtil';
import { AppSettings } from '../../utils/appsettings';
import { useDeleteImportMessage } from '../../components/ImportMessage';

const CLASS_NAME = "ProfileImporter";

function ProfileImporter(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { loadingProps, setLoadingProps } = useLoadingContext();
    //importer
    const [_fileSelection, setFileSelection] = useState('');
    const [_error, setError] = useState({ show: false, message: null, caption: null });
    const [_cancelImportId, setCancelImportId] = useState(null);
    const _IMPORT_CHUNK_SIZE = 3 * 1024 * 1024; //8mb

    //-------------------------------------------------------------------
    // Region: Event handlers
    //-------------------------------------------------------------------
    const onImportClick = (e) => {
        console.log(generateLogMessageString(`onImportClick`, CLASS_NAME));

        //if (loadingProps.isImporting) return;

        //console.log(generateLogMessageString(`onProfileLibraryFileChange`, CLASS_NAME));

        let files = e.target.files;
        let readersChunked = [];
        if (!files.length) return;

        for (let i = 0; i < files.length; i++) {
            readersChunked.push(prepareFile(files[i]));
        }

        //once the files are completely read/prepped, then kick off import
        Promise.all(readersChunked).then((values) => {
            //console.log(values);
            importStart(values);
        });
    };

    const onErrorModalClose = () => {
        setError({ show: false, caption: null, message: null });
    }

    //-------------------------------------------------------------------
    // Region: Importing Code
    //-------------------------------------------------------------------
    //-------------------------------------------------------------------
    // Save nodeset - old way
    //-------------------------------------------------------------------
    /*
    const importFiles = async (items) => {

        var url = `profile/import`;
        if (props.caption == "Upgrade Global NodeSet file") {
            url = `profile/importupgrade`;
        }
        //url = `profile/import/slow`; //testing purposes
        console.log(generateLogMessageString(`importFiles||${url}`, CLASS_NAME));

        var msgFiles = "";
        items.forEach(function (f) {
            //msgFiles += msgFiles === "" ? `<br/>File(s) being imported: ${f.fileName}` : `<br/>${f.fileName}`;
            msgFiles += msgFiles === "" ? `File(s) being imported: ${f.fileName}` : `<br/>${f.fileName}`;
        });

        //show a processing message at top. One to stay for duration, one to show for timed period.
        //var msgImportProcessingId = new Date().getTime();
        setLoadingProps({
            isLoading: true, message: `Importing...This may take a few minutes.`
        });

        await axiosInstance.post(url, items).then(result => {
            if (result.status === 200) {
                //check for success message OR check if some validation failed
                //remove processing message, show a result message
                //inline for isSuccess, pop-up for error
                var revisedMessages = null;
                if (result.data.isSuccess) {

                    //synch flow would wait, now we do async so we have to check import log on timer basis. 
                    //    revisedMessages = [{
                    //        id: new Date().getTime(),
                    //        severity: result.data.isSuccess ? "success" : "danger",
                    //        body: `Profiles were imported successfully.`,
                    //        isTimed: result.data.isSuccess
                    //    }];
                }
                else {
                    setError({ show: true, caption: 'Import Error', message: `An error occurred processing the import file(s): ${result.data.message}` });
                }

                //asynch flow - trigger the component we use to show import messages, importing items changing is the trigger
                //update spinner, messages
                var importingLogs = loadingProps.importingLogs == null || loadingProps.importingLogs.length === 0 ? [] :
                    JSON.parse(JSON.stringify(loadingProps.importingLogs));
                importingLogs.push({ id: result.data.data, status: AppSettings.ImportLogStatus.InProgress, message: null });
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: revisedMessages,
                    importingLogs: importingLogs,
                    activateImportLog: true,
                    isImporting: false
                });

                //bubble up to parent to let them know the import log id associated with this import. 
                //then they can track how this specific import is doing in terms of completed or not
                if (props.onImportStarted) props.onImportStarted(result.data.data);

            } else {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, isImporting: false
                    //, inlineMessages: [{ id: new Date().getTime(), severity: "danger", body: `An error occurred processing the import file(s).`, isTimed: false, isImporting: false }]
                });
                setError({ show: true, caption: 'Import Error', message: `An error occurred processing the import file(s)` });
            }
        }).catch(e => {
            if (e.response && e.response.status === 401) {
                setLoadingProps({ isLoading: false, message: null, isImporting: false });
            }
            else {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, isImporting: false
                    //,inlineMessages: [{ id: new Date().getTime(), severity: "danger", body: e.response.data ? e.response.data : `An error occurred saving the imported profile.`, isTimed: false, isImporting: false }]
                });
                setError({ show: true, caption: 'Import Error', message: e.response && e.response.data ? e.response.data : `A system error has occurred during the profile import. Please contact your system administrator.` });
                console.log(generateLogMessageString('handleOnSave||saveFile||' + JSON.stringify(e), CLASS_NAME, 'error'));
                console.log(e);
            }
        });
    }
    */

    //-------------------------------------------------------------------
    // Multi-step import - step 1 of 3
    //-------------------------------------------------------------------
    const importStart = async (items) => {

        const url = `importlog/init`;
        console.log(generateLogMessageString(`initImport||${url}`, CLASS_NAME));

        let msgFiles = "";
        items.forEach(function (f) {
            msgFiles += msgFiles === "" ? `File(s) being imported: ${f.fileName}` : `<br/>${f.fileName}`;
        });

        //map items to a collection with out the data so we can send up streamlined init data
        let itemsNoContent = items.map(function (f) {
            return {
                fileName: f.fileName,
                totalBytes: f.totalBytes,
                totalChunks: f.totalChunks
            };
        });

        //show a processing message at top. One to stay for duration, one to show for timed period.
        //var msgImportProcessingId = new Date().getTime();
        setLoadingProps({
            isLoading: true, message: `Starting upload of files to server...This may take a few minutes.`
        });

        await axiosInstance.post(url, itemsNoContent).then(result => {
            if (result.status === 200) {
                //check for success message OR check if some validation failed
                //remove processing message, show a result message
                //inline for isSuccess, pop-up for error
                var revisedMessages = null;
                if (result.data.id) {
                    //trigger the 2nd step of the import if we get success on part 1, pass import id
                    importUploadFiles(result.data, items);
                }
                else {
                    setError({ show: true, caption: 'Import Error', message: `An error occurred processing the import file(s): ${result.data.message}` });
                }

                //asynch flow - trigger the component we use to show import messages, importing items changing is the trigger
                //update spinner, messages
                var importingLogs = loadingProps.importingLogs == null || loadingProps.importingLogs.length === 0 ? [] :
                    JSON.parse(JSON.stringify(loadingProps.importingLogs));
                importingLogs.push({ id: result.data.id, status: AppSettings.ImportLogStatus.InProgress, message: null });
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: revisedMessages,
                    importingLogs: importingLogs,
                    activateImportLog: true,
                    isImporting: false
                });

                //bubble up to parent to let them know the import log id associated with this import. 
                //then they can track how this specific import is doing in terms of completed or not
                if (props.onImportStarted) props.onImportStarted(result.data.id);

            } else {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, isImporting: false
                    //, inlineMessages: [{ id: new Date().getTime(), severity: "danger", body: `An error occurred processing the import file(s).`, isTimed: false, isImporting: false }]
                });
                setError({ show: true, caption: 'Import Error', message: `An error occurred processing the import file(s)` });
            }
        }).catch(e => {
            if (e.response && e.response.status === 401) {
                setLoadingProps({ isLoading: false, message: null, isImporting: false });
            }
            else {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, isImporting: false
                    //,inlineMessages: [{ id: new Date().getTime(), severity: "danger", body: e.response.data ? e.response.data : `An error occurred saving the imported profile.`, isTimed: false, isImporting: false }]
                });
                setError({ show: true, caption: 'Import Error', message: e.response && e.response.data ? e.response.data : `A system error has occurred during the profile import. Please contact your system administrator.` });
                console.log(generateLogMessageString('importStart||' + JSON.stringify(e), CLASS_NAME, 'error'));
                console.log(e);
            }
        });
    }

    //-------------------------------------------------------------------
    // Multi-step import - step 2 of 3
    //-------------------------------------------------------------------
    const importUploadFiles = async (importItem, items) => {

        let url = `importlog/uploadfiles`;
        console.log(generateLogMessageString(`importUploadFiles||${url}`, CLASS_NAME));

        let errorCount = 0;
        let errorMessage;

        //collection of promises which represent each upload action for each chunk of each file
        let chunkCalls = [];

        importItem.files.forEach(function (fileImport) {
            //match up with one of the files we have prepped locally via filename
            let fileSource = items.find(x => { return x.fileName.toLowerCase() === fileImport.fileName.toLowerCase() });
            if (fileSource == null) {
                //throw exception, cancel import
                console.log(generateLogMessageString(`importUploadFiles||fileSource||cannot find ${fileImport.fileName} in fileSource.`, CLASS_NAME, 'critical'));
                if (errorMessage) {
                    setError({ show: true, caption: 'Import Error', message: `An unexpected error occurred pre-processing this import. File not found: ${fileImport.fileName}` });
                }
                setCancelImportId(importItem.id);
            }

            //loop over locally prepared chunks and kick off upload
            fileSource.chunks.forEach(function (c) {

                let chunk = {
                    importActionId: importItem.id,
                    importFileId: fileImport.id,
                    fileName: fileSource.fileName,
                    chunkOrder: c.chunkOrder,
                    contents: c.contents
                };

                //wait for all chunks to complete and then call the process files item
                let url = `importlog/uploadfiles`;
                console.log(generateLogMessageString(`uploadFileChunk||${url}`, CLASS_NAME));

                chunkCalls.push(
                    axiosInstance.post(url, chunk).then(result => {
                        if (result.status === 200) {
                            if (result.data.isSuccess) {
                                //Need to locally keep track of all uploads so we know when to move on to last step
                                chunk.uploadComplete = true;
                            }
                            else {
                                errorCount += 1;
                                errorMessage = errorMessage == null || errorMessage === '' ? '' : `${errorMessage} `;
                                errorMessage += `An error occurred processing the import file '${chunk.fileName}': ${result.data.message}`;
                            }
                        } else {
                            errorCount += 1;
                            errorMessage = errorMessage == null || errorMessage === '' ? '' : `${errorMessage} `;
                            errorMessage += `An error occurred processing the import file '${chunk.fileName}'.`;
                            //hide a spinner, show a message
                            setLoadingProps({
                                isLoading: false, message: null, isImporting: false
                                //, inlineMessages: [{ id: new Date().getTime(), severity: "danger", body: `An error occurred processing the import file(s).`, isTimed: false, isImporting: false }]
                            });
                        }
                    }).catch(e => {
                        errorCount += 1;
                        if (e.response && e.response.status === 401) {
                            setLoadingProps({ isLoading: false, message: null, isImporting: false });
                        }
                        else {
                            //hide a spinner, show a message
                            setLoadingProps({
                                isLoading: false, message: null, isImporting: false
                                //,inlineMessages: [{ id: new Date().getTime(), severity: "danger", body: e.response.data ? e.response.data : `An error occurred saving the imported profile.`, isTimed: false, isImporting: false }]
                            });
                            errorMessage = errorMessage == null || errorMessage === '' ? '' : `${errorMessage} `; 
                            errorMessage += `${e.response?.data ? e.response.data : 'A system error has occurred during the profile import. Please contact your system administrator.' }`;
                            console.log(generateLogMessageString('importUploadFiles||error||' + JSON.stringify(e), CLASS_NAME, 'error'));
                            console.log(e);
                        }
                    })
                );
            });
        });

        //when all chunk calls for all files complete, then we call final step to process uploaded files
        Promise.all(chunkCalls).then(() => {
            console.log(generateLogMessageString('importUploadFiles||promise.all||start process files', CLASS_NAME, 'info'));
            //now perform the processing of the files
            if (errorCount === 0) {
                importProcessFiles(importItem.id);
            }
            else {
                //handle scenario where errors occurred
                //make api call to clean out the uploaded file chunks.
                console.log(generateLogMessageString(`importUploadFiles||promise.all||cannot process file chunks - ${errorCount} error(s) occurred.`, CLASS_NAME, 'warn'));
                if (errorMessage) {
                    setError({ show: true, caption: 'Import Error', message: errorMessage });
                }
                setCancelImportId(importItem.id);
            }
        });
    }

    //-------------------------------------------------------------------
    // Multi-step import - step 3 of 3
    //-------------------------------------------------------------------
    const importProcessFiles = async (id) => {

        var url = `importlog/processfiles`;
        if (props.caption == "Upgrade Global NodeSet file") {
            url = `importlog/admin/processfiles/upgrade`; //only permitted by admin user
        }
        console.log(generateLogMessageString(`processUploadedFiles||${url}`, CLASS_NAME));

        await axiosInstance.post(url, {id: id}).then(result => {
            if (result.status === 200) {
                //check for success message OR check if some validation failed
                //remove processing message, show a result message
                //inline for isSuccess, pop-up for error
                if (result.data.isSuccess) {
                }
                else {
                    setError({ show: true, caption: 'Import Error', message: `An error occurred processing the import file(s): ${result.data.message}` });
                }

                //bubble up to parent to let them know the import log id associated with this import. 
                //then they can track how this specific import is doing in terms of completed or not
                if (props.onImportProcessFiles) props.onImportProcessFiles(id);

            } else {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, isImporting: false
                    //, inlineMessages: [{ id: new Date().getTime(), severity: "danger", body: `An error occurred processing the import file(s).`, isTimed: false, isImporting: false }]
                });
                setError({ show: true, caption: 'Import Error', message: `An error occurred processing the import file(s)` });
            }
        }).catch(e => {
            if (e.response && e.response.status === 401) {
                setLoadingProps({ isLoading: false, message: null, isImporting: false });
            }
            else {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, isImporting: false
                    //,inlineMessages: [{ id: new Date().getTime(), severity: "danger", body: e.response.data ? e.response.data : `An error occurred saving the imported profile.`, isTimed: false, isImporting: false }]
                });
                setError({ show: true, caption: 'Import Error', message: e.response && e.response.data ? e.response.data : `A system error has occurred during the profile import. Please contact your system administrator.` });
                console.log(generateLogMessageString('handleOnSave||saveFile||' + JSON.stringify(e), CLASS_NAME, 'error'));
                console.log(e);
            }
        });
    }

    /* old way
    const readFileAsText = (file) => {
        return new Promise(function (resolve, reject) {
            let fr = new FileReader();

            fr.onload = function () {
                resolve({ fileName: file.name, data: fr.result });
            };

            fr.onerror = function () {
                reject(fr);
            };

            fr.readAsText(file);
        });
    }
    */

    const prepareFile = (file) => {
        return new Promise(function (resolve, reject) {
            let fr = new FileReader();

            fr.onload = function () {
                resolve(prepareFileChunks(file.name, file.size, fr.result));
            };

            fr.onerror = function () {
                reject(fr);
            };

            fr.readAsText(file);
        });
    }

    const prepareFileChunks = (fileName, size, contents) => {

        let addChunk = true;
        let chunks = [];
        //set first chunk boundary
        let chunkStart = 0;
        let chunkEnd = Math.min(size, chunkStart + _IMPORT_CHUNK_SIZE);
        let counter = 1;

        while (addChunk) {
            //slice current chunk
            const chunk = contents.slice(chunkStart, chunkEnd);
            //const chunk = new Uint8Array(contents.slice(chunkStart, chunkEnd));
            chunks.push({ fileName: fileName, chunkOrder: counter, contents: chunk });
            //determine if we need another chunk
            addChunk = size > (chunkEnd);
            //increment the next chunk boundaries and counter
            chunkStart = chunkEnd;
            chunkEnd = Math.min(size, chunkStart + _IMPORT_CHUNK_SIZE);
            counter++;
        }

        //return fully prepared file
        return {
            fileName: fileName,
            chunks: chunks,
            totalBytes: size,
            totalChunks: chunks.length
        };
    }

    //this will always force the file selector to trigger event after selection.
    //it wasn't firing if selection was same between 2 instances
    const resetFileSelection = () => {
        console.log(generateLogMessageString(`resetFileSelection`, CLASS_NAME));
        setFileSelection('');
    }

    //-------------------------------------------------------------------
    // Region: hook - trigger delete of message on completion automatically
    //-------------------------------------------------------------------
    useDeleteImportMessage({ id: _cancelImportId });

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    let buttonCss = `${props.cssClass} ${props.disabled ? "disabled" : ""}`;
    if (!props.useCssClassOnly) {
        buttonCss = `btn btn-secondary auto-width ${buttonCss}`;
    }
    var caption = props.caption == null ? "Import" : props.caption;
    
    return (
        <>
            <label className={buttonCss}>
                {renderMenuIcon(props.iconName)}{caption}
                <input type="file" value={_fileSelection} multiple onClick={resetFileSelection} disabled={props.disabled ? "disabled" : ""} onChange={onImportClick} style={{ display: "none" }} />
            </label>
            <ErrorModal modalData={_error} callback={onErrorModalClose} />
        </>
    )
}

export default ProfileImporter;
