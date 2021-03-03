import React from 'react'
import {Switch, Route } from "react-router-dom"

//common components
import PrivateRoute from './authentication/PrivateRoute'

//page level imports
import Login from "../views/Login"
import Home from "../views/Home"
import ProfileList from "../views/ProfileList"
import ProfileEntity from "../views/ProfileEntity"
import PageNotFound from "../views/PageNotFound"
import AdvancedSearch from "../views/AdvancedSearch"
import NamespaceList from '../views/NamespaceList'
import NamespaceEntity from '../views/NamespaceEntity'

function Routes() {
    return(
        <Switch>
            <PrivateRoute exact path="/" component={Home} />
            <PrivateRoute path="/profiles/library/namespace/:namespace" component={NamespaceEntity} />
            <PrivateRoute path="/profiles/library" component={NamespaceList} />
            {/*Handles profiles/all and profiles/mine in the component*/}
            <PrivateRoute path="/profiles/:type" component={ProfileList} />
            {/* order matters in the profile/ routes*/}
            <PrivateRoute path="/profile/extend/:parentId" component={ProfileEntity} />
            <PrivateRoute path="/profile/:id" component={ProfileEntity} />
            <PrivateRoute path="/advancedsearch" component={AdvancedSearch} />
            <Route exact path="/login" component={Login} />
            <Route component={PageNotFound} />
        </Switch>

    )

}

export default Routes