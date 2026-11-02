using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Sim;

internal sealed record OpenedGame(
    Game Game,
    IReadOnlyList<GameEvent> SetupEvents)
{
    /// <summary>
    /// Deals a standalone game through the same recipe compilation as server
    /// creation: <see cref="ScenarioDealer.Prepare"/> binds a scenario's
    /// encounter groups, and <see cref="WorldSetup.DealConfigured"/> receives them.
    /// </summary>
    internal static OpenedGame Open(SimulationData data, SimulationConfig config, uint seed)
    {
        var abilities = new AbilityRunner(data.Abilities);
        var setupEvents = new List<GameEvent>();
        string campaign = SimulationHarnessSupport.CampaignKey(config);
        var scenario = ScenarioDealer.Prepare(data.Setup,
            new DealRequest(campaign, config.Heroes, config.ModularSets), data.Cards);
        var world = WorldSetup.DealConfigured(
            data.Cards,
            scenario.Blueprints,
            [.. config.Heroes.Select(hero => data.Setup.Hero(hero).Name)],
            seed,
            abilities,
            new WorldSetupOptions(data.Setup.Campaign(campaign).Expert,
                EncounterGroups: scenario.EncounterGroups),
            setupEvents);
        return new OpenedGame(Game.Begin(world, data.Cards, abilities), setupEvents);
    }
}
