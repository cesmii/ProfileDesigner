import React, { useState } from 'react'
import { Dropdown } from 'react-bootstrap'

import axiosInstance from "../../services/AxiosService";

import { useLoadingContext } from "../../components/contexts/LoadingContext";
import { SVGIcon, SVGDownloadIcon } from '../../components/SVGIcon'
import { isOwner, renderProfileIcon } from './ProfileRenderHelpers';
import { cleanFileName, formatDate, generateLogMessageString } from '../../utils/UtilityService';
import { getProfileCaption } from '../../services/ProfileService'
import { AppSettings } from '../../utils/appsettings';

const CLASS_NAME = "ProfileItemRow";

function ProfileItemRow(props) { //props are item, showActions

    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_error, setError] = useState({ show: false, message: null, caption: null });

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    //const getTypeDefinitionsUrl = () => {
    //    //var val = encodeURIComponent(props.item.namespace);
    //    return (props.item.isReadOnly || props.item.authorId == null || props.currentUserId !== props.item.authorId) ?
    //        `/types/library/p=${props.item.id}` : `/types/mine/p=${props.item.id}`;
    //};

    //const getTypeDefinitionNewUrl = () => {
    //    return `/type/new/p=${props.item.id}`; 
    //};

    const downloadItem = async () => {
        console.log(generateLogMessageString(`downloadItem||start`, CLASS_NAME));
        //add a row to download messages and this will kick off download
        var msgs = loadingProps.downloadItems || [];
        msgs.push({ profileId: props.item.id, fileName: cleanFileName(props.item.namespace), immediateDownload: true });
        setLoadingProps({ downloadItems: JSON.parse(JSON.stringify(msgs)) });
    }
    const importItem = async () => {
        console.log(generateLogMessageString(`importItem||start`, CLASS_NAME));
        var url = `profile/cloudlibrary/import`;
        console.log(generateLogMessageString(`importFromCloudLibary||${url}`, CLASS_NAME));

        var data = { id: props.item.cloudLibraryId };

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



    const downloadItemAsAASX = async () => {
        console.log(generateLogMessageString(`downloadItem||start`, CLASS_NAME));
        //add a row to download messages and this will kick off download
        var msgs = loadingProps.downloadItems || [];
        msgs.push({ profileId: props.item.id, fileName: cleanFileName(props.item.namespace), immediateDownload: true, downloadFormat: AppSettings.ExportFormatEnum.AASX });
        setLoadingProps({ downloadItems: JSON.parse(JSON.stringify(msgs)) });
    }

    const onDeleteItem = () => {
        props.onDeleteCallback(props.item);
    }

    const onEditItem = () => {
        //format date if present
        //props.item.publishDate = formatDate(props.item.publishDate);
        props.onEditCallback(props.item);
    }

    const onImportItem = () => {
        //format date if present
        //props.item.publishDate = formatDate(props.item.publishDate);
        props.onImportCallback(props.item);
    }

    const onRowSelect = () => {
        //only some modes allow selecting row
        if (props.selectMode == null) return;

        //toggle selection and bubble up to parent to update the state
        props.item.selected = !props.item.selected;
        if (props.onRowSelect) props.onRowSelect(props.item);
    }

    const IsRowSelected = (item) => {
        if (props.selectedItems == null) return;
        var x = props.selectedItems.findIndex(p => { return p.toString() === item.id.toString(); });
        return x >= 0;
    }

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderActionsColumn = (item, showActions) => {

        if (!showActions) return;

        if (item.hasLocalProfile != false) {
            //if standard ua nodeset, author is null
            return (
                <div className="col-sm-4 ml-auto d-inline-flex justify-content-end align-items-center" >
                    <span className="my-0 mr-2"><a href={`/types/library/profile/${props.item.id}`} ><span className="mr-1" alt="view"><SVGIcon name="visibility" /></span>View Type Definitions</a></span>
                    <Dropdown className="action-menu icon-dropdown" onClick={(e) => e.stopPropagation()} >
                        <Dropdown.Toggle drop="left" title="Actions" >
                            <SVGIcon name="more-vert" />
                        </Dropdown.Toggle>
                        <Dropdown.Menu>
                            {/*{(props.currentUserId != null && props.currentUserId === item.authorId) &&*/}
                            {/*    <Dropdown.Item key="moreVert2" href={getTypeDefinitionNewUrl()} ><span className="mr-3" alt="extend"><SVGIcon name="extend" /></span>New Type Definition</Dropdown.Item>*/}
                            {/*}*/}
                            {isOwner(props.item, props.activeAccount) &&
                                <Dropdown.Item key="moreVert3" onClick={onDeleteItem} ><span className="mr-3" alt="delete"><SVGIcon name="delete" /></span>Delete Profile</Dropdown.Item>
                            }
                            <Dropdown.Item key="moreVert4" onClick={downloadItem} ><span className="mr-3" alt="arrow-drop-down"><SVGDownloadIcon name="download" /></span>Download Profile</Dropdown.Item>
                            <Dropdown.Item key="moreVert5" onClick={downloadItemAsAASX} ><span className="mr-3" alt="arrow-drop-down"><SVGDownloadIcon name="downloadAASX" /></span>Download Profile as AASX</Dropdown.Item>
                        </Dropdown.Menu>
                    </Dropdown>
                </div>
            );
        }
        else {
            return (
                <div className="col-sm-4 ml-auto d-inline-flex justify-content-end align-items-center" >
                    <button className="ml-1 btn btn-link" onClick={onImportItem} ><span className="mr-1" alt="view"><SVGDownloadIcon name="import" /></span>Import from Cloud Library</button>
                {/*    <Dropdown className="action-menu icon-dropdown" onClick={(e) => e.stopPropagation()} >*/}
                {/*        <Dropdown.Toggle drop="left" title="Actions" >*/}
                {/*            <SVGIcon name="more-vert" />*/}
                {/*        </Dropdown.Toggle>*/}
                {/*        <Dropdown.Menu>*/}
                {/*            <Dropdown.Item key="moreVert4" onClick={importItem} ><span className="mr-3" alt="arrow-drop-down"><SVGIcon name="visibility" /></span>View profile description</Dropdown.Item>*/}
                {/*        </Dropdown.Menu>*/}
                {/*    </Dropdown>*/}
                </div>
            );
        }
    }

    const renderSelectColumn = (item) => {

        var iconSelected = props.selectMode === "single" ? "task_alt" : "check_box";
        var iconUnselected = props.selectMode === "single" ? "radio_button_unchecked" : "check_box_outline_blank";

        return (
            <div className="mr-3 d-flex align-items-center" >
                {IsRowSelected(item) ?
                    <i className="material-icons mr-1" title="Check to de-select" >{iconSelected}</i>
                    :
                    <i className="material-icons mr-1" title="Check to select" >{iconUnselected}</i>
                }
            </div>
        );
    }

    //render simplified Profile show on profile type entity or profile type list 
    const renderRowSimple = () => {

        const isSelected = props.item != null && IsRowSelected(props.item) ? "selected" : "";
        const cssClass = `row py-1 align-items-center ${props.cssClass == null ? '' : props.cssClass} ${isSelected} ${props.selectMode != null ? "selectable" : ""}`;
        const avatarCss = `col-avatar mr-2 rounded-circle avatar-${!isOwner(props.item, props.activeAccount) ? "locked" : "unlocked"} elevated`;
        //var colCss = `${props.actionUI == null ? "col-sm-12" : "col-sm-10"} d-flex align-items-center`;
        const caption = props.item == null ? "" : getProfileCaption(props.item);
        const profileIcon = props.item == null ?
            renderProfileIcon({ authorId: null }, props.activeAccount, 20, false) :
            renderProfileIcon(props.item, props.activeAccount, 20, false);

        return (
            <div className={cssClass} onClick={onRowSelect} >
                <div className="col-sm-12 d-flex align-items-center" >
                    <div className={avatarCss} >{profileIcon}</div>
                    <div className="col-sm-11" >
                        <span className="font-weight-bold mr-2" >{props.profileCaption == null ? "Profile: " : `${props.profileCaption}: `}</span>
                        {caption}
                        {(props.actionUI != null) &&
                            <div className="ml-2 d-inline-flex" >
                                {props.actionUI}
                            </div>
                        }
                    </div>
                </div>
            </div>
        );
    };

    //render typical row that is shown in a list/grid
    const renderRow = () => {

        var isSelected = props.item != null && IsRowSelected(props.item) ? "selected" : "";
        var cssClass = `row py-1 align-items-center ${props.cssClass == null ? '' : props.cssClass} ${isSelected} ${props.selectMode != null ? "selectable" : ""}`;

        return (
            <div className={cssClass} onClick={onRowSelect} >
                <div className="col-sm-8 d-flex" >
                    {props.selectMode != null &&
                        renderSelectColumn(props.item)
                    }
                    <div className={`col-avatar mt-1 mr-2 rounded-circle avatar-${!isOwner(props.item, props.activeAccount) ? "locked" : "unlocked"} elevated`} >
                        {renderProfileIcon(props.item, props.activeAccount, 24, false)}
                    </div>
                    <div className="col-sm-11 d-flex align-items-center" >
                        <div className="d-block" >
                            <p className="my-0">
                                {props.profileCaption == null ? "Namespace: " : `${props.profileCaption}: `}
                                {!props.showActions || props.selectMode != null ?
                                    <span className="ml-2" >{props.item.namespace}</span>
                                    :
                                    <button className="ml-1 btn btn-link" onClick={onEditItem} >{props.item.namespace}</button>
                                }
                            </p>
                            {props.item.displayName != null &&
                                <p className="my-0 small-size" >Title: {props.item.displayName}</p>
                            }
                            {props.item.version != null &&
                                <p className="my-0 small-size" >Version: {props.item.version}</p>
                            }
                            {props.item.publishDate != null &&
                                <p className="my-0 small-size" >Published: {formatDate(props.item.publishDate)}</p>
                            }
                            {props.item.description != null &&
                                <p className="my-0 small-size" >Description: {props.item.description.substr(0, 80)}</p>
                            }
                        </div>
                    </div>
                </div>
                {renderActionsColumn(props.item, props.showActions && props.selectMode == null)}
            </div>
        );
    };

    //build the row
    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    //two different modes supported
    if (props.mode == null || props.mode === "normal") {
        if (props.item === null || props.item === {}) return null;
        if (props.item.namespace == null) return null;

        return (
            <>
                {renderRow()}
            </>
        );
    }
    else if (props.mode === "simple") {
        return (
            <>
                {renderRowSimple()}
            </>
        );
    }
    return null;
}

export default ProfileItemRow;