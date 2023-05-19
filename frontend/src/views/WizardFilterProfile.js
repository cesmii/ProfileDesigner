import React, { useState, useEffect } from 'react'
import { Helmet } from "react-helmet"

import { useHistory } from 'react-router-dom'

import ProfileListGrid from './shared/ProfileListGrid';

import { useLoadingContext } from '../components/contexts/LoadingContext';
import { useWizardContext } from '../components/contexts/WizardContext'
import { AppSettings } from '../utils/appsettings'
import { generateLogMessageString } from '../utils/UtilityService'
import { getWizardNavInfo, renderWizardBreadcrumbs, renderWizardButtonRow, renderWizardHeader, renderWizardIntroContent, WizardSettings } from '../services/WizardUtil'
import { toggleSearchFilterSelected } from '../services/ProfileService';

const CLASS_NAME = "WizardFilterProfile";

function WizardFilterProfile() {
    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const _pageId = 'FilterProfile';
    const history = useHistory();
    const { wizardProps, setWizardProps } = useWizardContext();
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const _currentPage = WizardSettings.panels.find(p => { return p.id === _pageId; });
    const _navInfo = getWizardNavInfo(wizardProps.mode, _pageId);
    const [_searchCriteria, setSearchCriteria] = useState(null);

    //-------------------------------------------------------------------
    // Region: hooks
    //-------------------------------------------------------------------
    useEffect(() => {

        //only want this to run once on load
        if (wizardProps != null && wizardProps.currentPage === _currentPage.id) {
            return;
        }

        //update state on load 
        setWizardProps({
            currentPage: _currentPage.id,
            searchCriteria: wizardProps.searchCriteria == null ? loadingProps.searchCriteria : wizardProps.searchCriteria
        });

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||wizardProps||Cleanup', CLASS_NAME));
            //setFilterValOnChild('');
        };
    }, [wizardProps.currentPage]);

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
    // Region: Event handling
    //-------------------------------------------------------------------
    const onRowClicked = (e) => {
        console.log(generateLogMessageString(`onRowClicked||` + e.id, CLASS_NAME));

        //toggle selected - including removing selected value if previously set
        var criteria = JSON.parse(JSON.stringify(wizardProps.searchCriteria));
        toggleSearchFilterSelected(criteria, AppSettings.SearchCriteriaCategory.Profile, parseInt(e.id.toString()));

        //persist selection during wizard lifetime, reset base type selection
        setWizardProps({ searchCriteria: criteria, parentId: null });
    };

    const onNextStep = () => {
        console.log(generateLogMessageString(`onNextStep`, CLASS_NAME));

        //TBD - check validation. If nothing selected, can we proceed?  
        if (wizardProps.profileId == null) {
            console.log(generateLogMessageString(`onNextStep||no profile selected`, CLASS_NAME));
        }

        //nav next
        history.push({
            pathname: _navInfo.next.href
        });
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderMainContent = () => {

        if (wizardProps.searchCriteria == null || wizardProps.searchCriteria.filters == null) return;

        var selItems = [];
        var filterProfilesCat = wizardProps.searchCriteria.filters
            .find(x => { return x.id.toString() === AppSettings.SearchCriteriaCategory.Profile.toString(); });
        if (filterProfilesCat != null && filterProfilesCat.items != null) {
            selItems = filterProfilesCat.items.map((p) => {
                return p.selected ? p.id : null;
            }).filter(x => x != null);
        }

        return (
            <>
            <div className="card row mb-3">
                    <div className="card-body">
                        <ProfileListGrid onGridRowSelect={onRowClicked} selectMode="multiple"
                            selectedItems={selItems} rowCssClass="mx-0"
                            searchCriteria={_searchCriteria} mode={AppSettings.ProfileListMode.Profile} hideSearchBox={true} />
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
        </>
    )
}

export default WizardFilterProfile;