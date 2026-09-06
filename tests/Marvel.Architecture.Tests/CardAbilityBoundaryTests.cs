using System.Reflection;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Architecture.Tests;

public sealed class CardAbilityBoundaryTests
{
    private static readonly Type[] NarrowPorts =
    [
        typeof(IAbilityDescriptions),
        typeof(IWindowAbilities),
        typeof(IEncounterCardAbilities),
        typeof(ICardDamageAbilities),
        typeof(ICardReadinessAbilities),
        typeof(IThreatCardAbilities),
        typeof(ICardPowerAbilities),
        typeof(IResourceCardAbilities),
        typeof(ICardContinuationAbilities),
        typeof(IActivationCompletionAbilities),
        typeof(ICardPlacementAbilities),
        typeof(ICardSetupAbilities),
        typeof(ICardConstantAbilities),
        typeof(ICardActionAbilities),
        typeof(IAttackCardAbilities),
        typeof(ICardPlayAbilities),
        typeof(IRevealCardAbilities),
    ];

    [Fact]
    public void NarrowPortsCannotSilentlySupplyCardBehavior()
    {
        var concreteDefaults = NarrowPorts
            .SelectMany(port => port.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => !method.IsAbstract)
            .Select(method => $"{method.DeclaringType!.Name}.{method.Name}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Empty(concreteDefaults);
        Assert.Empty(typeof(ICardAbilities).GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly));
    }

    [Fact]
    public void ProductionEntryPointsRequireCardComposition()
    {
        ParameterInfo gameAbilities = typeof(Game).GetMethod(
            nameof(Game.Begin),
            [typeof(World), typeof(ICardFacts), typeof(ICardAbilities)])!
            .GetParameters()[2];
        ParameterInfo setupAbilities = typeof(WorldSetup).GetMethod(
            nameof(WorldSetup.Deal),
            [
                typeof(ICardFacts),
                typeof(IReadOnlyList<CardBlueprint>),
                typeof(IReadOnlyList<string>),
                typeof(uint),
                typeof(ICardAbilities),
                typeof(List<Marvel.Rules.Events.GameEvent>),
                typeof(bool),
            ])!
            .GetParameters()[4];

        Assert.False(gameAbilities.IsOptional);
        Assert.False(gameAbilities.HasDefaultValue);
        Assert.False(setupAbilities.IsOptional);
        Assert.False(setupAbilities.HasDefaultValue);
        Assert.NotNull(typeof(Game).GetMethod(nameof(Game.BeginWithoutCardAbilities)));
        Assert.NotNull(typeof(WorldSetup).GetMethod(nameof(WorldSetup.DealWithoutCardAbilities)));
    }

    [Fact]
    public void CompatibilityAggregateAppearsOnlyAtCompositionAndPublicAdapters()
    {
        string[] actual = typeof(Game).Assembly.GetTypes()
            .SelectMany(type => type.GetMethods(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static
                | BindingFlags.Instance | BindingFlags.DeclaredOnly))
            .Where(method => method.GetParameters()
                .Any(parameter => parameter.ParameterType == typeof(ICardAbilities)))
            .Select(method => $"{method.DeclaringType!.FullName}.{method.Name}")
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
        [
            "Marvel.Rules.Play.Game.Begin",
            "Marvel.Rules.Play.Sequence.Answer",
            "Marvel.Rules.Play.Sequence.Finish",
            "Marvel.Rules.Play.Sequence.Work",
            "Marvel.Rules.Play.VillainPhase.Answered",
            "Marvel.Rules.Play.VillainPhase.Take",
            "Marvel.Rules.State.World.set_Abilities",
            "Marvel.Rules.State.WorldSetup.Deal",
        ], actual);
    }
}
