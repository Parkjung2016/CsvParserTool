using System.Security.Cryptography;
using System.Text;

namespace PJDev.Data
{
    public static class CryptoUtil
    {
        private static readonly byte[] Key =
            Encoding.UTF8.GetBytes("R7#2k@P!9xA0mQv3HfLwE8ZC1N4JY6BX");

        private static readonly byte[] IV =
            Encoding.UTF8.GetBytes("A9d!3QxL0@V#kP2M");

        public static byte[] Encrypt(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var encryptor = aes.CreateEncryptor();
            return encryptor.TransformFinalBlock(data, 0, data.Length);
        }

        public static byte[] Decrypt(byte[] data)
        {
            using var aes = Aes.Create();
            aes.Key = Key;
            aes.IV = IV;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            using var decryptor = aes.CreateDecryptor();
            return decryptor.TransformFinalBlock(data, 0, data.Length);
        }
    }
}