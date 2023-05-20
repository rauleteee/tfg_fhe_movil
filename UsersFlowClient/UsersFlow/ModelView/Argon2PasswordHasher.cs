using System;
using System.Text;
using Konscious.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace UsersFlow.ModelView
{
    public class Argon2PasswordHasher : IPasswordHasher<string>
    {
        /*
         * @author: IDBOTIC
         */
        private const int SaltSize = 16;
        private const int HashSize = 32;
        private const int Iterations = 4;

        public string HashPassword(string user, string password)
        {
            var salt = new byte[SaltSize];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt); // Generate a random salt
            }

            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(password))
            {
                Salt = salt,
                DegreeOfParallelism = 4,
                MemorySize = 1024 * 1024,
                Iterations = Iterations
            };
            var hash = argon2.GetBytes(HashSize);
            return $"{Iterations}" +
                $".{Convert.ToBase64String(salt)}" +
                $".{Convert.ToBase64String(hash)}";
        }

        public PasswordVerificationResult VerifyHashedPassword(string user, string hashedPassword, string providedPassword)
        {
            var parts = hashedPassword.Split('.');
            if (parts.Length != 3)
            {
                return PasswordVerificationResult.Failed;
            }

            var iterations = int.Parse(parts[0]);
            var salt = Convert.FromBase64String(parts[1]);
            var hash = Convert.FromBase64String(parts[2]);

            var argon2 = new Argon2id(Encoding.UTF8.GetBytes(providedPassword))
            {
                Salt = salt,
                DegreeOfParallelism = 4,
                MemorySize = 1024 * 1024,
                Iterations = iterations
            };
            var computedHash = argon2.GetBytes(HashSize);
            return hash.SequenceEqual(computedHash) ? PasswordVerificationResult.Success : PasswordVerificationResult.Failed;
        }
    }

}
