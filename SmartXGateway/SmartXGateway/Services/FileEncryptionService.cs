using System.Security.Cryptography;
using System.Text;

namespace SmartXGateway.Services
{
    public class FileEncryptionService
    {
        private readonly byte[] _key;

        public FileEncryptionService(IConfiguration config)
        {
            var keyString = config["Encryption:Key"]
                ?? throw new InvalidOperationException("Encryption:Key missing from configuration.");

            _key = Encoding.UTF8.GetBytes(keyString.PadRight(32).Substring(0, 32));
        }

        public async Task<string> EncryptAndSaveAsync(Stream source, string originalFileName, string uploadsPath)
        {
            Directory.CreateDirectory(uploadsPath);
            var safeName = $"{Guid.NewGuid()}_{Path.GetFileName(originalFileName)}.enc";
            var fullPath = Path.Combine(uploadsPath, safeName);

            using var aes = Aes.Create();
            aes.Key = _key;
            aes.GenerateIV();

            await using var fs = new FileStream(fullPath, FileMode.Create);
            await fs.WriteAsync(aes.IV, 0, aes.IV.Length);

            await using var crypto = new CryptoStream(fs, aes.CreateEncryptor(), CryptoStreamMode.Write);
            await source.CopyToAsync(crypto);
            await crypto.FlushFinalBlockAsync();

            return "/uploads/" + safeName;
        }

        public async Task<byte[]> DecryptAsync(string fullPath)
        {
            var bytes = await File.ReadAllBytesAsync(fullPath);
            using var aes = Aes.Create();
            aes.Key = _key;
            aes.IV = bytes.Take(16).ToArray();

            using var ms = new MemoryStream(bytes.Skip(16).ToArray());
            await using var crypto = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Read);
            using var outMs = new MemoryStream();
            await crypto.CopyToAsync(outMs);
            return outMs.ToArray();
        }
    }
}

/* ============================================================
   REFERENCES
   ------------------------------------------------------------
   Troelsen, A. and Japikse, P., 2017. Pro C# 7: With .NET and 
       .NET Core. 8th ed. New York: Apress.
   ============================================================ */