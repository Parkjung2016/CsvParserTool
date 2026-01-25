using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

public static class DllBuilder
{
    public static void Build(string sourceDir, string outputDllPath)
    {
        var csFiles = Directory.GetFiles(sourceDir, "*.cs");

        var syntaxTrees = csFiles
            .Select(f => CSharpSyntaxTree.ParseText(File.ReadAllText(f)))
            .ToList();
        var references = new List<MetadataReference>
        {
            MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(SerializableAttribute).Assembly.Location),
            MetadataReference.CreateFromFile(typeof(System.Runtime.AssemblyTargetedPatchBandAttribute).Assembly.Location),
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
            var errors = string.Join("\n", result.Diagnostics);
            throw new Exception(errors);
        }
    }
}
