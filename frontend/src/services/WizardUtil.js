import { Breadcrumb, Button } from "react-bootstrap";

//-------------------------------------------------------------------
// Region: Common Render Methods
//-------------------------------------------------------------------
export const WizardSettings = {
    mode: {
        CreateProfile: 1,
        ImportProfile: 2,
        SelectProfile: 3
    },
    //unique to each mode
    breadcrumbs: [
        {
            modeId: 1, items: [
                { pageId: 'Welcome', stepNum: 0, caption: 'Start Over', href: '/wizard/welcome' },
                { pageId: 'CreateProfile', stepNum: 1, caption: 'Create Profile', captionNext: `Next`, href: '/wizard/create-profile' },
                { pageId: 'FilterProfile', stepNum: 2, caption: 'Select Profile Filter(s)', captionNext: `Next`, href: '/wizard/filter-profile' },
                { pageId: 'SelectBaseType', stepNum: 3, caption: 'Select Base Type To Extend', captionNext: `Next`, href: '/wizard/select-base-type' },
                { pageId: 'ExtendBaseType', stepNum: 4, caption: 'Extend Base Type & Save', href: null }
            ]
        },
        {
            modeId: 2, items: [
                { pageId: 'Welcome', stepNum: 0, caption: 'Start Over', href: '/wizard/welcome' },
                { pageId: 'ImportProfile', stepNum: 1, caption: 'Import Profile', captionNext: `Next`, href: '/wizard/import-profile' },
                { pageId: 'SelectProfile', stepNum: 2, caption: 'Select Base Profile', captionNext: `Next`, href: '/wizard/select-profile' },
                { pageId: 'FilterProfile', stepNum: 3, caption: 'Select Profile Filter(s)', captionNext: `Next`, href: '/wizard/filter-profile' },
                { pageId: 'SelectBaseType', stepNum: 4, caption: 'Select Base Type To Extend', captionNext: `Next`, href: '/wizard/select-base-type' },
                { pageId: 'ExtendBaseType', stepNum: 5, caption: 'Extend Base Type & Save', href: null }
            ]
        },
        {
            modeId: 3, items: [
                { pageId: 'Welcome', stepNum: 0, caption: 'Start Over', href: '/wizard/welcome' },
                { pageId: 'SelectExistingProfile', stepNum: 1, caption: 'Select Existing Profile', captionNext: `Next`, href: '/wizard/select-existing-profile' },
                { pageId: 'FilterProfile', stepNum: 2, caption: 'Select Profile Filter(s)', captionNext: `Next`, href: '/wizard/filter-profile' },
                { pageId: 'SelectBaseType', stepNum: 3, caption: 'Select Base Type To Extend', captionNext: `Next`, href: '/wizard/select-base-type' },
                { pageId: 'ExtendBaseType', stepNum: 4, caption: 'Extend Base Type & Save', href: null }
            ]
        }
    ]
    , panels: [
        {
            id: 'Welcome', caption: `Welcome to the Smart Manufacturing Profile™ Designer`,
            introContent: (
                <p className="p-0 py-1 mb-2 lh-intro" >
                    An SM Profile defines the Information Model for a manufacturing asset or process, with a goal to arrive at common, re-usable interfaces for accessing data.<br/>
                    As a key design principal, all SM Profiles should derive from (extend, or re-use) other SM Profiles. You can start with core and standardized building blocks provided by the OPC Foundation, or import a starting point provided by created by another designer, standards group or organization.
                </p>
            )
        },
        {
            id: 'CreateProfile', caption: `Create Profile`,
            introContent: (
                <p>Enter basic profile meta information to create your profile. The Type definition created in the final step will be associated with this profile. </p>
            )
        },
        {
            id: 'ImportProfile', caption: `Import Profiles`,
            introContent: (
                <>
                    <p>If you are the SM Profile author, import your profiles (including any dependent profiles) using the 'Import' button. The import will tag you as the author for your profiles and permit you to edit them. The import will check to ensure referenced type models are valid OPC UA type models.</p>
                    <p>Any dependent profiles (OPC UA type models) that are imported will become read-only and added to the Profile Library. Type definitions within these dependent profiles can be viewed or extended to make new Type definitions, which can become part of one of your SM Profiles.</p>
                </>
            )
        },
        {
            id: 'SelectExistingProfile', caption: `Select Existing Profile`,
            introContent: (
                <p>Select an existing profile to associate with the type definition you will create in the final step of the wizard. Tap on the row to select an existing profile.</p>
            )
        },
        {
            id: 'SelectProfile', caption: `Select Imported Profile`,
            introContent: (
                <p>Select an existing profile to associate with the type definition you will create in the final step of the wizard. Tap on the row to select an existing profile.</p>
            )
        },
        {
            id: 'FilterProfile', caption: `Select Profile Filter(s)`,
            introContent: (
                <p>When creating your type definition, the type definition will be extended from a base type.
                    To make finding the right base type easier, select one or several profiles below to use as a filter
                    on the 'Select Base Type' screen.</p>
            )
        },
        {
            id: 'SelectBaseType', caption: `Select Base Type To Extend`,
            introContent: (
                <p>To help you with an easy path to a new type, use the filters below to search for existing types to extend. Tap on the row to select.</p>
            )
        },
        {
            id: 'ExtendBaseType', caption: `Extend Base Type`,
            introContent: (
                <p></p>
            )
        }
    ]
};

//-------------------------------------------------------------------
// Region: Common Render Methods
//-------------------------------------------------------------------
export const renderWizardHeader = (caption) => {
    return (
        <div className="row">
            <div className="col-lg-12 d-flex">
                <h1>{caption}</h1>
            </div>
        </div>
    );
};

export const renderWizardIntroContent = (content) => {
    return (
        <div className="header-actions-row mb-3 pr-0">
            {content}
        </div>
    );
}


export const getWizardNavInfo = (modeId, pageId) => {
    //get the breadcrumbs for this mode.
    //get the index of the current step - based on url match
    //get the previous item just before the current step
    const breadcrumb = WizardSettings.breadcrumbs.find(x => { return x.modeId === modeId; });
    if (breadcrumb == null || breadcrumb.items == null) return null;

    //get breadcrumb index based on pageId
    var iCurrent = breadcrumb.items.findIndex(x => { return x.pageId === pageId; });
    if (iCurrent === -1) {
        return { stepNum: 0, prev: null, next: null};
    }
    var stepNum = breadcrumb.items[iCurrent].stepNum
    var prev = breadcrumb.items[iCurrent - 1];
    var next = breadcrumb.items[iCurrent + 1];
    return { stepNum: stepNum, prev: prev, next: next };
};

export const renderWizardButtonRow = (navInfo) => {
    return (
        <div className="row pb-3">
            <div className="col-12 d-flex" >
                <a className="mb-2 auto-width btn btn-secondary d-flex align-items-center" href={navInfo.prev.href} ><i className="material-icons mr-1">{navInfo.prev.icon == null ? "arrow_left" : navInfo.prev.icon}</i>{navInfo.prev.caption}</a>
                <Button variant="primary" type="button" className="mb-2 ml-auto auto-width d-flex align-items-center" onClick={navInfo.next.callbackAction} >{navInfo.next.captionNext != null ? navInfo.next.captionNext : navInfo.next.caption}<i className="material-icons">{navInfo.next.icon == null ? "arrow_right" : navInfo.next.icon}</i></Button>
            </div>
        </div>
    );
};

export const renderWizardButtonRow_OLD = (prevInfo, nextInfo) => {
    return (
        <div className="row pb-3">
            <div className="col-12 d-flex" >
                <a className="mb-2 auto-width btn btn-secondary d-flex align-items-center" href={prevInfo.url} ><i className="material-icons mr-1">{prevInfo.icon == null ? "arrow_left" : prevInfo.icon}</i>{prevInfo.caption}</a>
                <Button variant="secondary" type="button" className="mb-2 ml-auto auto-width d-flex align-items-center" onClick={nextInfo.callbackAction} >{nextInfo.caption}<i className="material-icons">{nextInfo.icon == null ? "arrow_right" : nextInfo.icon}</i></Button>
            </div>
        </div>
    );
};

export const renderWizardBreadcrumbs = (modeId, currentStepNum) => {

    const breadcrumb = WizardSettings.breadcrumbs.find(x => { return x.modeId === modeId; });
    if (breadcrumb == null || breadcrumb.items == null) return;

    const contentItems = breadcrumb.items.map((item) => {
        if (item.stepNum === 0) {
            return (
                <Breadcrumb.Item key={`${modeId}.${item.stepNum}`} href={item.href} title={item.caption} ><i className="material-icons">home</i></Breadcrumb.Item >
            );
        }
        if (item.stepNum === currentStepNum) {
            return (
                <Breadcrumb.Item key={`${modeId}.${item.stepNum}`} className="font-weight-bold" active >{ item.caption }</Breadcrumb.Item >
            );
        }
        else if (item.stepNum < currentStepNum) {
            return (
                <Breadcrumb.Item key={`${modeId}.${item.stepNum}`} href={item.href}>{item.caption}</Breadcrumb.Item>
            );
        }
        else {
            return (
                <Breadcrumb.Item key={`${modeId}.${item.stepNum}`} active >{item.caption}</Breadcrumb.Item>
            );
        }
    });

    return (
        <Breadcrumb>
            {contentItems}
        </Breadcrumb>
    );
};

