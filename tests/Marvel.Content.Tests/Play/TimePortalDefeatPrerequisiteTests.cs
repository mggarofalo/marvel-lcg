using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// The authored Time Portal row on both encounter topologies; Kang's public admission stays closed.
public sealed class TimePortalDefeatPrerequisiteTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:defeat.1")]
    [Rule("rr:replacement-effect")]
    [Rule("rr:leaves-play.2")]
    public void DefeatedPortalReplacesDiscardInBothEncounterTopologies(bool redSkull, bool portal)
    {
        // rr:defeat.1: "If an ally, minion, or side scheme is defeated, it is discarded."
        // rr:replacement-effect: "A replacement effect replaces a specified effect with a
        // different effect." Time Portal (11033): "shuffle it into the encounter deck instead
        // of discarding it." ruling:22d159cabcc98d45 directs it to the encounter deck even
        // with Red Skull's separate side-scheme deck.
        var world = redSkull ? RedSkullFixture.Create().World : ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var origin = world.AreaOf(redSkull ? DeckType.SideSchemeDeck : DeckType.EncounterDeck);
        var scheme = world.CreateCard(portal ? "11033" : "01109", origin);
        World.MoveToTop(scheme, world.AreaOf(DeckType.SideSchemesArea));
        // rr:leaves-play.2: "Return each token and status card on that card to the supply."
        scheme.PlaceTokens("k_threat", 2);
        scheme.PlaceTokens("c_probe", 1);
        var events = new List<GameEvent>();
        Agendas.Happening(world);
        Defeat.Scheme(world, world.Facts, scheme, "test", events);
        events.AddRange(Agendas.Finish(world, world.Facts, runner));

        var expected = portal ? DeckType.EncounterDeck
            : redSkull ? DeckType.SideSchemeDiscardPile : DeckType.EncounterDiscardPile;
        Assert.Equal(expected, scheme.Area.Type);
        Assert.Contains(scheme, world.AreaOf(expected).Cards);
        Assert.DoesNotContain(scheme, world.AreaOf(portal && redSkull
            ? DeckType.SideSchemeDiscardPile : DeckType.RemovedArea).Cards);
        Assert.Equal(portal ? 1 : 0, events.OfType<CardsMoved>().Count(e =>
            e.Verb == "Shuffle_Into" && e.Cards.Any(c => c.Card == scheme.ObjectId)));
        Assert.DoesNotContain(scheme.Tokens, token => token.Value != 0);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
