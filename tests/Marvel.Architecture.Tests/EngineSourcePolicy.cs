using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Marvel.Architecture.Tests;

/// <summary>Symbol-bound restrictions on acquiring ambient state and creating gameplay streams.</summary>
internal static class EngineSourcePolicy
{
    internal static string[] Violations(Compilation compilation)
    {
        var failures = new HashSet<string>(StringComparer.Ordinal);
        var streamSites = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (SyntaxNode node in tree.GetRoot().DescendantNodes())
            {
                string? violation = Violation(model, node);
                if (violation is not null) failures.Add(violation);
                CountStreamSite(model, node, streamSites);
            }
        }
        foreach ((string owner, int count) in streamSites)
            if (count > 1) failures.Add($"rng-owner: multiple construction/seeding sites for {owner}");
        return failures.Order(StringComparer.Ordinal).ToArray();
    }

    private static void CountStreamSite(SemanticModel model, SyntaxNode node, Dictionary<string, int> counts)
    {
        if (node is not (InvocationExpressionSyntax or BaseObjectCreationExpressionSyntax)
            || model.GetSymbolInfo(node).Symbol is not IMethodSymbol method || !IsRandom(method)) return;
        string key = method.ToDisplayString();
        counts[key] = counts.GetValueOrDefault(key) + 1;
    }

    private static string? Violation(SemanticModel model, SyntaxNode node)
    {
        ISymbol? symbol = model.GetSymbolInfo(node).Symbol;
        if (symbol is null || node is UsingDirectiveSyntax) return null;
        string owner = (symbol as INamedTypeSymbol ?? symbol.ContainingType)?.ToDisplayString() ?? "";
        if (Ambient(owner, symbol.Name)) return $"ambient: {owner}.{symbol.Name}";
        return RandomViolation(model, node, symbol);
    }

    private static string? RandomViolation(SemanticModel model, SyntaxNode node, ISymbol symbol)
    {
        if (symbol is not IMethodSymbol method || !IsRandom(method)
            || node.Ancestors().OfType<DocumentationCommentTriviaSyntax>().Any()) return null;
        ISymbol? caller = model.GetEnclosingSymbol(node.SpanStart);
        return OwnsRandom(caller, method) ? null : $"rng-owner: {method.ToDisplayString()} from {caller?.ToDisplayString()}";
    }

    private static bool Ambient(string type, string member) =>
        type.StartsWith("System.Net.", StringComparison.Ordinal)
        || type.StartsWith("System.Threading.", StringComparison.Ordinal)
        || type is "System.IO.File" or "System.IO.Directory" or "System.IO.FileInfo"
            or "System.IO.DirectoryInfo" or "System.IO.FileStream" or "System.IO.FileSystemWatcher"
            or "System.DateTime" or "System.DateTimeOffset" or "System.TimeProvider"
            or "System.Random" or "System.Console" or "System.Environment"
            or "System.Diagnostics.Process" or "System.Diagnostics.Stopwatch"
            or "System.Security.Cryptography.RandomNumberGenerator"
        || type == "System.Guid" && member == "NewGuid";

    private static bool IsRandom(IMethodSymbol method) =>
        method.ContainingType.ToDisplayString() is "Marvel.Core.Random.EngineRandom"
            or "Marvel.Core.Random.MersenneTwister"
        && (method.MethodKind == MethodKind.Constructor || method.Name is "Seed" or "SetSeed" or "SetState");

    private static bool OwnsRandom(ISymbol? enclosing, IMethodSymbol called)
    {
        if (enclosing is IFieldSymbol { AssociatedSymbol: IPropertySymbol property }) enclosing = property;
        string? owner = enclosing?.ContainingType?.ToDisplayString();
        string target = called.ContainingType.ToDisplayString();
        // The published stream has one construction chain. No gameplay call may restore or reseed it.
        if (called.Name == "SetState" || called.Name == "SetSeed") return false;
        if (called.Name == "Seed")
            return owner == target && enclosing is IMethodSymbol { MethodKind: MethodKind.Constructor };
        return OwnsConstruction(enclosing, target, owner);
    }

    private static bool OwnsConstruction(ISymbol? enclosing, string target, string? owner) => target switch
    {
        "Marvel.Core.Random.EngineRandom" => owner == "Marvel.Rules.State.World"
            && enclosing is IMethodSymbol { MethodKind: MethodKind.Constructor },
        "Marvel.Core.Random.MersenneTwister" => owner == "Marvel.Core.Random.EngineRandom"
            && enclosing is IPropertySymbol { Name: "Generator" },
        _ => false,
    };
}
