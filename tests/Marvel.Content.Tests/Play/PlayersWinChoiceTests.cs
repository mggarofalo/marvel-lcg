using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic choice exercises a suspended ability rather than Kang's printed text.
public sealed class PlayersWinChoiceTests : CoreCardTestBase
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [Rule("rr:winning-the-game.1")]
    public void AnsweredChoiceStopsRemainingEffectsOnVictory(bool win, bool nested)
    {
        // "Some scenarios have alternate win or loss conditions. If these conditions
        // are met, the players win or lose accordingly."
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("01001a");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        world.CreateCard("01088", seat.Deck);
        world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var scheme = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        string middle = win ? "\"playersWin\":\"game\"" : "\"placeThreat\":{\"scheme\":{\"query\":\"mainScheme\"},\"amount\":1}";
        string json = """
            {"cards":[{"card":"01083","abilities":[{
              "trigger":{"event":"WhenCardDefeated","timing":"WhenDefeated","subject":"this"},
              "effect": EFFECT
            }]}]}
            """;
        string body = """
            {"seq":[{"choose":{"options":[
                {"placeThreat":{"scheme":{"query":"mainScheme"},"amount":1}},
                {"placeThreat":{"scheme":{"query":"mainScheme"},"amount":2}}
              ]}},{ MIDDLE },{"exhaust":"this"}]}
            """.Replace("MIDDLE", middle, StringComparison.Ordinal);
        string effect = nested ? "{\"seq\":[" + body + ",{\"exhaust\":\"you\"}]}" : body;
        json = json.Replace("EFFECT", effect, StringComparison.Ordinal);
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(json));
        var events = new List<GameEvent>();
        Agendas.Happening(world);
        Defeat.Character(world, Cards, ally, "test", events);
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(Outcome.Unfinished, world.Result);
        Assert.Equal(0, scheme.Tokens.GetValueOrDefault("k_threat"));
        Sequence.Answer(world, Cards, world.Abilities, prompt, Decision.Take(0), events);
        if (world.IsOver) Assert.True(ally.Ready);
        Sequence.Finish(world, Cards, world.Abilities, events);
        AssertOutcome(world, ally, scheme, events, win, nested);
    }

    private static void AssertOutcome(World world, Card ally, Card scheme,
        List<GameEvent> events, bool win, bool nested)
    {
        Assert.Equal(win ? Outcome.PlayersWin : Outcome.Unfinished, world.Result);
        Assert.Equal(win ? 1 : 2, scheme.Tokens.GetValueOrDefault("k_threat"));
        var exhaustions = events.OfType<FieldSet>().Where(change => change.Card == ally.ObjectId && change.Verb == "Exhaust").ToArray();
        Assert.Equal(win ? 0 : 1, exhaustions.Length);
        Assert.Equal(nested && !win ? 1 : 0, events.OfType<FieldSet>().Count(change =>
            change.Card == world.Seats[0].IdentityCard.ObjectId && change.Verb == "Exhaust"));
        Assert.Equal(win ? DeckType.AlliesArea : DeckType.DiscardPile, ally.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
