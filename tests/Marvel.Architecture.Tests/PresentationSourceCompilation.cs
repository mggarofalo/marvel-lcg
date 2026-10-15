using Marvel.Rules.State;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Marvel.Architecture.Tests;

internal static class PresentationSourceCompilation
{
    private static readonly string[] ImplicitUsings =
    [
        "System",
        "System.Collections.Generic",
        "System.IO",
        "System.Linq",
        "System.Net.Http",
        "System.Threading",
        "System.Threading.Tasks",
    ];

    internal static CSharpCompilation Compile(string sourceDirectory)
    {
        SyntaxTree implicitUsings = CSharpSyntaxTree.ParseText(
            string.Join("\n", ImplicitUsings.Select(value => $"global using {value};")),
            new CSharpParseOptions(LanguageVersion.Latest),
            "ImplicitUsings.g.cs");
        SyntaxTree[] source = SourceFiles(sourceDirectory)
            .Order(StringComparer.Ordinal)
            .Select(path => CSharpSyntaxTree.ParseText(
                File.ReadAllText(path),
                new CSharpParseOptions(LanguageVersion.Latest),
                path))
            .Prepend(implicitUsings)
            .ToArray();
        var options = new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary,
            nullableContextOptions: NullableContextOptions.Enable);
        CSharpCompilation compilation = CSharpCompilation.Create(
            "Marvel.View.SourcePolicy",
            source,
            References(),
            options);
        Diagnostic[] errors = compilation.GetDiagnostics()
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        Assert.True(errors.Length == 0,
            "The View source policy could not compile its subject:\n"
                + string.Join("\n", errors.Select(error => error.ToString())));
        return compilation;
    }

    internal static MetadataReference[] References()
    {
        string[] platformAssemblies = ((string?)AppContext.GetData(
                "TRUSTED_PLATFORM_ASSEMBLIES"))!
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries);
        return platformAssemblies
            .Append(typeof(World).Assembly.Location)
            .Distinct(StringComparer.Ordinal)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();
    }

    internal static string[] ExternalMarvelMembers(CSharpCompilation compilation)
    {
        var members = new HashSet<string>(StringComparer.Ordinal);
        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (SyntaxNode node in tree.GetRoot().DescendantNodes())
            {
                ISymbol? symbol = model.GetSymbolInfo(node).Symbol;
                if (symbol is not (IMethodSymbol or IPropertySymbol or IFieldSymbol or IEventSymbol)
                    || symbol.ContainingAssembly?.Name is not { } assembly
                    || !assembly.StartsWith("Marvel.", StringComparison.Ordinal)
                    || assembly == compilation.AssemblyName)
                {
                    continue;
                }

                members.Add(symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat));
            }
        }

        return members.Order(StringComparer.Ordinal).ToArray();
    }

    private static IEnumerable<string> SourceFiles(string directory) =>
        Directory.EnumerateFiles(directory, "*.cs")
            .Concat(Directory.EnumerateDirectories(directory)
                .Where(path => Path.GetFileName(path) is not ("bin" or "obj" or ".godot"))
                .SelectMany(SourceFiles));
}
