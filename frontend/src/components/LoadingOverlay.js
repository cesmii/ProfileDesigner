import { generateLogMessageString } from "../utils/UtilityService";
import { useLoadingContext } from "./contexts/LoadingContext";
import './styles/Loading.scss';

const CLASS_NAME = "LoadingOverlay";

function LoadingOverlay() {
    const { loadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Region: Render helpers
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

    const renderMessage = () => {
        if (loadingProps.message == null || loadingProps.message === "") return;
        return (
            <div className="preloader msg mt-5">
                <p className="msg-text text-center m0 p1" dangerouslySetInnerHTML={{ __html: loadingProps.message }} />
            </div>
        );
    };

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
            {renderMessage()}
        </>
    )
}

export { LoadingOverlay };
