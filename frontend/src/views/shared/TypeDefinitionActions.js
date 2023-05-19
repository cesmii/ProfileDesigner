import React, { useState } from 'react'
import { Dropdown } from 'react-bootstrap'

import axiosInstance from '../../services/AxiosService';
import { useLoadingContext } from "../../components/contexts/LoadingContext";
import { AppSettings } from '../../utils/appsettings';
import ConfirmationModal from '../../components/ConfirmationModal';
import { ErrorModal } from '../../services/CommonUtil';
import { isOwner } from './ProfileRenderHelpers';
import { cleanFileName, generateLogMessageString, renderMenuIcon } from '../../utils/UtilityService';
import { SVGIcon } from '../../components/SVGIcon'
import color from '../../components/Constants';
import { getProfileCaption } from '../../services/ProfileService';

const CLASS_NAME = "TypeDefinitionActions";

//-------------------------------------------------------------------
// Region: Shared UI with profileEntity and profileItemRow - actions triggers
//-------------------------------------------------------------------
function TypeDefinitionActions(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_deleteModal, setDeleteModal] = useState({ show: false, items: null });
    const [_error, setError] = useState({ show: false, message: null, caption: null });

    //-------------------------------------------------------------------
    // Region: Event handlers
    //-------------------------------------------------------------------
    const downloadProfile = async () => {
        console.log(generateLogMessageString(`downloadProfile||start`, CLASS_NAME));
        //add a row to download messages and this will kick off download
        var msgs = loadingProps.downloadItems || [];
        msgs.push({ profileId: props.item.profile?.id, fileName: cleanFileName(props.item.profile?.namespace), immediateDownload: true });
        setLoadingProps({ downloadItems: JSON.parse(JSON.stringify(msgs)) });
    }
    const downloadProfileAsAASX = async () => {
        console.log(generateLogMessageString(`downloadProfileAASX||start`, CLASS_NAME));
        //add a row to download messages and this will kick off download
        var msgs = loadingProps.downloadItems || [];
        msgs.push({ profileId: props.item.profile?.id, fileName: cleanFileName(props.item.profile?.namespace), immediateDownload: true, downloadFormat: AppSettings.ExportFormatEnum.AASX });
        setLoadingProps({ downloadItems: JSON.parse(JSON.stringify(msgs)) });
    }
    const downloadProfileAsSmipJson = async () => {
        console.log(generateLogMessageString(`downloadProfileSmipJson||start`, CLASS_NAME));
        //add a row to download messages and this will kick off download
        var msgs = loadingProps.downloadItems || [];
        msgs.push({ profileId: props.item.profile?.id, fileName: cleanFileName(props.item.profile?.namespace), immediateDownload: true, downloadFormat: AppSettings.ExportFormatEnum.SmipJson });
        setLoadingProps({ downloadItems: JSON.parse(JSON.stringify(msgs)) });
    }

    const onDeleteItem = () => {
        console.log(generateLogMessageString(`onDeleteItem`, CLASS_NAME));
        setDeleteModal({ show: true, item: props.item });
    }

    //on confirm click within the modal, this callback will then trigger the next step (ie call the API)
    const onDeleteConfirm = () => {
        console.log(generateLogMessageString(`onDeleteConfirm`, CLASS_NAME));
        deleteItem(_deleteModal.item);
        setDeleteModal({ show: false, item: null });
    };

    const onErrorModalClose = () => {
        //console.log(generateLogMessageString(`onErrorMessageOK`, CLASS_NAME));
        setError({ show: false, caption: null, message: null });
    }


    //-------------------------------------------------------------------
    // Region: API call to perform delete
    //-------------------------------------------------------------------
    const deleteItem = (item) => {
        console.log(generateLogMessageString(`deleteItem||Id:${item.id}`, CLASS_NAME));

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform delete call
        const data = { id: item.id };
        const url = `profiletypedefinition/delete`;
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
                    //force re-load to show the list w/o the deleted row
                    if (props.onDeleteCallback) props.onDeleteCallback(result.data.isSuccess);
                }
                else {
                    //update spinner, messages
                    setError({ show: true, caption: 'Delete Item Error', message: `An error occurred deleting this item: ${result.data.message}` });
                    setLoadingProps({ isLoading: false, message: null });
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

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
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
    // Region: Final render
    //-------------------------------------------------------------------
    if (!props.item) return null;

    const captionProfile = `'${getProfileCaption(props.item.profile)}'`;

    return (
        <>
            <Dropdown className={`action-menu icon-dropdown ${props.className == null ? '' : props.className}`} onClick={(e) => e.stopPropagation()} >
                <Dropdown.Toggle drop="left" title="Actions" >
                    <SVGIcon name="more-vert" />
                </Dropdown.Toggle>
                <Dropdown.Menu>
                    {/*{(props.currentUserId != null && props.currentUserId === item.authorId) &&*/}
                    {/*    <Dropdown.Item key="moreVert2" href={getTypeDefinitionNewUrl()} ><span className="mr-3" alt="extend"><SVGIcon name="extend" /></span>New Type Definition</Dropdown.Item>*/}
                    {/*}*/}
                    {!props.isReadOnly &&
                        <>
                        <Dropdown.Item key="moreVert3" onClick={onDeleteItem} >{renderMenuIcon("delete")}Delete Type Definition</Dropdown.Item>
                        <Dropdown.Divider />
                        </>
                    }
                    {props.showExtend &&
                        <>
                        <Dropdown.Item href={`/type/extend/${props.item.id}`}>{renderMenuIcon("extend")}Extend '{props.item.name}'</Dropdown.Item>
                        <Dropdown.Divider />
                        </>
                    }
                    <Dropdown.Item key="moreVert4" onClick={downloadProfile} >{renderMenuIcon("download")}Download {captionProfile}</Dropdown.Item>
                    <Dropdown.Item key="moreVert5" onClick={downloadProfileAsAASX} >{renderMenuIcon("download")}Download {captionProfile} as AASX</Dropdown.Item>
                    <Dropdown.Item key="moreVert6" onClick={downloadProfileAsSmipJson} >{renderMenuIcon("download")}Download {captionProfile} for SMIP import (experimental)</Dropdown.Item>
                </Dropdown.Menu>
            </Dropdown>
            {renderDeleteConfirmation()}
            <ErrorModal modalData={_error} callback={onErrorModalClose} />
        </>
    );

}

export default TypeDefinitionActions;
