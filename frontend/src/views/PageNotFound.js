import React, { Fragment } from 'react'

import HeaderNav from '../components/HeaderNav'

//TBD - add in some nicely formatted message
function PageNotFound() {

    return (
        <Fragment>
            <HeaderNav caption="Page Not Found" showSearch={false} />
            <div id="--cesmii-main-content">
                <div id="--cesmii-left-content">
                    &nbsp;
                </div>
            </div>
        </Fragment>
    )
}

export default PageNotFound