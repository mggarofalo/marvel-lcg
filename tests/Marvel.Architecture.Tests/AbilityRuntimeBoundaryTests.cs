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
    public void RuntimeSourcesUseNarrowCardCapabilities()
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
    public void RuntimeStateDoesNotLiveInAnExternalWorldRegistry()
    {
        string[] constructors = Sources()
            .Where(source => source.Text.Contains("ConditionalWeakTable", StringComparison.Ordinal))
            .Select(source => Path.GetFileName(source.Path))
            .ToArray();

        Assert.Empty(constructors);
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
            "AbilityOfferQueries",
            "AbilityProgram",
            "ICardPlayAbilities",
            "ICardReadinessAbilities",
            "IEncounterCardAbilities",
            "IResourceCardAbilities",
            "IThreatCardAbilities",
        ], dependencies);
    }

    [Fact]
    public void WorldOwnsItsBoundRuntimeAndContinuationPortsAreOpaque()
    {
        var runtime = typeof(Marvel.Rules.State.World).GetProperty("AbilityRuntime")!;
        Assert.Equal(typeof(Marvel.Rules.State.AbilityRuntime), runtime.PropertyType);
        Assert.False(runtime.SetMethod!.IsPublic);
        var parameters = typeof(ICardContinuationAbilities).GetMethods()
            .Where(method => method.Name is "Choosing" or "Chose" or "ResolveEachPlayer" or "ResumeAbility")
            .Select(method => method.GetParameters().Select(parameter => parameter.ParameterType).ToArray());
        Assert.All(parameters, types =>
        {
            Assert.Equal(typeof(Marvel.Rules.State.World), types[0]);
            Assert.Equal(typeof(PhaseStep), types[1]);
            Assert.InRange(types.Length, 2, 3);
        });
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
                    "Abilities" or "DamageAbilities" or "ThreatAbilities" or "PowerAbilities"
                    or "ResourceAbilities" or "EncounterAbilities" or "ContinuationAbilities"
                    or "ActivationCompletionAbilities" or "ReadinessAbilities" or "SetupAbilities"
                    or "PlacementAbilities" or "ConstantAbilities" or "ActionAbilities"
                    or "WindowAbilities" or "AttackAbilities" or "CardPlayAbilities"
                    or "RevealAbilities" or "CounterPools")
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
