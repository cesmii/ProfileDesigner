namespace CESMII.ProfileDesigner.Common
{
    using System;
    using System.Security.Cryptography;
    using Microsoft.AspNetCore.Cryptography.KeyDerivation;

    using CESMII.ProfileDesigner.Common.Models;

    public static class PasswordUtils
    {
        private static readonly string _delimiter = "$";
        private static readonly string _delimiterLegacy = "$";

        /// <summary>
        /// Generate a random password with no special characters
        /// </summary>
        /// <param name="length">
        /// The desired length of the password to be generated.
        /// </param>
        /// <returns>
        /// Randomly generated password.
        /// </returns>
        public static string GenerateRandomPassword(int length = 8)
        {
            const string Chars = "0123456789abcdefghijklmnopqrstuvwxyz";
            var rnd = new Random();
            var result = new System.Text.StringBuilder();
            for (var i = 0; i < length; i++)
            {
                var randomPosition = rnd.Next(0, Chars.Length - 1);
                var randomCase = rnd.Next(0, 1);
                var randomChar = Chars.Substring(randomPosition, 1);
                result.Append(randomCase == 0 ? randomChar.ToLower() : randomChar.ToUpper());
                //Code Smell: use a stringbuilder instead...result += (randomCase == 0 ? randomChar.ToLower() : randomChar.ToUpper());
            }

            return result.ToString();
        }

        /// <summary>
        /// Encrypt a new password.This will also include key derivation value, num bytes, iterations, salt, 
        /// </summary>
        /// <returns>
        /// Encrypted value of the password provided.
        /// </returns>
        public static string EncryptNewPassword(EncryptionConfig encrConfig, string password)
        {
            var randomSalt = new byte[16];
            //populate new salt w/ random bytes
            using (var rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetBytes(randomSalt);
            }

            EncryptionLevelConfig encrLevel = encrConfig.Levels.Find(e => e.Id == encrConfig.CurrentLevel);
            if (encrLevel == null) throw new ArgumentNullException($"Encryption Config Level is missing. Check appSettings.json. Current Level: {encrConfig.CurrentLevel}");

            // derive a 256-bit sub key (use HMACSHA256 with 20,000 iterations)
            var hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: randomSalt,
                prf: encrLevel.PRF,
                iterationCount: encrLevel.Iterations,
                numBytesRequested: encrLevel.NumBytes));

            //return 
            
            return  $"{encrLevel.Id}{_delimiter}" +
                    $"{Convert.ToBase64String(randomSalt)}{_delimiter}" + 
                    $"{hashed}";
        }

        /// <summary>
        /// Validate an existing password, Pass in the user's existing encoded password (a concatenated string separated by $
        /// with <encryption config id>$<salt>$<hash>). Encrypt the incoming password using the same
        /// settings in the encoded value and see if it matches on the hash
        /// </summary>
        /// <param name="encoded"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public static bool ValidatePassword(EncryptionConfig encrConfig, string encoded, string password, out bool updatePasswordEncryption)
        {
            updatePasswordEncryption = false;
            if (string.IsNullOrEmpty(encoded)) return false;
            if (string.IsNullOrEmpty(password)) return false;

            string[] passwordParts = encoded.Split(_delimiter);
            if (passwordParts.Length != 3) return false;

            //parse parts into their positions. Then take the plain password and excrypt to see if it matches the hash
            int encrId = Convert.ToInt32(passwordParts[0]);
            EncryptionLevelConfig encrLevel = encrConfig.Levels.Find(e => e.Id == encrId);
            if (encrLevel == null) throw new ArgumentNullException($"Encryption Config is missing. Check appSettings.json. Current Level: {encrConfig.CurrentLevel}");

            KeyDerivationPrf prf = (KeyDerivationPrf)Convert.ToInt16(encrLevel.PRF);
            int numBytes = Convert.ToInt32(encrLevel.NumBytes);
            int iterations = Convert.ToInt32(encrLevel.Iterations);
            var convertedSalt = Convert.FromBase64String(passwordParts[1]);
            
            //perform hash
            var hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: convertedSalt,
                prf: prf,
                iterationCount: iterations,
                numBytesRequested: numBytes);
            var hashed = Convert.ToBase64String(hash);

            //add support to update pw to latest level if current pw was a lower level.
            //have to get UserDAL the new password and it has to save it.
            updatePasswordEncryption = encrConfig.CurrentLevel > encrId;

            //true if they match, existing hash in 3rd position
            return hashed == passwordParts[2];
        }

        /// <summary>
        /// Encrypt the legacy password using the DJango approach
        /// </summary>
        /// <remarks>
        /// Reference: https://docs.djangoproject.com/en/3.1/topics/auth/passwords/
        /// By default, Django uses the PBKDF2 algorithm with a SHA256 hash
        /// The DB stores the password in this format: <algorithm>$<iterations>$<salt>$<hash>
        /// Example: "pbkdf2_sha256$216000$r1jfzvUNnesy$HazE155XipRbEWq4JWc2YNCetEH/zYd+H8zaobU1x10="
        /// </remarks>
        /// <param name="encoded">
        /// User's password in the user table as is from the DB (encrypted). It may be in the old format (described in remarks). Or in our converted format. 
        /// Either way, test if the password passed in matches the encrypted portion of the legacyPassword.
        /// </param>
        /// <param name="password">
        /// The password.
        /// </param>
        /// <returns>
        /// true/false - is there a match using the legacy password conversion
        /// </returns>
        public static bool ValidateLegacyPassword(string encoded, string password)
        {
            if (string.IsNullOrEmpty(encoded)) return false;
            if (string.IsNullOrEmpty(password)) return false;

            string[] passwordParts = encoded.Split(_delimiterLegacy);
            if (passwordParts.Length < 4) return false;

            if (passwordParts[0] != "pbkdf2_sha256") throw new NotSupportedException($"Password - invalid legacy encryption format: {passwordParts[0]}.");

            //by inspecting Django code
            //force salt to bytes
            int numBytes = 32;
            var salt = passwordParts[2];
            var convertedSalt = System.Text.Encoding.UTF8.GetBytes(salt);

            //perform hash
            var hash = KeyDerivation.Pbkdf2(
                password: password,
                salt: convertedSalt,
                prf: KeyDerivationPrf.HMACSHA256,
                iterationCount: Convert.ToInt32(passwordParts[1]),
                numBytesRequested: numBytes);
            var hashed = Convert.ToBase64String(hash);
            //true if they match
            return hashed == passwordParts[3];
        }

    }
}
