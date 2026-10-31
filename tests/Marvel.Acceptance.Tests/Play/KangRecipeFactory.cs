using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Server;
using Marvel.Session;
using Marvel.Tests;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>
/// Deals the declared Kang recipes through the production dealer and setup path,
/// behind a host's <see cref="IDurableGameFactory"/> seam.
/// </summary>
/// <remarks>
/// Kang is outside the accepted product boundary, so <see cref="DatasetGameFactory"/>
/// refuses it. This test factory runs the same steps as that factory's ordinary
/// standalone creation (<see cref="ScenarioDealer.Prepare"/>,
/// <see cref="WorldSetup.DealConfigured"/>, <see cref="Game.Begin"/>) over the complete
/// declared setup catalog and the authored ability book, and refuses any other scenario.
/// </remarks>
internal sealed class KangRecipeFactory : IObservedGameFactory
{
    internal static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    internal static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private static readonly AbilityProgram Abilities = AbilityLowering.Book(AbilityCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json"))));
    private static readonly SessionCompatibility Contracts = DatasetGameFactory.Load(RepositoryPaths.Root).Compatibility;

    /// <inheritdoc />
    public Game Game { get; private set; } = null!;

    public SessionCompatibility Compatibility => Contracts;

    public OpenedGame Create(GameSpecification specification)
    {
        ArgumentNullException.ThrowIfNull(specification);
        if (specification.Scenario is not ("kang" or "kang_expert") || specification.Campaign is not null)
            throw new ArgumentException("this factory deals only the standalone Kang recipes", nameof(specification));
        PlayerCountRules.Validate(specification.Heroes.Count);
        var runner = new AbilityRunner(Abilities);
        var events = new List<GameEvent>();
        var campaign = Setup.Campaign(specification.Scenario);
        var plan = ScenarioDealer.Prepare(Setup, new DealRequest(specification.Scenario, specification.Heroes,
            specification.ModularSets, PlayerDecks: specification.PlayerDecks), Cards);
        var world = WorldSetup.DealConfigured(Cards, plan.Blueprints,
            [.. specification.Heroes.Select(hero => Setup.Hero(hero).Name)], specification.Seed, runner,
            new WorldSetupOptions(campaign.Expert, EncounterGroups: plan.EncounterGroups), events);
        Game = Game.Begin(world, Cards, runner);
        return new OpenedGame(Game, events);
    }
}
