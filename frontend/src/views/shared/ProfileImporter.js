import React, { useState } from 'react'
import axiosInstance from "../../services/AxiosService";

import { useLoadingContext } from '../../components/contexts/LoadingContext';
import { generateLogMessageString } from '../../utils/UtilityService'
import { ErrorModal } from '../../services/CommonUtil';
import { AppSettings } from '../../utils/appsettings';

const CLASS_NAME = "ProfileImporter";

function ProfileImporter(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { loadingProps, setLoadingProps } = useLoadingContext();
    //importer
    const [_fileSelection, setFileSelection] = useState('');
    const [_error, setError] = useState({ show: false, message: null, caption: null });

    //-------------------------------------------------------------------
    // Region: Event handlers
    //-------------------------------------------------------------------
    const onImportClick = (e) => {
        console.log(generateLogMessageString(`onImportClick`, CLASS_NAME));

        //if (loadingProps.isImporting) return;

        //console.log(generateLogMessageString(`onProfileLibraryFileChange`, CLASS_NAME));

        let files = e.target.files;
        let readers = [];
        if (!files.length) return;

        for (let i = 0; i < files.length; i++) {
            readers.push(readFileAsText(files[i]));
        }

        Promise.all(readers).then((values) => {
            //console.log(values);
            importFiles(values);
        });
    };

    const onErrorModalClose = () => {
        //console.log(generateLogMessageString(`onErrorMessageOK`, CLASS_NAME));
        setError({ show: false, caption: null, message: null });
    }

    //-------------------------------------------------------------------
    // Region: Importing Code
    //-------------------------------------------------------------------
    //-------------------------------------------------------------------
    // Save nodeset
    const importFiles = async (items) => {

        var url = `profile/import`;
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

    //this will always force the file selector to trigger event after selection.
    //it wasn't firing if selection was same between 2 instances
    const resetFileSelection = () => {
        console.log(generateLogMessageString(`resetFileSelection`, CLASS_NAME));
        setFileSelection('');
    }

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
                {caption}
                <input type="file" value={_fileSelection} multiple onClick={resetFileSelection} disabled={props.disabled ? "disabled" : ""} onChange={onImportClick} style={{ display: "none" }} />
            </label>
            <ErrorModal modalData={_error} callback={onErrorModalClose} />
        </>
    )
}

export default ProfileImporter;
