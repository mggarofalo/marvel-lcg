using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Server;
using Marvel.Tests;
using Xunit;

namespace Marvel.Godot.Tests;

/// <summary>
/// Deals Rhino with the withheld Ship Command modular set for a native journey, through the production
/// dealer and setup path; the set stays outside public discovery.
/// </summary>
/// <remarks>
/// The native client asks for an admitted scenario and the named heroes; this test-only scene keeps those
/// heroes and replaces the modular set and seed. The Milano's Setup keyword puts it in the first player's
/// supports (rr:setup-keyword.1). Both seats keep their opening hands, and in the two-player journey the
/// first player ends their turn, so the client opens on the second seat's turn with the first player's
/// ready Milano offered in that seat's payment (rr:ability.8: "Any player can use such an ability on an
/// encounter card"). The prelude is a prepared component, not a transcript the client watches.
/// </remarks>
internal static class ShipCommandNativeScene
{
    internal const string Milano = "16142";
    internal static readonly string[] Heroes = ["spider_man", "captain_marvel"];
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private static readonly AbilityProgram Abilities = AbilityLowering.Book(AbilityCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json"))));

    internal static bool Handles(string contract) => contract.StartsWith("milano-", StringComparison.Ordinal);

    internal static int Players(string contract) => contract == "milano-1" ? 1 : 2;

    internal static OpenedGame Create(string contract, GameSpecification specification)
    {
        int players = Players(contract);
        Assert.Equal(Heroes.Take(players), specification.Heroes);
        var runner = new AbilityRunner(Abilities);
        var events = new List<GameEvent>();
        var plan = ScenarioDealer.Prepare(Setup, new DealRequest("rhino", [.. specification.Heroes], ["ship_command"]), Cards);
        var world = WorldSetup.DealConfigured(Cards, plan.Blueprints,
            [.. specification.Heroes.Select(hero => Setup.Hero(hero).Name)], Seed(contract), runner,
            new WorldSetupOptions(Setup.Campaign("rhino").Expert, EncounterGroups: plan.EncounterGroups), events);
        var game = Game.Begin(world, Cards, runner);
        int payer = players - 1;
        for (int answer = 0; !Reached(game, payer); answer++)
        {
            Assert.True(answer < 20, $"{contract} never reached the Milano payment");
            Prompt prompt = game.Pending!;
            // Keep each opening hand; the first player then ends their turn by declining the turn menu.
            game.Resolve(prompt.PublicKind == PublicDecisionKind.OpeningHand
                ? Decision.Take(prompt.Affordances.Single(offer => offer.Verb == Game.ResolveMulligans).Id)
                : Decision.Decline);
        }
        return new OpenedGame(game, events);
    }

    /// <summary>The Milano card in play.</summary>
    internal static Card MilanoCard(Game game) => game.State.Cards.Single(card => card.FaceId == Milano);

    // The paying seat's turn menu offers a hand card whose payment includes the first player's ready Milano.
    internal static bool Reached(Game game, int payer) =>
        game.Pending is { PublicKind: PublicDecisionKind.PlayerAction } prompt && prompt.Player == payer
        && MilanoCard(game) is { Ready: true } milano && milano.Area.PlayArea == PlayArea.Of(game.State.FirstPlayer)
        && prompt.Affordances.Any(offer => offer.CostOptions.Any(cost =>
            cost.Generators.Any(source => source.Effect == milano.ObjectId)));

    private static uint Seed(string contract) => contract == "milano-1" ? 3u : 7u;
}
