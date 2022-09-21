import React, { useEffect }  from 'react'
import { Helmet } from "react-helmet"
import { useIsAuthenticated, useMsal } from "@azure/msal-react";

import { Button } from 'react-bootstrap'

import { useLoadingContext } from '../components/contexts/LoadingContext';
import { useWizardContext } from '../components/contexts/WizardContext';
import { AppSettings } from '../utils/appsettings'
import { generateLogMessageString } from '../utils/UtilityService';
import { renderWizardHeader, renderWizardIntroContent, WizardSettings } from '../services/WizardUtil';
import LoginButton from '../components/LoginButton';

const CLASS_NAME = "WizardWelcome";

function WizardWelcome() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { instance } = useMsal();
    const _isAuthenticated = useIsAuthenticated();
    const _activeAccount = instance.getActiveAccount();

    const { loadingProps } = useLoadingContext();
    const { wizardProps, setWizardProps } = useWizardContext();
    const _currentPage = WizardSettings.panels.find(p => { return p.id === 'Welcome'; });

    //check for logged in status on this welcome page, redirect to public facing login if no active account
    if (_isAuthenticated && _activeAccount == null) {
        //MSAL logout
        instance.logout();
    }

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

    }, [wizardProps.currentPage, loadingProps.searchCriteriaRefreshed]);

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderTileButton = (pg, content, href) => {
        return (
            <div className="col-sm-6 d-flex" >
                <div className={`card d-flex flex-column h-100`} >
                    <div className={`card-body p-4 pt-3 d-flex flex-column h-100`} >
                        <div className="">
                            <h2 className="font-weight-bold">{pg.caption}</h2>
                            {content}
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
                    Import starting point or building block Profiles using the 'Import' button.
                    If you are the profile author, you can edit and add types to the imported profile, in all other cases, you will extend or re-use the starting point Profiles.
                </p>
                
                <ul className="p-0 pl-3 mb-0">
                    <li>Step 1 - Import one or many profiles you want to re-use or extend</li>
                    <li>Step 2 - Find a Type to start from in the <strong>Type Library</strong></li>
                    <li>Step 3 - Select Type to extend</li>
                    <li>Step 4 - Save the newly extended Type to your own Profile</li>
                </ul>
            </>
        );
    };

    const renderWelcomeContentContinue = (pg) => {
        return (
            <>
                <p>Your Profiles contain the Type definitions and extensions that you have created. You can import from file, or continue work you've previously started in this tool.</p>
                <ul className="p-0 pl-3 mb-0">
                    <li>Step 1 - Find your Profile in the <strong>Profile Library</strong></li>
                    <li>Step 2 - Select base type to extend</li>
                    <li>Step 3 - Save the newly extended Type to your own Profile</li>
                </ul>
            </>
        );
    };

    const renderMainContent = () => {
        //var pgCreate = WizardSettings.panels.find(p => { return p.id === 'CreateProfile'; });
        let pgImport = WizardSettings.panels.find(p => { return p.id === 'ImportProfile'; });
        let pgSelect = WizardSettings.panels.find(p => { return p.id === 'SelectExistingProfile'; });

        return (
            <>
                <div className="row mb-3">
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

    ///Public facing content to display before user is logged in
    const renderMainContentPublic = () => {

        const bgImageStyle1 = {
            backgroundImage: "url(/img/sm-platform.jpg)"
        };
        const bgImageStyle2 = {
            backgroundImage: "url(/img/sm-worker-landscape.jpg)"
        };

        return (
            <>
                <div className={`row mx-0 p-0 pt-4 mb-5`}>
                    <div className="col-sm-6 col-md-5 p-0 d-none d-sm-block" >
                        <div className="image-bg" >
                            <div className="overlay-icon cover rounded shadow" style={bgImageStyle1} >&nbsp;</div>
                        </div>
                    </div>
                    <div className="col-sm-6 col-md-7 px-0 pl-5" >
                        <h1>Welcome!</h1>
                        <p className="p-0 py-1 mb-2 lh-intro" >
                            SM Profiles are an innovative way of representing data in structured information models that provide
                            the ability to move "data-in-context" from source to consumption, and between components that
                            consume the data to provide a solution. Developers and end-users will adapt or customize the
                            information model with constructs that are specific to a particular domain, platform or application.
                            In other words, a profile is a digital extension mechanism to seamlessly connect, collect,
                            analyze and act at the edge, the cloud and in the Apps that connect to the SM Innovation Platform.
                            Profiles will be crowd sourced from industry.  Machine Builders, System Integrators, Product Vendors
                            in all shapes and sizes and even you, whoever you are, will be able to create profiles and submit
                            them to the CESMII Smart Manufacturing Marketplace, for others to download and use in their systems.
                        </p>
                        <div className="d-flex mt-auto mx-auto">
                            <LoginButton />
                        </div>
                        <p className="mt-3 mb-0 text-center" >
                            <span className="font-weight-bold mr-1" >Don't have an account?</span>
                            Email us at <a href="mailto:devops@cesmii.org" >devops@cesmii.org</a> to get registered.
                            Please provide your project name or SOPO number with your request.
                        </p>
                    </div>
                </div>
                <div className={`row mx-0 p-0 mb-5`}>
                    <div className="col-sm-6 col-md-7 px-0 pr-5" >
                        <h2>How it Works</h2>
                        <p className="p-0 py-1 mb-2 lh-intro" >
                            The SM Profile Designer &trade; allows disparate manufacturers and engineers to build manufacturing profiles that could be shared amongst a community of smart manufacturing entities. The profile is a class definition (or collection of class definitions) describing a piece of manufacturing equipment (or conceivably, a manufacturing process or manufactured good). Profiles have relationships to other profiles within the scope of the user's work context. These relationships are of the kinds typically seen in a UML (Unified Modeling Language) diagram, including inheritance, aggregation (or composition), interface implementation, and dependency.
                        </p>
                    </div>
                    <div className="col-sm-6 col-md-5 p-0 d-none d-sm-block" >
                        <div className="image-bg" >
                            <div className="overlay-icon cover rounded shadow" style={bgImageStyle2} >&nbsp;</div>
                        </div>
                    </div>
                </div>
                <div className={`row mx-0 p-0 pt-2 pb-5 mb-5 d-none d-lg-block`}>
                    <div className="col-12 p-0" >
                        <div className="d-flex image-bg" >
                            <img className="mx-auto rounded shadow mw-100 h-auto" src="/img/sm-profile-diagram.jpg" alt="sm-profile-diagram" />
                        </div>
                    </div>
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
        let content = (
            <p className="mb-0">
                The <strong>Type Library</strong> stores all the Type definitions associated with the SM Profiles available in your workspace.<br/>
                From here, you can quickly search for, view and extend any Type definition. You can <em>edit</em> Type definitions that belong to one of your Profiles, but you cannot edit Type definitions that were created by someone else -- to customize those, you must <em>extend</em> the Type.<br/>
                The type library has powerful filtering tools that can help you find the right starting point for extension.
            </p>
        );
        return renderTileWideButton("About Type Library", content, '/types/library', "Go to Type Library", "primary");
    };

    const renderAboutProfileLibrary = () => {
        let content = (
            <p className="mb-0">
                The <strong>Profile Library</strong> stores all SM Profiles available in your workspace. You can add to your workspace by importing or creating SM Profiles.<br/>
                Each SM Profile contains a collection of Type definitions -- you can find them in the Type Library.<br/>
                Profiles may be standardized <a href="http://opcfoundation.org/UA/">OPC UA Nodesets</a>, <a href="https://opcfoundation.org/about/opc-technologies/opc-ua/ua-companion-specifications/">Companion Specification</a> Nodesets, such as those created by formal working groups.
                or custom profiles you have created or imported into your workspace.<br/>
                You can manage your workspace in the <strong>Profile Library</strong>.
            </p>
        );
        return renderTileWideButton("About Profile Library", content, '/profiles/library', "Go to Profile Library", "primary");
    };

    const renderAboutProfileDesigner = () => {
        return null;

    /*
        return (
            <div className="row mb-3">
                <div className="col-sm-12">
                    <div className="card">
                        <div className="card-body">
                            <h2 className="headline-3">About SM Profile Designer</h2>
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
    */
    };

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Helmet>
                <title>{AppSettings.Titles.Main + " | " + _currentPage.caption}</title>
            </Helmet>
            {_isAuthenticated 
                ? <>
                {renderWizardHeader(_currentPage.caption)}
                {renderWizardIntroContent(_currentPage.introContent)}
                {renderMainContent()}
                {renderAboutProfileDesigner()}
                </>
                : <>
                {renderMainContentPublic()}
                </>
            }
        </>
    )
}

export default WizardWelcome;