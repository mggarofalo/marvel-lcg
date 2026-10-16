using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Marvel.Architecture.Tests;

/// <summary>Finds empty test bodies and exact duplicates with the same inputs in one class.</summary>
internal static class TestQualityPolicy
{
    internal static IEnumerable<string> Diagnostics(SyntaxTree tree)
    {
        foreach (TypeDeclarationSyntax type in tree.GetRoot().DescendantNodes().OfType<TypeDeclarationSyntax>())
        {
            var bodies = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (MethodDeclarationSyntax method in type.Members.OfType<MethodDeclarationSyntax>().Where(IsTest))
            {
                string location = $"{tree.FilePath}:{tree.GetLineSpan(method.Span).StartLinePosition.Line + 1}";
                string name = $"{type.Identifier.ValueText}.{method.Identifier.ValueText}";
                SyntaxNode? body = (SyntaxNode?)method.Body ?? method.ExpressionBody;
                if (body is null || method.Body is { } block
                    && block.Statements.All(statement => statement is EmptyStatementSyntax))
                {
                    yield return $"{location}: TQ001: {name} has no test behavior";
                    continue;
                }
                string inputs = Tokens(method.ParameterList) + Tokens(method.ReturnType)
                    + string.Join("|", method.AttributeLists.SelectMany(list => list.Attributes)
                        .Where(attribute => AttributeName(attribute).EndsWith("Data", StringComparison.Ordinal))
                        .Select(Tokens));
                string key = inputs + "|" + Tokens(body);
                if (bodies.TryGetValue(key, out string? first))
                    yield return $"{location}: TQ002: {name} duplicates {first} with identical inputs and body";
                else bodies.Add(key, name);
            }
        }
    }

    private static bool IsTest(MethodDeclarationSyntax method) => method.AttributeLists
        .SelectMany(list => list.Attributes)
        .Any(attribute => AttributeName(attribute) is "Fact" or "Theory");

    private static string AttributeName(AttributeSyntax attribute)
    {
        string name = attribute.Name.ToString().Split('.').Last();
        return name.EndsWith("Attribute", StringComparison.Ordinal) ? name[..^9] : name;
    }

    private static string Tokens(SyntaxNode node) =>
        string.Join("\u001f", node.DescendantTokens().Select(token => token.Text));
}
