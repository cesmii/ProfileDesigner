import { useEffect } from 'react';

import axiosInstance from '../services/AxiosService'
import { useLoadingContext } from "./contexts/LoadingContext";
import { useAuthState } from './authentication/AuthContext';
import { generateLogMessageString, getTypeDefIconName } from '../utils/UtilityService';
import { getTypeDefPreferences } from '../services/ProfileService';

const CLASS_NAME = "OnLookupLoad";

// Component that handles scenario when f5 / refresh happens
// We want to turn off processing flag in that scenario as protection against
// scenario where exception occurs and isLoading remains true.
// renders nothing, just attaches side effects
export const OnLookupLoad = () => {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const authTicket = useAuthState();

    //-------------------------------------------------------------------
    // Region: hooks
    // useEffect - get static lookup data
    //-------------------------------------------------------------------
    useEffect(() => {
        // Load lookup data upon certain triggers in the background
        async function fetchData() {

            var url = `lookup/all`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

            await axiosInstance.get(url).then(result => {
                if (result.status === 200) {
                    //set the data in local storage
                    setLoadingProps({
                        lookupDataStatic: result.data,
                        refreshLookupData: false,
                        lookupDataRefreshed: loadingProps.lookupDataRefreshed + 1
                    });
                } else {
                    setLoadingProps({
                        lookupDataStatic: null,
                        refreshLookupData: false,
                        lookupDataRefreshed: loadingProps.lookupDataRefreshed + 1
                    });
                }
            }).catch(e => {
                if (e.response && e.response.status === 401) {
                }
                else {
                    console.log(generateLogMessageString('useEffect||fetchLookupData||' + JSON.stringify(e), CLASS_NAME, 'error'));
                    console.log(e);
                }
            });
        };


        if (loadingProps.lookupDataStatic == null || loadingProps.refreshLookupData === true) {
            //console.log(generateLogMessageString('useEffect||refreshLookupData||Trigger fetch', CLASS_NAME));
            fetchData();
        }

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||refreshLookupData||Cleanup', CLASS_NAME));
            //setFilterValOnChild('');
        };
    }, [loadingProps.lookupDataStatic, loadingProps.refreshLookupData]);

    //-------------------------------------------------------------------
    // Region: hooks
    // useEffect - load & cache search criteria under certain conditions
    //-------------------------------------------------------------------
    useEffect(() => {
        async function fetchData() {

            var url = `lookup/searchcriteria`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

            await axiosInstance.get(url).then(result => {
                if (result.status === 200) {

                    //init the page size value
                    result.data.take = getTypeDefPreferences().pageSize;

                    //set the data in local storage
                    setLoadingProps({
                        searchCriteria: result.data,
                        refreshSearchCriteria: false,
                        searchCriteriaRefreshed: loadingProps.searchCriteriaRefreshed + 1
                    });

                } else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving the type definition filters.', isTimed: true }]
                    });
                }

            }).catch(e => {
                if ((e.response && e.response.status === 401) || e.toString().indexOf('Network Error') > -1) {
                    //do nothing, this is handled in routes.js using common interceptor
                    //setAuthTicket(null); //the call of this will clear the current user and the token
                }
                else {
                    setLoadingProps({
                        isLoading: false, message: null, inlineMessages: [
                            { id: new Date().getTime(), severity: "danger", body: 'An error occurred retrieving the type definition filters.', isTimed: true }]
                    });
                }
            });
        }

        //if not logged in yet, return
        if (authTicket == null || authTicket.token == null) return;

        //trigger retrieval of lookup data - if necessary
        if (loadingProps == null || loadingProps.searchCriteria == null || loadingProps.searchCriteria.filters == null
            || loadingProps.refreshSearchCriteria) {
            //console.log(generateLogMessageString('useEffect||refreshSearchCriteria||Trigger fetch', CLASS_NAME));
            fetchData();
        }

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||refreshSearchCriteria||Cleanup', CLASS_NAME));
        };

    }, [loadingProps.searchCriteria, loadingProps.refreshSearchCriteria, authTicket.token]);

    //-------------------------------------------------------------------
    // Region: hooks
    // useEffect - load & cache favorites list
    //-------------------------------------------------------------------
    useEffect(() => {
        // Load lookup data upon certain triggers in the background
        async function fetchData() {

            var url = `profiletypedefinition/lookup/favorites`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));

            await axiosInstance.get(url).then(result => {
                if (result.status === 200) {
                    //convert the data into a format for the sideMenuLinkList
                    var favoritesListLocal = result.data.data.map(p => {
                        return { url: `/type/${p.id}`, caption: p.name, iconName: getTypeDefIconName(p), authorId: p.authorId };
                    });

                    //set the data in local storage
                    setLoadingProps({
                        favoritesList: favoritesListLocal,
                        refreshFavoritesList: false
                    });
                } else {
                    setLoadingProps({
                        favoritesList: null,
                        refreshFavoritesList: false
                    });
                }
            }).catch(e => {
                if (e.response && e.response.status === 401) {
                }
                else {
                    console.log(generateLogMessageString('useEffect||fetchFavorites||' + JSON.stringify(e), CLASS_NAME, 'error'));
                    console.log(e);
                }
            });
        };

        if (loadingProps.favoritesList == null || loadingProps.refreshFavoritesList === true) {
            //console.log(generateLogMessageString('useEffect||refreshFavoritesList||Trigger fetch', CLASS_NAME));
            fetchData();
        }

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||refreshLookupData||Cleanup', CLASS_NAME));
        };
    }, [loadingProps.favoritesList, loadingProps.refreshFavoritesList]);

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    // renders nothing, since nothing is needed
    return null;
};