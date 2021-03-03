import React, { createContext, useContext, useReducer, useEffect } from "react";
import { generateLogMessageString } from '../../utils/UtilityService'

const CLASS_NAME = "AuthContext";

let reducer = (authTicket, newAuthTicket) => {
    console.log(generateLogMessageString(`reducer||authTicket`, CLASS_NAME));
    //remove if null
    if (newAuthTicket == null) {
        localStorage.removeItem("authTicket");
        return null;
    }
    return { ...authTicket, ...newAuthTicket };
};

export const AuthContext = createContext();

export function useAuthContext() {
    //console.log('useAuth');
    return useContext(AuthContext);
};

const localState = () => {
    var result = localStorage.getItem("authTicket");
    if (result === "undefined" || result === "null") return null;
    return JSON.parse(result);
};

export function AuthContextProvider(props) {
    const [authTicket, setAuthTicket] = useReducer(reducer, localState() || null);

    useEffect(() => {
        console.log(generateLogMessageString(`useEffect||authTicket`, CLASS_NAME));
        if (authTicket == null) {
            localStorage.removeItem("authTicket");
        }
        else {
            localStorage.setItem("authTicket", JSON.stringify(authTicket));
        }
    }, [authTicket]);

    return (
        <AuthContext.Provider value={{ authTicket, setAuthTicket }}>
            {props.children}
        </AuthContext.Provider>
    );
}

