namespace CESMII.ProfileDesigner.Common.Models
{
    using System.Collections.Generic;
    using Microsoft.AspNetCore.Cryptography.KeyDerivation;

    public class PasswordConfig
    {
        //TBD - add in password settings - max retries, password minimums, etc. 
        public int SessionLength { get; set; }
        public int RandomPasswordLength { get; set; }

        public EncryptionConfig EncryptionSettings { get; set; }
    }

    public class EncryptionConfig
    {
        public int CurrentLevel { get; set; }

        public List<EncryptionLevelConfig> Levels { get; set; }
    }

    public class EncryptionLevelConfig
    {
        public int Id { get; set; }

        public KeyDerivationPrf PRF { get; set; }

        public int Iterations { get; set; }

        public int NumBytes { get; set; }

    }
}
