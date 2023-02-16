import React from 'react'
import { formatDate } from '../../utils/UtilityService';
import { generateLogMessageString } from '../../utils/UtilityService'
import { Button } from 'react-bootstrap'

const CLASS_NAME = "AdminCloudLibApprovalRow";

function AdminCloudLibApprovalRow(props) { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------

    const onChangeApprovalStatus = (item) => {
        console.log(generateLogMessageString('onChangeApprovalStatus', CLASS_NAME));
        if (props.onChangeApprovalStatus) props.onChangeApprovalStatus(item);
    }

    //-------------------------------------------------------------------
    // Region: Render helpers
    //-------------------------------------------------------------------

    //build the row
    //-------------------------------------------------------------------
    // Region: Render final output
    //-------------------------------------------------------------------
    var cssClass = props.cssClass + (props.isHeader ? " bottom header" : " center border-top");

    if (props.isHeader) {
        return (
            <>
                <tr className={`mx-0 my-1 p-0 py-1 ${cssClass}`}>
                    <th className="" >
                        User
                    </th>
                    <th className="" >
                        Title
                    </th>
                    <th className="" >
                        Namespace
                    </th>
                    <th className="" >
                        Version
                    </th>
                    <th className="" >
                        PublicationDate
                    </th>
                    <th className="" >
                        Nodeset
                    </th>
                    <th className="" >
                        Approval Status
                    </th>
                    <th className="" >
                        Approval Status Description
                    </th>
                    <th className="" >
                        License
                    </th>
                    <th className="" >
                        Copyright
                    </th>
                    <th className="" >
                        Contributor
                    </th>
                    <th className="" >
                        Category
                    </th>
                    <th className="" >
                        Description
                    </th>
                </tr>
            </>
        );
    }

    //item row
    if (props.item === null || props.item === {}) return null;

    return (
        <>
            <tr className={`mx-0 my-1 p-0 py-1 ${cssClass}`}>
                <td className="py-2 align-middle" >
                    <a href={`/admin/user/${props.item.author.id}`}>{props.item.author.displayName}</a>
                </td>
                <td className="py-2 align-middle" >
                    {props.item.title}
                </td>
                <td className="py-2 align-middle" >
                    {props.item.namespace}
                </td>
                <td className="py-2 align-middle" >
                    {props.item.version}
                </td>
                <td className="py-2 align-middle" >
                    {formatDate(props.item.publishDate)}
                </td>
                <td className="py-2 align-middle" >
                    <a href={`/cloudlibrary/viewer/${props.item.cloudLibraryId}`}>View in Profile Designer</a> (ID: {props.item.cloudLibraryId})
                </td>
                <td className="py-2 align-middle" >
                    {props.item.cloudLibApprovalStatus}
                    <Button variant="secondary" className="auto-width mx-2" onClick={() => onChangeApprovalStatus(props.item)}>Change Status</Button>
                </td>
                <td className="py-2 align-middle" >
                    {props.item.cloudLibApprovalDescription}
                </td>
                <td className="py-2 align-middle" >
                    {props.item.license}
                </td>
                <td className="py-2 align-middle" >
                    {props.item.copyrightText}
                </td>
                <td className="py-2 align-middle" >
                    {props.item.contributorName}
                </td>
                <td className="py-2 align-middle" >
                    {props.item.categoryName}
                </td>
                <td className="py-2 align-middle" >
                    {props.item.description}
                </td>

            </tr>
        </>
    );
}

export default AdminCloudLibApprovalRow;