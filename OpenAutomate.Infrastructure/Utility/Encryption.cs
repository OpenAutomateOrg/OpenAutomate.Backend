using OpenAutomate.Core.Domain.Entities;
using System.Security.Cryptography;
using System.Text;


namespace OpenAutomate.Infrastructure.Utility
{
    public class Encryption
    {
        public static void EncrypPassword<T>(T user, string password) where T : User
        {
            if (string.IsNullOrEmpty(password))
                return;

            user.PasswordHash = PasswordHash.GetHash(password);
            
        }
        
        public static string DecryptPassword<T>(T user, string passwordHash) where T : User
        {
            if (string.IsNullOrEmpty(passwordHash))
                return string.Empty;

            return PasswordHash.GetHash(passwordHash);
        }

    }

    public class PasswordHash
    {
        public static string GetHash(string text)
        {
            using (var sha256 = SHA256.Create())
            {
                var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(text));
                return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower();
            }
        }


    }
}
