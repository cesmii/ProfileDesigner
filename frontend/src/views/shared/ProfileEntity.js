import React from 'react'

import Form from 'react-bootstrap/Form'

import { generateLogMessageString, validate_namespaceFormat, validate_Required } from '../../utils/UtilityService'
import { useAuthState } from "../../components/authentication/AuthContext";

import '../styles/ProfileEntity.scss';

const CLASS_NAME = "ProfileEntity";

function ProfileEntity(props) {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const authTicket = useAuthState();

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
        if (val == null) return '';
        //check and append timezone so we get consistent conversion
        if (val.indexOf('T00:00:00') === -1) val += `T00:00:00`;

        var dt = new Date(val);
        var mm = dt.getMonth() + 1;
        mm = mm < 10 ? `0${mm.toString()}` : mm.toString();
        var dd = dt.getDate();
        dd = dd < 10 ? `0${dd.toString()}` : dd.toString();
        var result = `${dt.getFullYear()}-${mm}-${dd}`;
        console.log(generateLogMessageString(`prepDateVal||inbound:${val}||outbound:${result}`, CLASS_NAME));
        return result;
    }
    //-------------------------------------------------------------------
    // Region: Render Helpers
    //-------------------------------------------------------------------
    const renderForm = () => {
        return (
            <>
                <div className="row">
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
                                value={props.item.namespace} onBlur={validateForm_namespace} onChange={onChange} readOnly={mode === "view"} />
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
                            <Form.Label>Publish Date</Form.Label>
                            <Form.Control id="publishDate" mindate="2010-01-01" type="date" value={prepDateVal(props.item.publishDate)} onChange={onChangePublishDate} readOnly={mode === "view"} />
                        </Form.Group>
                    </div>
                </div>
                <div className="row mt-2">
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label>Author name</Form.Label>
                            <Form.Control id="author" type="" placeholder="" value={props.item.author == null ? '' : props.item.author.name} onChange={onChangeAuthor} />
                        </Form.Group>
                    </div>
                    <div className="col-md-12">
                        <Form.Group>
                            <Form.Label>Publisher Organization</Form.Label>
                            <Form.Control id="organization" type="" placeholder="" value={props.item.organization == null ? '' : props.item.organization} onChange={onChange} />
                        </Form.Group>
                    </div>
                </div>
            </>
        );
    };

    //-------------------------------------------------------------------
    // Region: Render
    //-------------------------------------------------------------------
    if (props.item == null) return;

    var mode = "view";
    if (!props.item.isReadOnly && props.item.authorId === authTicket.user.id && props.item.id > 0) mode = "edit";
    if (props.item.id === null || props.item.id === 0) mode = "new";

    //return final ui
    return (
        <>
            {renderForm() }
        </>
    )
}

export default ProfileEntity;
