using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class ThorFamilyFeudIndependentReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData("06001a")]
    [InlineData("06001b")]
    [Rule("rr:in-play-and-out-of-play.1")]
    [Rule("rr:in-play-and-out-of-play.2")]
    public void BothFormsCountForeignControlledAndEnemyCardsWithUnhostedMjolnir(string form)
    {
        // "the faceup side of a player's identity card is in play"; faceup minions are in play.
        var world = ThorActualFixture.Board(form, 2);
        world.CreateCard("06002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 0));
        world.CreateCard("06007", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("06010", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("06028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var fixture = new RedSkullFixture(world, AuthoredCards.Runner());
        var scheme = fixture.Reveal("06027", 1);
        fixture.Finish();
        Assert.Equal(8, scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:in-play-and-out-of-play.8")]
    public void ReenteringTheSamePhysicalSchemeUsesFreshCountAndPrintedStartingThreat()
    {
        // "Cards in a player's hand, deck, and discard pile are out of play."
        var world = ThorActualFixture.Board("06001a");
        var fixture = new RedSkullFixture(world, AuthoredCards.Runner());
        var asgard = world.CreateCard("06007", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var scheme = fixture.Reveal("06027");
        fixture.Finish();
        Assert.Equal(4, scheme.Tokens["k_threat"]);
        World.MoveToTop(scheme, world.AreaOf(DeckType.EncounterDiscardPile));
        World.MoveToTop(asgard, world.Seats[0].Hand);
        World.MoveToTop(scheme, world.AreaOf(DeckType.RevealingArea));
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 2, Subject: scheme.ObjectId, Seat: 0));
        fixture.Finish();
        Assert.Equal(3, scheme.Tokens["k_threat"]);
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:traits.2")]
    public void ActualRevealUsesLiveGrantedTraitsAndBlankingDoesNotErasePrintedTraits()
    {
        // "Traits are not considered to be part of a card's printed text box."
        // Synthetic lasting trait grant isolates the census input; the actual reveal row is unchanged.
        var world = ThorActualFixture.Board("06001a", 2);
        var source = world.Seats[0].IdentityCard;
        var loki = world.CreateCard("06028", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        loki.Exhaust();
        world.Effects.Register(PrintedText.UntilEndOfPhase(world, source, loki));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Traits.Granted + "ASGARD",
            Card: source.ObjectId, Affects: world.Seats[1].IdentityCard.ObjectId,
            Lasts: Duration.UntilEndOf(TimingPoints.EndOfRound)));
        var fixture = new RedSkullFixture(world, AuthoredCards.Runner());
        var scheme = fixture.Reveal("06027", 1);
        fixture.Finish();
        Assert.True(PrintedText.IsBlanked(world, loki));
        Assert.False(loki.Ready);
        Assert.Equal(5, scheme.Tokens["k_threat"]);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:in-play-and-out-of-play.10")]
    [Rule("rr:in-play-and-out-of-play.11")]
    public void SyntheticHostedMjolnirTopologyCountsOnceAndConcealedCopiesDoNotCount()
    {
        // "Facedown cards attached to in-play cards are out of play."
        // Deliberately synthetic topology: Mjolnir is hosted by Lady Sif rather than Thor.
        var world = ThorActualFixture.Board("06001a", 2);
        var ally = world.CreateCard("06002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        world.CreateCard("06009", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), ally.ObjectId, 0));
        foreach (var area in new[] { world.Seats[1].Hand, world.Seats[1].Deck,
            world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1)), world.AreaOf(DeckType.AsideDeck),
            world.AreaOf(DeckType.RemovedArea), world.AreaOf(DeckType.EncounterDeck),
            world.AreaOf(DeckType.EncounterDiscardPile), world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)) })
            world.CreateCard("06007", area);
        foreach (bool faceup in new[] { false, true })
            BeneathCards.Place(world, world.CreateCard("06007", world.Seats[1].Hand), ally, faceup, "review", []);
        var fixture = new RedSkullFixture(world, AuthoredCards.Runner());
        var scheme = fixture.Reveal("06027", 1);
        fixture.Finish();
        Assert.Equal(5, scheme.Tokens["k_threat"]);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
