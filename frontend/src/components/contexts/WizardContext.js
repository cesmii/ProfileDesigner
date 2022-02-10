import React, { useContext, useReducer, useEffect } from "react";

let reducer = (info, newInfo) => {
    if (newInfo === null) {
        sessionStorage.removeItem("wizardContext");
        return initialState;
    }
    return { ...info, ...newInfo };
};

const initialState = {
    currentPage: null,
    mode: null,  //enum - createProfile, ImportProfile, SelectProfile
    //ID null - create profile flow, id not null & > 0 - existing profile route.
    //The profile that this type belongs inside. If id > 0, then the rest of the object may be null
    //If id is null, then we create the new profile using the properties of the object.
    profile: null,
    profileId: null,
    parentId: null //the type id being used to extend
};

const localState = JSON.parse(sessionStorage.getItem("wizardContext"));

const WizardContext = React.createContext();

export function useWizardContext() {
    return useContext(WizardContext);
}

function WizardContextProvider(props) {
    //set to cached version or null 
    const [wizardProps, setWizardProps] = useReducer(reducer, localState || initialState);

    useEffect(() => {
        if (wizardProps == null) {
            sessionStorage.removeItem("wizardContext");
        }
        else {
            sessionStorage.setItem("wizardContext", JSON.stringify(wizardProps));
        }
    }, [wizardProps]);

    return (
        <WizardContext.Provider value={{ wizardProps, setWizardProps }}>
            {props.children}
        </WizardContext.Provider>
    );
}

//export { WizardContext, WizardContextProvider};
export { WizardContextProvider};

