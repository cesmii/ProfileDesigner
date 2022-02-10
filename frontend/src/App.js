import { BrowserRouter as Router } from "react-router-dom"
import { Helmet } from "react-helmet"
import { ErrorBoundary } from 'react-error-boundary'
import axios from "axios"
import axiosInstance from './services/AxiosService'

import { useAuthDispatch } from "./components/authentication/AuthContext";
import { useLoadingContext } from "./components/contexts/LoadingContext";
import Navbar from './components/Navbar'
import { LoadingOverlay } from "./components/LoadingOverlay"
import MainContent from './components/MainContent'
import Footer from './components/Footer'
import { AppSettings } from './utils/appsettings'
import { generateLogMessageString } from './utils/UtilityService'
import { logout } from './components/authentication/AuthActions'
import ErrorPage from './components/ErrorPage'
import { OnLookupLoad } from './components/OnLookupLoad'

import './App.scss';

const CLASS_NAME = "App";

function App() {
    //console.log(generateLogMessageString(`init || ENV || ${process.env.NODE_ENV}`, CLASS_NAME));
    //console.log(generateLogMessageString(`init || API || ${process.env.REACT_APP_BASE_API_URL}`, CLASS_NAME));

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const dispatch = useAuthDispatch() //get the dispatch method from the useDispatch custom hook
    const { setLoadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    //  TBD - is this the best place for this? 
    //  If a network error occurs (ie API not there), catch it here and handle gracefully.  
    //-------------------------------------------------------------------
    const OnApiResponseError = (error) => {
        //session expired - go to login
        if (error.response && error.response.status === 401) {
            console.log(generateLogMessageString(`axiosInstance.interceptors.response||error||${error.response.status}||${error.config.baseURL}${error.config.url}`, CLASS_NAME));
            setLoadingProps({ isLoading: false, message: null, isImporting: false });
            //updates state and removes user auth ticket from local storage
            let logoutAction = logout(dispatch);
            if (!logoutAction) {
                console.error(generateLogMessageString(`axiosInstance.interceptors.response||An error occurred setting the logout state.`, CLASS_NAME));
            }
        }
        //no status is our only indicator the API is not up and running
        else if (!error.status) {
            console.log(generateLogMessageString(`axiosInstance.interceptors.response||error||${error.config.baseURL}${error.config.url}||${error}`, CLASS_NAME));
            if (error.message && error.message.toLowercase().indexOf('request aborted') > -1) {
                //do nothing...
            }
            else {
                // API unavailable network error
                setLoadingProps({
                    isLoading: false, message: null, isImporting: false, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: 'A system error has occurred. Please contact your system administrator.', isTimed: true }]
                });
            //for now, don't log user out because it creates more confusion
            //updates state and removes user auth ticket from local storage
            //let logoutAction = logout(dispatch);
            //if (!logoutAction) {
            //    console.error(generateLogMessageString(`axiosInstance.interceptors.response||An error occurred setting the logout state.`, CLASS_NAME));
            //}

            }
        }
    };

    //Catch exceptions in the flow when we use our axiosInstance
    axiosInstance.interceptors.response.use(
        response => {
            return response
        },
        error => {
            OnApiResponseError(error);
            return Promise.reject(error)
        }
    )

    //Catch exceptions in the flow when we use axios not as part of our axiosInstance
    axios.interceptors.response.use(
        response => {
            return response
        },
        error => {
            OnApiResponseError(error);
            return Promise.reject(error)
        }
    )

    //-------------------------------------------------------------------
    // Region: hooks - moved into separate component
    // useEffect - get various lookup data - onlookupLoad component houses the useEffect checks
    //-------------------------------------------------------------------
    OnLookupLoad();

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    return (

    <div>
        <Helmet>
            <title>{AppSettings.Titles.Main}</title>
            <link href="https://fonts.googleapis.com/css2?family=Material+Icons" rel="stylesheet"></link>
        </Helmet>
        <Router>
            <Navbar />
            <LoadingOverlay />
                <ErrorBoundary
                    FallbackComponent={ErrorPage}
                    onReset={() => {
                        // reset the state of your app so the error doesn't happen again
                    }}
                >
                    <div id="--cesmii-content-wrapper">
                        <MainContent />
                    </div>
                </ErrorBoundary>
            <Footer />
        </Router>
    </div>
    
  );
}

export default App;
