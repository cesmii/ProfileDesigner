import React, { useState, useEffect }  from 'react'
import { useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"

import { AppSettings } from '../utils/appsettings'
import { generateLogMessageString } from '../utils/UtilityService';
import { useLoadingContext } from "../components/contexts/LoadingContext";
import { useWizardContext } from '../components/contexts/WizardContext';
import { getWizardNavInfo, renderWizardBreadcrumbs, renderWizardHeader, renderWizardIntroContent, WizardSettings } from '../services/WizardUtil';
import ProfileImporter from './shared/ProfileImporter';
import { ErrorModal } from '../services/CommonUtil'

const CLASS_NAME = "WizardImportProfile";

function WizardImportProfile() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const _pageId = 'ImportProfile';
    const history = useHistory();
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const { wizardProps, setWizardProps } = useWizardContext();
    const _currentPage = WizardSettings.panels.find(p => { return p.id === _pageId; });
    const _mode = WizardSettings.mode.ImportProfile;
    const _navInfo = getWizardNavInfo(_mode, _pageId);
    const [_error, setError] = useState({ show: false, message: null, caption: null });
    //track the import kicked off by child importer button
    const [_importStatus, setImportStatus] = useState({isComplete: null, isStarted: null });
    const [_importLogId, setImportLogId] = useState(null);

    //-------------------------------------------------------------------
    // Region: hooks
    //  trigger from some other component to kick off an import log refresh and start tracking import status
    //-------------------------------------------------------------------
    useEffect(() => {

        //only want this to run once on load
        if (wizardProps != null && wizardProps.currentPage === _currentPage.id) {
            return;
        }

        //update state on load 
        setWizardProps({ currentPage: _currentPage.id, mode: _mode });

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||wizardProps||Cleanup', CLASS_NAME));
            //setFilterValOnChild('');
        };
    }, [wizardProps.currentPage, _mode]);

    //-------------------------------------------------------------------
    // Region: hooks
    //  Anytime the import log changes, check to see if our import has finished
    //-------------------------------------------------------------------
    //the first effect is triggered when the activateImportLog is changed indicating the state in loadingProps.importLogs 
    //is ready to evaluate. 
    useEffect(() => {

        if (loadingProps.activateImportLog == null || !loadingProps.activateImportLog) return;

        //if we get here, this indicates we are ready to check for completion.
        //only gets here as import log loop is kicked off
        console.log(generateLogMessageString(`useEffect||import log activated`, CLASS_NAME));
        setImportStatus({ ..._importStatus, isStarted: true });

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||wizardProps||Cleanup', CLASS_NAME));
        };
    }, [loadingProps.activateImportLog]);


    //the 2nd effect is triggered when the activateImportLog is changed indicating the state in loadingProps.importLogs 
    //is ready to evaluate. 
    useEffect(() => {

        if (_importLogId == null || _importStatus.isStarted == null || !_importStatus.isStarted) return;

        //we get here if the import is started and the importLogs state is up to date
        //and we have an import log id to check against in the centralized logs list 
        console.log(generateLogMessageString(`useEffect||waiting loop for import to finish`, CLASS_NAME));

        //check for completeness - nothing in importLogs OR our id is not in the importLogs
        var match = loadingProps.importingLogs == null ? null :
            loadingProps.importingLogs.find(p => { return p.id === _importLogId; });

        //if we finished and are successful, then allow to proceed
        if (match == null || match.status === AppSettings.ImportLogStatus.Completed) {
            setImportStatus({ ..._importStatus, isComplete: true, isStarted: null });
            setImportLogId(null);

            //update lookup data and search criteria on complete.
            setLoadingProps({
                refreshProfileList: true,
                refreshLookupData: true,
                refreshSearchCriteria: true
            });

            onNextStep();
            return;
        }

        //if we are in progress then return
        if (match == null || match.status === AppSettings.ImportLogStatus.InProgress) {
            return;
        }

        //if we failed or cancelled, then let user know and don't proceed
        var msg = 'The import failed: ' +
                (match.status === AppSettings.ImportLogStatus.Cancelled || match.status === AppSettings.ImportLogStatus.Failed ?
                `${match.message}` : 'Review the import message posted above and try again.');
        setImportStatus({ ..._importStatus, isComplete: true, isStarted: null });
        setImportLogId(null);
        setError({ show: true, caption: _currentPage.caption, message: msg });

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||wizardProps||Cleanup', CLASS_NAME));
            //setFilterValOnChild('');
        };
    }, [_importStatus.isStarted, _importLogId, loadingProps.importingLogs]);

    //-------------------------------------------------------------------
    // Region: Event handling
    //-------------------------------------------------------------------
    //this will be called once the useEffect determines the import has completed
    const onNextStep = () => {
        console.log(generateLogMessageString(`onNextStep`, CLASS_NAME));

        history.push({
            pathname: _navInfo.next.href
        });
    };

    const onImportStarted = (id) => {
        console.log(generateLogMessageString(`onImportStarted`, CLASS_NAME));

        //init the tracking of the import log by capturing the id associated with this import
        //this will trigger a useEffects area to check and only advance once the import is completed. 
        setImportStatus({ id: id, isComplete: false, isStarted: null });
        setImportLogId(id);
    };

    const onErrorModalClose = () => {
        //console.log(generateLogMessageString(`onErrorMessageOK`, CLASS_NAME));
        setError({ show: false, caption: null, message: null });
    }

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderButtonRow = () => {
        return (
            <div className="row pb-3">
                <div className="col-12 d-flex" >
                    <a className="mb-2 btn btn-secondary d-flex align-items-center" href={_navInfo.prev.href} ><i className="material-icons mr-1">{_navInfo.prev.icon == null ? "arrow_left" : _navInfo.prev.icon}</i>{_navInfo.prev.caption}</a>
                    <ProfileImporter caption={_importLogId == null ? "Select & Import Profile(s)" : "Processing..."} cssClass="ml-auto" disabled={_importLogId != null} onImportStarted={onImportStarted} />
                </div>
            </div>
        );
    };

    const renderMainContent = () => {
        return (
            <>
                <div className="card row mb-3">
                    <div className="card-body col-sm-12">
                        <p className="mb-0" >
                            Tap the Import Profile(s) button to select XML nodeset files and begin the import process.
                            You can import one or more nodeset files.
                            Depending on the size of the nodeset files and number of files being imported, the import may take a few minutes.
                        </p>
                    </div>
                </div>
                <div className="row mb-3">
                    <div className="col-sm-12">
                        {renderButtonRow()}
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
            <ErrorModal modalData={_error} callback={onErrorModalClose} />
        </>
    )
}

export default WizardImportProfile;