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

            var rootNode = CSharpSyntaxTree.ParseText(File.ReadAllText(file)).GetRoot();
            var attributes = RuleAttributes(rootNode).ToArray();
            bool conditional = attributes.Any(attribute => IsConditional(rootNode, attribute))
                || rootNode
                .DescendantTrivia(descendIntoTrivia: true)
                .Where(trivia => trivia.IsKind(SyntaxKind.DisabledTextTrivia))
                .Select(trivia => CSharpSyntaxTree.ParseText(trivia.ToFullString()).GetRoot())
                .Any(disabled => RuleAttributes(disabled).Any());
            if (conditional)
            {
                throw new InvalidOperationException(
                    $"{site} contains a conditional Rule attribute; citations must apply "
                    + "in every build configuration");
            }

            foreach (var attribute in attributes)
            {
                if (attribute.ArgumentList?.Arguments.FirstOrDefault()?.Expression
                    is LiteralExpressionSyntax literal
                    && literal.IsKind(SyntaxKind.StringLiteralExpression))
                {
                    found.Add(new Cited(literal.Token.ValueText, site));
                }
            }
        }

        return found;
    }

    private static IEnumerable<AttributeSyntax> RuleAttributes(SyntaxNode root) =>
        root.DescendantNodes()
            .OfType<AttributeSyntax>()
            .Where(candidate => candidate.Name.ToString() == "Rule");

    private static bool IsConditional(SyntaxNode root, AttributeSyntax attribute)
    {
        int depth = 0;
        foreach (var directive in root
            .DescendantTrivia(descendIntoTrivia: true)
            .Where(trivia => trivia.HasStructure)
            .Select(trivia => trivia.GetStructure())
            .OfType<DirectiveTriviaSyntax>()
            .Where(directive => directive.SpanStart < attribute.SpanStart)
            .OrderBy(directive => directive.SpanStart))
        {
            if (directive.IsKind(SyntaxKind.IfDirectiveTrivia))
            {
                depth++;
            }
            else if (directive.IsKind(SyntaxKind.EndIfDirectiveTrivia))
            {
                depth--;
            }
        }

        return depth > 0;
    }
}
