import React from 'react'
import { useHistory } from 'react-router-dom'
import { useMsal } from "@azure/msal-react";

import SideMenuItem from './SideMenuItem'
import { useLoadingContext } from './contexts/LoadingContext'
import color from './Constants'
import ProfileExplorer from '../views/shared/ProfileExplorer';
import { SideMenuLinkList, OnClickUnsavedCheck } from './SideMenuLinkList'
import './styles/SideMenu.scss';
import './styles/SideMenuItem.scss';
import { AppSettings } from '../utils/appsettings';
import { isInRole } from '../utils/UtilityService';

//const CLASS_NAME = "SideMenu";
function SideMenu() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();
    const { loadingProps } = useLoadingContext();
    const { instance } = useMsal();
    const _activeAccount = instance.getActiveAccount();

    //-------------------------------------------------------------------
    // Load Profile Counts if some part of the app indicates the need to do so.
    //-------------------------------------------------------------------
    //not used right now
    //useEffect(() => {
    //    async function fetchProfileCounts() {
    //        console.log(generateLogMessageString('useEffect||fetchProfileCounts||async', CLASS_NAME));
    //        //console.log(authTicket);

    //        var url = `profile/count`;
    //        await axiosInstance.get(url).then(result => {
    //            if (result.status === 200) {
    //                setLoadingProps({ profileCount: { all: result.data.all, mine: result.data.mine }, refreshProfileCount: null });
    //            } else {
    //                setLoadingProps({ profileCount: { all: null, mine: null }, refreshProfileCount: null  });
    //            }
    //        }).catch(e => {
    //            if (e.response && e.response.status === 401) {
    //                setLoadingProps({ isLoading: false, message: null, refreshProfileCount: null});
    //            }
    //            else {
    //                console.log(generateLogMessageString('useEffect||fetchProfileCounts||' + JSON.stringify(e), CLASS_NAME, 'error'));
    //                console.log(e);
    //                setLoadingProps({isLoading: false, message: null, refreshProfileCount: null });
    //            }
    //        });
    //    }

    //    //if this is changed to true, then go get new profile counts. 
    //    //this would be set to true on Add Profile save or import or login. 
    //    if (loadingProps.refreshProfileCount) {
    //        fetchProfileCounts();
    //    }
    //}, [loadingProps.refreshProfileCount]);

    ////profile type defs counts
    //useEffect(() => {
    //    async function fetchTypeCounts() {
    //        console.log(generateLogMessageString('useEffect||fetchTypeCounts||async', CLASS_NAME));
    //        //console.log(authTicket);

    //        var url = `profiletypedefinition/count`;
    //        await axiosInstance.get(url).then(result => {
    //            if (result.status === 200) {
    //                setLoadingProps({ typeCount: { all: result.data.all, mine: result.data.mine }, refreshTypeCount: null });
    //            } else {
    //                setLoadingProps({ typeCount: { all: null, mine: null }, refreshTypeCount: null });
    //            }
    //        }).catch(e => {
    //            if (e.response && e.response.status === 401) {
    //                setLoadingProps({ isLoading: false, message: null, refreshTypeCount: null });
    //            }
    //            else {
    //                console.log(generateLogMessageString('useEffect||fetchTypeCounts||' + JSON.stringify(e), CLASS_NAME, 'error'));
    //                console.log(e);
    //                setLoadingProps({ isLoading: false, message: null, refreshTypeCount: null });
    //            }
    //        });
    //    }

    //    //if this is changed to true, then go get new profile counts. 
    //    //this would be set to true on Add Profile save or import or login. 
    //    if (loadingProps.refreshTypeCount) {
    //        fetchTypeCounts();
    //    }
    //}, [loadingProps.refreshTypeCount]);

    ////get favorites and convert them to a format we can is in our sideMenulinks list
    //useEffect(() => {
    //    //if this is changed to true, then go get new profile counts. 
    //    //this would be set to true on Add Profile save or import or login. 
    //    if (loadingProps.favoritesList == null || loadingProps.favoritesList.length === 0) {
    //        fetchProfileCounts();
    //    }

    //}, [loadingProps.favoritesList]);

    //-------------------------------------------------------------------
    // Determine profile id based on location. If pattern doesn't match, show nothing
    //-------------------------------------------------------------------
    const renderProfileExplorer = () => {
        //parse relative url. If pattern matches profile edit/add pattern, then extract the id
        var path = history.location.pathname.split("/");

        if (path == null || path.length < 2 || history.location.pathname.toLowerCase().indexOf('/type/') === -1) return;
        //TBD - handle extend and new scenario better
        if (history.location.pathname.toLowerCase().indexOf('/type/extend') > -1) return;
        if (history.location.pathname.toLowerCase().indexOf('/type/new') > -1) return;

        //if we get here, the is an edit/view profile
        var id = parseInt(path[2]);

        //state is not defined when using address bar or href links. 
        //if (history.location.state == null) return;        
        //    var id = parseInt(history.location.state.id);

        return (
            <ProfileExplorer activeAccount={_activeAccount} currentProfileId={id} />
        );
    };

    //const profileCountAllCaption = () => {
    //    if (loadingProps.profileCount == null || loadingProps.profileCount.all == null) return;
    //    if (loadingProps.profileCount.all === 1) return ('1 item');
    //    return (`${loadingProps.profileCount.all} items`);
    //};

    //const profileCountMineCaption = () => {
    //    if (loadingProps.profileCount == null || loadingProps.profileCount.mine == null) return;
    //    if (loadingProps.profileCount.mine === 1) return ('1 item');
    //    return (`${loadingProps.profileCount.mine} items`);
    //};

    //const typeCountAllCaption = () => {
    //    if (loadingProps.typeCount == null || loadingProps.typeCount.all == null) return;
    //    if (loadingProps.typeCount.all === 1) return ('1 item'); 
    //    return (`${loadingProps.typeCount.all} items`);
    //};

    //const typeCountMineCaption = () => {
    //    if (loadingProps.typeCount == null || loadingProps.typeCount.mine == null) return;
    //    if (loadingProps.typeCount.mine === 1) return ('1 item');
    //    return (`${loadingProps.typeCount.mine} items`);
    //};

    //sub menu items
    //var profilesSubMenu = [/*{ url: "/", caption: "Import" },*/ { url: "/type/new", caption: "New Type Definition" } ];

    return (
        <div className="siderail-left" >
            <ul>
                <SideMenuItem caption="Welcome Wizard" bgColor={color.shark} iconName="home" navUrl="/" />
                <SideMenuItem caption="Type Library" bgColor={color.shark} iconName={AppSettings.IconMapper.TypeDefinition} navUrl="/types/library" />
                <SideMenuItem caption="Profile Library" bgColor={color.shark} iconName={AppSettings.IconMapper.Profile} navUrl="/profiles/library" />
                {(isInRole(_activeAccount, AppSettings.AADAdminRole)) &&
                    <SideMenuItem caption="Cloud Lib Approval Queue" bgColor={color.shark} iconName="cloud-upload" navUrl="/admin/cloudlibrary/approval/list" />
                }
            </ul>
            {(loadingProps.favoritesList != null && loadingProps.favoritesList.length > 0) &&
                <SideMenuLinkList caption='Favorites' bgColor={color.citron} iconName='favorite' items={loadingProps.favoritesList} activeAccount={_activeAccount} ></SideMenuLinkList>
            }
            {(loadingProps.recentFileList != null && loadingProps.recentFileList.length > 0) &&
                <SideMenuLinkList caption='Recent / Open Items' bgColor={color.shark} iconName='access-time' items={loadingProps.recentFileList} activeAccount={_activeAccount} ></SideMenuLinkList>
            }
            {renderProfileExplorer()}
        </div>
    )

}

export default SideMenu