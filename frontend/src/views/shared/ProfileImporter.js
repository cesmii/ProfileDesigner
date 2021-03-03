import React, { useEffect, useState } from 'react'
import axios from 'axios'
import { useHistory } from 'react-router-dom'

import { useLoadingContext } from '../../components/contexts/LoadingContext';
import { useAuthContext } from '../../components/authentication/AuthContext';
import { AppSettings } from '../../utils/appsettings'
import { generateLogMessageString } from '../../utils/UtilityService'

import Card from 'react-bootstrap/Card'
import ListGroup from 'react-bootstrap/ListGroup'

import { SVGIcon } from '../../components/SVGIcon'
import color from '../../components/Constants'

const CLASS_NAME = "ProfileImporter";

const entityInfo = {
    name: "Profile",
    namePlural: "Profiles",
    entityUrl: "/profile/:id",
    listUrl: "/profile/all"
}

function ProfileImporter() {

    //-------------------------------------------------------------------
    // Region: Initialization
    //-------------------------------------------------------------------
    const history = useHistory();
    const { loadingProps, setLoadingProps } = useLoadingContext();
    const [_dataRows, setDataRows] = useState([]);
    const { authTicket } = useAuthContext();
    let _fileReader;
    let _isMyProfile;

    //-------------------------------------------------------------------
    // Region: Event Handling
    //-------------------------------------------------------------------
    const onMyProfileFileChange = (e) => {
        console.log(generateLogMessageString(`onMyProfileFileChange`, CLASS_NAME));
        _isMyProfile = true;
        onFileUpload(e.target.files[0]);
    }

    const onProfileLibraryFileChange = (e) => {
        console.log(generateLogMessageString(`onProfileLibraryFileChange`, CLASS_NAME));
        _isMyProfile = false;
        onFileUpload(e.target.files[0]);
    }

    const onFileReadEnd = (e) => {

        console.log(generateLogMessageString(`onFileReadEnd`, CLASS_NAME));
        var data = _fileReader.result;
        var profile = prepareAndValidateImportedProfile(data);

        if (profile == null) return;

        saveFile(profile, _isMyProfile);
    }

    const onFileUpload = (fileInfo) => {
        console.log(generateLogMessageString(`onFileUpload`, CLASS_NAME));
        console.log(fileInfo);
        //Read the file in. add a callback fn to process read file
        _fileReader = new FileReader();
        _fileReader.onloadend = onFileReadEnd;
        _fileReader.readAsText(fileInfo);
    }

    //-------------------------------------------------------------------
    // Region: Get profile list data - prerequisite
    //-------------------------------------------------------------------
    useEffect(() => {
        //TBD - enhance the mock api to return profiles by user id
        async function fetchData() {
            //show a spinner
            setLoadingProps({ isLoading: true, message: null });

            var url = `${AppSettings.BASE_API_URL}/profile`;
            console.log(generateLogMessageString(`useEffect||fetchData||${url}`, CLASS_NAME));
            const result = await axios(url);

            //set state on fetch of data - this list is used downstream when checking for unique profile names
            setDataRows(result.data);

            //hide a spinner
            setLoadingProps({ isLoading: false, message: null });
        }
        fetchData();
        //this will execute on unmount
        return () => {
            console.log(generateLogMessageString('useEffect||Cleanup', CLASS_NAME));
        };
    }, []);

    //-------------------------------------------------------------------
    // Region: File processing and saving methods
    //-------------------------------------------------------------------
    //dup name check
    const findUniqueName = (name) => {

        var uniqueName = name;
        var isNameUnique = false;
        var counter = 1;
        while (!isNameUnique) {
            var matchIndex = _dataRows.findIndex((p) => { return p.name.toLowerCase() === uniqueName.toLowerCase(); });
            isNameUnique = (matchIndex === -1);
            if (!isNameUnique) {
                uniqueName = `${name}(${counter.toString()})`;
                counter++;
            }
        }

        return uniqueName;
    }

    //-------------------------------------------------------------------
    // Save file to repo
    //-------------------------------------------------------------------
    const saveFile = (item, isMyProfile) => {
        //Assign a unique id
        //set ownership to me if I am importing into my profiles.
        item.id = new Date().getTime(); //TBD - in phase II, the server side and likely the db will issue the new id
        if (isMyProfile) {
            item.author = authTicket.user;
        }
        //check dup name and append (1) if already present
        item.name = findUniqueName(item.name);
        axios.post(
            `${AppSettings.BASE_API_URL}/profile`, item)
            .then(resp => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "success", body: `Profile '${item.name}' was imported successfully.` }
                    ],
                    profileCount: { all: loadingProps.profileCount.all + 1, mine: loadingProps.profileCount.mine + (_isMyProfile ? 1 : 0) }
                });
                //navigate to the profile page to see the newly imported file
                history.push(entityInfo.entityUrl.replace(':id', resp.data.id));
            })
            .catch(error => {
                //hide a spinner, show a message
                setLoadingProps({
                    isLoading: false, message: null, inlineMessages: [
                        { id: new Date().getTime(), severity: "danger", body: `An error occurred saving the imported profile.` }
                    ]
                });
                console.log(generateLogMessageString('handleOnSave||saveFile||' + JSON.stringify(error), CLASS_NAME, 'error'));
                console.log(error);
            });
    }

    //-------------------------------------------------------------------
    // Prepare and validate an imported profile
    //  Return either the imported file object OR null if stuff fails
    //  Write a message to the screen if errors occur
    //-------------------------------------------------------------------
    const prepareAndValidateImportedProfile = (data) => { //props are item, showActions
        console.log(generateLogMessageString(`prepareAndValidateImportedProfile`, CLASS_NAME));

        // Region: Prep and validate
        var errors = [];
        //try to parse, if fails, then stop
        var result = null;
        try {
            result = JSON.parse(data);
        }
        catch (err) {
            setLoadingProps({
                isLoading: false, message: null, inlineMessages: [
                    { id: new Date().getTime(), severity: "danger", body: `The imported file was not in the expected format. Please use an approved file containing JSON data with the proper expected file structre.` }
                ]
            });
            return null;
        }

        //TBD - add validation

        if (errors.length === 0) {
            return result;
        }

        // Region: Process errors and return result
        if (errors.length > 0) {
            var msg = '';
            errors.forEach(e => {
                msg += msg === '' ? e : '\r\n' + e;
            })
            setLoadingProps({
                isLoading: false, message: null, inlineMessages: [
                    { id: new Date().getTime(), severity: "danger", body: `An error(s) occurred importing this profile: ${errors.length > 1 ? "\r\n" : ""} ${msg}` }
                ]
            });
        }
        //return true false
        return (errors.length > 0 ? null : result);
    }

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------

    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    return (
        <>
            <Card style={{ height: '360px', maxWidth: '800px', padding: '32px' }} className="mt-5 elevated">
                <Card.Body className="">
                    <Card.Text className="text-muted mb-4">
                        Import existing profiles to your folder or to the profiles library. Then extend them to make new ones!
                    </Card.Text>
                    <ListGroup className="list-group-flush">
                        <ListGroup.Item className="d-flex align-items-center">
                            <SVGIcon name="folder-shared" size="48" fill={color.outerSpace} alt="My Profiles" className="mr-4" />
                            <p className="h4 m-0">My profiles</p>
                            <label className="btn btn-secondary ml-auto">
                                Import<input type="file" onChange={onMyProfileFileChange} style={{ display: "none" }} />
                            </label>
                        </ListGroup.Item>
                        <ListGroup.Item className="d-flex align-items-center">
                            <SVGIcon name="folder-profile" size="48" fill={color.outerSpace} alt="My Profiles" className="mr-4" />
                            <p className="h4 m-0">Profiles library</p>
                            <label className="btn btn-secondary ml-auto">
                                Import<input type="file" onChange={onProfileLibraryFileChange} style={{ display: "none" }} />
                            </label>
                        </ListGroup.Item>
                    </ListGroup>
                </Card.Body>
            </Card>
        </>
    )
}

export default ProfileImporter;
