using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic ability isolates terminal behavior inside a compound effect.
public sealed class PlayersWinInstructionTests : CoreCardTestBase
{
    [Theory]
    [InlineData(true, false)]
    [InlineData(false, false)]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [Rule("rr:winning-the-game.1")]
    public void TerminalInstructionStopsLaterTextWhileNonterminalControlContinues(bool win, bool eachTime)
    {
        // "Some scenarios have alternate win or loss conditions. If these conditions
        // are met, the players win or lose accordingly."
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("01001a");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        world.CreateCard("01088", seat.Deck);
        world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var scheme = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        string initial = InitialEffect(win, eachTime);
        string json = """
            {"cards":[{"card":"01083","abilities":[{
              "trigger":{"event":"WhenCardDefeated","timing":"WhenDefeated","subject":"this"},
              "effect":{"seq":[FIRST,{"exhaust":"this"},{"placeThreat":{"scheme":{"query":"mainScheme"},"amount":2}}]}
            }]}]}
            """.Replace("FIRST", initial, StringComparison.Ordinal);
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse(json));
        var events = new List<GameEvent>();
        Agendas.Happening(world);
        Defeat.Character(world, Cards, ally, "test", events);
        Sequence.Finish(world, Cards, world.Abilities, events);
        Assert.Equal(win ? Outcome.PlayersWin : Outcome.Unfinished, world.Result);
        Assert.Equal(win ? 0 : eachTime ? 4 : 3, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(eachTime ? win ? 1 : 2 : 0,
            world.AreaOf(DeckType.EncounterDiscardPile).Cards.Count);
        Assert.Equal(win ? 0 : 1, events.OfType<FieldSet>().Count(change =>
            change.Card == ally.ObjectId && change.Verb == "Exhaust"));
        Assert.Equal(win ? DeckType.AlliesArea : DeckType.DiscardPile, ally.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static string InitialEffect(bool win, bool eachTime)
    {
        string first = win ? "\"playersWin\":\"game\"" : "\"placeThreat\":{\"scheme\":{\"query\":\"mainScheme\"},\"amount\":1}";
        return eachTime
            ? "{\"eachTime\":{\"effect\":{\"discardTop\":{\"from\":\"encounterDeck\",\"count\":2}},\"when\":{\"and\":[]},\"then\":{" + first + "}}}"
            : "{" + first + "}";
    }
}
