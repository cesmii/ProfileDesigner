import React from 'react'

import LoginButton from '../../components/LoginButton';

//const CLASS_NAME = "WelcomeContent";

function WelcomeContent(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: hooks
    //  trigger from some other component to kick off an import log refresh and start tracking import status
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
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
                    <div className="col-md-6 col-lg-7 p-0 d-none d-sm-block" >
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
                        <div className="image-bg" style={{ display: "none" }} >
                            <div className="overlay-icon cover rounded shadow" style={bgImageStyle1} >&nbsp;</div>
                        </div>
                    </div>
                    <div className="col-md-6 col-lg-5 px-0 pl-md-3 pl-lg-5" >
                        {props.showLogin &&
                            <LoginButton />
                        }
                        <p className="p-0 py-1 mb-2 lh-intro" style={{ display: "none" }} >
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
                    </div>
                </div>
                <div className={`row mx-0 p-0 mb-5`}>
                    <div className="col-sm-6 col-md-5 p-0 d-none d-sm-block pr-5" >
                        <div className="image-bg" >
                            <div className="overlay-icon cover rounded shadow" style={bgImageStyle2} >&nbsp;</div>
                        </div>
                    </div>
                    <div className="col-sm-6 col-md-7 px-0 " >
                        <h2>How it Works</h2>
                        <p className="p-0 py-1 mb-2 lh-intro" >
                            The SM Profile Designer &trade; allows disparate manufacturers and engineers to build manufacturing profiles that could be shared amongst a community of smart manufacturing entities. The profile is a class definition (or collection of class definitions) describing a piece of manufacturing equipment (or conceivably, a manufacturing process or manufactured good). Profiles have relationships to other profiles within the scope of the user's work context. These relationships are of the kinds typically seen in a UML (Unified Modeling Language) diagram, including inheritance, aggregation (or composition), interface implementation, and dependency.
                        </p>
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

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            {renderMainContentPublic()}
        </>
    )
}

export default WelcomeContent;