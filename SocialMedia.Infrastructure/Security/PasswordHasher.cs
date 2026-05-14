using Konscious.Security.Cryptography;
using SocialMedia.Application.Services.Interfaces.Common;
using System.Security.Cryptography;
using System.Text;

namespace SocialMedia.Infrastructure.Security
{
    public class PasswordHasher : IPasswordHasher
    {
        public string Hash(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);

            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                Iterations = 2,
                MemorySize = 19456,
                DegreeOfParallelism = 1
            };

            byte[] hash = argon2.GetBytes(32);

            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        public bool Verify(string storedHash, string password)
        {
            var parts = storedHash.Split('.');
            var salt = Convert.FromBase64String(parts[0]);
            var hash = Convert.FromBase64String(parts[1]);

            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                Iterations = 2,
                MemorySize = 19456,
                DegreeOfParallelism = 1
            };

            var computed = argon2.GetBytes(32);

            return CryptographicOperations.FixedTimeEquals(computed, hash);
        }
    }
}
