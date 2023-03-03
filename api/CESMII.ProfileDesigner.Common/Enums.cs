namespace CESMII.ProfileDesigner.Common.Enums
{
    using System.ComponentModel;

    public enum LookupTypeEnum
    {
        ProfileType = 1,
        AttributeType = 2,
        //EngUnit = 3, No longer used. Replaced by EngineeringUnitModel
        TaskStatus = 4
    }

    public enum ProfileItemTypeEnum
    {
        Interface = 1,
        Class = 2,
        VariableType = 12,
        CustomDataType = 3,
        Structure = 18, //9,
        Enumeration = 19, //10,
        Object = 11,
        Method = 20
    }

    public enum AttributeTypeIdEnum
    {
        Property = 5,
        DataVariable = 6,
        StructureField = 7,
        EnumField = 8
    }

    public enum TaskStatusEnum
    {
        NotStarted = 13,
        InProgress = 14,
        Completed = 15,
        Failed = 16,
        Cancelled = 17
    }

    /// <summary>
    /// These are used by the profile type definition. 
    /// These are the parent categories and will be used in the front end and the search endpoint
    /// </summary>
    public enum SearchCriteriaCategoryEnum
    {
        Author = 1,
        Popular = 2,
        TypeDefinitionType = 3,
        Profile = 4
    }

    public enum SearchCriteriaSortByEnum
    {
        Author = 1,
        Popular = 2,
        Name = 3
    }

    /// <summary>
    /// These are used by the profile search groups. 
    /// These are the parent categories and will be used in the front end and the search endpoint
    /// </summary>
    public enum ProfileSearchCriteriaCategoryEnum
    {
        /// <summary>
        /// Category group of profile sources
        /// </summary>
        Source = 1
    }

    /// <summary>
    /// These are used by the profile definition. 
    /// These are the parent categories and will be used in the front end and the search endpoint
    /// </summary>
    public enum ProfileSearchCriteriaSourceEnum
    {
        /// <summary>
        /// Profiles owned or editable by the user
        /// </summary>
        Mine = 1,
        /// <summary>
        /// Read-only profiles available to/imported by the user
        /// </summary>
        BaseProfile = 2,
        /// <summary>
        /// Cloud Library profiles available for import
        /// </summary>
        CloudLib = 3,
    }

    public enum ProfileStateEnum
    {
        /// <summary>
        /// Core nodeset: ua or ua/di nodeset
        /// Not owned by anyone and one of the foundational nodesets - can't be changed
        /// Readonly state
        /// </summary>
        Core = 1,
        /// <summary>
        /// A published nodeset that the current user has downloaded into their view
        /// ua/robotics or it could be a nodeset that they published that is now in the CloudLib
        /// Readonly state
        /// </summary>
        CloudLibPublished = 2,
        /// <summary>
        /// A nodeset that is in a pending state of being published. This would be pending but ot approved yet. 
        /// Readonly state
        /// </summary>
        CloudLibPending = 3,
        /// <summary>
        /// A nodeset that is in submitted but rejected by approver. This could be pending or rejected but not withdrawn
        /// Readonly state
        /// </summary>
        CloudLibApproved = 4,
        /// <summary>
        /// A nodeset that is in submitted but rejected by approver. This could be pending or rejected but not withdrawn
        /// Readonly state
        /// </summary>
        CloudLibRejected = 5,
        /// <summary>
        /// A nodeset that is in submitted but rejected by approver. This could be pending or rejected but not withdrawn
        /// Readonly state
        /// </summary>
        CloudLibCancelled = 6,
        /// <summary>
        /// A nodeset that is currently being modified by the user. This user owns this proifle and can modify.
        /// </summary>
        Local = 7,
        /// <summary>
        /// Unknown scenario is not expected...
        /// </summary>
        Unknown = 0
    }

    public enum PermissionEnum
    {
        /*
        [Description("CanViewProfile")]
        CanViewProfile = 20,

        [Description("CanManageProfile")]
        CanManageProfile = 21,

        [Description("CanDeleteProfile")]
        CanDeleteProfile = 22,

        [Description("CanManageUsers")]
        CanManageUsers = 100,

        [Description("CanManageSystemSettings")]
        CanManageSystemSettings = 110,

        [Description("CanImpersonateUsers")]
        CanImpersonateUsers = 120,
        */
        [Description("UserAzureADMapped")]
        UserAzureADMapped = 130
    }
}
