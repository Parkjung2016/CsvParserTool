using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CSVParserTool
{
    /// <summary>
    /// Unity IL2CPP용 <c>MessagePackGenerated.cs</c> 생성 — mpc/dotnet 없이 EXE 단독 오프라인 동작.
    /// </summary>
    public static class MessagePackUnityResolverGenerator
    {
        public const string GeneratedFileName = "MessagePackGenerated.cs";
        public const string ResolverTypeName = "PJDevDataGeneratedResolver";

        private const string FormatterNamespace = "PJDev.Data.Formatters.PJDev.Data";

        public static void Generate(
            string scriptsDir,
            IReadOnlyList<CsvTableParseResult> tables,
            Action<string> log)
        {
            if (string.IsNullOrWhiteSpace(scriptsDir))
                return;

            Directory.CreateDirectory(scriptsDir);
            string outputPath = Path.Combine(scriptsDir, GeneratedFileName);

            if (tables == null || tables.Count == 0)
            {
                WriteEmptyResolver(outputPath);
                log?.Invoke($"MessagePack: no tables — wrote empty resolver → {outputPath}");
                return;
            }

            var model = BuildModel(tables);
            string code = BuildSource(model);
            File.WriteAllText(outputPath, code, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            log?.Invoke($"MessagePack: {outputPath} ({model.DataClasses.Count} type(s))");
        }

        private sealed class GenerationModel
        {
            public List<string> EnumNames = new List<string>();
            public List<string> ListTypes = new List<string>();
            public List<DataClassModel> DataClasses = new List<DataClassModel>();
        }

        private sealed class DataClassModel
        {
            public string ClassName;
            public List<FieldModel> Fields = new List<FieldModel>();
        }

        private sealed class FieldModel
        {
            public string PropertyName;
            public string ColumnType;
        }

        private static GenerationModel BuildModel(IReadOnlyList<CsvTableParseResult> tables)
        {
            var model = new GenerationModel();
            var enumSeen = new HashSet<string>(StringComparer.Ordinal);
            var listSeen = new HashSet<string>(StringComparer.Ordinal);
            var classSeen = new HashSet<string>(StringComparer.Ordinal);

            foreach (CsvTableParseResult table in tables)
            {
                if (table == null || string.IsNullOrWhiteSpace(table.ClassName))
                    continue;

                if (classSeen.Add(table.ClassName))
                {
                    var dataClass = new DataClassModel { ClassName = table.ClassName };
                    for (int i = 0; i < table.Headers.Length; i++)
                    {
                        string columnType = table.ColumnTypes[i];
                        dataClass.Fields.Add(new FieldModel
                        {
                            PropertyName = CsvTableParser.SanitizeIdentifier(table.Headers[i]),
                            ColumnType = columnType
                        });
                        RegisterListType(columnType, listSeen, model.ListTypes);
                        RegisterEnumType(columnType, table.EnumMembers, enumSeen, model.EnumNames);
                    }

                    model.DataClasses.Add(dataClass);
                }

                if (table.EnumDeclarationOrder != null)
                {
                    foreach (string enumName in table.EnumDeclarationOrder)
                    {
                        if (enumSeen.Add(enumName))
                            model.EnumNames.Add(enumName);
                    }
                }
            }

            model.EnumNames.Sort(StringComparer.Ordinal);
            model.ListTypes.Sort(StringComparer.Ordinal);
            model.DataClasses.Sort((a, b) => string.Compare(a.ClassName, b.ClassName, StringComparison.Ordinal));
            return model;
        }

        private static void RegisterListType(string columnType, HashSet<string> seen, List<string> target)
        {
            if (!columnType.StartsWith("List<", StringComparison.Ordinal))
                return;

            if (seen.Add(columnType))
                target.Add(columnType);
        }

        private static void RegisterEnumType(
            string columnType,
            IReadOnlyDictionary<string, IReadOnlyList<string>> enums,
            HashSet<string> seen,
            List<string> target)
        {
            if (enums == null || !enums.ContainsKey(columnType))
                return;

            if (seen.Add(columnType))
                target.Add(columnType);
        }

        private static string BuildSource(GenerationModel model)
        {
            var sb = new StringBuilder();
            AppendFileHeader(sb);
            AppendResolver(sb, model);
            foreach (string enumName in model.EnumNames)
                AppendEnumFormatter(sb, enumName);
            foreach (DataClassModel dataClass in model.DataClasses)
                AppendDataFormatter(sb, dataClass);
            return sb.ToString();
        }

        private static void AppendFileHeader(StringBuilder sb)
        {
            sb.AppendLine("// <auto-generated>");
            sb.AppendLine("// THIS (.cs) FILE IS GENERATED BY DataTool. DO NOT CHANGE IT.");
            sb.AppendLine("// </auto-generated>");
            sb.AppendLine();
        }

        private static void AppendResolver(StringBuilder sb, GenerationModel model)
        {
            var entries = new List<ResolverEntry>();
            foreach (string listType in model.ListTypes)
                entries.Add(new ResolverEntry(ToGlobalType(listType), BuildListFormatterExpr(listType)));
            foreach (string enumName in model.EnumNames)
                entries.Add(new ResolverEntry($"global::PJDev.Data.{enumName}", $"new {FormatterNamespace}.{enumName}Formatter()"));
            foreach (DataClassModel dataClass in model.DataClasses)
                entries.Add(new ResolverEntry($"global::PJDev.Data.{dataClass.ClassName}", $"new {FormatterNamespace}.{dataClass.ClassName}Formatter()"));

            entries.Sort((a, b) => string.Compare(a.TypeExpression, b.TypeExpression, StringComparison.Ordinal));

            AppendPragmaDisable(sb);
            sb.AppendLine("namespace PJDev.Data.Resolvers");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {ResolverTypeName} : global::MessagePack.IFormatterResolver");
            sb.AppendLine("    {");
            sb.AppendLine($"        public static readonly global::MessagePack.IFormatterResolver Instance = new {ResolverTypeName}();");
            sb.AppendLine();
            sb.AppendLine($"        private {ResolverTypeName}()");
            sb.AppendLine("        {");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        public global::MessagePack.Formatters.IMessagePackFormatter<T> GetFormatter<T>()");
            sb.AppendLine("        {");
            sb.AppendLine("            return FormatterCache<T>.Formatter;");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        private static class FormatterCache<T>");
            sb.AppendLine("        {");
            sb.AppendLine("            internal static readonly global::MessagePack.Formatters.IMessagePackFormatter<T> Formatter;");
            sb.AppendLine();
            sb.AppendLine("            static FormatterCache()");
            sb.AppendLine("            {");
            sb.AppendLine($"                var f = {ResolverTypeName}GetFormatterHelper.GetFormatter(typeof(T));");
            sb.AppendLine("                if (f != null)");
            sb.AppendLine("                {");
            sb.AppendLine("                    Formatter = (global::MessagePack.Formatters.IMessagePackFormatter<T>)f;");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine();
            sb.AppendLine($"    internal static class {ResolverTypeName}GetFormatterHelper");
            sb.AppendLine("    {");
            sb.AppendLine("        private static readonly global::System.Collections.Generic.Dictionary<global::System.Type, int> lookup;");
            sb.AppendLine();
            sb.AppendLine($"        static {ResolverTypeName}GetFormatterHelper()");
            sb.AppendLine("        {");
            sb.AppendLine($"            lookup = new global::System.Collections.Generic.Dictionary<global::System.Type, int>({entries.Count})");
            sb.AppendLine("            {");
            for (int i = 0; i < entries.Count; i++)
                sb.AppendLine($"                {{ typeof({entries[i].TypeExpression}), {i} }},");
            sb.AppendLine("            };");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine("        internal static object GetFormatter(global::System.Type t)");
            sb.AppendLine("        {");
            sb.AppendLine("            int key;");
            sb.AppendLine("            if (!lookup.TryGetValue(t, out key))");
            sb.AppendLine("            {");
            sb.AppendLine("                return null;");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            switch (key)");
            sb.AppendLine("            {");
            for (int i = 0; i < entries.Count; i++)
                sb.AppendLine($"                case {i}: return {entries[i].FormatterExpression};");
            sb.AppendLine("                default: return null;");
            sb.AppendLine("            }");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            AppendPragmaRestore(sb);
            sb.AppendLine();
        }

        private sealed class ResolverEntry
        {
            public string TypeExpression;
            public string FormatterExpression;

            public ResolverEntry(string typeExpression, string formatterExpression)
            {
                TypeExpression = typeExpression;
                FormatterExpression = formatterExpression;
            }
        }

        private static void AppendEnumFormatter(StringBuilder sb, string enumName)
        {
            AppendPragmaDisable(sb);
            sb.AppendLine($"namespace {FormatterNamespace}");
            sb.AppendLine("{");
            sb.AppendLine();
            sb.AppendLine($"    public sealed class {enumName}Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::PJDev.Data.{enumName}>");
            sb.AppendLine("    {");
            sb.AppendLine($"        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::PJDev.Data.{enumName} value, global::MessagePack.MessagePackSerializerOptions options)");
            sb.AppendLine("        {");
            sb.AppendLine("            writer.Write((global::System.Int32)value);");
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine($"        public global::PJDev.Data.{enumName} Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)");
            sb.AppendLine("        {");
            sb.AppendLine($"            return (global::PJDev.Data.{enumName})reader.ReadInt32();");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            AppendPragmaRestore(sb);
            sb.AppendLine();
        }

        private static void AppendDataFormatter(StringBuilder sb, DataClassModel dataClass)
        {
            AppendPragmaDisable(sb);
            sb.AppendLine($"namespace {FormatterNamespace}");
            sb.AppendLine("{");
            sb.AppendLine($"    public sealed class {dataClass.ClassName}Formatter : global::MessagePack.Formatters.IMessagePackFormatter<global::PJDev.Data.{dataClass.ClassName}>");
            sb.AppendLine("    {");
            sb.AppendLine();
            sb.AppendLine($"        public void Serialize(ref global::MessagePack.MessagePackWriter writer, global::PJDev.Data.{dataClass.ClassName} value, global::MessagePack.MessagePackSerializerOptions options)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (value == null)");
            sb.AppendLine("            {");
            sb.AppendLine("                writer.WriteNil();");
            sb.AppendLine("                return;");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            global::MessagePack.IFormatterResolver formatterResolver = options.Resolver;");
            sb.AppendLine($"            writer.WriteArrayHeader({dataClass.Fields.Count});");
            foreach (FieldModel field in dataClass.Fields)
                sb.AppendLine("            " + BuildSerializeStatement(field));
            sb.AppendLine("        }");
            sb.AppendLine();
            sb.AppendLine($"        public global::PJDev.Data.{dataClass.ClassName} Deserialize(ref global::MessagePack.MessagePackReader reader, global::MessagePack.MessagePackSerializerOptions options)");
            sb.AppendLine("        {");
            sb.AppendLine("            if (reader.TryReadNil())");
            sb.AppendLine("            {");
            sb.AppendLine("                return null;");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            options.Security.DepthStep(ref reader);");
            sb.AppendLine("            global::MessagePack.IFormatterResolver formatterResolver = options.Resolver;");
            sb.AppendLine("            var length = reader.ReadArrayHeader();");
            sb.AppendLine($"            var ____result = new global::PJDev.Data.{dataClass.ClassName}();");
            sb.AppendLine();
            sb.AppendLine("            for (int i = 0; i < length; i++)");
            sb.AppendLine("            {");
            sb.AppendLine("                switch (i)");
            sb.AppendLine("                {");
            for (int i = 0; i < dataClass.Fields.Count; i++)
            {
                FieldModel field = dataClass.Fields[i];
                sb.AppendLine($"                    case {i}:");
                sb.AppendLine("                        " + BuildDeserializeStatement(field) + ";");
                sb.AppendLine("                        break;");
            }
            sb.AppendLine("                    default:");
            sb.AppendLine("                        reader.Skip();");
            sb.AppendLine("                        break;");
            sb.AppendLine("                }");
            sb.AppendLine("            }");
            sb.AppendLine();
            sb.AppendLine("            reader.Depth--;");
            sb.AppendLine("            return ____result;");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            AppendPragmaRestore(sb);
            sb.AppendLine();
        }

        private static string BuildSerializeStatement(FieldModel field)
        {
            string access = $"value.{field.PropertyName}";
            switch (GetKind(field.ColumnType))
            {
                case ColumnKind.Int:
                    return $"writer.Write({access});";
                case ColumnKind.Float:
                    return $"writer.Write({access});";
                case ColumnKind.Bool:
                    return $"writer.Write({access});";
                case ColumnKind.String:
                    return $"global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Serialize(ref writer, {access}, options);";
                case ColumnKind.Enum:
                    return $"global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::PJDev.Data.{field.ColumnType}>(formatterResolver).Serialize(ref writer, {access}, options);";
                case ColumnKind.List:
                    return $"global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<{ToGlobalType(field.ColumnType)}>(formatterResolver).Serialize(ref writer, {access}, options);";
                default:
                    throw new InvalidOperationException($"Unsupported column type: {field.ColumnType}");
            }
        }

        private static string BuildDeserializeStatement(FieldModel field)
        {
            string target = $"____result.{field.PropertyName}";
            switch (GetKind(field.ColumnType))
            {
                case ColumnKind.Int:
                    return $"{target} = reader.ReadInt32()";
                case ColumnKind.Float:
                    return $"{target} = reader.ReadSingle()";
                case ColumnKind.Bool:
                    return $"{target} = reader.ReadBoolean()";
                case ColumnKind.String:
                    return $"{target} = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<string>(formatterResolver).Deserialize(ref reader, options)";
                case ColumnKind.Enum:
                    return $"{target} = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<global::PJDev.Data.{field.ColumnType}>(formatterResolver).Deserialize(ref reader, options)";
                case ColumnKind.List:
                    return $"{target} = global::MessagePack.FormatterResolverExtensions.GetFormatterWithVerify<{ToGlobalType(field.ColumnType)}>(formatterResolver).Deserialize(ref reader, options)";
                default:
                    throw new InvalidOperationException($"Unsupported column type: {field.ColumnType}");
            }
        }

        private enum ColumnKind
        {
            Int,
            Float,
            Bool,
            String,
            Enum,
            List
        }

        private static ColumnKind GetKind(string columnType)
        {
            if (columnType.StartsWith("List<", StringComparison.Ordinal))
                return ColumnKind.List;

            switch (columnType)
            {
                case "int":
                    return ColumnKind.Int;
                case "float":
                    return ColumnKind.Float;
                case "bool":
                    return ColumnKind.Bool;
                case "string":
                    return ColumnKind.String;
                default:
                    return ColumnKind.Enum;
            }
        }

        private static string ToGlobalType(string typeName)
        {
            if (typeName.StartsWith("List<", StringComparison.Ordinal))
            {
                string inner = typeName.Substring("List<".Length);
                inner = inner.Substring(0, inner.Length - 1);
                return $"global::System.Collections.Generic.List<{ToGlobalPrimitive(inner)}>";
            }

            return $"global::PJDev.Data.{typeName}";
        }

        private static string ToGlobalPrimitive(string typeName)
        {
            switch (typeName)
            {
                case "int":
                    return "global::System.Int32";
                case "float":
                    return "global::System.Single";
                case "bool":
                    return "global::System.Boolean";
                case "string":
                    return "global::System.String";
                default:
                    return $"global::PJDev.Data.{typeName}";
            }
        }

        private static string BuildListFormatterExpr(string listType)
        {
            string inner = listType.Substring("List<".Length);
            inner = inner.Substring(0, inner.Length - 1);
            switch (inner)
            {
                case "int":
                    return "new global::MessagePack.Formatters.ListFormatter<int>()";
                case "float":
                    return "new global::MessagePack.Formatters.ListFormatter<float>()";
                case "bool":
                    return "new global::MessagePack.Formatters.ListFormatter<bool>()";
                case "string":
                    return "new global::MessagePack.Formatters.ListFormatter<string>()";
                default:
                    throw new InvalidOperationException($"Unsupported list element type: {inner}");
            }
        }

        private static void WriteEmptyResolver(string outputPath)
        {
            var sb = new StringBuilder();
            AppendFileHeader(sb);
            AppendPragmaDisable(sb);
            sb.AppendLine("namespace PJDev.Data.Resolvers");
            sb.AppendLine("{");
            sb.AppendLine($"    public class {ResolverTypeName} : global::MessagePack.IFormatterResolver");
            sb.AppendLine("    {");
            sb.AppendLine($"        public static readonly global::MessagePack.IFormatterResolver Instance = new {ResolverTypeName}();");
            sb.AppendLine($"        private {ResolverTypeName}() {{ }}");
            sb.AppendLine("        public global::MessagePack.Formatters.IMessagePackFormatter<T> GetFormatter<T>() => null;");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            AppendPragmaRestore(sb);
            File.WriteAllText(outputPath, sb.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        }

        private static void AppendPragmaDisable(StringBuilder sb)
        {
            sb.AppendLine("#pragma warning disable 618");
            sb.AppendLine("#pragma warning disable 612");
            sb.AppendLine("#pragma warning disable 414");
            sb.AppendLine("#pragma warning disable 168");
            sb.AppendLine("#pragma warning disable CS1591");
            sb.AppendLine();
        }

        private static void AppendPragmaRestore(StringBuilder sb)
        {
            sb.AppendLine();
            sb.AppendLine("#pragma warning restore 168");
            sb.AppendLine("#pragma warning restore 414");
            sb.AppendLine("#pragma warning restore 618");
            sb.AppendLine("#pragma warning restore 612");
        }
    }
}
