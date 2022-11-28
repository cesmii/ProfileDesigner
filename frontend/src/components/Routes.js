import React from 'react'
import {Switch } from "react-router-dom"

//common components
import PrivateRoute from './authentication/PrivateRoute'
import WizardRoute from './authentication/WizardRoute'
import { AdminRoute } from './authentication/AdminRoute'
import { PublicFixedRoute } from './PublicRoute'
//import LoginSuccessRoute from './authentication/LoginSuccessRoute'

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
import CloudLibList from '../views/CloudLibList'
import AdminUserEntity from '../views/admin/AdminUserEntity'
import AdminUserList from '../views/admin/AdminUserList'
import Login from '../views/Login'
import NotAuthorized from '../views/NotAuthorized'
//import LoginSuccess from '../views/LoginSuccess'

//const CLASS_NAME = "Routes";


function Routes() {

    //-------------------------------------------------------------------
    //  Routes
    //-------------------------------------------------------------------
    return(
        <Switch>
            <WizardRoute exact path="/" component={WizardWelcome} roles={['cesmii.profiledesigner.user']} />
            <PublicFixedRoute exact path="/login/success" component={Login} roles={['cesmii.profiledesigner.user']} />
            {/*<LoginSuccessRoute exact path="/loginsuccess" component={LoginSuccess} roles={['cesmii.profiledesigner.user']} />*/}
            <PublicFixedRoute path="/login/returnUrl=:returnUrl" component={Login} />
            <PublicFixedRoute exact path="/login" component={Login} />
            <PrivateRoute path="/profiles/library" component={ProfileList} roles={['cesmii.profiledesigner.user']} />
            <PrivateRoute path="/cloudlibrary/search" component={CloudLibList} roles={['cesmii.profiledesigner.user']} />
            {/*Handles types/all and types/mine in the component*/}
            <PrivateRoute path="/types/library/profile/:profileId" component={ProfileTypeDefinitionList} roles={['cesmii.profiledesigner.user']} />
            <PrivateRoute path="/types/library" component={ProfileTypeDefinitionList} roles={['cesmii.profiledesigner.user']} />
            {/* order matters in the profile/ routes*/}
            {/* ProfileTypeDefinitionEntity - Depending on entry point, this is not always part of the wizard - 
             * But the wizardContext is initialized in either case*/}
            <WizardRoute path="/type/extend/:parentId" component={ProfileTypeDefinitionEntity} roles={['cesmii.profiledesigner.user']} />
            <WizardRoute path="/type/:id/p=:profileId" component={ProfileTypeDefinitionEntity} roles={['cesmii.profiledesigner.user']} />
            <WizardRoute path="/type/:id" component={ProfileTypeDefinitionEntity} roles={['cesmii.profiledesigner.user']} />
            <WizardRoute path="/wizard/welcome" component={WizardWelcome} roles={['cesmii.profiledesigner.user']} />
            <WizardRoute path="/wizard/create-profile" component={WizardNewProfile} roles={['cesmii.profiledesigner.user']} />
            <WizardRoute path="/wizard/import-profile" component={WizardImportProfile} roles={['cesmii.profiledesigner.user']} />
            <WizardRoute path="/wizard/select-profile" component={WizardSelectProfile} roles={['cesmii.profiledesigner.user']} />
            <WizardRoute path="/wizard/select-existing-profile" component={WizardSelectProfile} roles={['cesmii.profiledesigner.user']} />
            <WizardRoute path="/wizard/filter-profile" component={WizardFilterProfile} roles={['cesmii.profiledesigner.user']} />
            <WizardRoute path="/wizard/select-base-type" component={WizardSelectBaseType} roles={['cesmii.profiledesigner.user']} />
            <WizardRoute path="/wizard/extend/:parentId/p=:profileId" component={ProfileTypeDefinitionEntity} roles={['cesmii.profiledesigner.user']} />
            <WizardRoute path="/wizard/extend/:parentId" component={ProfileTypeDefinitionEntity} roles={['cesmii.profiledesigner.user']} />
            <AdminRoute path="/admin/user/list" component={AdminUserList} roles={['cesmii.profiledesigner.admin']}/>
            <AdminRoute path="/admin/user/:id" component={AdminUserEntity} roles={['cesmii.profiledesigner.admin']} />
            <PublicFixedRoute path="/notpermitted" component={NotAuthorized} />
            <PublicFixedRoute path="/notauthorized" component={NotAuthorized} />
            <PublicFixedRoute component={PageNotFound} />
        </Switch>

    )

}

export default Routes