using System;
using System.Security.Cryptography;

namespace AthsVideoRecording.Security
{
    public static class PasswordHasher
    {
        public const int SaltSize = 16; // 128-bit
        public const int HashSize = 32; // 256-bit
        public const int Iterations = 100_000;

        public static (byte[] hash, byte[] salt) HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] hash = pbkdf2.GetBytes(HashSize);
            return (hash, salt);
        }

        public static bool Verify(string password, byte[] hash, byte[] salt)
        {
            using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] computed = pbkdf2.GetBytes(HashSize);
            return CryptographicOperations.FixedTimeEquals(computed, hash);
        }

        public static string GenerateRandomPassword(int length = 24)
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz23456789!@#$%^&*()_-+=[]{}:"; // omit confusing chars
            Span<char> buffer = new char[length];
            byte[] rnd = RandomNumberGenerator.GetBytes(length);
            for (int i = 0; i < length; i++)
            {
                buffer[i] = chars[rnd[i] % chars.Length];
            }
            return new string(buffer);
        }
    }
}
