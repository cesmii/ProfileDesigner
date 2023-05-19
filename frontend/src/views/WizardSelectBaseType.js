import React, { useState, useEffect } from 'react'
import { useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import axiosInstance from "../services/AxiosService";

import { AppSettings } from '../utils/appsettings'
import { generateLogMessageString } from '../utils/UtilityService'
import { useLoadingContext } from "../components/contexts/LoadingContext";
import { useWizardContext } from '../components/contexts/WizardContext'
import { getWizardNavInfo, renderWizardBreadcrumbs, renderWizardButtonRow, renderWizardHeader, renderWizardIntroContent, WizardSettings } from '../services/WizardUtil'
import ProfileTypeDefinitionListGrid from './shared/ProfileTypeDefinitionListGrid';
import { ErrorModal } from '../services/CommonUtil';

const CLASS_NAME = "WizardSelectBaseType";

function WizardSelectBaseType() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const _pageId = 'SelectBaseType';
    const history = useHistory();
    const { setLoadingProps } = useLoadingContext();
    const { wizardProps, setWizardProps } = useWizardContext();
    const _currentPage = WizardSettings.panels.find(p => { return p.id === _pageId; });
    const _navInfo = getWizardNavInfo(wizardProps.mode, _pageId);
    const [_error, setError] = useState({ show: false, message: null, caption: null });
    const [_searchCriteriaChanged, setSearchCriteriaChanged] = useState(0);

    //-------------------------------------------------------------------
    // Region: hooks
    //-------------------------------------------------------------------
    useEffect(() => {

        //only want this to run once on load
        if (wizardProps != null && wizardProps.currentPage === _currentPage.id) {
            return;
        }

        //update state on load 
        setWizardProps({ currentPage: _currentPage.id });
        setSearchCriteriaChanged(_searchCriteriaChanged + 1);

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||wizardProps||Cleanup', CLASS_NAME));
            //setFilterValOnChild('');
        };
    }, [wizardProps.currentPage]);

    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------
    const validateWizard = () => {

        //TBD - check validation. If nothing selected, don't proceed
        if (wizardProps.parentId == null) {
            console.log(generateLogMessageString(`onNextStep||no profile type def selected`, CLASS_NAME));
            //message to let user know we can't proceed
            setError({ show: true, caption: _currentPage.caption, message: "A Type Definition must be selected before proceeding." });
            return false;
        }

        //if profile is null, then can't proceed. This is either populated as new with null id 
        //or if user selected on the select profile screen (different than the Filter Profile screen)
        if (wizardProps.profile == null) {
            console.log(generateLogMessageString(`onNextStep||no parent profile selected`, CLASS_NAME));
            //TBD - add inline message to let user know we can't proceed
            var msg = "A profile must be selected before finishing the wizard. Please select a profile on the select profile screen.";
            var url = '/wizard/welcome';
            switch (wizardProps.mode) {
                case AppSettings.WizardSettings.Mode.CreateProfile:
                    msg = "A profile must be created before finishing the wizard. Please create the profile on the create profile screen.";
                    url = '/wizard/create-profile';
                    break;
                case AppSettings.WizardSettings.Mode.ImportProfile:
                    msg = "A profile must be imported and selected before finishing the wizard. Please select an imported profile on the select profile screen.";
                    url = '/wizard/select-profile';
                    break;
                case AppSettings.WizardSettings.Mode.SelectProfile:
                default:
                    msg = "A profile must be selected before finishing the wizard. Please select a profile on the select profile screen.";
                    url = '/wizard/select-profile';
                    break;
            }
            //message to let user know we can't proceed
            setError({ show: true, caption: _currentPage.caption, message: msg });

            //nav to relevant screen
            history.push({ pathname: url });
            return false;
        }

        //if we get here, all good
        return true;
    }

    //-------------------------------------------------------------------
    // Region: Event handling
    //-------------------------------------------------------------------
    const onRowClicked = (e) => {
        console.log(generateLogMessageString(`onRowClicked||` + e.id, CLASS_NAME));

        //toggle selected - including removing selected value if previously set
        setWizardProps({ parentId: wizardProps.parentId === e.id ? null : e.id });
    };

    //bubble up search criteria changed so the parent page can control the search criteria
    const onSearchCriteriaChanged = (criteria) => {
        console.log(generateLogMessageString(`onSearchCriteriaChanged`, CLASS_NAME));
        //update state
        setWizardProps({ searchCriteria: criteria });
        //trigger api to get data
        setSearchCriteriaChanged(_searchCriteriaChanged + 1);
    };

    const onNextStep = () => {
        console.log(generateLogMessageString(`onNextStep||Finish`, CLASS_NAME));

        if (!validateWizard()) {
            return;
        }

        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        //if profile was selected (not created), then we call complete immediately.
        //else - we create profile then call complete. 
        if (wizardProps.profile != null && wizardProps.profile.id != null && wizardProps.profile.id > 0) {
            onNextStepComplete(wizardProps.profile.id);
            return;
        }

        //save new profile
        console.log(generateLogMessageString(`onNextStep||add profile api call`, CLASS_NAME));
        var url = `profile/add`;
        axiosInstance.post(url, wizardProps.profile)
            .then(resp => {

                if (resp.data.isSuccess) {
                    //hide a spinner
                    setLoadingProps({
                        isLoading: false, message: null, refreshProfileCount: true
                    });

                    //update cached profile id in case user navigates back to this screen from type def edit screen.
                    var profileNew = JSON.parse(JSON.stringify(wizardProps.profile));
                    profileNew.id = resp.data.data;
                    setWizardProps({ profile: profileNew });

                    //console.log(resp.data);
                    //on successful save, navigate to the extend screen
                    onNextStepComplete(resp.data.data);
                    return;
                }
                else {
                    setLoadingProps({ isLoading: false, message: null });
                    setError({ show: true, caption: "Create Profile", message: resp.data.message });
                }

            })
            .catch(error => {
                //hide a spinner, show a message
                setLoadingProps({ isLoading: false, message: null });
                setError({ show: true, caption: "Create Profile", message: 'An error occurred saving the profile. Please try again.' });
                console.log(generateLogMessageString('handleOnSave||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
            });

    };

    //perform final navigation
    const onNextStepComplete = (profileId) => {
        console.log(generateLogMessageString(`onNextStepComplete||Finish`, CLASS_NAME));
        //nav next
        history.push({
            pathname: profileId == null ?
                `/wizard/extend/${wizardProps.parentId}` :
                `/wizard/extend/${wizardProps.parentId}/p=${profileId}`
        });
    };

    const onErrorModalClose = () => {
        //console.log(generateLogMessageString(`onErrorMessageOK`, CLASS_NAME));
        setError({ show: false, caption: null, message: null });
    }

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderMainContent = () => {

        var selItems = [];
        if (wizardProps.parentId != null) {
            selItems.push(wizardProps.parentId);
        }

        return (
            <>
            <div className="card row mb-3">
                    <div className="card-body">
                        <span className="headline-2" >Filters</span>
                        <ProfileTypeDefinitionListGrid onGridRowSelect={onRowClicked} selectMode="single" 
                            selectedItems={selItems} rowCssClass="mx-0"
                            searchCriteria={wizardProps.searchCriteria}
                            onSearchCriteriaChanged={onSearchCriteriaChanged} searchCriteriaChanged={_searchCriteriaChanged}
                            showProfileFilter={true}
                        />
                </div>
            </div>
            </>
        );
    };

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    //assign nextInfo callback after method declared
    _navInfo.next.callbackAction = onNextStep;

    return (
        <>
            <Helmet>
                <title>{AppSettings.Titles.Main + " | " + _currentPage.caption}</title>
            </Helmet>
            {renderWizardBreadcrumbs(wizardProps.mode, _navInfo.stepNum)}
            {renderWizardHeader(`Step ${_navInfo.stepNum}: ${_currentPage.caption}`)}
            {renderWizardIntroContent(_currentPage.introContent)}
            {renderMainContent()}
            {renderWizardButtonRow(_navInfo)}
            <ErrorModal modalData={_error} callback={onErrorModalClose} />
        </>
    )
}

export default WizardSelectBaseType;