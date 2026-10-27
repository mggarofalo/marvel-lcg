using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DoctorStrangeFinalEventReviewTests
{
    [Theory]
    [InlineData("01004", false)]
    [InlineData("01075", true)]
    [InlineData("09007", false)]
    [Rule("rr:cancel.4")]
    [Rule("rr:surge.1")]
    public void ActualCancellationRetainsItsPrintedSurgeScope(string face, bool replacement)
    {
        // A canceled treachery is "still regarded as revealed" and discarded.
        // Current rr:surge.1 supplies a When Revealed ability, so 01004 and
        // 09007 both cancel it; 01075 independently reveals a replacement.
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var source = CreateSource(world, face);
        var payment = world.CreateCard("01089", world.Seats[1].Hand);
        var original = world.CreateCard("01191", world.AreaOf(DeckType.EncounterDeck));
        var next = world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var identity = world.Seats[1].IdentityCard;
        var events = new List<GameEvent>();
        Deal.EncounterCard(world, original, 1, "identified review deal", events);
        world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCards), 1, 4, Plan: true));
        var prompt = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(1, prompt.Player);
        Assert.Equal(original.ObjectId, world.Agenda.Occurrence!.Subject);
        var offer = Assert.Single(prompt.Affordances, item => item.AnchorId == source.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt,
            Decision.Take(offer.Id, [], [payment.ObjectId]), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.True(identity.Ready);
        Assert.Equal(DeckType.EncounterDiscardPile, original.Area.Type);
        Assert.Equal(replacement ? DeckType.EngagedEnemiesArea : DeckType.EncounterDeck, next.Area.Type);
        Assert.Equal(replacement, next.FaceUp);
        Assert.Equal(replacement ? 2 : 1, events.OfType<CardsFlipped>().Sum(item => item.Cards.Count));
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(face == "01075" ? DeckType.AlliesArea : DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(face != "01075", source.Ready);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.IsOver);
    }

    [Theory]
    [InlineData("01004")]
    [InlineData("01075")]
    [InlineData("09007")]
    [Rule("ruling:7075a85e02f2203e")]
    public void ActualRowsRequireTheExactTriggeringOriginReceipt(string face)
    {
        // The ruling distinguishes first-dealt cards from direct separate-zone
        // reveals; a receipt for a different card cannot qualify this reveal.
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var source = CreateSource(world, face);
        world.CreateCard("01089", world.Seats[1].Hand);
        var card = world.CreateCard("01191", world.AreaOf(DeckType.RevealingArea));
        var occurrence = new Occurrence(64, [Steps.CardRevealed], card.ObjectId, 1);
        var deck = Places.Reference(world.AreaOf(DeckType.EncounterDeck));
        var discard = Places.Reference(world.AreaOf(DeckType.EncounterDiscardPile));
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        AssertOffer(occurrence, false);
        AssertOffer(occurrence with { RevealOrigin = new EncounterRevealOrigin(source.ObjectId, deck) }, false);
        AssertOffer(occurrence with { RevealOrigin = new EncounterRevealOrigin(card.ObjectId, discard) }, false);
        AssertOffer(occurrence with { RevealOrigin = new EncounterRevealOrigin(card.ObjectId, deck) }, true);
        AssertOffer(occurrence with { RevealOrigin = new EncounterRevealOrigin(card.ObjectId, discard, Dealt: true) }, true);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.True(source.Ready);
        Assert.False(world.Agenda.IsBusy);

        void AssertOffer(Occurrence trigger, bool expected) => Assert.Equal(expected,
            runner.Waiting(world, trigger, WindowKind.Interrupt).Any(offer => offer.Card == source.ObjectId));
    }

    [Theory]
    [InlineData("01004", true, true, true, true, true)]
    [InlineData("01004", false, true, true, true, false)]
    [InlineData("01004", true, false, true, true, false)]
    [InlineData("01004", true, true, false, true, false)]
    [InlineData("01075", true, true, true, true, true)]
    [InlineData("01075", false, true, true, true, true)]
    [InlineData("01075", true, false, true, true, true)]
    [InlineData("01075", true, true, false, true, false)]
    [InlineData("01075", true, true, true, false, false)]
    [InlineData("09007", true, true, true, true, true)]
    [InlineData("09007", false, true, true, true, false)]
    [InlineData("09007", true, false, true, true, false)]
    [InlineData("09007", true, true, false, true, false)]
    public void ExactDeckReceiptPreservesPrintedFormKindAndCosts(
        string face, bool hero, bool treachery, bool funded, bool ready, bool expected)
    {
        // Generated printed text: only the two events are Hero Interrupts and
        // treachery-only; Black Widow exhausts and spends a mental resource.
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        if (!hero) world.Seats[1].IdentityCard.TurnTo("01001b");
        var source = CreateSource(world, face);
        if (!ready) source.Exhaust();
        if (funded) world.CreateCard("01089", world.Seats[1].Hand);
        var card = world.CreateCard(treachery ? "01191" : "01101", world.AreaOf(DeckType.RevealingArea));
        var occurrence = new Occurrence(65, [Steps.CardRevealed], card.ObjectId, 1)
        {
            RevealOrigin = new EncounterRevealOrigin(card.ObjectId,
                Places.Reference(world.AreaOf(DeckType.EncounterDeck))),
        };
        Assert.Equal(expected, runner.Waiting(world, occurrence, WindowKind.Interrupt)
            .Any(offer => offer.Card == source.ObjectId));
    }

    private static Card CreateSource(World world, string face) => world.CreateCard(face,
        face == "01075" ? world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1) : world.Seats[1].Hand);
}
