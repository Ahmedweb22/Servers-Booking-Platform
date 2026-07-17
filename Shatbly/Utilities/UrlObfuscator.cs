using System;
using System.Text;

namespace Shtbly.Utilities
{
    public static class UrlObfuscator
    {
        private static readonly byte[] XorKey = Encoding.UTF8.GetBytes("ShtblyBookingSystemSecretKey123");

        public static string Encrypt(int id)
        {
            byte[] idBytes = BitConverter.GetBytes(id);
            byte[] encrypted = new byte[idBytes.Length];
            for (int i = 0; i < idBytes.Length; i++)
            {
                encrypted[i] = (byte)(idBytes[i] ^ XorKey[i % XorKey.Length]);
            }
            return Convert.ToBase64String(encrypted)
                .Replace('+', '-')
                .Replace('/', '_')
                .TrimEnd('=');
        }

        public static int Decrypt(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token)) return 0;
                
                string incoming = token.Replace('-', '+').Replace('_', '/');
                int padding = (4 - incoming.Length % 4) % 4;
                incoming += new string('=', padding);
                byte[] encrypted = Convert.FromBase64String(incoming);
                byte[] decrypted = new byte[encrypted.Length];
                for (int i = 0; i < encrypted.Length; i++)
                {
                    decrypted[i] = (byte)(encrypted[i] ^ XorKey[i % XorKey.Length]);
                }
                return BitConverter.ToInt32(decrypted, 0);
            }
            catch
            {
                return 0;
            }
        }
    }
}
