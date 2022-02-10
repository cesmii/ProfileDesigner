namespace CESMII.ProfileDesigner.Common.Models
{

    public class ProfilesConfig
    {
        public ReservedProfilesConfig ReservedProfileNames { get; set; }
    }

    public class ReservedProfilesConfig
    {
        public string CompositionRootProfileName { get; set; }
        public string InterfaceRootProfileName { get; set; }
        public string PropertyVariableRootProfileName { get; set; }
        public string StructureRootProfileName { get; set; }
    }
}
