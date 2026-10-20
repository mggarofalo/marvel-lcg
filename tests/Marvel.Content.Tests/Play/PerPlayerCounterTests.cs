using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PerPlayerCounterTests
{
    [Theory]
    [InlineData(1, true, 2)]
    [InlineData(2, true, 4)]
    [InlineData(4, true, 8)]
    [InlineData(4, false, 2)]
    [Rule("rr:per-player-icon")]
    public void EntryUsesOriginalPlayerCountAndEmitsTheActualCounterTotal(int players, bool perPlayer, long expected)
    {
        // "The value associated with this icon is multiplied by the number of players who started the scenario."
        // 04064: "Uses (2 [per-player] ammo counters)."
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(cards, players);
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"04064","startingCounters":{"type":"ammo","count":2,"uses":true,"perPlayer":FLAG},"abilities":[]}]}
            """.Replace("FLAG", perPlayer ? "true" : "false", StringComparison.Ordinal)));
        world.Abilities = runner;
        var villain = world.CreateCard("04058", world.AreaOf(DeckType.VillainArea));
        var gun = world.CreateCard("04064", world.AreaOf(DeckType.UpgradesArea, PlayArea.Villains, villain.ObjectId));
        var events = new List<GameEvent>();
        Reveal.EnterPlay(world, cards, gun, events, abilities: runner);
        Assert.Equal(expected, gun.Tokens["c_ammo"]);
        Assert.Contains(events.OfType<FieldSet>(), change => change.Card == gun.ObjectId && change.Field == "c_ammo" && change.To == expected);
    }
}

