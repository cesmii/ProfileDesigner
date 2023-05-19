import React, { useEffect } from 'react'
import Form from 'react-bootstrap/Form'
import { useMsal } from "@azure/msal-react";

import { generateLogMessageString, validate_namespaceFormat, validate_Required } from '../../utils/UtilityService'
import '../styles/ProfileEntity.scss';
import { isOwner } from './ProfileRenderHelpers';

import { Prompt } from 'react-router'

import spdxExpressionValidate from 'spdx-expression-validate';
import { useLoadingContext } from "../../components/contexts/LoadingContext";

const CLASS_NAME = "ProfileEntityForm";

function ProfileEntityForm(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const { instance } = useMsal();
    const _activeAccount = instance.getActiveAccount();
    const { loadingProps, setLoadingProps } = useLoadingContext();

    // Execute only one time after the component mounts.
    useEffect(() => {
        // Init flags to detect unsaved changes and warn a user when they try to leave the page
        setLoadingProps({ bIsProfileEditUnsaved: false });
    }, []);

    //-------------------------------------------------------------------
    // Region: Validation
    //-------------------------------------------------------------------
    const validateForm_namespace = (e) => {
        var isValid = {
            namespace: validate_Required(e.target.value),
            namespaceFormat: validate_namespaceFormat(e.target.value)
        };
        if (props.onValidate) props.onValidate(isValid);
    };

    const validateForm_license = (e) => {
        var isValid = {
            licenseExpression: e.target.value == null || e.target.value ==="" ? true : spdxExpressionValidate(e.target.value)
        };
        if (props.onValidate) props.onValidate(isValid);
    };

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    //on change handler to update state
    const onChange = (e) => {
        //console.log(generateLogMessageString(`onEntityChange||e:${e.target}`, CLASS_NAME));
        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        props.item[e.target.id] = e.target.value;

        //pass a copy of the updated object to parent to update state
        if (props.onChange) props.onChange(JSON.parse(JSON.stringify(props.item)));
        setLoadingProps({ bIsProfileEditUnsaved: true });
    }

    const onChangeAuthor = (e) => {
        //console.log(generateLogMessageString(`onEntityChange||e:${e.target}`, CLASS_NAME));
        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        props.item[e.target.id] = e.target.value === '' ? null : { id: null, name: e.target.value };

        //pass a copy of the updated object to parent to update state
        if (props.onChange) props.onChange(JSON.parse(JSON.stringify(props.item)));
    }

    //on change publish date handler to update state
    const onChangePublishDate = (e) => {

        //if user types directly into year field, it prematurely does an onChange event fire.
        //This prevents that:
        if (e.target.value !== '') {
            var dt = new Date(e.target.value);
            if (dt.getFullYear() < 2000) return;
        }

        //update the state
        props.item[e.target.id] = e.target.value === '' ? null : e.target.value;

        //pass a copy of the updated object to parent to update state
        if (props.onChange) props.onChange(JSON.parse(JSON.stringify(props.item)));
    }

    //Dates will come in two formats:
    //  a. W/ Timezone info (typically from server): 2021-09-24T00:00:00
    //  b. No timezone info (after editing in control): 2021-09-24
    const prepDateVal = (val) => {
        if (val == null || val === '') return '';
        //check and append timezone so we get consistent conversion
        //if (val.indexOf('T00:00:00') === -1) val += `T00:00:00`;
        //DB changed to support date and time timestamp so T:00:00:00 no longer reliable, just check for T
        if (val.indexOf('T') === -1) val += `T00:00:00`;

        var dt = new Date(val);
        var mm = dt.getUTCMonth() + 1;
        mm = mm < 10 ? `0${mm.toString()}` : mm.toString();
        var dd = dt.getUTCDate();
        dd = dd < 10 ? `0${dd.toString()}` : dd.toString();
        var result = `${dt.getUTCFullYear()}-${mm}-${dd}`;
        console.log(generateLogMessageString(`prepDateVal||inbound:${val}||outbound:${result}`, CLASS_NAME));
        return result;
    }

    const onChangeKeywords = (e) => {
        //console.log(generateLogMessageString(`onChangeKeywords||e:${e.target}`, CLASS_NAME));
        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.

        props.item.keywords = e.target.value.split(",").map(x => x.trim(' '));

        //pass a copy of the updated object to parent to update state
        if (props.onChange) props.onChange(JSON.parse(JSON.stringify(props.item)));
    }
    const onChangeLicense = (e) => {
        //console.log(generateLogMessageString(`onChangeLicense||e:${e.target}`, CLASS_NAME));
        //note you must update the state value for the input to be read only. It is not enough to simply have the onChange handler.
        props.item.license = e.target.value;

        //pass a copy of the updated object to parent to update state
        if (props.onChange) props.onChange(JSON.parse(JSON.stringify(props.item)));
    }

    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderForm = () => {
        var isReadOnly = mode === "view";
        return (
            <>
                <Prompt
                    when={loadingProps.bIsProfileEditUnsaved}
                    message="Unsaved changes will be lost. Ok to exit the page? line 128 in shared-ProfileEntity.js"
                />
                <div className="row">
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label>Title</Form.Label>
                            <Form.Control id="title" type="" placeholder="" value={props.item.title ? props.item.title : ''} onChange={onChange} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                    <div className="col-12">
                        <Form.Group className="mb-1">
                            <Form.Label>Namespace*</Form.Label>
                            {!props.isValid.namespace &&
                                <span className="invalid-field-message inline">
                                    Required
                                </span>
                            }
                            {!props.isValid.namespaceFormat &&
                                <span className="invalid-field-message inline">
                                    Invalid format (http://www.mycompany.org/myprofile)
                                </span>
                            }
                            <Form.Control className={(!props.isValid.namespace || !props.isValid.namespaceFormat ? 'invalid-field' : '')} id="namespace" type=""
                                value={props.item.namespace ? props.item.namespace : ''} onBlur={validateForm_namespace} onChange={onChange} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                    <div className="col-sm-6">
                        <Form.Group className="mb-1">
                            <Form.Label>Version</Form.Label>
                            <Form.Control id="version" type="" value={props.item.version == null ? '' : props.item.version} onChange={onChange} readOnly={mode === "view"} />
                        </Form.Group>
                    </div>
                    <div className="col-sm-6">
                        <Form.Group className="mb-1">
                            <Form.Label>Publication Date</Form.Label>
                            <Form.Control id="publishDate" mindate="2010-01-01" type={mode === "view" ? "" : "date"} value={prepDateVal(props.item.publishDate)} onChange={onChangePublishDate} readOnly={mode === "view"} />
                        </Form.Group>
                    </div>
                </div>
                <div className="row mt-2">
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label>Description</Form.Label>
                            <Form.Control as="textarea" id="description" type="" placeholder="" value={props.item.description ? props.item.description : ''} onChange={onChange} readOnly={isReadOnly}>{props.item.description}</Form.Control>
                        </Form.Group>
                    </div>
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label>Contributor</Form.Label>
                            <Form.Control id="contributorName" type="" placeholder="" value={props.item.contributorName ? props.item.contributorName : ''} onChange={onChange} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label>Keywords</Form.Label>
                            <Form.Control id="keywordsConcatenated" type="" placeholder="" value={props.item.keywords == null ? '' : props.item.keywords?.join(', ')} onChange={onChangeKeywords} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label>Copyright</Form.Label>
                            <Form.Control id="copyrightText" type="" placeholder="" value={props.item.copyrightText == null ? '' : props.item.copyrightText} onChange={onChange} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label>License</Form.Label>
                            {!props.isValid.licenseExpression &&
                                <span className="invalid-field-message inline">
                                    Invalid license expression. (Refer to the <a href="https://spdx.org/licenses/" target="_blank" rel="noreferrer">SPDX License list</a> for valid identifiers.)
                                </span>
                            }
                            <Form.Control id="license" type="" placeholder="" value={props.item.license == null ? '' : props.item.license} onChange={onChangeLicense}
                                onBlur={validateForm_license} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label>License URL</Form.Label>
                            <Form.Control id="licenseUrl" type="" placeholder="" value={props.item.licenseUrl == null ? '' : props.item.licenseUrl} onChange={onChange} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                    {/*<div className="col-md-12">*/}
                    {/*    <Form.Group>*/}
                    {/*        <Form.Label>Contributor Organization</Form.Label>*/}
                    {/*        <Form.Control id="contributorName" type="" placeholder="" value={props.item.contributorName == null ? '' : props.item.contributorName} onChange={onChange} readOnly={isReadOnly} />*/}
                    {/*    </Form.Group>*/}
                    {/*</div>*/}
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label>Category</Form.Label>
                            <Form.Control id="categoryName" type="" placeholder="" value={props.item.categoryName == null ? '' : props.item.categoryName} onChange={onChange} readOnly={isReadOnly} />
                        </Form.Group>
                    </div>
                    {/*<div className="col-md-12">*/}
                    {/*    <Form.Group>*/}
                    {/*        <Form.Label>Documentation URL</Form.Label>*/}
                    {/*        <Form.Control id="documentationUrl" type="" placeholder="" value={props.item.documentationUrl == null ? '' : props.item.documentationUrl} onChange={onChange} readOnly={isReadOnly} />*/}
                    {/*    </Form.Group>*/}
                    {/*</div>*/}
                    {/*<div className="col-md-12">*/}
                    {/*    <Form.Group>*/}
                    {/*        <Form.Label>Icon URL</Form.Label>*/}
                    {/*        <Form.Control id="iconUrl " type="" placeholder="" value={props.item.iconUrl == null ? '' : props.item.iconUrl} onChange={onChange} readOnly={isReadOnly} />*/}
                    {/*    </Form.Group>*/}
                    {/*</div>*/}
                    {/*<div className="col-md-12">*/}
                    {/*    <Form.Group>*/}
                    {/*        <Form.Label>Purchasing Information URL</Form.Label>*/}
                    {/*        <Form.Control id="purchasingInformationUrl " type="" placeholder="" value={props.item.purchasingInformationUrl == null ? '' : props.item.purchasingInformationUrl} onChange={onChange} readOnly={isReadOnly} />*/}
                    {/*    </Form.Group>*/}
                    {/*</div>*/}
                    {/*<div className="col-md-12">*/}
                    {/*    <Form.Group>*/}
                    {/*        <Form.Label>Release notes URL</Form.Label>*/}
                    {/*        <Form.Control id="releaseNotesUrl" type="" placeholder="" value={props.item.releaseNotesUrl == null ? '' : props.item.releaseNotesUrl} onChange={onChange} readOnly={isReadOnly} />*/}
                    {/*    </Form.Group>*/}
                    {/*</div>*/}
                    {/*<div className="col-md-12">*/}
                    {/*    <Form.Group>*/}
                    {/*        <Form.Label>Test Specification URL</Form.Label>*/}
                    {/*        <Form.Control id="testSpecificationUrl" type="" placeholder="" value={props.item.testSpecificationUrl == null ? '' : props.item.testSpecificationUrl} onChange={onChange} readOnly={isReadOnly} />*/}
                    {/*    </Form.Group>*/}
                    {/*</div>*/}
                    {/*    <div className="col-md-12">*/}
                    {/*        <Form.Group>*/}
                    {/*            <Form.Label>Additional Properties</Form.Label>*/}
                    {/*            <Form.Control id="additionalProperties" type="" placeholder="" value={props.item.additionalProperties == null ? '' : props.item.additionalProperties} onChange={onChange} readOnly={isReadOnly} />*/}
                    {/*        </Form.Group>*/}
                    {/*    </div>*/}
                </div>
                <div className="row mt-2">
                    {/*    <div className="col-md-12">*/}
                    {/*        <Form.Group>*/}
                    {/*            <Form.Label>Author name</Form.Label>*/}
                    {/*            <Form.Control id="author" type="" placeholder="" value={props.item.author == null ? '' : props.item.author?.name} onChange={onChangeAuthor} readOnly={isReadOnly} />*/}
                    {/*        </Form.Group>*/}
                    {/*    </div>*/}
                    {/*        <div className="col-md-12">*/}
                    {/*            <Form.Group>*/}
                    {/*                <Form.Label>Publisher Organization</Form.Label>*/}
                    {/*                <Form.Control id="organization" type="" placeholder="" value={props.item.organization == null ? '' : props.item.organization} onChange={onChange} readOnly={isReadOnly} />*/}
                    {/*            </Form.Group>*/}
                    {/*        </div>*/}
                </div>
            </>
        );
    };

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    if (props.item == null) return;

    var mode = "view";
    if (props.item.id === null || props.item.id === 0) mode = "new";
    else if (!props.item.isReadOnly && isOwner(props.item, _activeAccount) && props.item.id > 0) mode = "edit";

    //return final ui
    return (
        <>
            {renderForm()}
        </>
    )
}

export default ProfileEntityForm;
