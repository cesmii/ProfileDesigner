import React, { createContext, useContext, useReducer } from "react";
import { AuthReducer, initialState } from "./AuthReducer";
import { generateLogMessageString } from '../../utils/UtilityService'

const CLASS_NAME = "AuthContext";

const AuthStateContext = createContext();
const AuthDispatchContext = createContext();

export function useAuthState() {
    const context = useContext(AuthStateContext);
    if (context === undefined) {
        console.error(generateLogMessageString(`useAuthState||useAuthState must be used within an AuthProvider`, CLASS_NAME, 'critical'));
        throw new Error("useAuthState must be used within an AuthProvider");
    }

    return context;
}

export function useAuthDispatch() {
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


