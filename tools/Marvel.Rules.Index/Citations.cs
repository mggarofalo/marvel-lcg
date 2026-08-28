using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

// `RepositoryPaths` is linked in from `tests/Shared/`. It answers "where is
// this repository" and not "where is this test", and one copy of that answer
// is the reason it lives in one file.
using Marvel.Tests;

namespace Marvel.Rules.Index;

/// <summary>One <c>[Rule]</c> attribute, and where it sits.</summary>
/// <param name="Id">The cited id.</param>
/// <param name="Site">The file it was found in, relative to the repository root.</param>
internal readonly record struct Cited(string Id, string Site);

/// <summary>One citation as it appears in a parsed source configuration.</summary>
/// <param name="Position">The attribute's source position.</param>
/// <param name="Id">The cited id.</param>
internal readonly record struct ParsedCitation(int Position, string Id);

/// <summary>
/// Every citation the test suite makes, read off the source.
/// </summary>
/// <remarks>
/// <para>
/// <b>The source and not the assemblies.</b> Reflection would be the more
/// precise answer and needs the suite built first, which makes a report of what
/// has been written depend on whether it currently compiles. The attribute is a
/// literal string in every use, so parsing the source answers the same question
/// without counting examples in comments or strings.
/// </para>
/// <para>
/// <b>It does not validate.</b> A citation naming no rule is a build failure —
/// <c>RuleCitationTests.EveryCitedRuleExists</c> — and validating here as well
/// would put the same claim in two places, only one of which can fail a build.
/// What this does with an unknown id is count it, and say so.
/// </para>
/// </remarks>
internal static class Citations
{
    /// <summary>Reads every citation under <c>tests/</c>.</summary>
    public static IReadOnlyList<Cited> Read() =>
        Read(RepositoryPaths.Repository("tests"), RepositoryPaths.Root);

    /// <summary>Reads every citation under one source root.</summary>
    internal static IReadOnlyList<Cited> Read(string root, string repositoryRoot)
    {
        var found = new List<Cited>();

        foreach (string file in Directory
            .EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
            .OrderBy(path => path, StringComparer.Ordinal))
        {
            string site = Path.GetRelativePath(repositoryRoot, file)
                .Replace(Path.DirectorySeparatorChar, '/');

            string source = File.ReadAllText(file);
            var configurations = Configurations(source).ToArray();
            var citations = Parse(source, configurations[0]);
            if (configurations.Skip(1).Any(symbols => !citations.SequenceEqual(Parse(source, symbols))))
            {
                throw new InvalidOperationException(
                    $"{site} contains a conditional Rule attribute; citations must apply "
                    + "in every build configuration");
            }

            foreach (var citation in citations)
            {
                found.Add(new Cited(citation.Id, site));
            }
        }

        return found;
    }

    private static ParsedCitation[] Parse(
        string source,
        IEnumerable<string> symbols) =>
        CSharpSyntaxTree.ParseText(
                source,
                new CSharpParseOptions(preprocessorSymbols: symbols))
            .GetRoot()
            .DescendantNodes()
            .OfType<AttributeSyntax>()
            .Where(attribute => attribute.Name.ToString() == "Rule")
            .Select(attribute => (Attribute: attribute, Literal:
                attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression
                    as LiteralExpressionSyntax))
            .Where(pair => pair.Literal?.IsKind(SyntaxKind.StringLiteralExpression) == true)
            .Select(pair => new ParsedCitation(pair.Attribute.SpanStart, pair.Literal!.Token.ValueText))
            .ToArray();

    private static IEnumerable<IReadOnlyList<string>> Configurations(string source)
    {
        var root = CSharpSyntaxTree.ParseText(source).GetRoot();
        string[] symbols = root
            .DescendantTrivia(descendIntoTrivia: true)
            .Where(trivia => trivia.HasStructure)
            .Select(trivia => trivia.GetStructure())
            .SelectMany(directive => directive switch
            {
                IfDirectiveTriviaSyntax conditional => conditional.Condition.DescendantTokens(),
                ElifDirectiveTriviaSyntax conditional => conditional.Condition.DescendantTokens(),
                _ => [],
            })
            .Where(token => token.IsKind(SyntaxKind.IdentifierToken))
            .Select(token => token.ValueText)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(symbol => symbol, StringComparer.Ordinal)
            .ToArray();

        // The report deliberately requires one citation set for every source
        // configuration. Exhausting the symbols makes that choice independent
        // of the framework or build configuration used to run this tool.
        if (symbols.Length > 16)
        {
            throw new InvalidOperationException(
                "A source file has too many preprocessor symbols to verify Rule citations");
        }

        for (int mask = 0; mask < 1 << symbols.Length; mask++)
        {
            yield return symbols
                .Where((_, index) => (mask & (1 << index)) != 0)
                .ToArray();
        }
    }
}
