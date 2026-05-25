using System;
using System.IO;
using System.Reflection;

namespace CSVParserTool
{
    /// <summary>
    /// .NET Framework net48: <c>ClosedXML</c> / <c>MessagePack</c> 등이 서로 다른
    /// <c>System.Memory</c>·<c>System.Runtime.CompilerServices.Unsafe</c> 참조를 가질 때
    /// <c>0x80131040</c>(매니페스트 불일치)가 납니다. <c>*.exe.config</c>가 없는 배포에서도
    /// exe 옆에 복사된 실제 satellite DLL로 로드되게 합니다.
    /// </summary>
    public static class NetFxAssemblyLoadFix
    {
        private static bool _registered;

        public static void Register()
        {
            if (_registered)
                return;

            _registered = true;

            Assembly entry = Assembly.GetEntryAssembly();
            string baseDir = entry != null
                ? Path.GetDirectoryName(entry.Location)
                : Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            if (string.IsNullOrEmpty(baseDir))
                return;

            AppDomain.CurrentDomain.AssemblyResolve += (_, args) =>
            {
                if (string.IsNullOrEmpty(args.Name))
                    return null;

                var an = new AssemblyName(args.Name);
                string shortName = an.Name;
                if (string.IsNullOrEmpty(shortName))
                    return null;

                switch (shortName)
                {
                    case "System.Memory":
                    case "System.Runtime.CompilerServices.Unsafe":
                    case "System.Buffers":
                    case "System.Numerics.Vectors":
                    case "System.Threading.Tasks.Extensions":
                    case "System.Collections.Immutable":
                    case "Microsoft.Bcl.AsyncInterfaces":
                    {
                        string path = Path.Combine(baseDir, shortName + ".dll");
                        if (!File.Exists(path))
                            return null;
                        try
                        {
                            return Assembly.LoadFrom(path);
                        }
                        catch
                        {
                            return null;
                        }
                    }

                    default:
                        return null;
                }
            };
        }
    }
}
