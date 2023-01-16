import { useState, useEffect } from 'react';
import axiosInstance from "../../services/AxiosService";

import { AppSettings } from '../../utils/appsettings'
import { generateLogMessageString } from '../../utils/UtilityService'
import { useLoadingContext } from "../../components/contexts/LoadingContext";
import ConfirmationModal from '../../components/ConfirmationModal';
import { ErrorModal } from '../../services/CommonUtil';

const CLASS_NAME = "CloudLibraryImporterNew";

// Component that handles import FROM Cloud Library calls.
// Call API, bubble up messages
// trigger will be list of items changes
export function CloudLibraryImporterNew(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_importConfirmModal, setImportConfirmModal] = useState({ show: false, items: null });
    const [_error, setError] = useState({ show: false, message: null, caption: null });


    //-------------------------------------------------------------------
    // useEffect - if list of items is changed, then we update local state
    //-------------------------------------------------------------------
    useEffect(() => {

        //if we have items to import, call the import API
        if (props.items?.length > 0) {
            console.log(generateLogMessageString(`useEffect | onImportItemsChanged`, CLASS_NAME));
            //if called from cloud library viewer page, we directly initiate import
            //otherwise, we show confirmation first
            if (!props.bypassConfirmation) {
                setImportConfirmModal({ show: true, items: props.items });
            }
            else {
                console.log(generateLogMessageString(`useEffect | bypassConfirmation | importItems`, CLASS_NAME));
                importItems(props.items);
            }
        }

    }, [props.items, props.bypassConfirmation]);

    //-------------------------------------------------------------------
    // Region: Execute the import
    //-------------------------------------------------------------------
    const importItems = async (items) => {
        console.log(generateLogMessageString(`importItems||Count:${items.length}`, CLASS_NAME));

        setLoadingProps({
            isLoading: true, message: `Importing from Cloud Library...This may take a few minutes.`
        });

        //perform import call
        const url = `profile/cloudlibrary/import`;
        console.log(generateLogMessageString(`importFromCloudLibary||${url}`, CLASS_NAME));

        const data = //items.length === 1 ? { id: items[0].id.toString() } :
            items.map((item) => { return { id: item.cloudLibraryId.toString() }; });

        //var data = { id: props.item.cloudLibraryId };

        //show a processing message at top. One to stay for duration, one to show for timed period.
        //var msgImportProcessingId = new Date().getTime();
        setLoadingProps({
            isLoading: true, message: `Importing from Cloud Library...This may take a few minutes.`
        });

        await axiosInstance.post(url, data).then(result => {
            if (result.status === 200) {
                //check for success message OR check if some validation failed
                //remove processing message, show a result message
                //inline for isSuccess, pop-up for error
                var revisedMessages = null;
                if (result.data.isSuccess) {
                }
                else {
                    setError({ show: true, caption: 'Import Error', message: `An error occurred processing the import: ${result.data.message}` });
                }

                //asynch flow - trigger the component we use to show import messages, importing items changing is the trigger
                //update spinner, messages
                var importingLogs = loadingProps.importingLogs == null || loadingProps.importingLogs.length === 0 ? [] :
                    JSON.parse(JSON.stringify(loadingProps.importingLogs));
                const importLogId = result.data.data;
                importingLogs.push({ id: importLogId, status: AppSettings.ImportLogStatus.InProgress, message: null });
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: revisedMessages,
                    importingLogs: importingLogs,
                    activateImportLog: true,
                    isImporting: false
                });

                //parent component should handle this.
                //setSelectedCloudProfiles([]);
                //setSelectedCloudProfileIds([]);

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
        })

    };

    //-------------------------------------------------------------------
    // Region: event handlers
    //-------------------------------------------------------------------
    const onErrorModalClose = () => {
        //console.log(generateLogMessageString(`onErrorMessageOK`, CLASS_NAME));
        setError({ show: false, caption: null, message: null });
    }

    //on confirm click within the modal, this callback will then trigger the next step (ie call the API)
    const onImportConfirm = async () => {
        console.log(generateLogMessageString(`onImportConfirm`, CLASS_NAME));
        await importItems(_importConfirmModal.items);
        setImportConfirmModal({ show: false, items: null });
    };

    const onImportCancel = () => {
        console.log(generateLogMessageString(`onImportCancel`, CLASS_NAME));
        setImportConfirmModal({ show: false, items: null });
        if (props.onImportCancel) props.onImportCancel();
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderImportConfirmation = () => {

        if (!_importConfirmModal.show) return;
        if (_importConfirmModal.items.length == 0) {
            setImportConfirmModal({ show: false, items: null });
            return;
        }

        const message = _importConfirmModal.items.length === 1 ?
            `You are about to import profile '${_importConfirmModal.items[0].title}' and its dependent profiles. Are you sure?` :
            `You are about to import the following ${_importConfirmModal.items.length} profiles and their dependent profiles: '${_importConfirmModal.items.map(i => i.title).join("', '")}'. Are you sure?`;
        var caption = `Import Profile${_importConfirmModal.items.length === 1 ? "" : "s"}`;

        return (
            <>
                <ConfirmationModal showModal={_importConfirmModal.show} caption={caption} message={message}
                    /*icon={{ name: "warning", color: color.trinidad }}*/
                    confirm={{ caption: "Import", callback: onImportConfirm, buttonVariant: "primary" }}
                    cancel={{
                        caption: "Cancel",
                        callback: () => {
                            console.log(generateLogMessageString(`onImportCancel`, CLASS_NAME));
                            setImportConfirmModal({ show: false, items: null });
                            onImportCancel();
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
            {renderImportConfirmation()}
            <ErrorModal modalData={_error} callback={onErrorModalClose} />
        </>
    );
};