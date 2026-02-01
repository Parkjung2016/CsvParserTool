using System.IO;
using System.Text;
using Skddkkkk.Data;

public static class CsvEncryptor
{
    public static void EncryptCsvToFile(string csvPath, string outputPath)
    {
        using var fs = new FileStream(
              csvPath,
              FileMode.Open,
              FileAccess.Read,
              FileShare.ReadWrite
          );

        using var ms = new MemoryStream();
        fs.CopyTo(ms);

        byte[] encrypted = CryptoUtil.Encrypt(ms.ToArray());
        File.WriteAllBytes(outputPath, encrypted);
    }
}
