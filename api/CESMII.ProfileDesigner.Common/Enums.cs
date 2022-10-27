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
    /// These are used by the profile definition. 
    /// These are the parent categories and will be used in the front end and the search endpoint
    /// </summary>
    public enum ProfileSearchCriteriaCategoryEnum
    {
        /// <summary>
        /// Profiles owned or editable by the user
        /// </summary>
        Profile = 1,
        /// <summary>
        /// Read-only profiles available to/imported by the user
        /// </summary>
        BaseProfile = 2,
        /// <summary>
        /// Cloud Library profiles available for import
        /// </summary>
        CloudLib = 3,
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
