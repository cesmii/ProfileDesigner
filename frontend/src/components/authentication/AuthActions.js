export async function login(dispatch, data) {
    try {
        dispatch({ type: 'LOGIN_SUCCESS', payload: data });
        localStorage.setItem('authTicket', JSON.stringify(data));
        return true;
    } catch (error) {
        dispatch({ type: 'LOGIN_ERROR', error: error });
    }
}

export async function logout(dispatch) {
    dispatch({ type: 'LOGOUT' });
    localStorage.removeItem('authTicket');
    sessionStorage.removeItem('searchCriteria');
    sessionStorage.removeItem('lookupDataStatic');
    sessionStorage.removeItem('wizardContext');

    var appContext = localStorage.getItem('appContext');
    if (appContext != null) {
        appContext = JSON.parse(appContext);
        appContext.profileCount = null;
        appContext.typeCount = null;
        localStorage.setItem('appContext', JSON.stringify(appContext));
    }
    return true;
}
