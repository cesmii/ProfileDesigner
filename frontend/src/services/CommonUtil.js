import ConfirmationModal from "../components/ConfirmationModal";
import color from "../components/Constants";

//-------------------------------------------------------------------
// Region: Common Render Methods
//-------------------------------------------------------------------
//render ok as a modal to force user to say ok.
//shield from external calls. Create wrapper calls to simplify the calling code
const renderOKModal = (modalData) => {

    if (!modalData.show) return null;

    return (
        <ConfirmationModal showModal={modalData.show} caption={modalData.caption} message={modalData.message} msgId={modalData.msgId}
            //icon={{ name: "warning", color: color.trinidad }}
            icon={{ name: modalData.iconName, color: modalData.color }}
            confirm={null}
            cancel={{
                caption: modalData.captionOK,
                callback: (e) => {
                    if (modalData.callback) modalData.callback(e);
                },
                buttonVariant: modalData.severity //'danger'
            }} />
    );
};

//render error message as a modal to force user to say ok.
export function ErrorModal(props) {

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    props.modalData.iconName = null ?? "warning";
    props.modalData.severity = null ?? "danger";
    props.modalData.captionOK = null ?? "OK";
    props.modalData.color = null ?? color.trinidad;
    props.modalData.callback = props.callback;
    props.modalData.msgId = null ?? props.msgId;
    return (
        renderOKModal(props.modalData)
    )
}

//render info message as a modal to force user to say ok.
export function InfoModal(props) {

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    props.modalData.iconName = null ?? "info";
    props.modalData.severity = null ?? "";
    props.modalData.captionOK = null ?? "OK";
    props.modalData.color = null ?? color.primary;
    props.modalData.callback = props.callback;
    props.modalData.msgId = null ?? props.msgId;
    return (
        renderOKModal(props.modalData)
    )
}
