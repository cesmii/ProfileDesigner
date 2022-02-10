namespace CESMII.ProfileDesigner.Data.Entities
{
    using System.ComponentModel.DataAnnotations.Schema;

    /// TBD - remove once we cutover to new data type approach
    /// <summary>
    /// Join table between a profile and a custom data type (which is a type of profile)
    /// </summary>
    /// <remarks>A profile can can have many customDataTypes and a customDataType can have many profiles.</remarks>
    //public class ProfileCustomDataType : AbstractEntity
    //{
    //    [Column(name: "profile_id")]
    //    public int ProfileId { get; set; }

    //    [Column(name: "custom_data_type_id")]
    //    public int CustomDataTypeId { get; set; }

    //    /// <summary>
    //    /// Different than the profile's name. This is the name for this usage of this as the variable type. 
    //    /// Profile.name could be MessageCode but this var type calls it MyMessageCode.
    //    /// </summary>
    //    [Column(name: "name")]
    //    public string Name { get; set; }

    //    /// <summary>
    //    /// Different than the profile's description. This is the description for this usage of this as the variable type. 
    //    /// </summary>
    //    [Column(name: "description")]
    //    public string Description { get; set; }

    //    /// <summary>
    //    /// This identifier is purely used in OPC UA nodesets. This will help us identify a node
    //    /// using something other than name and namespace. 
    //    /// </summary>
    //    [Column(name: "opc_node_id")]
    //    public string OpcNodeId { get; set; }

    //    /// <summary>
    //    /// The namespace of the OpcNodeId
    //    /// </summary>
    //    [Column(name: "namespace")]
    //    public string Namespace { get; set; }

    //    public virtual ProfileItem Profile { get; set; }

    //    public virtual ProfileItem CustomDataType { get; set; }
    //}
}