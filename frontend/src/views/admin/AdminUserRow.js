import React from 'react'

//const CLASS_NAME = "AdminUserRow";

function AdminUserRow(props) { //props are item, showActions

    //-------------------------------------------------------------------
    // Region: Event Handling of child component events
    //-------------------------------------------------------------------
    const onDeleteItem = (e) => {
        if (props.onDeleteItem) props.onDeleteItem(props.item);
        e.preventDefault();
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
                    <th className="pl-2" >

                    </th>
                    <th className="" >
                        User Name
                    </th>
                    <th className="" >
                        First Name
                    </th>
                    <th className="" >
                        Last Name
                    </th>
                    <th className="text-center" >
                        Status
                    </th>
                    <th className="pr-2 text-right" >
                        Copy
                    </th>
                    <th className="pr-2 text-right" >
                        Delete
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
                <td className="py-2 pl-2" >
                    {props.item.isActive &&
                        <a className="btn btn-icon-outline circle mr-2 d-inline-flex" href={`/admin/user/${props.item.id}`} ><i className="material-icons">edit</i></a>
                    }
                </td>
                <td className="py-2 align-middle" >
                    {props.item.userName}
                </td>
                <td className="py-2 align-middle" >
                    {props.item.firstName}
                </td>
                <td className="py-2 align-middle" >
                    {props.item.lastName}
                </td>
                <td className="py-2 pr-2 text-center align-middle" >
                    <i className={`material-icons ${props.item.isActive ? "text-success" : "text-danger"}`} title={props.item.isActive ? "Active" : "Deleted"}>{props.item.isActive ? "toggle_on" : "toggle_off"}</i>
                </td>
                <td className="py-2 pr-2 text-right" >
                    {props.item.isActive &&
                        <a className="btn btn-icon-outline circle ml-auto d-inline-flex" href={`/admin/user/copy/${props.item.id}`} title="Copy" ><i className="material-icons">content_copy</i></a>
                    }
                </td>
                <td className="py-2 pr-2 text-right" >
                    {props.item.isActive &&
                        <button className="btn btn-icon-outline circle ml-auto" title="Delete Item" onClick={onDeleteItem} ><i className="material-icons">close</i></button>
                    }
                </td>
            </tr>
        </>
    );
}

export default AdminUserRow;