import React from 'react'
import { useHistory } from 'react-router-dom'

import SideMenuItem from './SideMenuItem'
import { useAuthContext } from "./authentication/AuthContext";
import { useLoadingContext } from './contexts/LoadingContext'
import color from './Constants'
import ProfileExplorer from '../views/shared/ProfileExplorer';
import SideMenuLinkList from './SideMenuLinkList'

//const CLASS_NAME = "SideMenu";
function SideMenu() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();
    const { authTicket } = useAuthContext();
    const { loadingProps } = useLoadingContext();

    //-------------------------------------------------------------------
    // Determine profile id based on location. If pattern doesn't match, show nothing
    //-------------------------------------------------------------------
    const renderProfileExplorer = () => {
        //parse relative url. If pattern matches profile edit/add pattern, then extract the id
        var path = history.location.pathname.split("/");
        
        if (path == null || path.length < 2 || history.location.pathname.toLowerCase().indexOf('/profile/') === -1) return;        
        //TBD - handle extend and new scenario better
        if (history.location.pathname.toLowerCase().indexOf('/profile/extend') > -1) return;        
        if (history.location.pathname.toLowerCase().indexOf('/profile/new') > -1) return;        

        //if we get here, the is an edit/view profile
        var id = parseInt(path[2]);

        //state is not defined when using address bar or href links. 
        //if (history.location.state == null) return;        
        //    var id = parseInt(history.location.state.id);

        return (
            <ProfileExplorer currentUserId={authTicket.user.id} currentProfileId={id} />
        );
    };

    const profileCountAllCaption = () => {
        if (loadingProps.profileCount == null || loadingProps.profileCount.all == null) return;
        if (loadingProps.profileCount.all === 1) return ('1 item'); 
        return (`${loadingProps.profileCount.all} items`);
    };

    const profileCountMineCaption = () => {
        if (loadingProps.profileCount == null || loadingProps.profileCount.mine == null) return;
        if (loadingProps.profileCount.mine === 1) return ('1 item');
        return (`${loadingProps.profileCount.mine} items`);
    };

    //sub menu items
    var profilesSubMenu = [{ url: "/", caption: "Import" }, { url: "/profile/new", caption: "New" } ];

    return (
        <div id="--cesmii-sidemenu-left">
            <ul>
                <SideMenuItem caption="Profiles library" bgColor={color.cornflower} iconName="folder-profile" navUrl="/profiles/all" subText={profileCountAllCaption()} subMenuItems={profilesSubMenu} />
                <SideMenuItem caption="My profiles" bgColor={color.shark} iconName="folder-shared" navUrl="/profiles/mine" subText={profileCountMineCaption()} />
            </ul>
            {(loadingProps.favoritesList != null && loadingProps.favoritesList.length > 0) && 
                <SideMenuLinkList caption='Favorites' iconName='favorite' items={loadingProps.favoritesList} currentUserId={authTicket.user.id} ></SideMenuLinkList>
            }
            {(loadingProps.recentFileList != null && loadingProps.recentFileList.length > 0) &&
                <SideMenuLinkList caption='Recent / Open Items' iconName='access-time' items={loadingProps.recentFileList} currentUserId={authTicket.user.id} ></SideMenuLinkList>
            }
            {renderProfileExplorer()}
        </div>
    )

}

export default SideMenu