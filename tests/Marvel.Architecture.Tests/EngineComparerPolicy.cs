using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace Marvel.Architecture.Tests;

/// <summary>Shared tables may retain framework comparers, never mutable comparison state.</summary>
internal static class EngineComparerPolicy
{
    internal static bool SafeArguments(ExpressionSyntax initializer, SemanticModel model)
    {
        foreach (ArgumentSyntax argument in initializer.DescendantNodes().OfType<ArgumentSyntax>())
        {
            if (model.GetOperation(argument) is not IArgumentOperation { Parameter: { } parameter }) continue;
            string type = parameter.Type.OriginalDefinition.ToDisplayString();
            if (type is not ("System.Collections.Generic.IEqualityComparer<T>"
                or "System.Collections.Generic.IComparer<T>")) continue;
            if (!FrameworkComparer(argument.Expression, model)) return false;
        }
        return true;
    }

    private static bool FrameworkComparer(ExpressionSyntax expression, SemanticModel model)
    {
        Optional<object?> constant = model.GetConstantValue(expression);
        if (constant.HasValue && constant.Value is null) return true;
        if (model.GetSymbolInfo(expression).Symbol is not IPropertySymbol property) return false;
        string owner = property.ContainingType.OriginalDefinition.ToDisplayString();
        return owner == "System.StringComparer" && property.Name is "Ordinal" or "OrdinalIgnoreCase"
            || property.Name == "Default" && owner is "System.Collections.Generic.EqualityComparer<T>"
                or "System.Collections.Generic.Comparer<T>";
    }
}
