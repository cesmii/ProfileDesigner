import React, { useState, useEffect } from 'react'
import { useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"

import { useAuthState } from '../components/authentication/AuthContext';
import ProfileListGrid from './shared/ProfileListGrid';
import { AppSettings } from '../utils/appsettings'
import { generateLogMessageString } from '../utils/UtilityService'
import { useWizardContext } from '../components/contexts/WizardContext'
import { getWizardNavInfo, renderWizardBreadcrumbs, renderWizardButtonRow, renderWizardHeader, renderWizardIntroContent, WizardSettings } from '../services/WizardUtil'
import { ErrorModal } from '../services/CommonUtil'

const CLASS_NAME = "WizardSelectProfile";

function WizardSelectProfile() {

    //TBD - send flag to profilegrid list to only return profiles that are mine. There is an endpoint for this. We 
    //will need logic in the profileGridList to check for this circumstance. 
    //we can only associate a type with profiles I am author of. 

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();
    const authTicket = useAuthState();
    const _pageId = history.location.pathname.indexOf('/select-existing-profile') > -1 ? 'SelectExistingProfile' : 'SelectProfile';
    const { wizardProps, setWizardProps } = useWizardContext();
    //if we come into this page in the continue work flow, we need to update mode value for downstream
    const _currentPage = WizardSettings.panels.find(p => { return p.id === _pageId; });
    const _mode = history.location.pathname.indexOf('/select-existing-profile') > -1 ? WizardSettings.mode.SelectProfile : wizardProps.mode;
    const _navInfo = getWizardNavInfo(_mode, _pageId);
    const [_error, setError] = useState({ show: false, message: null, caption: null });

    //-------------------------------------------------------------------
    // Region: hooks
    //-------------------------------------------------------------------
    useEffect(() => {

        //only want this to run once on load
        if (wizardProps != null && wizardProps.currentPage === _currentPage.id) {
            return;
        }

        //update state on load - update mode is important if we come into this page in the continur work flow
        setWizardProps({ currentPage: _currentPage.id, mode: _mode });

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||wizardProps||Cleanup', CLASS_NAME));
            //setFilterValOnChild('');
        };
    }, [wizardProps.currentPage]);


    //-------------------------------------------------------------------
    // Region: Event handling
    //-------------------------------------------------------------------
    const onRowClicked = (e) => {
        console.log(generateLogMessageString(`onRowClicked||` + e.id, CLASS_NAME));

        //toggle selected - including removing selected value if previously set
        setWizardProps({ profile: wizardProps.profile == null || wizardProps.profile.id !== e.id ? e : null });
    };

    const onNextStep = () => {
        console.log(generateLogMessageString(`onNextStep`, CLASS_NAME));

        if (!validateForm()) {
            //alert("validation failed");
            return;
        }

        history.push({
            pathname: _navInfo.next.href
        });
    };

    const onErrorModalClose = () => {
        //console.log(generateLogMessageString(`onErrorMessageOK`, CLASS_NAME));
        setError({ show: false, caption: null, message: null });
    }

    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));

        //validation. If nothing selected, can't proceed
        if (wizardProps.profile == null) {
            setError({ show: true, caption: _currentPage.caption, message: 'A base profile must be selected before proceeding' });
            console.error(generateLogMessageString(`onNextStep||no profile selected`, CLASS_NAME));
            return false;
        }

        //validation. Check if profile ownership is same as logged in user
        if (wizardProps.profile.isReadOnly || wizardProps.profile.authorId == null || wizardProps.profile.authorId !== authTicket.user.id ) {
            setError({ show: true, caption: _currentPage.caption, message: 'Invalid selection. The base profile must be a profile you have authored or created.' });
            console.error(generateLogMessageString(`onNextStep||ownership validation error`, CLASS_NAME));
            return false;
        }

        return true;
    }

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderMainContent = () => {
        var selItems = [];
        if (wizardProps.profile != null) selItems.push(wizardProps.profile.id);

        return (
            <>
            <div className="card row mb-3">
                    <div className="card-body col-sm-12">
                        <ProfileListGrid isMine={true} onGridRowSelect={onRowClicked} selectMode="single"
                            selectedItems={selItems} rowCssClass="mx-0" />
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
            {renderWizardBreadcrumbs(_mode, _navInfo.stepNum)}
            {renderWizardHeader(`Step ${_navInfo.stepNum}: ${_currentPage.caption}`)}
            {renderWizardIntroContent(_currentPage.introContent)}
            {renderMainContent()}
            {renderWizardButtonRow(_navInfo)}
            <ErrorModal modalData={_error} callback={onErrorModalClose} />
        </>
    )
}

export default WizardSelectProfile;