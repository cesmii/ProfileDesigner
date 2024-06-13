import React, { useState, useEffect } from 'react'
import { Helmet } from "react-helmet"
import axiosInstance from "../services/AxiosService";

import { Button } from 'react-bootstrap'

import { useMsal } from "@azure/msal-react";

import { AppSettings } from '../utils/appsettings'
import { generateLogMessageString, renderTitleBlock, scrollTop, isInRole, renderMenuIcon } from '../utils/UtilityService'
import { useLoadingContext } from "../components/contexts/LoadingContext";
import ConfirmationModal from '../components/ConfirmationModal';
import CloudLibSlideOut from './shared/CloudLibSlideOut.js'
import ProfileListGrid from './shared/ProfileListGrid';
import ProfileImporter from './shared/ProfileImporter';

import Dropdown from 'react-bootstrap/Dropdown'

import color from '../components/Constants'
import './styles/ProfileList.scss';
import { ErrorModal } from '../services/CommonUtil';

const CLASS_NAME = "ProfileList";

function ProfileList() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const caption = 'Profile Library';
    const iconName = AppSettings.IconMapper.Profile;
    const iconColor = color.shark;
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_deleteModal, setDeleteModal] = useState({ show: false, items: null });
    //importer
    const [_error, setError] = useState({ show: false, message: null, caption: null });
    const [_searchCriteria, setSearchCriteria] = useState(null);
    const [_searchCriteriaChanged, setSearchCriteriaChanged] = useState(0);
    const [_cloudLibSlideOut, setCloudLibSlideOut] = useState({ isOpen: false });

    const { instance } = useMsal();
    const _activeAccount = instance.getActiveAccount();

    //-------------------------------------------------------------------
    // Region: Pass profile id into component if profileId passed in from url
    //-------------------------------------------------------------------
    //-------------------------------------------------------------------
    // Region: search criteria check and populate
    //-------------------------------------------------------------------
    useEffect(() => {
        //check for searchcriteria - trigger fetch of search criteria data - if not already triggered
        if ((loadingProps.profileSearchCriteria == null || loadingProps.profileSearchCriteria.filters == null) && !loadingProps.refreshProfileSearchCriteria) {
            setLoadingProps({ refreshProfileSearchCriteria: true });
            return;
        }
        else if (loadingProps.profileSearchCriteria == null || loadingProps.profileSearchCriteria.filters == null) {
            return;
        }
        //implies it is in progress on re-loading criteria
        else if (loadingProps.refreshProfileSearchCriteria) {
            return;
        }

        setSearchCriteria(JSON.parse(JSON.stringify(loadingProps.profileSearchCriteria)));

    }, [loadingProps.profileSearchCriteria]);


    //-------------------------------------------------------------------
    // Region: hooks - show or hide panel - update body tag
    //-------------------------------------------------------------------
    useEffect(() => {
        document.body.className = _cloudLibSlideOut.isOpen ?
            //remove (if present) and re-append
            document.body.className.replace('slideout-open-no-scroll', '') + 'slideout-open-no-scroll'
            //remove (if present)
            : document.body.className.replace('slideout-open-no-scroll', '');
        //on unmount
        return () => {
            document.body.className = document.body.className.replace('slideout-open-no-scroll', '');
        }
    }, [_cloudLibSlideOut]);

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

    const onCloudLibImportClick = () => {
        console.log(generateLogMessageString(`onCloudLibImportClick`, CLASS_NAME));
        //setCloudLibImporterModal({ show: true });
        setCloudLibSlideOut({ isOpen: true });
    };

    const onCloseSlideOut = () => {
        console.log(generateLogMessageString(`onCloseSlideOut`, CLASS_NAME));
        setCloudLibSlideOut({ isOpen: false });
    }

    const onErrorModalClose = () => {
        //console.log(generateLogMessageString(`onErrorMessageOK`, CLASS_NAME));
        setError({ show: false, caption: null, message: null });
    }

    //bubble up search criteria changed so the parent page can control the search criteria
    const onSearchCriteriaChanged = (criteria) => {
        console.log(generateLogMessageString(`onSearchCriteriaChanged`, CLASS_NAME));
        //update state
        setSearchCriteria(criteria);
        //trigger api to get data
        setSearchCriteriaChanged(_searchCriteriaChanged + 1);
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderHeaderRow = () => {
        return (
            <div className="row pb-3">
                <div className="col-sm-7 me-auto d-flex">
                    {renderTitleBlock(caption, iconName, iconColor)}
                </div>
                <div className="col-sm-5 d-flex align-items-center justify-content-end">
                    <Dropdown className="import-menu icon-dropdown auto-width mx-2" onClick={(e) => e.stopPropagation()} >
                        <Dropdown.Toggle variant="secondary" className="auto-width mx-2">
                            Import...
                        </Dropdown.Toggle>
                        <Dropdown.Menu className="py-0" >
                            <Dropdown.Item className="py-2" onClick={onCloudLibImportClick}>{renderMenuIcon("cloud-download")}Import from Cloud Library</Dropdown.Item>
                            <Dropdown.Item className="py-2" as="button" >
                                {<ProfileImporter caption="Import NodeSet file" iconName="file-upload" cssClass="mb-0" useCssClassOnly="true" />}
                            </Dropdown.Item>
                            {(isInRole(_activeAccount, 'cesmii.profiledesigner.admin')) &&
                                <>
                                    <Dropdown.Divider className="my-0" />
                                    <Dropdown.Item className="pb-2" as="button" >
                                    {<ProfileImporter caption="Upgrade Global NodeSet file" iconName="settings" cssClass="mb-0" useCssClassOnly="true" />}
                                    </Dropdown.Item>
                                </>
                            }
                        </Dropdown.Menu>
                    </Dropdown>
                    <Button variant="secondary" type="button" className="auto-width mx-2" href="/profile/new" >Create Profile</Button>
                </div>
            </div>
        );
    };

    const renderIntroContent = () => {
        return (
            <div className="header-actions-row mb-3 pr-0">
                <p className="mb-2" >
                    If you are the SM Profile author, import your profiles (including any dependent profiles) using the 'Import' button. The import will tag you as the author for your profiles and permit you to edit them. The import will also check to ensure referenced type models are valid OPC UA type models.
                </p>
                <p className="mb-2" >
                    Any dependent profiles (OPC UA type models) that are imported will become read-only and added to the Profile Library. Type definitions within these dependent profiles can be viewed or extended to make new Type definitions, which can become part of one of your SM Profiles.
                </p>
            </div>
        );
    }

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
            {(_searchCriteria != null) &&
                <ProfileListGrid searchCriteria={_searchCriteria} mode={AppSettings.ProfileListMode.Profile}
                    onGridRowSelect={onGridRowSelect} 
                    onSearchCriteriaChanged={onSearchCriteriaChanged} searchCriteriaChanged={_searchCriteriaChanged} hideSearchBox={false} />
            }
            <ErrorModal modalData={_error} callback={onErrorModalClose} />
            <CloudLibSlideOut isOpen={_cloudLibSlideOut.isOpen} onClosePanel={onCloseSlideOut} onImportStarted={onCloseSlideOut} />
        </>
    )
}

export default ProfileList;