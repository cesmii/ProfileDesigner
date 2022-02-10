import React, { createContext, useContext, useReducer } from "react";
import { AuthReducer, initialState } from "./AuthReducer";
import { generateLogMessageString } from '../../utils/UtilityService'

const CLASS_NAME = "AuthContext";

const AuthStateContext = createContext();
const AuthDispatchContext = createContext();

export function useAuthState() {
    //console.log(generateLogMessageString(`useAuthState`, CLASS_NAME));
    const context = useContext(AuthStateContext);
    if (context === undefined) {
        console.error(generateLogMessageString(`useAuthState||useAuthState must be used within an AuthProvider`, CLASS_NAME, 'critical'));
        throw new Error("useAuthState must be used within an AuthProvider");
    }

    return context;
}

export function useAuthDispatch() {
    //console.log(generateLogMessageString(`useAuthDispatch`, CLASS_NAME));
    const context = useContext(AuthDispatchContext);
    if (context === undefined) {
        console.error(generateLogMessageString(`useAuthDispatch||useAuthDispatch must be used within an AuthProvider`, CLASS_NAME, 'critical'));
        throw new Error("useAuthDispatch must be used within an AuthProvider");
    }

    return context;
}

export const AuthContextProvider = ({ children }) => {
    const [authTicket, dispatch] = useReducer(AuthReducer, initialState);

    return (
        <AuthStateContext.Provider value={authTicket}>
            <AuthDispatchContext.Provider value={dispatch}>
                {children}
            </AuthDispatchContext.Provider>
        </AuthStateContext.Provider>
    );
};

//import React, { createContext, useContext, useReducer, useEffect } from "react";
//import { generateLogMessageString } from '../../utils/UtilityService'

//const CLASS_NAME = "AuthContext";

//let reducer = (authTicket, newAuthTicket) => {
//    console.log(generateLogMessageString(`reducer||authTicket||${newAuthTicket == null || newAuthTicket.token == null ? 'null' : newAuthTicket.token.substring(newAuthTicket.token.length - 60) }`, CLASS_NAME));
//    //remove if null
//    if (newAuthTicket == null) {
//        localStorage.removeItem("authTicket");
//        return null;
//    }
//    return { ...authTicket, ...newAuthTicket };
//};

//export const AuthContext = createContext();

//export function useAuthContext() {
//    //console.log('useAuth');
//    return useContext(AuthContext);
//};

//const localState = () => {
//    var result = localStorage.getItem("authTicket");
//    if (result === "undefined" || result == null) return null;
//    return JSON.parse(result);
//};

//export function AuthContextProvider(props) {
//    const [authTicket, setAuthTicket] = useReducer(reducer, localState() || null);

//    useEffect(() => {
//        console.log(generateLogMessageString(`useEffect||authTicket||${authTiauthTicketll || authTicket.authTicketull ? 'null' : authTicket.token.substring(authTicket.token.length - 60)}`, CLASS_NAME));
//        if (authTicket == null) {
//            localStorage.removeItem("authTicket");
//        }
//        else {
//            localStorage.setItem("authTicket", JSON.stringify(authTicket));
//        }

//    }, [authTicket]);

//    return (
//        <AuthContext.Provider value={{ authTicket, setAuthTicket }}>
//            {props.children}
//        </AuthContext.Provider>
//    );
//}

