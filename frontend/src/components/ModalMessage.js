import React from 'react'
import { ErrorModal, InfoModal } from '../services/CommonUtil';
import { AppSettings } from '../utils/appsettings';
import { generateLogMessageString } from '../utils/UtilityService';
import { useLoadingContext } from './contexts/LoadingContext';

const CLASS_NAME = "ModalMessage";

function ModalMessage() { //props are item, showActions
    
    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { loadingProps, setLoadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onDismiss = (e) => {
        console.log(generateLogMessageString('onDismiss||', CLASS_NAME));
        var id = e.currentTarget.getAttribute("data-id");
        dismissMessage(id);
    };

    const dismissMessage = (msgId, warnOnNotFound = false) => {
        var x = loadingProps.modalMessages?.findIndex(msg => { return msg.id.toString() === msgId; });
        //no item found
        if (x < 0 && warnOnNotFound) {
            console.warn(generateLogMessageString(`dismissMessage||no item found to dismiss with this id`, CLASS_NAME));
            return;
        }
        //delete the message
        loadingProps.modalMessages.splice(x, 1);
        //update state
        setLoadingProps({ modalMessages: JSON.parse(JSON.stringify(loadingProps.modalMessages)) });
    }

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    //TBD - check for dup messages and don't show.
    const renderMessages = loadingProps.modalMessages?.map((msg) => {

        const modalData = { show: true, caption: msg.caption ?? AppSettings.Titles.Caption, message: msg.body };
        if (msg.severity === 'error' || 'critical' || 'danger') {
            return <ErrorModal modalData={modalData} callback={onDismiss} msgId={msg.id} />
        }
        else {
            return <InfoModal modalData={modalData} callback={onDismiss} msgId={msg.id} />
        }

    });


    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    if (loadingProps == null || loadingProps.modalMessages == null || loadingProps.modalMessages.length === 0) return null;

    return (renderMessages);
};

export default ModalMessage;