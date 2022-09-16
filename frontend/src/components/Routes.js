import React from 'react'
import {Switch } from "react-router-dom"

//common components
import PrivateRoute from './authentication/PrivateRoute'
import WizardRoute from './authentication/WizardRoute'
import AdminRoute from './authentication/AdminRoute'
import { PublicRoute } from './PublicRoute'

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
import AdminUserEntity from '../views/admin/AdminUserEntity'
import AdminUserList from '../views/admin/AdminUserList'

//const CLASS_NAME = "Routes";


function Routes() {

    //-------------------------------------------------------------------
    //  Routes
    //-------------------------------------------------------------------
    return(
        <Switch>
            <WizardRoute exact path="/" component={WizardWelcome} />
            <PrivateRoute path="/profiles/library" component={ProfileList} />
            {/*Handles types/all and types/mine in the component*/}
            <PrivateRoute path="/types/library/profile/:profileId" component={ProfileTypeDefinitionList} />
            <PrivateRoute path="/types/library" component={ProfileTypeDefinitionList} />
            {/* order matters in the profile/ routes*/}
            {/* ProfileTypeDefinitionEntity - Depending on entry point, this is not always part of the wizard - 
             * But the wizardContext is initialized in either case*/}
            <WizardRoute path="/type/extend/:parentId" component={ProfileTypeDefinitionEntity} />
            <WizardRoute path="/type/:id/p=:profileId" component={ProfileTypeDefinitionEntity} />
            <WizardRoute path="/type/:id" component={ProfileTypeDefinitionEntity} />
            <WizardRoute path="/wizard/welcome" component={WizardWelcome} />
            <WizardRoute path="/wizard/create-profile" component={WizardNewProfile} />
            <WizardRoute path="/wizard/import-profile" component={WizardImportProfile} />
            <WizardRoute path="/wizard/select-profile" component={WizardSelectProfile} />
            <WizardRoute path="/wizard/select-existing-profile" component={WizardSelectProfile} />
            <WizardRoute path="/wizard/filter-profile" component={WizardFilterProfile} />
            <WizardRoute path="/wizard/select-base-type" component={WizardSelectBaseType} />
            <WizardRoute path="/wizard/extend/:parentId/p=:profileId" component={ProfileTypeDefinitionEntity} />
            <WizardRoute path="/wizard/extend/:parentId" component={ProfileTypeDefinitionEntity} />
            <AdminRoute path="/admin/user/list" component={AdminUserList} roles={['cesmii.profiledesigner.admin']}/>
            <AdminRoute path="/admin/user/:id" component={AdminUserEntity} roles={['cesmii.profiledesigner.admin']} />
            <PublicRoute component={PageNotFound} />
        </Switch>

    )

}

export default Routes