import React, { useContext, useReducer, useEffect } from "react";
import Button from 'react-bootstrap/Button'
import { generateLogMessageString } from '../../utils/UtilityService'
import Spinner from '../../components/Spinner'

let reducer = (info, newInfo) => {
    if (newInfo === null) {
        localStorage.removeItem("appContext");
        return initialState;
    }
    return { ...info, ...newInfo };
};

const initialState = {
    isLoading: false,
    message: null, 
    inlineMessages:null,
    recentFileList:[],
    favoritesList: [], 
    profileCount: {all: null, mine: null }
};

const localState = JSON.parse(localStorage.getItem("appContext"));

const LoadingContext = React.createContext();

export function useLoadingContext() {
    return useContext(LoadingContext);
}

function LoadingContextProvider(props) {
    const [loadingProps, setLoadingProps] = useReducer(reducer, localState || initialState);

    useEffect(() => {
        if (loadingProps == null) {
            localStorage.removeItem("appContext");
        }
        else {
            localStorage.setItem("appContext", JSON.stringify(loadingProps));
        }
    }, [loadingProps]);

    return (
        <LoadingContext.Provider value={{ loadingProps, setLoadingProps }}>
            {props.children}
        </LoadingContext.Provider>
    );
}

function LoadingUI(props) {
    if (props.loadingProps == null || props.loadingProps.isLoading === false) return null;

    var msg = props.loadingProps.message == null || props.loadingProps.message === "" ? "" :
        (<p className="text-center">{props.loadingProps.message}</p>);

    return (
        <div className="preloader">
            <Spinner />
            {msg}
        </div>
    )
}

function InlineMessageUI(props) {
    if (props.loadingProps == null || props.loadingProps.inlineMessages == null || props.loadingProps.inlineMessages.length === 0) return null;

    const onDismiss = (e) => {
        console.log(generateLogMessageString('onDismiss||', "InlineMessageUI"));
        var id = e.currentTarget.getAttribute("data-id");
        var x = props.loadingProps.inlineMessages.findIndex(msg => { return msg.id.toString() === id; });
        //no item found
        if (x < 0) {
            console.warn(generateLogMessageString(`onDismiss||no item found to dismiss with this id`, "InlineMessageUI"));
            return;
        }
        //delete the message
        props.loadingProps.inlineMessages.splice(x, 1);
        //raise event to parent so it knows of the delete
        props.onDismiss(JSON.parse(JSON.stringify(props.loadingProps.inlineMessages)));
    }

    console.log(generateLogMessageString('loading', "InlineMessageUI"));
    //TBD - check for dup messages and don't show.
    const mainBody = props.loadingProps.inlineMessages.map((msg) => {
        var sev = msg.severity == null || msg.severity === "" ? "info" : msg.severity;
        return (
            <div key={"inline-msg-" + msg.id} className={"alert alert-" + sev + " ml-5 mr-5 mt-3 mb-2"} >
                <div className="dismiss-btn">
                    <Button variant="icon-solo square" data-id={msg.id} onClick={onDismiss} className="align-items-center" ><i className="material-icons">close</i></Button>
                </div>
                <div className="text-center" >{msg.body}</div>
            </div>
        )
    });
    return (mainBody)
}


//-------------------------------------------------------------------
// Region: Update an array of recent links to use in the recent file list ui
//          Calling code will handle updating the context & storage
//-------------------------------------------------------------------
function UpdateRecentFileList(existingList, link) {
    if (existingList == null) existingList = [];

    //Check for dup entry.If dup, remove old one.
    var i = existingList.findIndex(x => x.url === link.url);
    //item found, remove it and then we re-add to top of list
    if (i >= 0) {
        existingList.splice(i, 1);
    }
    //add item to top of list
    var newList = [];
    newList.push(link);
    //TBD - limit to 20 most recent items - change this??
    //return new list
    return newList.concat(existingList.slice(0, existingList.length > 19 ? 19 : existingList.length));
}

//-------------------------------------------------------------------
// Region: Update an array of favorite links to use in the favorites list ui
//          This is a toggle. If it finds one, it removes. If not found, it adds. 
//          Calling code will handle updating the context & storage
//-------------------------------------------------------------------
function toggleFavoritesList(existingList, link) {
    if (existingList == null) existingList = [];

    //Check for existing entry.If dup, remove old one.
    var i = existingList.findIndex(x => x.url === link.url);
    //item found, remove it
    if (i >= 0) { existingList.splice(i, 1); }
    if (i < 0) { existingList.push(link) };
    return JSON.parse(JSON.stringify(existingList));
}
export { LoadingContext, LoadingContextProvider, LoadingUI, InlineMessageUI, UpdateRecentFileList, toggleFavoritesList};

