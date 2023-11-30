import React from 'react'
import { Routes as SwitchRoutes, Route } from 'react-router-dom';

//common components
import PrivateRoute from './authentication/PrivateRoute'
import WizardRoute from './authentication/WizardRoute'
import AdminRoute from './authentication/AdminRoute'
import { PublicRoute, PublicFixedRoute } from './PublicRoute'

//page level imports
import ProfileTypeDefinitionList from "../views/ProfileTypeDefinitionList"
import ProfileTypeDefinitionEntity from "../views/ProfileTypeDefinitionEntity"
//wizard pages
import WizardWelcome from "../views/WizardWelcome"
import WizardNewProfile from "../views/WizardNewProfile"
import WizardImportProfile from '../views/WizardImportProfile'
import WizardSelectProfile from "../views/WizardSelectProfile"
import WizardSelectBaseType from "../views/WizardSelectBaseType"
import WizardFilterProfile from '../views/WizardFilterProfile'

import PageNotFound from "../views/PageNotFound"
import ProfileList from '../views/ProfileList'
import ProfileEntity from '../views/ProfileEntity'
import CloudLibList from '../views/CloudLibList'
import CloudLibViewer from '../views/CloudLibViewer'
import AdminUserEntity from '../views/admin/AdminUserEntity'
import AdminUserList from '../views/admin/AdminUserList'
import AdminCloudLibApprovalList from '../views/admin/AdminCloudLibApprovalList'
import Login from '../views/Login'
import NotAuthorized from '../views/NotAuthorized'
import LoginSuccess from '../views/LoginSuccess'
import { AppSettings } from '../utils/appsettings'
//import LoginSuccess from '../views/LoginSuccess'

//const CLASS_NAME = "Routes";

//Upgrade from 5.2 to v6
//https://github.com/remix-run/react-router/blob/main/docs/upgrading/v5.md

function Routes() {

    //-------------------------------------------------------------------
    //  Routes
    //-------------------------------------------------------------------
    return(
        <SwitchRoutes>
            <Route path='/' element={<WizardRoute />}>
                <Route path='/' element={<WizardWelcome />} />
            </Route>
            <Route element={<PublicFixedRoute />}>
                <Route path='/login' element={<Login />} />
                <Route path='/login/success' element={<LoginSuccess />} />
                <Route path='/login/returnUrl/:returnUrl' element={<Login />} />
            </Route>
            {/* order matters in the profile/ routes*/}
            <Route element={<PrivateRoute />}>
                <Route path='/profiles/library' element={<ProfileList />} />
                <Route path="/profile/:id" element={<ProfileEntity />} />
                <Route path="/cloudlibrary/search" element={<CloudLibList />} />
                <Route path="/cloudlibrary/viewer/:id" element={<CloudLibViewer />} />
                <Route path="/types/library/profile/:profileId" element={<ProfileTypeDefinitionList />} />
                <Route path="/types/library" element={<ProfileTypeDefinitionList />} />
            </Route>
            <Route element={<AdminRoute roles={[AppSettings.AADAdminRole]} />} >
                <Route path="/admin/user/list" element={<AdminUserList />} />
                <Route path="/admin/user/:id" element={<AdminUserEntity />} />
                <Route path="/admin/cloudlibrary/approval/list" element={<AdminCloudLibApprovalList />} />
            </Route>
            {/* ProfileTypeDefinitionEntity - Depending on entry point, this is not always part of the wizard -
             * But the wizardContext is initialized in either case*/}
            <Route element={<WizardRoute />}>
                <Route path="/type/extend/:parentId" element={<ProfileTypeDefinitionEntity />} />
                <Route path="/type/:id/p=:profileId" element={<ProfileTypeDefinitionEntity />} />
                <Route path="/type/:id" element={<ProfileTypeDefinitionEntity />} />
                <Route path="/wizard/welcome" element={<WizardWelcome />} />
                <Route path="/wizard/create-profile" element={<WizardNewProfile />} />
                <Route path="/wizard/import-profile" element={<WizardImportProfile />} />
                <Route path="/wizard/select-profile" element={<WizardSelectProfile />} />
                <Route path="/wizard/select-existing-profile" element={<WizardSelectProfile />} />
                <Route path="/wizard/filter-profile" element={<WizardFilterProfile />} />
                <Route path="/wizard/select-base-type" element={<WizardSelectBaseType />} />
                <Route path="/wizard/extend/:parentId/:profileId" element={<ProfileTypeDefinitionEntity />} />
                <Route path="/wizard/extend/:parentId" element={<ProfileTypeDefinitionEntity />} />
            </Route>
            <Route element={<PublicFixedRoute />}>
                <Route path='/notpermitted' element={<NotAuthorized />} />
                <Route path='/notauthorized' element={<NotAuthorized />} />
                <Route element={<PageNotFound />} />
            </Route>
        </SwitchRoutes>

    )

}

export default Routes