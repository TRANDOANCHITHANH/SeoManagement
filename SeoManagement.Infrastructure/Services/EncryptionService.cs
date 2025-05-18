using Microsoft.Extensions.Configuration;
using System.Security.Cryptography;
using System.Text;

namespace SeoManagement.Infrastructure.Services
{
	public class EncryptionService
	{
		private readonly string _encryptionKey;
		public EncryptionService(IConfiguration configuration)
		{
			var key = configuration.GetSection("Encryption:Key").Value;
			if (key != null && key.StartsWith("env:"))
			{
				var envVar = key.Replace("env:", "");
				_encryptionKey = Environment.GetEnvironmentVariable(envVar);
			}
			else
			{
				_encryptionKey = key;
			}

			if (string.IsNullOrEmpty(_encryptionKey) || _encryptionKey.Length != 32)
			{
				throw new InvalidOperationException("Encryption key must be 32 bytes long.");
			}
		}

		public string Encrypt(string plainText)
		{
			if (string.IsNullOrEmpty(plainText)) return null;

			using var aes = Aes.Create();
			aes.Key = Encoding.UTF8.GetBytes(_encryptionKey);
			aes.IV = new byte[16];
			var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);
			using var ms = new MemoryStream();
			using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
			using (var sw = new StreamWriter(cs))
			{
				sw.Write(plainText);
			}
			return Convert.ToBase64String(ms.ToArray());
		}

		public string Decrypt(string cipherText)
		{
			if (string.IsNullOrEmpty(cipherText)) return null;

			using var aes = Aes.Create();
			aes.Key = Encoding.UTF8.GetBytes(_encryptionKey);
			aes.IV = new byte[16];
			var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);
			using var ms = new MemoryStream(Convert.FromBase64String(cipherText));
			using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
			using var sr = new StreamReader(cs);
			return sr.ReadToEnd();
		}
	}
}
