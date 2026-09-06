using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Marvel.Rules.Play;
using Marvel.Tests;
using Xunit;

namespace Marvel.Architecture.Tests;

public sealed class AbilityRuntimeBoundaryTests
{
    private static readonly string RuntimeDirectory = Path.Combine(
        RepositoryPaths.Root, "src", "Marvel.Cards", "Run");

    [Fact]
    public void RuntimeSourcesDoNotRecoverTheCompatibilityFacade()
    {
        string[] violations = Sources()
            .Where(source => Path.GetFileName(source.Path) != "AbilityRunner.cs")
            .SelectMany(source => source.Tree.GetRoot().DescendantTokens()
                .Where(token => token.ValueText is "ICardAbilities" or "AbilityRunner")
                .Select(token => $"{Path.GetFileName(source.Path)}:{token.ValueText}"))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void OnlyGameRuntimeConstructsTheWeakWorldRegistry()
    {
        string[] constructors = Sources()
            .Where(source => source.Text.Contains("ConditionalWeakTable", StringComparison.Ordinal))
            .Select(source => Path.GetFileName(source.Path))
            .ToArray();

        Assert.Equal(["AbilityGameRuntimes.cs"], constructors);
    }

    [Fact]
    public void ExecutorTakesOnlyNarrowNestedEntryPorts()
    {
        var executor = Sources().Single(source =>
                Path.GetFileName(source.Path) == "AbilityResolutionExecution.cs")
            .Tree.GetRoot(TestContext.Current.CancellationToken)
            .DescendantNodes().OfType<ClassDeclarationSyntax>()
            .Single(type => type.Identifier.ValueText == "AbilityResolutionExecution");
        string[] dependencies = executor.Members.OfType<ConstructorDeclarationSyntax>()
            .Single().ParameterList.Parameters
            .Select(parameter => parameter.Type!.ToString())
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
        [
            "AbilityGameRuntimes",
            "AbilityOfferQueries",
            "AbilityProgram",
            "ICardPlayAbilities",
            "ICardReadinessAbilities",
            "IEncounterCardAbilities",
            "IResourceCardAbilities",
        ], dependencies);
    }

    [Fact]
    public void ExecutionAndPaymentSourcesDoNotRecoverWorldCardCapabilities()
    {
        string[] executionFiles =
        [
            "AbilityCostPayment.cs",
            "AbilityEventPayment.cs",
            "AbilityExpressionEvaluation.cs",
            "AbilityPaymentRules.cs",
            "AbilityStructuralExecution.cs",
        ];
        string[] violations = Sources()
            .Where(source => Path.GetFileName(source.Path)
                    .StartsWith("AbilityResolutionExecution", StringComparison.Ordinal)
                || executionFiles.Contains(Path.GetFileName(source.Path), StringComparer.Ordinal))
            .SelectMany(source => source.Tree.GetRoot(TestContext.Current.CancellationToken)
                .DescendantNodes()
                .OfType<MemberAccessExpressionSyntax>()
                .Where(access => access.Name.Identifier.ValueText is
                    "Abilities" or "ResourceAbilities" or "DamageAbilities")
                .Where(access => access.Expression is IdentifierNameSyntax identifier
                        && identifier.Identifier.ValueText is "world" or "World"
                    || access.Expression is MemberAccessExpressionSyntax owner
                        && owner.Name.Identifier.ValueText == "World")
                .Select(access => $"{Path.GetFileName(source.Path)}:{access}"))
            .ToArray();

        Assert.Empty(violations);
    }

    [Fact]
    public void CardPlayOffersExplicitResourceCapabilityOverloads()
    {
        var methods = typeof(CardPlay).GetMethods();

        Assert.Contains(methods, method => method.Name == nameof(CardPlay.Generators)
            && method.GetParameters().Any(parameter =>
                parameter.ParameterType == typeof(IResourceCardAbilities)));
        Assert.Contains(methods, method => method.Name == nameof(CardPlay.Spend)
            && method.GetParameters().Any(parameter =>
                parameter.ParameterType == typeof(IResourceCardAbilities)));
    }

    private static IEnumerable<(string Path, string Text, Microsoft.CodeAnalysis.SyntaxTree Tree)> Sources() =>
        Directory.GetFiles(RuntimeDirectory, "*.cs")
            .Order(StringComparer.Ordinal)
            .Select(path =>
            {
                string text = File.ReadAllText(path);
                return (path, text, CSharpSyntaxTree.ParseText(text, path: path));
            });
}
