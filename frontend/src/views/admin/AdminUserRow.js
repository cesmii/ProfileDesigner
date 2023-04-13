import React from 'react'
import { formatDate } from '../../utils/UtilityService';

//const CLASS_NAME = "AdminUserRow";

function AdminUserRow(props) { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------

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
                        Display Name
                    </th>
                    <th className="" >
                        Email Address
                    </th>
                    <th className="" >
                        Organization
                    </th>
                    <th className="" >
                        Object ID (AAD)
                    </th>
                    <th className="" >
                        Last Login
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
                    {props.item.displayName != null ? props.item.displayName : '(Not logged in yet)'}
                </td>
                <td className="py-2 align-middle" >
                    {props.item.email}
                </td>
                <td className="py-2 align-middle" >
                    {props.item.organization != null && props.item.organization.name != null ? props.item.organization.name : ' '}
                </td>
                <td className="py-2 align-middle" >
                    {props.item.objectIdAAD}
                </td>
                <td className="py-2 align-middle" >
                    {formatDate(props.item.lastLogin)}
                </td>
            </tr>
        </>
    );
}

export default AdminUserRow;