import { generateLogMessageString } from '../../utils/UtilityService'

const CLASS_NAME = "AuthReducer";

let user = localStorage.getItem("authTicket")
    ? JSON.parse(localStorage.getItem("authTicket")).user
    : null;
let token = localStorage.getItem("authTicket")
    ? JSON.parse(localStorage.getItem("authTicket")).token
    : null;

export const initialState = {
    user: null || user,
    token: null || token
};

export const AuthReducer = (initState, action) => {
    switch (action.type) {
        case "LOGIN_SUCCESS":
            return {
                ...initState,
                user: action.payload.user,
                token: action.payload.token
            };
        case "LOGOUT":
            return {
                ...initState,
                user: null,
                token: null
            };
        default:
            console.error(generateLogMessageString(`useAuthState||Unhandled action type: ${action.type}`, CLASS_NAME, 'critical'));
            throw new Error(`Unhandled action type: ${action.type}`);
    }
};