import { generateLogMessageString } from "../utils/UtilityService";
import { useLoadingContext } from "./contexts/LoadingContext";
import './styles/Loading.scss';

const CLASS_NAME = "LoadingOverlay";

//-------------------------------------------------------------------
// Region: Shared Render helpers
//-------------------------------------------------------------------
const renderSpinner = () => {
    return (
        // <div className="preloader">
        <div className="sk-folding-cube">
            <div className="sk-cube1 sk-cube"></div>
            <div className="sk-cube2 sk-cube"></div>
            <div className="sk-cube4 sk-cube"></div>
            <div className="sk-cube3 sk-cube"></div>
        </div>
        // </div>

    );
};

const renderMessage = (msg) => {
    if (msg == null || msg === "") return;
    return (
        <div className="preloader msg mt-5">
            <p className="msg-text text-center m0 p1" dangerouslySetInnerHTML={{ __html: msg }} />
        </div>
    );
};

function LoadingOverlay() {
    const { loadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Region: Render 
    //-------------------------------------------------------------------
    if (loadingProps == null || loadingProps.isLoading === false) return null;

    console.log(generateLogMessageString(`Show loading indicator...`, CLASS_NAME));

    return (
        <>
            <div className="preloader">
                {renderSpinner()}
            </div>
            {/*optional message to display below processing indicator */}
            {renderMessage(loadingProps.message)}
        </>
    )
}

function LoadingOverlayInline(props) {

    //-------------------------------------------------------------------
    // Region: hooks
    //-------------------------------------------------------------------
/*
    useEffect(() => {

        //check for logged in status - redirect to home page if already logged in.
        if (props.show) {
            history.push(returnUrl ? decodeURIComponent(returnUrl) : '/');
        }

    }, [props.show]);
*/
    //-------------------------------------------------------------------
    // Region: Render 
    //-------------------------------------------------------------------
    if (!props.show) return null;

    console.log(generateLogMessageString(`Show inline loading indicator...`, CLASS_NAME));

    return (
        <>
            <div className="preloader">
                {renderSpinner()}
            </div>
            {/*optional message to display below processing indicator */}
            {renderMessage(props.msg)}
        </>
    )
}

export { LoadingOverlay, LoadingOverlayInline };
