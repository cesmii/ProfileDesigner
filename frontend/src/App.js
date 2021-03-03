import { useEffect } from 'react'
import { BrowserRouter as Router } from "react-router-dom"
import { Helmet } from "react-helmet"
import axios from 'axios'

//import Popper from 'popper.js';
//import Dropdown from 'bootstrap'

import Navbar from './components/Navbar'
import SideMenu from './components/SideMenu'
import MainContent from './components/MainContent'
import Footer from './components/Footer'
import { AppSettings } from './utils/appsettings'
import { generateLogMessageString } from './utils/UtilityService'
import { useAuthContext } from "./components/authentication/AuthContext";
import { LoadingUI, useLoadingContext } from "./components/contexts/LoadingContext";

import './App.scss';

const CLASS_NAME = "App";

function App() {
    console.log(generateLogMessageString(`init || ${process.env.NODE_ENV}`, CLASS_NAME));
    console.log(generateLogMessageString(`init || ${process.env.REACT_APP_BASE_API_URL}`, CLASS_NAME));
    //console.log(process.env);

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { authTicket } = useAuthContext();
    const { loadingProps, setLoadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Region: Event handlers
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchProfileCounts() {
            console.log(generateLogMessageString('useEffect||fetchProfileCountsOnLogin||async', CLASS_NAME));
            //in future system, a single endpoint would just return an object with all count and mine count.
            const result = await axios(`${AppSettings.BASE_API_URL}/profile/`);

            if (result.data != null) {
                var mine = result.data.filter((p) => {
                    return p.author != null && p.author.id === authTicket.user.id;
                });

                setLoadingProps({
                    profileCount: {
                        all: result.data != null ? result.data.length : null,
                        mine: mine.length
                    }
                });
            }
        }

        //initialize profile counts. Trigger when user logs in only
        if (authTicket != null && authTicket.user != null) {
            fetchProfileCounts();
        }
        else {
            setLoadingProps({profileCount: {all: null, mine: null}});
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [authTicket]);

    return (

    <div>
        <Helmet>
            <title>{AppSettings.Titles.Main}</title>
            <link href="https://fonts.googleapis.com/css2?family=Material+Icons" rel="stylesheet"></link>
        </Helmet>
        <Router>
            <Navbar />
            <LoadingUI loadingProps={loadingProps} ></LoadingUI>
            <div id="--cesmii-content-wrapper">
                {/*Only show the side menu if we are logged in*/}
                {(authTicket != null && authTicket.token != null) && <SideMenu />}
                <MainContent />
            </div>

            <Footer />
        </Router>
    </div>
    
  );
}

export default App;
