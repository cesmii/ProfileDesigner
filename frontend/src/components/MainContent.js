import React from 'react'
import Routes from './Routes'

import { generateLogMessageString } from '../utils/UtilityService'
import { InlineMessageUI, useLoadingContext } from './contexts/LoadingContext'
import { ScrollToTop } from './ScrollToTop';

const CLASS_NAME = "MainContent";

function MainContent() {
    const { loadingProps, setLoadingProps } = useLoadingContext();
    //this will cause a scroll to top on route change. It should only run when the path name, route name changes.
    ScrollToTop();

    //This is raised after user clicks dismiss on the message. The passed in collection is 
    //a new copy of the messages list.
    const onDismissInlineMessage = (inlineMessages) => {
        console.log(generateLogMessageString(`onDismissInlineMessage`, CLASS_NAME));
        setLoadingProps({ inlineMessages: inlineMessages });
    };

    return (
        <div id="--routes-wrapper">
            <InlineMessageUI loadingProps={loadingProps} onDismiss={onDismissInlineMessage} ></InlineMessageUI>
            <Routes />
        </div>
    )
}

export default MainContent