using System;
using System.Security.Cryptography;
using System.Text;
using Integrative.Encryption;

namespace ReaderDownloaderUI {
    internal static class EncryptionProvider {
        public static string Encrypt(string input) {
            byte[] data = Encoding.UTF8.GetBytes(input);
            byte[] encrypted = CrossProtect.Protect(data, null, DataProtectionScope.CurrentUser);

            return Convert.ToBase64String(encrypted);
        }

        public static string Decrypt(string input) {
            if (String.IsNullOrWhiteSpace(input)) {
                return String.Empty;
            }

            byte[] encrypted = Convert.FromBase64String(input);
            byte[] decrypted = CrossProtect.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);

            return Encoding.UTF8.GetString(decrypted);
        }
    }
}
