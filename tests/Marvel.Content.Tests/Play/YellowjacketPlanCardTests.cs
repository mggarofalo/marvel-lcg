using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual rows on a prepared encounter board; no public Ant-Man setup admission.
public sealed class YellowjacketPlanCardTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [Rule("rr:reveal.3")]
    [Rule("rr:encounter-deck.2")]
    public void RevealsNemesisMinionToTheRevealingPlayer(int player)
    {
        // Revealed minions engage the revealing player; discard until the criteria are met.
        var world = ThorActualFixture.Board("06001a", 2);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var below = world.CreateCard("12028", deck);
        var yellowjacket = world.CreateCard("12027", deck);
        var skipped = world.CreateCard("01103", deck);
        var plan = world.CreateCard("12029",
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(player)));
        Resolve(world, plan, player);
        Assert.Equal(DeckType.EngagedEnemiesArea, yellowjacket.Area.Type);
        Assert.Equal(PlayArea.Of(player), yellowjacket.Area.PlayArea);
        Assert.Equal(DeckType.EncounterDiscardPile, skipped.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, plan.Area.Type);
        Assert.Same(below, deck.Cards[^1]);
        Assert.False(world.IsOver);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:reveal.step.4")]
    [Rule("rr:uses-x-type")]
    public void RevealsTheFoundSizeIncreaseCopyAndResolvesItsPrintedAttachment(bool yellowjacketPresent)
    {
        // Card-text reveals use "this same resolution procedure"; Uses places its counters on entry.
        var world = ThorActualFixture.Board("06001a");
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var host = yellowjacketPresent ? world.CreateCard("12027",
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))) : villain;
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var other = world.CreateCard("12028", deck);
        var found = world.CreateCard("12028", deck);
        var skipped = world.CreateCard("01103", deck);
        var plan = world.CreateCard("12029", world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)));
        Resolve(world, plan, 0);
        Assert.Equal(host.ObjectId, found.Area.Host);
        Assert.Equal(3, found.Tokens.GetValueOrDefault("c_size"));
        Assert.Same(deck, other.Area);
        Assert.Same(other, deck.Cards[^1]);
        Assert.Equal(DeckType.EncounterDiscardPile, skipped.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, plan.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:encounter-deck.1")]
    [Rule("rr:encounter-deck.2")]
    public void EmptyingTheDeckResetsOnceAndStillRevealsALastMatchingCard(bool matchingLast)
    {
        // "Do not continue the discard effect with the newly shuffled encounter deck."
        var world = ThorActualFixture.Board("06001a");
        var deck = world.AreaOf(DeckType.EncounterDeck);
        foreach (var card in deck.Cards.ToArray()) World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        var last = world.CreateCard(matchingLast ? "12027" : "01098", deck);
        var first = world.CreateCard("01103", deck);
        var plan = world.CreateCard("12029", world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)));
        var events = Resolve(world, plan, 0);
        Assert.Equal(matchingLast ? DeckType.EngagedEnemiesArea : DeckType.EncounterDeck, last.Area.Type);
        Assert.Equal(DeckType.EncounterDeck, first.Area.Type);
        Assert.Equal(matchingLast ? 1 : 2, deck.Cards.Count);
        Assert.Single(events, item => item.Verb == "Reset");
        Assert.Equal(1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens[EncounterDeck.AccelerationToken]);
        Assert.Equal(DeckType.EncounterDiscardPile, plan.Area.Type);
        Assert.False(world.IsOver);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static List<GameEvent> Resolve(World world, Card card, int player)
    {
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            Round: 1, Number: 4, Subject: card.ObjectId, Seat: player));
        return AntManNemesisCardFixture.Finish(world, runner);
    }
}
