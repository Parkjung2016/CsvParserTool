using System.IO;

namespace CSVParserTool
{
    public static class ClassFileWriter
    {
        public static void Save(string directory, string className, string code)
        {
            Directory.CreateDirectory(directory);

            string path = Path.Combine(directory, className + ".cs");

            File.WriteAllText(path, code);
        }
    }
}
