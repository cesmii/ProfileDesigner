import { generateLogMessageString } from "../utils/UtilityService";
import { useLoadingContext } from "./contexts/LoadingContext";

import Button from 'react-bootstrap/Button'
import { LoadingIcon } from "./SVGIcon";

const CLASS_NAME = "InlineMessage";

function InlineMessage() {
    const { loadingProps, setLoadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const dismissMessage = (msgId, warnOnNotFound = false) => {
        var x = loadingProps.inlineMessages.findIndex(msg => { return msg.id.toString() === msgId; });
        //no item found
        if (x < 0 && warnOnNotFound) {
            console.warn(generateLogMessageString(`dismissMessage||no item found to dismiss with this id`, CLASS_NAME));
            return;
        }
        //delete the message
        loadingProps.inlineMessages.splice(x, 1);
        //update state
        setLoadingProps({ inlineMessages: JSON.parse(JSON.stringify(loadingProps.inlineMessages))});
    }

    const onDismiss = (e) => {
        console.log(generateLogMessageString('onDismiss||', CLASS_NAME));
        const id = e.currentTarget.getAttribute("data-id");
        dismissMessage(id);
    }

    const dismissMessageTimed = (msgId, ms = 6000) => {
        console.log(generateLogMessageString('dismissMessageTimed||', CLASS_NAME));
        setTimeout(() => {
            dismissMessage(msgId);
        }, ms);
    }

    //TBD - check for dup messages and don't show.
    const renderMessages = loadingProps.inlineMessages?.map((msg) => {
        //apply special handling for sev="processing"
        const isProcessing = msg.severity === "processing";
        const sev = msg.severity == null || msg.severity === "" || msg.severity === "processing" ? "info" : msg.severity;

        //dismiss the message on a timed basis - short if isTimed is true, 
        //long if isTimed is false - still get rid of it after awhile
        dismissMessageTimed(msg.id, msg.isTimed ? 6000 : 9000000); //6 seconds or 15 minutes

        return (
            <div key={"inline-msg-" + msg.id} className="row mb-2" >
            <div className={"col-sm-12 alert alert-" + sev + ""} >
                {(msg.hideDismissBtn == null || !msg.hideDismissBtn) &&
                    <div className="dismiss-btn">
                        <Button id={`btn-inline-msg-dismiss-${msg.id}`} variant="icon-solo square small" data-id={msg.id} onClick={onDismiss} className="align-items-center" ><i className="material-icons">close</i></Button>
                    </div>
                }
                <div className="text-center" >
                    {isProcessing &&
                        <LoadingIcon size="20" />
                    }
                    <span className={isProcessing ? 'ml-1' : ''} dangerouslySetInnerHTML={{ __html: msg.body }} />
                </div>
            </div>
            </div >
        )
    });

    //-------------------------------------------------------------------
    // Region: Render 
    //-------------------------------------------------------------------
    if (loadingProps == null || loadingProps.inlineMessages == null || loadingProps.inlineMessages.length === 0) return null;

    return (renderMessages);
}

export { InlineMessage };
