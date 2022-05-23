namespace CESMII.ProfileDesigner.Common
{
    using System;
    using System.Security.Cryptography;
    using Microsoft.AspNetCore.Cryptography.KeyDerivation;

    using CESMII.ProfileDesigner.Common.Models;

    public class PasswordUtils
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
            var result = string.Empty;
            for (var i = 0; i < length; i++)
            {
                var randomPosition = rnd.Next(0, Chars.Length - 1);
                var randomCase = rnd.Next(0, 1);
                var randomChar = Chars.Substring(randomPosition, 1);
                result += (randomCase == 0 ? randomChar.ToLower() : randomChar.ToUpper());
            }

            return result;
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
            using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
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
            
            return  $"{((int)encrLevel.Id).ToString()}{_delimiter}" +
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

        #region Django Reference - relevant encryption code

        /* CODE: \fp-develop\Lib\site-packages\django\contrib\auth\hashers.py
            //
            //This is where the authentication happens
            //

            class PBKDF2PasswordHasher(BasePasswordHasher):
                """
                Secure password hashing using the PBKDF2 algorithm (recommended)

                Configured to use PBKDF2 + HMAC + SHA256.
                The result is a 64 byte binary string.  Iterations may be changed
                safely but you must rename the algorithm if you change SHA256.
                """
                algorithm = "pbkdf2_sha256"
                iterations = 216000
                digest = hashlib.sha256

                def encode(self, password, salt, iterations=None):
                    assert password is not None
                    assert salt and '$' not in salt
                    iterations = iterations or self.iterations
                    hash = pbkdf2(password, salt, iterations, digest=self.digest)
                    hash = base64.b64encode(hash).decode('ascii').strip()
                    return "%s$%d$%s$%s" % (self.algorithm, iterations, salt, hash)

                def verify(self, password, encoded):
                    algorithm, iterations, salt, hash = encoded.split('$', 3)
                    assert algorithm == self.algorithm
                    encoded_2 = self.encode(password, salt, int(iterations))
                    return constant_time_compare(encoded, encoded_2)

                def safe_summary(self, encoded):
                    algorithm, iterations, salt, hash = encoded.split('$', 3)
                    assert algorithm == self.algorithm
                    return {
                        _('algorithm'): algorithm,
                        _('iterations'): iterations,
                        _('salt'): mask_hash(salt),
                        _('hash'): mask_hash(hash),
                    }

                def must_update(self, encoded):
                    algorithm, iterations, salt, hash = encoded.split('$', 3)
                    return int(iterations) != self.iterations

                def harden_runtime(self, password, encoded):
                    algorithm, iterations, salt, hash = encoded.split('$', 3)
                    extra_iterations = self.iterations - int(iterations)
                    if extra_iterations > 0:
                        self.encode(password, salt, extra_iterations)
         
        */

        /* CODE: fp-develop\Lib\site-packages\django\utils\crypto.py
            //
            //This is used to initially generate Salt value. Salt is unique per user. 
            //
            # RemovedInDjango40Warning: when the deprecation ends, replace with:
            #   def get_random_string(length, allowed_chars='...'):
            
            def get_random_string(length=NOT_PROVIDED, allowed_chars=(
                'abcdefghijklmnopqrstuvwxyz'
                'ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789'
            )):
                """
                Return a securely generated random string.

                The bit length of the returned value can be calculated with the formula:
                    log_2(len(allowed_chars)^length)

                For example, with default `allowed_chars` (26+26+10), this gives:
                  * length: 12, bit length =~ 71 bits
                  * length: 22, bit length =~ 131 bits
                """
                if length is NOT_PROVIDED:
                    warnings.warn(
                        'Not providing a length argument is deprecated.',
                        RemovedInDjango40Warning,
                    )
                    length = 12
                return ''.join(secrets.choice(allowed_chars) for i in range(length))


            //
            //This is where the encryption happens. Notice the use of force bytes on salt and pw.
            //
            def pbkdf2(password, salt, iterations, dklen=0, digest=None):
                """Return the hash of password using pbkdf2."""
                if digest is None:
                    digest = hashlib.sha256
                dklen = dklen or None
                password = force_bytes(password)
                salt = force_bytes(salt)
                return hashlib.pbkdf2_hmac(digest().name, password, salt, iterations, dklen)
         
         */

        /* CODE: \fp-develop\Lib\site-packages\django\utils\encoding.py
            //
            //This is the force_bytes call on a string
            //

            def force_bytes(s, encoding='utf-8', strings_only=False, errors='strict'):
                """
                Similar to smart_bytes, except that lazy instances are resolved to
                strings, rather than kept as lazy objects.

                If strings_only is True, don't convert (some) non-string-like objects.
                """
                # Handle the common case first for performance reasons.
                if isinstance(s, bytes):
                    if encoding == 'utf-8':
                        return s
                    else:
                        return s.decode('utf-8', errors).encode(encoding, errors)
                if strings_only and is_protected_type(s):
                    return s
                if isinstance(s, memoryview):
                    return bytes(s)
                return str(s).encode(encoding, errors)


         
         */

        #endregion

    }
}
