using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class DllBuilder
{

    public static void BuildFromCsv(
        string csvPath,
        string outputDllPath
    )
    {
        string classSource = CsvClassGenerator.GenerateClass(csvPath);


        var baseDir = AppContext.BaseDirectory;


        string dataRecordSource = File.ReadAllText("IDataRecord.cs");
        string cryptoUtilSource = File.ReadAllText("CryptoUtil.cs");
        var syntaxTrees = new[]
        {
            CSharpSyntaxTree.ParseText(dataRecordSource),
            CSharpSyntaxTree.ParseText(classSource),
            CSharpSyntaxTree.ParseText(cryptoUtilSource),
        };

        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
        };

        var compilation = CSharpCompilation.Create(
            Path.GetFileNameWithoutExtension(outputDllPath),
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
        );

        using var fs = new FileStream(outputDllPath, FileMode.Create);
        var result = compilation.Emit(fs);

        if (!result.Success)
        {
            var errors = string.Join(
                "\n",
                result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error)
            );
            throw new Exception(errors);
        }
    }
}