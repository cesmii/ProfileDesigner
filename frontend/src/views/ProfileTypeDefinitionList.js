import React, { useState, useEffect } from 'react'
import { useParams } from 'react-router-dom'
import { Helmet } from "react-helmet"

import { useLoadingContext } from '../components/contexts/LoadingContext'
import { AppSettings } from '../utils/appsettings'
import { generateLogMessageString, renderTitleBlock } from '../utils/UtilityService'
import ProfileTypeDefinitionListGrid from './shared/ProfileTypeDefinitionListGrid';

import color from '../components/Constants'
import { clearSearchCriteria, toggleSearchFilterSelected } from '../services/ProfileService'

const CLASS_NAME = "ProfileTypeDefinitionList";
//const entityInfo = {
//    name: "Type Definition",
//    namePlural: "Types",
//    entityUrl: "/type/:id",
//    listUrl: "/types/library"
//}

function ProfileTypeDefinitionList() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const { profileId } = useParams()
    const caption = `Type Library`;
    const iconName = 'profile';
    const iconColor = color.shark;
    const [ _initSearchCriteria, setInitSearchCriteria ] = useState(true);
    const [ _searchCriteria, setSearchCriteria ] = useState(null);
    const [_searchCriteriaChanged, setSearchCriteriaChanged] = useState(0);

    //-------------------------------------------------------------------
    // Region: Pass profile id into component if profileId passed in from url
    //-------------------------------------------------------------------
    useEffect(() => {

        if (!_initSearchCriteria) return;

        //check for searchcriteria - trigger fetch of search criteria data - if not already triggered
        if ((loadingProps.searchCriteria == null || loadingProps.searchCriteria.filters == null) && !loadingProps.refreshSearchCriteria) {
            setLoadingProps({ refreshSearchCriteria: true });
        }
        //start with a blank criteria slate. Handle possible null scenario if criteria hasn't loaded yet. 
        var criteria = loadingProps.searchCriteria == null ? null : JSON.parse(JSON.stringify(loadingProps.searchCriteria));

        if (criteria != null) {
            criteria = clearSearchCriteria(criteria);
            //add in any profile filter passed in url
            if (profileId != null) {
                toggleSearchFilterSelected(criteria, AppSettings.SearchCriteriaCategory.Profile, parseInt(profileId));
            }
        }

        //update state
        setInitSearchCriteria(false);
        if (criteria != null) {
            setSearchCriteria(criteria);
            setSearchCriteriaChanged(_searchCriteriaChanged + 1);
        }
        setLoadingProps(criteria);

        //this will execute on unmount
        return () => {
            //console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, [profileId, _initSearchCriteria, loadingProps.searchCriteriaRefreshed]);

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onGridRowSelect = (item) => {
        console.log(generateLogMessageString(`onGridRowSelect||Name: ${item.name}||selected: ${item.selected}`, CLASS_NAME));
        //TBD - handle selection here...
    };

    //bubble up search criteria changed so the parent page can control the search criteria
    const onSearchCriteriaChanged = (criteria) => {
        console.log(generateLogMessageString(`onSearchCriteriaChanged`, CLASS_NAME));
        //update state
        setSearchCriteria(criteria);
        //trigger api to get data
        setSearchCriteriaChanged(_searchCriteriaChanged + 1);
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderHeaderRow = () => {
        return (
            <div className="row pb-3">
                <div className="col-lg-6 mr-auto d-flex">
                    {renderTitleBlock(caption, iconName, iconColor)}
                </div>
                <div className="col-lg-6 d-flex align-items-center justify-content-end">
                    {/*    <HeaderSearch itemCount={_itemCount} searchValue={_searchVal} onSearch={handleOnSearchChange} searchMode="standard" currentUserId={authTicket.user.id} /> */}
                    {/*    {(type == null || type.toLowerCase() === 'mine') &&*/}
                    {/*        <Button variant="secondary" type="button" className="auto-width ml-2" onClick={onAdd} >Add</Button>*/}
                    {/*    }*/}
                </div>
            </div>
        );
    };

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Helmet>
                <title>{AppSettings.Titles.Main + " | " + caption}</title>
            </Helmet>
            {renderHeaderRow()}
            <ProfileTypeDefinitionListGrid onGridRowSelect={onGridRowSelect} searchCriteria={_searchCriteria}
                onSearchCriteriaChanged={onSearchCriteriaChanged} searchCriteriaChanged={_searchCriteriaChanged} />
        </>
    )
}

export default ProfileTypeDefinitionList;