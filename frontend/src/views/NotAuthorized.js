import React from 'react'
import { useLocation } from 'react-router-dom';
import { Helmet } from 'react-helmet';

import { AppSettings } from '../utils/appsettings';
import { renderTitleBlock } from '../utils/UtilityService';

//TBD - add in some nicely formatted message
function NotAuthorized() {

    const location = useLocation();

    const caption = location.pathname.indexOf('/notpermitted') > -1 ? "Not Permitted" : "Not Authorized";
    const msg = location.pathname.indexOf('/notpermitted') > -1 ?
        "Restricted Area." :
        "Unauthorized Area.";

    const renderHeaderRow = () => {
        return (
            <div className="row pb-3">
                <div className="col-12 d-flex">
                    {renderTitleBlock(caption, null, null)}
                </div>
            </div>
        );
    };

    return (
        <>
            <Helmet>
                <title>{AppSettings.Titles.Main + " | " + caption}</title>
            </Helmet>
            {renderHeaderRow()}
            <div className="card p-4">
                <div className="row">
                    <div className="col-12">
                        <p className="fw-bold" >{ msg }</p>
                        <p>
                            Please re-check your account information or contact the system administrator about your account.
                        </p>
                        <p>
                        <a href="/" >Home</a>
                        </p>
                    </div>
                </div>
            </div>
        </>
    )
}

export default NotAuthorized