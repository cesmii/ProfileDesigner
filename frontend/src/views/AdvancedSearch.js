import React, { useState, useRef } from 'react'
import { useHistory } from 'react-router-dom'
import { Button } from 'react-bootstrap'
import axiosInstance from "../services/AxiosService";

import { useLoadingContext } from "../components/contexts/LoadingContext";
import { useAuthState } from "../components/authentication/AuthContext";
import { generateLogMessageString, pageDataRows, renderTitleBlock } from '../utils/UtilityService';
import { getTypeDefPreferences, setProfileTypePageSize } from '../services/ProfileService';
import AdvancedSearchRow from './shared/AdvancedSearchRow'
import ProfileTypeDefinitionRow from './shared/ProfileTypeDefinitionRow';
import GridPager from '../components/GridPager'
import color from '../components/Constants'
import './styles/AdvancedSearch.scss';

const CLASS_NAME = "AdvancedSearch";

function AdvancedSearch() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();

    const authTicket = useAuthState();
    const _profilePreferences = getTypeDefPreferences();
    const _scrollToRef = useRef(null);
    const searchRowNew = {
        id: null, fieldName: null, operator: -1, val: null, isValid: { fieldName: false, operator: false, val: false} };
    const [_searchCriteria, setSearchCriteria] = useState([{ id: 0, fieldName: null, operator: null, val: null, isValid: { fieldName: false, operator: false, val: false} }]);
    const [criteriaCounter, setCriteriaCounter] = useState(0);
    const { setLoadingProps } = useLoadingContext();
    const [ _globalOperator ] = useState('and');
    //result set
    const [_dataRows, setDataRows] = useState({
        all: [], filtered: [], paged: [],
        pager: { currentPage: 1, pageSize: _profilePreferences.pageSize, itemCount: 0 }
    });
    const [_isValid, setIsValid] = useState(true);  //set to true so the message doesn't show up on init

    //-------------------------------------------------------------------
    // Region: Search helpers
    //-------------------------------------------------------------------
    const filterMatches = (data) => {
        //loop criteria and then filter out based on each criteria element
        var filtered = data.filter(item => {
            var matchAny = 0;
            _searchCriteria.forEach(criteria => {
                if (checkItemMatch(item, criteria)) {
                    matchAny += 1;
                }
            });
            //TBD - add support for AND vs OR
            if (_globalOperator === 'or') {
                //if we are doing an or comparison, then match any > 0 is good
                return (matchAny > 0);
            }
            //if we are doing an AND comparison, then match any == searchCriteria length is good
            else if (_globalOperator == null || _globalOperator === 'and') {
                //if we are doing an or comparison, then match any > 0 is good
                return (matchAny === _searchCriteria.length);
            }
            //shouldn't get here. Should be either and/or global operator
            return null;
        });

        //update state with data returned
        var pagedData = pageDataRows(filtered, 1, _profilePreferences.pageSize); //also updates state
        //set state on fetch of data
        setDataRows({
            all: filtered, filtered: filtered, paged: pagedData,
            pager: { currentPage: 1, pageSize: _profilePreferences.pageSize, itemCount: filtered == null ? 0 : filtered.length }
        });
    }

    // a field could be a simple field item["id"] or a complex field item["author"]["fullName"].
    // accomodate both scenarios and check for existence of nulls in the objects. 
    const findTargetValue = (item, fieldNames, index) => {

        var result = item[fieldNames[index]];
        if (result == null) {
            return null;
        }
        //go next level down if required
        if (index < fieldNames.length - 1) {
            return findTargetValue(result, fieldNames, index + 1);
        }
        //this means we are at the deepest point and have the value. 
        return result;
    }

    const checkItemMatch = (row, criteria) => {

        if (criteria.operator == null) return false;

        var fieldNames = criteria.fieldName.split('.');
        var targetValue = findTargetValue(row, fieldNames, 0);

        switch (criteria.operator) {
            case "contain":
                return targetValue.toLowerCase().indexOf(criteria.val.toLowerCase()) > -1;
            case "!contain":
                return targetValue.toLowerCase().indexOf(criteria.val.toLowerCase()) === -1;
            case "equal":
                return targetValue.toLowerCase() === criteria.val.toLowerCase();
            case "lte":
                return parseFloat(targetValue) <= parseFloat(criteria.val);
            case "lt":
                return parseFloat(targetValue) <= parseFloat(criteria.val);
            case "=":
                return parseFloat(targetValue) === parseFloat(criteria.val);
            case "gt":
                return parseFloat(targetValue) > parseFloat(criteria.val);
            case "gte":
                return parseFloat(targetValue) >= parseFloat(criteria.val);
            default:
                return false;
        }
    };


    //validate
    const validateForm = () => {
        console.log(generateLogMessageString('validateForm', CLASS_NAME));

        //loop over all criteria and find any invalids
        var isValid = true;
        _searchCriteria.forEach(criteria => {
            if (!(criteria.isValid.fieldName && criteria.isValid.operator && criteria.isValid.val)) {
                isValid = false;
                return;
            }
        });

        setIsValid(isValid);
        return isValid;
    };

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onCancel = () => {
        console.log(generateLogMessageString('onCancel', CLASS_NAME));
        history.goBack();
    };

    const onSearch = () => {
        console.log(generateLogMessageString('onSearch', CLASS_NAME));

        //validate
        if (_searchCriteria == null || _searchCriteria.length === 0 || !validateForm()) return;

        //show a spinner
        //setLoadingProps({ isLoading: true, message: null });

        //TBD - in final app, the actual search approach would happen server side. For now, build out a client side search
        axiosInstance.get(`profile`)
            .then(resp => {
                filterMatches(resp.data);
                //hide a spinner
                setLoadingProps({ isLoading: false, message: null });
            })
            .catch(error => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: `An error occurred searching for matching profile records.`, isTimed: false  }
                    ]
                });
                console.log(generateLogMessageString('onSearch||error||' + JSON.stringify(error), CLASS_NAME, 'error'));
                console.log(error);
            });
    };

    //on search criteria change
    const onChange = (item) => {
        console.log(generateLogMessageString('onChange', CLASS_NAME));

        var aIndex = _searchCriteria.findIndex(a => { return a.id === item.id; });
        //no item found
        if (aIndex === -1) {
            return; 
        }
        //replace item
        _searchCriteria[aIndex] = JSON.parse(JSON.stringify(item));

        //update the state
        setSearchCriteria(JSON.parse(JSON.stringify(_searchCriteria)));

        //update valid state
        validateForm();

    }

    const onAdd = () => {
        var i = criteriaCounter + 1;

        var itemAdd = JSON.parse(JSON.stringify(searchRowNew));
        itemAdd.id = i;
        _searchCriteria.push(itemAdd);
        setCriteriaCounter(i);
        setSearchCriteria(JSON.parse(JSON.stringify(_searchCriteria)));
    }

    const onDelete = (id) => {
        //console.log(generateLogMessageString(`onDeleteClick||id:${props.item.id}`, CLASS_NAME));
        //props.onDelete(props.item.id);
        var removeIndex = _searchCriteria.findIndex((item) => { return item.id === id });
        _searchCriteria.splice(removeIndex, 1);
        setSearchCriteria(JSON.parse(JSON.stringify(_searchCriteria)));
    };

    const onChangePage = (currentPage, pageSize) => {
        console.log(generateLogMessageString(`onChangePage||Current Page: ${currentPage}, Page Size: ${pageSize}`, CLASS_NAME));
        var pagedData = pageDataRows(_dataRows.filtered, currentPage, pageSize);
        //update state - several items w/in keep their existing vals
        setDataRows({
            all: _dataRows.all, filtered: _dataRows.filtered, paged: pagedData,
            pager: { currentPage: currentPage, pageSize: pageSize, itemCount: _dataRows.filtered == null ? 0 : _dataRows.filtered.length }
        });

        //scroll screen to top of grid on page change
        ////scroll a bit higher than the top edge so we get some of the header in the view
        window.scrollTo({ top: (_scrollToRef.current.offsetTop - 120), behavior: 'smooth' });
        //scrollToRef.current.scrollIntoView();

        //preserve choice in local storage
        setProfileTypePageSize(pageSize);
    };

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------

    const renderAdvSearchHeaderRow = () => {
        return (<AdvancedSearchRow key="header" item={null} isHeader={true} />)
    }

    const renderSearchCriteriaGrid = () => {

        var itemCount = null;
        if (_dataRows.all != null && _dataRows.all.length > 0) {
            itemCount = _dataRows.all.length === 1 ? `1 match` : `${_dataRows.all.length} matches`;
        }

        var infoRow = (
            <div key="searchCriteria_info" className='row pb-0' >
                <div className="col col-auto-size left" >
                    <p className="h6 mr-auto">
                        Enter search criteria
                        {!_isValid &&
                            <span className="ml-2 invalid-field-message inline">
                                All criteria fields are required
                            </span>
                        }
                    </p>
                </div>
                <div className="col col-auto-size right" >
                    <p className="h6 mr-auto">{itemCount}</p>
                </div>
            </div>
        );

        const mainBody = _searchCriteria.map((row, i) => {
            return (<AdvancedSearchRow key={row.id} item={row} i={i} isHeader={false} cssClass={i === _searchCriteria.length - 1 ? 'pb-4' : ''}
                onDelete={onDelete} onChange={onChange} onAdd={onAdd} />)
        });

        return (
            <div className="flex-grid search-criteria">
                {infoRow}
                {renderAdvSearchHeaderRow()}
                {mainBody}
            </div>
        );
    }

    //render pagination ui
    const renderPagination = () => {
        return <GridPager currentPage={_dataRows.pager.currentPage} pageSize={_dataRows.pager.pageSize} itemCount={_dataRows.pager.itemCount} onChangePage={onChangePage} />
    }

    //-------------------------------------------------------------------
    // Region: Header Nav
    //-------------------------------------------------------------------
    const renderHeaderRow = () => {
        return (
            <div className="row pb-3">
                <div className="col-lg-8 mr-auto d-flex">
                    {renderTitleBlock("Advanced Search", "search", color.shark)}
                </div>
                <div className="col-lg-4 d-flex align-items-center justify-content-end">
                    <Button variant="text-solo" className="mr-3" onClick={onCancel} >Cancel</Button>
                    <Button variant="secondary" onClick={onSearch} disabled={!_isValid ? 'disabled' : ''} >Search</Button>
                </div>
            </div>
        );
    };

 
    //-------------------------------------------------------------------
    // Region: render results
    //-------------------------------------------------------------------
    //render the main grid
    const renderItemsGrid = () => {
        if (_searchCriteria.length === 0) return;
        if (_dataRows.paged == null || _dataRows.paged.length === 0) {
            return (
                <div className="alert alert-info-custom mt-2 mb-2">
                    <div className="text-center" >There are no items matching your criteria.</div>
                </div>
            )
        }
        const mainBody = _dataRows.paged.map((item) => {
            return (<ProfileTypeDefinitionRow key={item.id} item={item} currentUserId={authTicket.user.id} showActions={true} cssClass="profile-list-item" />)
        });

        return (
            <div className="flex-grid">
                {mainBody}
            </div>
        );
    }

    const renderResults = () => {
        return (
            <>
                {renderItemsGrid()}
                {renderPagination()}
            </>
        );
    }

    //-------------------------------------------------------------------
    // Region: Final Render 
    //-------------------------------------------------------------------
    return (
        <>
            {renderHeaderRow()}
            <div ref={_scrollToRef} id="--cesmii-main-content">
                <div id="--cesmii-left-content">
                    {/*content */}
                    {/* Search criteria */}
                    <div className="mt-5">
                        {renderSearchCriteriaGrid()}                    
                    </div>
                    {/* Results */}
                    <div className="my-2 py-2">
                        {renderResults()}
                    </div>
                </div>
            </div>
        </>
    )
}

export default AdvancedSearch