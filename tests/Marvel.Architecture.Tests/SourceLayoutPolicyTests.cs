using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Marvel.Tests;
using Xunit;

namespace Marvel.Architecture.Tests;

public sealed class SourceLayoutPolicyTests
{
    private const int LargeTypeLines = 300;
    private const int MaximumTypeLines = 500;
    private const int MaximumParameters = 7;
    private static readonly HashSet<string> VagueTypeNames = new(StringComparer.Ordinal)
    {
        "Data",
        "Entry",
        "Helper",
        "Helpers",
        "Info",
        "Manager",
        "Misc",
        "Record",
        "Stuff",
        "Utilities",
        "Utility",
    };

    [Fact]
    public void SourceLayoutMatchesTheReviewedBaseline()
    {
        string[] expected = File.ReadAllLines(
            RepositoryPaths.Repository(
                "tests", "Marvel.Architecture.Tests", "source-layout-baseline.txt"));
        string[] actual = Diagnostics().ToArray();

        Assert.Equal(
            expected.Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal),
            expected);
        Assert.All(expected, diagnostic => Assert.True(
            diagnostic.Contains(": SL003:", StringComparison.Ordinal)
            || diagnostic.Contains(": SL005:", StringComparison.Ordinal),
            $"Hard source-layout violations cannot be admitted: {diagnostic}"));

        string[] unadmitted = actual.Except(expected, StringComparer.Ordinal).ToArray();
        string[] stale = expected.Except(actual, StringComparer.Ordinal).ToArray();
        Assert.True(
            unadmitted.Length == 0 && stale.Length == 0,
            $"Unadmitted diagnostics:{Environment.NewLine}"
            + string.Join(Environment.NewLine, unadmitted)
            + $"{Environment.NewLine}Stale baseline entries:{Environment.NewLine}"
            + string.Join(Environment.NewLine, stale));
    }

    private static IEnumerable<string> Diagnostics()
    {
        foreach (string path in ApplicationSources())
        {
            string relative = Path.GetRelativePath(RepositoryPaths.Root, path)
                .Replace('\\', '/');
            SyntaxTree tree = CSharpSyntaxTree.ParseText(
                File.ReadAllText(path), path: relative);
            CompilationUnitSyntax root = tree.GetCompilationUnitRoot();
            MemberDeclarationSyntax[] types = TopLevelTypes(root).ToArray();
            bool testSource = relative.StartsWith("tests/", StringComparison.Ordinal);

            if (types.Length > 1)
            {
                string names = string.Join(",", types.Select(TypeName));
                yield return Diagnostic(
                    relative, types[1], "SL001", $"top-level-types={names}");
            }

            if (types.Length == 1 && !testSource)
            {
                string name = TypeName(types[0]);
                if (!string.Equals(Path.GetFileNameWithoutExtension(path), name,
                        StringComparison.Ordinal))
                {
                    yield return Diagnostic(relative, types[0], "SL002", name);
                }
                if (VagueTypeNames.Contains(name))
                {
                    yield return Diagnostic(relative, types[0], "SL006", name);
                }
            }

            if (testSource)
            {
                continue;
            }

            foreach (TypeDeclarationSyntax type in root.DescendantNodes()
                         .OfType<TypeDeclarationSyntax>())
            {
                int lines = DeclarationLines(type);
                if (lines > MaximumTypeLines)
                {
                    yield return Diagnostic(
                        relative, type, "SL004", $"{type.Identifier.ValueText}={lines}");
                }
                else if (lines > LargeTypeLines)
                {
                    yield return Diagnostic(
                        relative, type, "SL003", $"{type.Identifier.ValueText}={lines}");
                }
            }

            foreach ((SyntaxNode declaration, string name, int parameters) in
                     BehavioralSignatures(root))
            {
                if (parameters > MaximumParameters)
                {
                    yield return Diagnostic(
                        relative, declaration, "SL005", $"{name}={parameters}");
                }
            }
        }
    }

    private static IEnumerable<MemberDeclarationSyntax> TopLevelTypes(
        CompilationUnitSyntax root)
    {
        foreach (MemberDeclarationSyntax member in root.Members)
        {
            if (member is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
            {
                yield return member;
            }
            else if (member is BaseNamespaceDeclarationSyntax @namespace)
            {
                foreach (MemberDeclarationSyntax child in @namespace.Members)
                {
                    if (child is BaseTypeDeclarationSyntax or DelegateDeclarationSyntax)
                    {
                        yield return child;
                    }
                }
            }
        }
    }

    private static string TypeName(MemberDeclarationSyntax declaration) => declaration switch
    {
        BaseTypeDeclarationSyntax type => type.Identifier.ValueText,
        DelegateDeclarationSyntax type => type.Identifier.ValueText,
        _ => throw new InvalidOperationException(
            $"unsupported top-level declaration {declaration.Kind()}"),
    };

    private static int DeclarationLines(SyntaxNode declaration)
    {
        FileLinePositionSpan span = declaration.GetLocation().GetLineSpan();
        return span.EndLinePosition.Line - span.StartLinePosition.Line + 1;
    }

    private static IEnumerable<(SyntaxNode Declaration, string Name, int Parameters)>
        BehavioralSignatures(CompilationUnitSyntax root)
    {
        foreach (MethodDeclarationSyntax method in root.DescendantNodes()
                     .OfType<MethodDeclarationSyntax>())
        {
            yield return (method, method.Identifier.ValueText, method.ParameterList.Parameters.Count);
        }

        foreach (ConstructorDeclarationSyntax constructor in root.DescendantNodes()
                     .OfType<ConstructorDeclarationSyntax>())
        {
            yield return (
                constructor,
                constructor.Identifier.ValueText,
                constructor.ParameterList.Parameters.Count);
        }

        foreach (LocalFunctionStatementSyntax function in root.DescendantNodes()
                     .OfType<LocalFunctionStatementSyntax>())
        {
            yield return (
                function,
                function.Identifier.ValueText,
                function.ParameterList.Parameters.Count);
        }
    }

    private static string Diagnostic(
        string path,
        SyntaxNode declaration,
        string rule,
        string detail)
    {
        int line = declaration.GetLocation().GetLineSpan().StartLinePosition.Line + 1;
        return $"{path}:{line}: {rule}: {detail}";
    }

    private static IEnumerable<string> ApplicationSources()
    {
        string output = GitOutput(
            "ls-files", "-z", "--cached", "--others", "--exclude-standard", "--",
            "src", "tests", "tools");
        return output.Split('\0', StringSplitOptions.RemoveEmptyEntries)
            .Where(path => Path.GetExtension(path).Equals(".cs", StringComparison.OrdinalIgnoreCase))
            .Select(path => Path.Combine(RepositoryPaths.Root, path))
            .Where(File.Exists)
            .Order(StringComparer.Ordinal);
    }

    private static string GitOutput(params string[] arguments)
    {
        var start = new ProcessStartInfo("git")
        {
            UseShellExecute = false,
            RedirectStandardOutput = true,
            WorkingDirectory = RepositoryPaths.Root,
        };
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using var process = Process.Start(start)
            ?? throw new InvalidOperationException("Could not start git");
        string output = process.StandardOutput.ReadToEnd();
        if (!process.WaitForExit(TimeSpan.FromMinutes(1)) || process.ExitCode != 0)
        {
            throw new InvalidOperationException("Could not enumerate application source files");
        }
        return output;
    }
}
