import React, { useEffect }  from 'react'
import { useState } from 'react';

import { useLoadingContext } from '../../components/contexts/LoadingContext';
import { useWizardContext } from '../../components/contexts/WizardContext';
import { clearSearchCriteria } from '../../services/ProfileService';
import { generateLogMessageString } from '../../utils/UtilityService';

const CLASS_NAME = "WizardMaster";

function WizardMaster(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const { setWizardProps } = useWizardContext();
    const [_searchCriteriaRefreshedLocal, setSearchCriteriaRefreshedLocal] = useState(0);

    //-------------------------------------------------------------------
    // Region: hooks
    //  trigger from some other component to kick off an import log refresh and start tracking import status
    //-------------------------------------------------------------------
    useEffect(() => {
        //check for searchcriteria - trigger fetch of search criteria data - if not already triggered
        if ((loadingProps.searchCriteria == null || loadingProps.searchCriteria.filters == null) && !loadingProps.refreshSearchCriteria) {
            setLoadingProps({ refreshSearchCriteria: true });
        }

        //only update this if an external component triggered a refresh of the data
        //this component survives through the wizard lifetime
        if (_searchCriteriaRefreshedLocal === loadingProps.searchCriteriaRefreshed) return;

        //update the state
        setSearchCriteriaRefreshedLocal(loadingProps.searchCriteriaRefreshed);

        //start with a blank criteria slate. Handle possible null scenario if criteria hasn't loaded yet. 
        var criteria = loadingProps.searchCriteria == null ? null : JSON.parse(JSON.stringify(loadingProps.searchCriteria));
        criteria = criteria == null ? null : clearSearchCriteria(criteria);

        console.log(generateLogMessageString('useEffect||wizardMaster||updateSearchCriteria', CLASS_NAME));
        //init the wizard props search criteria - sometimes this is not yet initiated in load,
        //if we import in the wizard, this will get triggered and need to be updated. 
        //b/c this page is a mater page, it will survive the navigation in the wizard. 
        setWizardProps({searchCriteria: criteria});

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||wizardProps||Cleanup', CLASS_NAME));
        };
    }, [loadingProps.searchCriteriaRefreshed, _searchCriteriaRefreshedLocal, loadingProps.refreshSearchCriteria, loadingProps.searchCriteria, setLoadingProps, setWizardProps]);

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            {props.children}
        </>
    )
}

export default WizardMaster;