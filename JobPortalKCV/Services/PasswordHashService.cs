using System;
using System.Security.Cryptography;
using System.Text;

namespace JobPortalKCV.Services
{
    public class PasswordVerificationResult
    {
        public bool Success { get; set; }
        public bool NeedsRehash { get; set; }
    }

    public static class PasswordHashService
    {
        private const int CurrentIterations = 10000;
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const string Prefix = "PBKDF2";

        public static string HashPassword(string password)
        {
            var salt = new byte[SaltSize];

            using (var rng = RandomNumberGenerator.Create())
                rng.GetBytes(salt);

            using (var deriveBytes = new Rfc2898DeriveBytes(password ?? "", salt, CurrentIterations))
            {
                var hash = deriveBytes.GetBytes(HashSize);
                return Prefix + "$" + CurrentIterations + "$" + Convert.ToBase64String(salt) + "$" + Convert.ToBase64String(hash);
            }
        }

        public static PasswordVerificationResult VerifyPassword(string password, string storedHash)
        {
            if (String.IsNullOrWhiteSpace(storedHash))
                return Failed();

            var parts = storedHash.Split('$');

            if (parts.Length != 4 || parts[0] != Prefix)
            {
                return new PasswordVerificationResult
                {
                    Success = SlowEquals(ToBytes(password ?? ""), ToBytes(storedHash)),
                    NeedsRehash = true
                };
            }

            try
            {
                var iterations = Int32.Parse(parts[1]);
                var salt = Convert.FromBase64String(parts[2]);
                var expectedHash = Convert.FromBase64String(parts[3]);

                using (var deriveBytes = new Rfc2898DeriveBytes(password ?? "", salt, iterations))
                {
                    var actualHash = deriveBytes.GetBytes(expectedHash.Length);
                    return new PasswordVerificationResult
                    {
                        Success = SlowEquals(actualHash, expectedHash),
                        NeedsRehash = iterations < CurrentIterations
                    };
                }
            }
            catch (FormatException)
            {
                return Failed();
            }
            catch (OverflowException)
            {
                return Failed();
            }
            catch (ArgumentException)
            {
                return Failed();
            }
        }

        private static PasswordVerificationResult Failed()
        {
            return new PasswordVerificationResult { Success = false, NeedsRehash = false };
        }

        private static byte[] ToBytes(string value)
        {
            return Encoding.UTF8.GetBytes(value);
        }

        private static bool SlowEquals(byte[] a, byte[] b)
        {
            var diff = (uint)a.Length ^ (uint)b.Length;

            for (var i = 0; i < a.Length && i < b.Length; i++)
                diff |= (uint)(a[i] ^ b[i]);

            return diff == 0;
        }
    }
}
