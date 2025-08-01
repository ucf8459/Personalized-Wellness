using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace WellnessPlatform.Services
{
    public interface IEncryptionService
    {
        string EncryptData(string plainText, string? userId = null);
        string DecryptData(string encryptedText, string? userId = null);
        string GenerateEncryptionKey();
        bool IsEncrypted(string text);
        string HashSensitiveData(string data);
    }

    public class EncryptionService : IEncryptionService
    {
        private readonly IConfiguration _configuration;
        private readonly string _masterKey;
        private readonly Dictionary<string, string> _userKeys = new();

        public EncryptionService(IConfiguration configuration)
        {
            _configuration = configuration;
            _masterKey = _configuration["Encryption:MasterKey"] ?? GenerateMasterKey();
        }

        public string EncryptData(string plainText, string? userId = null)
        {
            if (string.IsNullOrEmpty(plainText))
                return plainText;

            try
            {
                var key = GetUserKey(userId);
                using var aes = Aes.Create();
                aes.Key = Convert.FromBase64String(key);
                aes.GenerateIV();

                using var encryptor = aes.CreateEncryptor();
                using var msEncrypt = new MemoryStream();
                using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
                using var swEncrypt = new StreamWriter(csEncrypt);

                swEncrypt.Write(plainText);
                swEncrypt.Flush();
                csEncrypt.FlushFinalBlock();

                var encrypted = msEncrypt.ToArray();
                var result = Convert.ToBase64String(aes.IV) + ":" + Convert.ToBase64String(encrypted);
                
                return "ENC:" + result;
            }
            catch (Exception ex)
            {
                // Log the error but don't expose encryption details
                throw new InvalidOperationException("Data encryption failed", ex);
            }
        }

        public string DecryptData(string encryptedText, string? userId = null)
        {
            if (string.IsNullOrEmpty(encryptedText) || !IsEncrypted(encryptedText))
                return encryptedText;

            try
            {
                var key = GetUserKey(userId);
                var encryptedData = encryptedText.Substring(4); // Remove "ENC:" prefix
                var parts = encryptedData.Split(':');
                
                if (parts.Length != 2)
                    throw new InvalidOperationException("Invalid encrypted data format");

                var iv = Convert.FromBase64String(parts[0]);
                var cipherText = Convert.FromBase64String(parts[1]);

                using var aes = Aes.Create();
                aes.Key = Convert.FromBase64String(key);
                aes.IV = iv;

                using var decryptor = aes.CreateDecryptor();
                using var msDecrypt = new MemoryStream(cipherText);
                using var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                using var srDecrypt = new StreamReader(csDecrypt);

                return srDecrypt.ReadToEnd();
            }
            catch (Exception ex)
            {
                // Log the error but don't expose decryption details
                throw new InvalidOperationException("Data decryption failed", ex);
            }
        }

        public string GenerateEncryptionKey()
        {
            using var aes = Aes.Create();
            aes.GenerateKey();
            return Convert.ToBase64String(aes.Key);
        }

        public bool IsEncrypted(string text)
        {
            return !string.IsNullOrEmpty(text) && text.StartsWith("ENC:");
        }

        public string HashSensitiveData(string data)
        {
            if (string.IsNullOrEmpty(data))
                return string.Empty;

            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(data));
            return Convert.ToBase64String(hashBytes);
        }

        private string GenerateMasterKey()
        {
            var key = GenerateEncryptionKey();
            // In production, this should be stored securely and not generated at runtime
            return key;
        }

        private string GetUserKey(string? userId)
        {
            if (string.IsNullOrEmpty(userId))
                return _masterKey;

            if (!_userKeys.ContainsKey(userId))
            {
                // Generate a user-specific key derived from master key
                using var sha256 = SHA256.Create();
                var userKeyBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(_masterKey + userId));
                _userKeys[userId] = Convert.ToBase64String(userKeyBytes);
            }

            return _userKeys[userId];
        }
    }
} 