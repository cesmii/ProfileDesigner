import React, { useEffect }  from 'react'
import { Helmet } from "react-helmet"

import { Button } from 'react-bootstrap'

import { useLoadingContext } from '../components/contexts/LoadingContext';
import { useWizardContext } from '../components/contexts/WizardContext';
import { AppSettings } from '../utils/appsettings'
import { generateLogMessageString } from '../utils/UtilityService';
import { renderWizardHeader, renderWizardIntroContent, WizardSettings } from '../services/WizardUtil';

const CLASS_NAME = "WizardWelcome";

function WizardWelcome() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { loadingProps } = useLoadingContext();
    const { wizardProps, setWizardProps } = useWizardContext();
    const _currentPage = WizardSettings.panels.find(p => { return p.id === 'Welcome'; });

    //-------------------------------------------------------------------
    // Region: hooks
    //  trigger from some other component to kick off an import log refresh and start tracking import status
    //-------------------------------------------------------------------
    useEffect(() => {

        //only want this to run once on load
        if (wizardProps != null && wizardProps.currentPage === _currentPage.id) {
            return;
        }

        /*moved to WizardMaster page
        //check for searchcriteria - trigger fetch of search criteria data - if not already triggered
        if ((loadingProps.searchCriteria == null || loadingProps.searchCriteria.filters == null) && !loadingProps.refreshSearchCriteria) {
            setLoadingProps({ refreshSearchCriteria: true });
        }
        //start with a blank criteria slate. Handle possible null scenario if criteria hasn't loaded yet. 
        var criteria = loadingProps.searchCriteria == null ? null : JSON.parse(JSON.stringify(loadingProps.searchCriteria));
        criteria = criteria == null ? null : clearSearchCriteria(criteria);

        //init the wizard props when we enter the wizard
        setWizardProps({
            currentPage: _currentPage.id,
            mode: null, profile: null, profileId: null, parentId: null,
            searchCriteria: criteria
        });
        */
        setWizardProps({
            currentPage: _currentPage.id,
            mode: null, profile: null, profileId: null, parentId: null
        });

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||wizardProps||Cleanup', CLASS_NAME));
            //setFilterValOnChild('');
        };
    }, [wizardProps.currentPage, loadingProps.searchCriteriaRefreshed]);

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderTileButton = (pg, welcomeContent, href) => {
        return (
            <div className="col-md-4 d-flex" >
                <div className={`card d-flex flex-column h-100`} >
                    <div className={`card-body p-4 pt-3 d-flex flex-column h-100`} >
                        <div className="">
                            <h2 className="font-weight-bold">{pg.caption}</h2>
                            {welcomeContent}
                        </div>
                        <div className="mt-auto mx-auto">
                            <Button variant="secondary" type="button" className="m-auto auto-width" href={href} >{pg.caption}</Button>
                        </div>
                    </div>
                </div>
            </div>
        );
    };

    const renderWelcomeContentCreate = (pg) => {
        return (
            <>
                <p>Create a new profile & type definition - start from scratch.</p>
                {pg.introContent}
                <ul className="p-0 pl-3 mb-0">
                    <li>Step 1 - Create a new profile</li>
                    <li>Step 2 - Select dependent profiles</li>
                    <li>Step 3 - Select base type to extend</li>
                    <li>Step 4 - Save newly extended type </li>
                </ul>
            </>
        );
    };

    const renderWelcomeContentImport = (pg) => {
        return (
            <>
                <p>
                    Import profile(s) (including any dependent profiles) built outside of this tool using the 'Import' button.
                    If you are the profile author, you can edit and add types to the imported profile.
                </p>
                
                <ul className="p-0 pl-3 mb-0">
                    <li>Step 1 - Import one or many profiles (including dependent profiles)</li>
                    <li>Step 2 - Select the profile to associate with the type definition you will create in the final step</li>
                    <li>Step 3 - Select base type to extend</li>
                    <li>Step 4 - Save newly extended type </li>
                </ul>
            </>
        );
    };

    const renderWelcomeContentContinue = (pg) => {
        return (
            <>
                <p>Already have a profile created or imported? Use this path to select that profile and continue building types.</p>
                <ul className="p-0 pl-3 mb-0">
                    <li>Step 1 - Select the profile to associate with the type definition you will create in the final step</li>
                    <li>Step 2 - Select base type to extend</li>
                    <li>Step 3 - Save newly extended type </li>
                </ul>
            </>
        );
    };

    const renderMainContent = () => {
        var pgCreate = WizardSettings.panels.find(p => { return p.id === 'CreateProfile'; });
        var pgImport = WizardSettings.panels.find(p => { return p.id === 'ImportProfile'; });
        var pgSelect = WizardSettings.panels.find(p => { return p.id === 'SelectExistingProfile'; });

        return (
            <>
                <div className="row mb-3">
                    {renderTileButton(pgCreate, renderWelcomeContentCreate(pgCreate), '/wizard/create-profile')}
                    {renderTileButton(pgImport, renderWelcomeContentImport(pgImport), '/wizard/import-profile')}
                    {renderTileButton(pgSelect, renderWelcomeContentContinue(pgSelect), '/wizard/select-existing-profile')}
                </div>
                <div className="row mb-3">
                    {renderAboutProfileLibrary()}
                    {renderAboutTypeLibrary()}
                </div>
            </>
        );
    };

    const renderTileWideButton = (caption, content, href, captionLink, variant = 'secondary') => {
        return (
            <div className="col-sm-6 d-flex" >
                <div className={`card d-flex flex-column h-100`} >
                    <div className={`card-body p-4 pt-3 d-flex flex-column h-100`} >
                        <div className="">
                            <h2>{caption}</h2>
                            {content}
                        </div>
                        <div className="mt-auto mx-auto">
                            <Button variant={variant} type="button" className="m-auto auto-width" href={href} >{captionLink}</Button>
                        </div>
                    </div>
                </div>
            </div>
        );
    };

    const renderAboutTypeLibrary = () => {
        var content = (
            <p className="mb-0">
                The <strong>Type Library</strong> stores all types associated with profiles created or imported into the designer.
                You can quickly access, view and edit types. Use the type library built in filtering tools to find specific type definitions relevant to you.
                Type definitions you authored can be edited from here and the Extend button allows you to extend existing types and build your own. 
            </p>
        );
        return renderTileWideButton("About Type Library", content, '/types/library', "Go to Type Library", "primary");
    };

    const renderAboutProfileLibrary = () => {
        var content = (
            <p className="mb-0">
                The <strong>Profile Library</strong> stores all profiles imported or created in the system. A profile contains a collection of
                type definitions which are located in the type library.
                Profiles are either standard OPC UA nodeset profiles (ie. http://opcfoundation.org/UA/, http://opcfoundation.org/UA/DI),
                other standard OPC UA nodeset profiles (ie. http://opcfoundation.org/UA/Robotics/) or custom profiles you have created or imported into the designer.
                Manage profiles by visiting the <strong>Profile Library</strong>.
            </p>
        );
        return renderTileWideButton("About Profile Library", content, '/profiles/library', "Go to Profile Library", "primary");
    };


    const renderAboutProfileDesigner = () => {
        return null;

        return (
            <div className="row mb-3">
                <div className="col-sm-12">
                    <div className="card">
                        <div className="card-body">
                            <h2 className="headline-3">About Profile Designer</h2>
                            <ul>
                                <li>For documentation click here</li>
                                <li>In other news...</li>
                                <li>In other news...</li>
                            </ul>
                        </div>
                    </div>
                </div>
            </div>
        );
    };

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Helmet>
                <title>{AppSettings.Titles.Main + " | " + _currentPage.caption}</title>
            </Helmet>
            {renderWizardHeader(_currentPage.caption)}
            {renderWizardIntroContent(_currentPage.introContent)}
            {renderMainContent()}
            {renderAboutProfileDesigner()}
        </>
    )
}

export default WizardWelcome;