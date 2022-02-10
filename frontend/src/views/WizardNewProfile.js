import React, { useState,useEffect } from 'react'
import { useHistory } from 'react-router-dom'
import { Helmet } from "react-helmet"
import Form from 'react-bootstrap/Form'

import axiosInstance from '../services/AxiosService'
import { useLoadingContext } from '../components/contexts/LoadingContext'
import { useWizardContext } from '../components/contexts/WizardContext'
import { AppSettings } from '../utils/appsettings'
import { generateLogMessageString } from '../utils/UtilityService'
import { isProfileValid, profileNew, validate_All } from '../services/ProfileService'
import ProfileEntity from './shared/ProfileEntity'
import { getWizardNavInfo, renderWizardBreadcrumbs, renderWizardButtonRow, renderWizardHeader, renderWizardIntroContent, WizardSettings } from '../services/WizardUtil'
import { ErrorModal } from '../services/CommonUtil'

const CLASS_NAME = "WizardNewProfile";

//TBD - re-factor this to use a shared ProfileEntity.js component.
//TBD - The child component will do all the validation and then bubble up stuff to this parent component. 
function WizardNewProfile() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const _pageId = 'CreateProfile';
    const history = useHistory();
    const { setLoadingProps } = useLoadingContext();
    const { wizardProps, setWizardProps } = useWizardContext();
    const [_isValid, setIsValid] = useState({ namespace: true, namespaceFormat: true, selectedItem: true });
    const [_item, setItem] = useState(JSON.parse(JSON.stringify(profileNew)));
    const _currentPage = WizardSettings.panels.find(p => { return p.id === _pageId; });
    const _mode = WizardSettings.mode.CreateProfile;
    const _navInfo = getWizardNavInfo(_mode, _pageId);
    const [_error, setError] = useState({ show: false, message: null, caption: null });

    //-------------------------------------------------------------------
    // Region: hooks
    //-------------------------------------------------------------------
    useEffect(() => {

        //only want this to run once on load
        if (wizardProps != null && wizardProps.currentPage === _currentPage.id) {
            return;
        }

        //update state on load 
        setWizardProps({currentPage: _currentPage.id, mode: _mode});

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||wizardProps||Cleanup', CLASS_NAME));
            //setFilterValOnChild('');
        };
    }, [wizardProps.currentPage]);


    //if the profile is present in the wizard cache, use that. Else, start new
    useEffect(() => {

        //only want this to run once on load
        if (wizardProps.profile == null) {
            //init me as the author
            var p = JSON.parse(JSON.stringify(profileNew));
            //p.authorId = authTicket.user.id;
            //p.author = {
            //    id: authTicket.user.id, name: authTicket.user.fullName
            //};
            setItem(JSON.parse(JSON.stringify(p)));
            return;
        }
        else
        {
            //update local item state if wizard is present
            setItem(JSON.parse(JSON.stringify(wizardProps.profile)));
        }

        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||wizardProps||Cleanup', CLASS_NAME));
            //setFilterValOnChild('');
        };
    }, [wizardProps.profile]);

    //-------------------------------------------------------------------
    // Region: Event handling
    //-------------------------------------------------------------------
    const onNextStepComplete = () => {
        console.log(generateLogMessageString(`onNextStepComplete`, CLASS_NAME));

        //set wizard props profile item value before we proceed.
        setWizardProps({ profile: JSON.parse(JSON.stringify(_item)), parentId: null });

        history.push({
            pathname: _navInfo.next.href
        });
    };

    const onNextStep = () => {
        console.log(generateLogMessageString('onNextStep', CLASS_NAME));

        //do client side validation
        if (!validateForm()) {
            //alert("validation failed");
            return;
        }

        //do server side validation
        //we don't save the profile to several steps away. Find issues here before we proceed.
        //show a spinner
        setLoadingProps({ isLoading: true, message: "" });

        var url = `profile/validate`;
        axiosInstance.post(url, _item).then(result => {
            if (result.status === 200) {
                //check for success message OR check if some validation failed
                if (result.data.isSuccess) {
                    //if all good, then call navigate step
                    onNextStepComplete();
                }
                else {
                    setError({ show: true, caption: _currentPage.caption, message: result.data.message });
                }
            } else {
                setError({ show: true, caption: _currentPage.caption, message: 'An error occurred validating the profile. Please try again.' });
            }

            //hide spinner
            setLoadingProps({ isLoading: false, message: null });

        }).catch(e => {
            if (e.response && e.response.status === 401) {
            }
            else {
                setError({ show: true, caption: _currentPage.caption, message: 'An error occurred validating the profile. Please try again.' });
                console.log(generateLogMessageString('onNextStep||' + JSON.stringify(e), CLASS_NAME, 'error'));
                console.log(e);
            }
            setLoadingProps({ isLoading: false, message: null });
        });
    };

    //on validate handler from child form
    const onValidate = (isValid) => {
        setIsValid({
            namespace: isValid.namespace,
            namespaceFormat: isValid.namespaceFormat
        });
    }

    //on change handler to update state
    const onChangeEntity = (item) => {
        console.log(generateLogMessageString(`onChangeEntity`, CLASS_NAME));
        setItem(JSON.parse(JSON.stringify(item)));
    }

    const onErrorModalClose = () => {
        //console.log(generateLogMessageString(`onErrorMessageOK`, CLASS_NAME));
        setError({ show: false, caption: null, message: null });
    }

    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------
    const validateForm = () => {
        console.log(generateLogMessageString(`validateForm`, CLASS_NAME));
        var isValid = validate_All(_item);
        setIsValid(isValid);
        return isProfileValid(isValid);
    }

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------
    const renderMainContent = () => {
        return (
            <>
                <div className="card row mb-3">
                    <div className="card-body col-sm-6">
                        <Form noValidate>
                            <ProfileEntity item={_item} onChange={onChangeEntity} onValidate={onValidate} isValid={_isValid} />
                        </Form>
                    </div>
                </div>
            </>
        );
    };

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    //assign nextInfo callback after method declared
    _navInfo.next.callbackAction = onNextStep;

    return (
        <>
            <Helmet>
                <title>{AppSettings.Titles.Main + " | " + _currentPage.caption}</title>
            </Helmet>
            {renderWizardBreadcrumbs(_mode, _navInfo.stepNum)}
            {renderWizardHeader(`Step ${_navInfo.stepNum}: ${_currentPage.caption}`)}
            {renderWizardIntroContent(_currentPage.introContent)}
            {renderMainContent()}
            {renderWizardButtonRow(_navInfo)}
            <ErrorModal modalData={_error} callback={onErrorModalClose} />
        </>
    )
}

export default WizardNewProfile;