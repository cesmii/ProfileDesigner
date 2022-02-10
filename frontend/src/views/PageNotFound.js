import React from 'react'
import { Helmet } from 'react-helmet';

import { AppSettings } from '../utils/appsettings';
import { renderTitleBlock } from '../utils/UtilityService';

//TBD - add in some nicely formatted message
function PageNotFound() {

    const caption = "Page Not Found";

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
                        &nbsp; 
                    </div>
                </div>
            </div>
        </>
    )
}

export default PageNotFound