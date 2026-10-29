using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class YellowjacketPlanIndependentReviewTests
{
    [Fact]
    [Rule("rr:reveal.step.3")]
    [Rule("rr:reveal.step.4")]
    public void CancelledPlanLeavesTheEncounterDeckUntouchedAndIsDiscarded()
    {
        // rr:reveal.step.4: "If the card is a treachery, discard it."
        var world = ThorActualFixture.Board("06001a");
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var found = world.CreateCard("12028", deck);
        var skipped = world.CreateCard("01103", deck);
        var before = deck.Cards.ToArray();
        var plan = world.CreateCard("12029", world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: "cancelWhenRevealed", Affects: plan.ObjectId, Lasts: new Duration(Uses: 1)));
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            Round: 1, Number: 4, Subject: plan.ObjectId, Seat: 0));

        AntManNemesisCardFixture.Finish(world, runner);

        Assert.Equal(before, deck.Cards);
        Assert.Same(deck, found.Area);
        Assert.Same(deck, skipped.Area);
        Assert.Equal(DeckType.EncounterDiscardPile, plan.Area.Type);
        Assert.False(world.IsOver);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
