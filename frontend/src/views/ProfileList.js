import React, { useState } from 'react'
import { Helmet } from "react-helmet"
import axiosInstance from "../services/AxiosService";

import { Button } from 'react-bootstrap'

import { AppSettings } from '../utils/appsettings'
import { generateLogMessageString, renderTitleBlock, scrollTop } from '../utils/UtilityService'
import { useLoadingContext } from "../components/contexts/LoadingContext";
import ConfirmationModal from '../components/ConfirmationModal';
import ProfileEntityModal from './modals/ProfileEntityModal';
import ProfileListGrid from './shared/ProfileListGrid';
import ProfileImporter from './shared/ProfileImporter';

import color from '../components/Constants'
import './styles/ProfileList.scss';
import { ErrorModal } from '../services/CommonUtil';

const CLASS_NAME = "ProfileList";

function ProfileList() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const caption = 'Profile Library';
    const iconName = 'folder-shared';
    const iconColor = color.shark;
    const { setLoadingProps } = useLoadingContext();
    const [_deleteModal, setDeleteModal] = useState({ show: false, items: null });
    //importer
    const [_error, setError] = useState({ show: false, message: null, caption: null });
    //used in popup profile add/edit ui. Default to new version
    const [_profileEntityModal, setProfileEntityModal] = useState({ show: false, item: null});

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onGridRowSelect = (item) => {
        console.log(generateLogMessageString(`onGridRowSelect||Name: ${item.namespace}||selected: ${item.selected}`, CLASS_NAME));
        //TBD - handle selection here...
    };

    //-------------------------------------------------------------------
    // Region: Add/Update event handlers
    //-------------------------------------------------------------------
    const onAdd = () => {
        console.log(generateLogMessageString(`onAdd`, CLASS_NAME));
        setProfileEntityModal({ show: true, item: null});
    };

    const onEdit = (item) => {
        console.log(generateLogMessageString(`onEdit`, CLASS_NAME));
        setProfileEntityModal({ show: true, item: item});
    };

    const onSave = (id) => {
        console.log(generateLogMessageString(`onSave`, CLASS_NAME));
        setProfileEntityModal({ show: false, item: null });
        //force re-load to show the newly added, edited items
        setLoadingProps({ refreshProfileList:true});
    };

    const onSaveCancel = () => {
        console.log(generateLogMessageString(`onSaveCancel`, CLASS_NAME));
        setProfileEntityModal({ show: false, item: null });
    };

    const onErrorModalClose = () => {
        //console.log(generateLogMessageString(`onErrorMessageOK`, CLASS_NAME));
        setError({ show: false, caption: null, message: null });
    }

    //-------------------------------------------------------------------
    // Region: Delete event handlers
    //-------------------------------------------------------------------
    // Delete ONE - from row
    const onDeleteItemClick = (item) => {
        console.log(generateLogMessageString(`onDeleteItemClick`, CLASS_NAME));
        setDeleteModal({ show: true, items: [item] });
    };

    //// Delete MANY - from button above grid
    //const onDeleteManyClick = () => {
    //    console.log(generateLogMessageString(`onDeleteManyClick`, CLASS_NAME));
    //    setDeleteModal({ show: true, items: _dataRows.all });
    //};

    //on confirm click within the modal, this callback will then trigger the next step (ie call the API)
    const onDeleteConfirm = () => {
        console.log(generateLogMessageString(`onDeleteConfirm`, CLASS_NAME));
        deleteItems(_deleteModal.items);
        setDeleteModal({ show: false, item: null });
    };

    //render the delete modal when show flag is set to true
    //callbacks are tied to each button click to proceed or cancel
    const renderDeleteConfirmation = () => {

        if (!_deleteModal.show) return;

        var message = _deleteModal.items.length === 1 ?
            `You are about to delete your profile '${_deleteModal.items[0].namespace}'. This will delete all type definitions associated with this profile. This action cannot be undone. Are you sure?` :
            `You are about to delete ${_deleteModal.items.length} profiles. This will delete all type definitions associated with these profiles. This action cannot be undone. Are you sure?`;
        var caption = `Delete Profile${_deleteModal.items.length === 1 ? "" : "s"}`;

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

    const deleteItems = (items) => {
        console.log(generateLogMessageString(`deleteItems||Count:${items.length}`, CLASS_NAME));

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //perform delete call
        var data = items.length === 1 ? { id: items[0].id } :
            items.map((item) => { return { id: item.id }; });
        var url = items.length === 1 ? `profile/delete` : `profile/deletemany`;
        axiosInstance.post(url, data)  //api allows one or many
            .then(result => {

                if (result.data.isSuccess) {
                    //hide a spinner, show a message
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            {
                                id: new Date().getTime(), severity: "success",
                                body: items.length === 1 ? `Profile was deleted` : `${items.length} Profiles were deleted`, isTimed: true
                            }
                        ],
                        //get profile count from server...this will trigger that call on the side menu
                        refreshProfileCount: true,
                        refreshProfileList: true
                    });
                }
                else {
                    setError({ show: true, caption: 'Delete Error', message: `An error occurred deleting ${items.length === 1 ? "this profile" : "these profiles"} : ${result.data.message}` });
                    //update spinner, messages
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: null
                    });
                }

            })
            .catch(error => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: `An error occurred deleting ${items.length === 1 ? "this profile" : "these profiles"}.`, isTimed: false }
                    ]
                });
                console.log(generateLogMessageString('deleteProfiles||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
                console.log(error);
                //scroll back to top
                scrollTop();
            });
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderHeaderRow = () => {
        return (
            <div className="row pb-3">
                <div className="col-sm-7 mr-auto d-flex">
                    {renderTitleBlock(caption, iconName, iconColor)}
                </div>
                <div className="col-sm-5 d-flex align-items-center justify-content-end">
                    <Button variant="secondary" type="button" className="auto-width mx-2" onClick={onAdd} >Create Profile</Button>
                    <ProfileImporter caption="Import" cssClass="mb-0" />
                </div>
            </div>
        );
    };

    const renderIntroContent = () => {
        return (
            <div className="header-actions-row mb-3 pr-0">
                <p className="mb-2" >
                    If you are the profile author, import the profiles (including any dependent profiles) using the 'Import' button. The import
                    will tag you as the author for your profiles and permit you to edit the imported profiles.
                    The import will check to ensure referenced type models are valid OPC UA type models.
                </p>
                <p className="mb-2" >
                    Any dependent profiles (OPC UA type models) that are imported will become read only and added to the library.
                    Types within these dependent profiles can be viewed or extended to make new type definitions.
                </p>
            </div>
        );
    }

    //renderProfileEntity as a modal to force user to say ok.
    const renderProfileEntity = () => {

        if (!_profileEntityModal.show) return;

        return (
            <ProfileEntityModal item={_profileEntityModal.item} showModal={_profileEntityModal.show} onSave={onSave} onCancel={onSaveCancel} showSavedMessage={true} />
        );
    };

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Helmet>
                <title>{AppSettings.Titles.Main + " | " + caption}</title>
            </Helmet>
            {renderHeaderRow()}
            {renderIntroContent()}
            <ProfileListGrid onGridRowSelect={onGridRowSelect} onEdit={onEdit} onDeleteItemClick={onDeleteItemClick} />
            {renderProfileEntity()}
            {renderDeleteConfirmation()}
            <ErrorModal modalData={_error} callback={onErrorModalClose} />
        </>
    )
}

export default ProfileList;