using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Marvel.Architecture.Tests;

/// <summary>Shared engine data must be immutable, including collection elements.</summary>
internal static class EngineStaticStatePolicy
{
    internal static string[] Violations(Compilation compilation)
    {
        var failures = new List<string>();
        foreach (SyntaxTree tree in compilation.SyntaxTrees)
        {
            SemanticModel model = compilation.GetSemanticModel(tree);
            foreach (SyntaxNode node in tree.GetRoot().DescendantNodes())
            {
                ISymbol? symbol = node switch
                {
                    VariableDeclaratorSyntax variable => model.GetDeclaredSymbol(variable),
                    PropertyDeclarationSyntax property when property.Initializer is not null
                        || property.AccessorList?.Accessors.Any(accessor => accessor.Body is null
                            && accessor.ExpressionBody is null) == true => model.GetDeclaredSymbol(property),
                    EventDeclarationSyntax declaration => model.GetDeclaredSymbol(declaration),
                    _ => null,
                };
                if (symbol is null || !symbol.IsStatic || symbol is IFieldSymbol { IsConst: true }) continue;
                if (!SafeDeclaration(symbol, node, model)) failures.Add(symbol.ToDisplayString());
            }
        }
        return failures.Order(StringComparer.Ordinal).ToArray();
    }

    private static bool SafeDeclaration(ISymbol symbol, SyntaxNode node, SemanticModel model)
    {
        bool readOnly = symbol is IFieldSymbol { IsReadOnly: true }
            or IPropertySymbol { SetMethod: null };
        ExpressionSyntax? initializer = Initializer(node);
        if (!readOnly || initializer is null) return false;
        if (!EngineComparerPolicy.SafeArguments(initializer, model)) return false;
        TypeInfo info = model.GetTypeInfo(initializer);
        ITypeSymbol? type = info.Type ?? info.ConvertedType;
        if (!OwnsBackingStorage(type, initializer, model)) return false;
        if (Immutable(type, new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default))) return true;
        return ReviewedConstruction(symbol.ToDisplayString(), initializer, type);
    }

    private static ExpressionSyntax? Initializer(SyntaxNode node) => node switch
    {
        VariableDeclaratorSyntax variable => variable.Initializer?.Value,
        PropertyDeclarationSyntax property => property.Initializer?.Value,
        _ => null,
    };

    private static bool ReviewedConstruction(string name, ExpressionSyntax initializer, ITypeSymbol? type)
    {
        if (name == "Marvel.Content.Behavior.CanonicalCoreScene.OperationHandlers")
            return StaticDispatchTable(initializer, type);
        // Options are mutable during construction; these exact codecs seal them before publication.
        return name is "Marvel.Rules.Events.EventJson.Options"
            or "Marvel.Session.JournalJson.Options" or "Marvel.Session.SessionSaveJson.Options";
    }

    private static bool StaticDispatchTable(ExpressionSyntax initializer, ITypeSymbol? type)
    {
        // This exact scene dispatch table contains functions, never captured scene instances.
        ObjectCreationExpressionSyntax[] creations = initializer.DescendantNodes()
            .OfType<ObjectCreationExpressionSyntax>().ToArray();
        return creations is [{ Initializer: { } entries }]
            && entries.Expressions.All(entry => entry is AssignmentExpressionSyntax
                { Right: LambdaExpressionSyntax lambda }
                && lambda.Modifiers.Any(modifier => modifier.Text == "static"))
            && type?.OriginalDefinition.ToDisplayString().StartsWith(
                "System.Collections.Frozen.FrozenDictionary<", StringComparison.Ordinal) == true;
    }

    private static bool OwnsBackingStorage(ITypeSymbol? type, ExpressionSyntax initializer, SemanticModel model) =>
        type?.OriginalDefinition.ToDisplayString().StartsWith(
            "System.Collections.ObjectModel.ReadOnlyDictionary<", StringComparison.Ordinal) != true
        || OwnsDictionary(initializer, model);

    private static bool OwnsDictionary(ExpressionSyntax initializer, SemanticModel model) => initializer switch
    {
        BaseObjectCreationExpressionSyntax { ArgumentList.Arguments: [{ Expression: BaseObjectCreationExpressionSyntax created }] }
            => model.GetTypeInfo(created).Type?.OriginalDefinition.ToDisplayString()
                .StartsWith("System.Collections.Generic.Dictionary<", StringComparison.Ordinal) == true,
        _ => false,
    };

    private static bool Immutable(ITypeSymbol? type, HashSet<ITypeSymbol> visiting)
    {
        if (type is null || type.TypeKind is TypeKind.Array or TypeKind.Error) return false;
        if (Scalar(type)) return true;
        string name = type.OriginalDefinition.ToDisplayString();
        if (name is "System.Type" or "System.Text.RegularExpressions.Regex") return true;
        if (!visiting.Add(type)) return false;
        try
        {
            if (type is INamedTypeSymbol generic && IsImmutableCollection(name))
                return generic.TypeArguments.All(argument => Immutable(argument, visiting));
            if (!type.IsValueType && !type.IsRecord) return false;
            return ImmutableFields(type, visiting);
        }
        finally { visiting.Remove(type); }
    }

    private static bool Scalar(ITypeSymbol type) =>
        type.SpecialType is >= SpecialType.System_Boolean and <= SpecialType.System_Double
        || type.SpecialType == SpecialType.System_String || type.TypeKind == TypeKind.Enum;

    private static bool ImmutableFields(ITypeSymbol type, HashSet<ITypeSymbol> visiting)
    {
        bool ownFields = type.GetMembers().OfType<IFieldSymbol>().Where(field => !field.IsStatic)
            .All(field => (type.IsValueType || field.IsReadOnly) && Immutable(field.Type, visiting));
        return ownFields && (type.BaseType is null
            || type.BaseType.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType
            || Immutable(type.BaseType, visiting));
    }

    private static bool IsImmutableCollection(string type) => type is
        "System.Collections.Immutable.ImmutableArray<T>"
        or "System.Collections.Immutable.ImmutableList<T>"
        or "System.Collections.Immutable.ImmutableHashSet<T>"
        or "System.Collections.Immutable.ImmutableSortedSet<T>"
        or "System.Collections.Immutable.ImmutableQueue<T>"
        or "System.Collections.Immutable.ImmutableStack<T>"
        or "System.Collections.Immutable.ImmutableDictionary<TKey, TValue>"
        or "System.Collections.Immutable.ImmutableSortedDictionary<TKey, TValue>"
        or "System.Collections.Frozen.FrozenSet<T>"
        or "System.Collections.Frozen.FrozenDictionary<TKey, TValue>"
        or "System.Collections.ObjectModel.ReadOnlyDictionary<TKey, TValue>";
}
