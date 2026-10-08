using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Ability-initiated Core reveals disclose information at the transition.</summary>
public sealed class CoreAbilityRevealDisclosureTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:reveal.step.1")]
    [Rule("rr:cancel.4")]
    [Fact]
    public void BlackWidowReplacementRevealPublishesItsFaceAndInformationExactlyOnce()
    {
        // "Turn the encounter card faceup." Black Widow cancels the first
        // card and reveals a new one; that new information precedes its own
        // interrupt window even though the ability already moved it.
        var world = new World(Cards, players: 1);
        var seat = world.CreateSeat("T'Challa");
        seat.IdentityCard = world.CreateCard("01040a,01040b", seat.Hero);
        seat.IdentityCard.TurnTo("01040a");
        world.CreateCard("01087", seat.Deck);
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var scheme = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        var widow = world.CreateCard("01075", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        var payment = world.CreateCard("01089", seat.Hand);
        var original = world.CreateCard("01186",
            world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)));
        var concealed = world.CreateCard("01110", world.AreaOf(DeckType.EncounterDeck));
        var replacement = world.CreateCard("01101", world.AreaOf(DeckType.EncounterDeck));
        var runner = AuthoredCards.Runner();
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(
            Steps.RevealEncounterCard, 1, 4, Subject: original.ObjectId, Seat: 0));
        var first = Sequence.Work(world, Cards, runner, events)!;
        Assert.Single(new Resolution(world, first, events).Information,
            signal => signal.Kind == InformationKind.Reveal);
        events.Clear();

        var interrupt = Assert.Single(first.Affordances, offer => offer.AnchorId == widow.ObjectId);
        Assert.Equal("Cancel the revealed encounter card's effects and discard it, then reveal the next encounter card. "
            + "The replacement card's effects remain unresolved.", interrupt.Description);
        Assert.Equal("Exhaust Black Widow", interrupt.CostDescription);
        Assert.DoesNotContain(Cards.Title(replacement.FaceId), interrupt.Description);
        Assert.DoesNotContain(Cards.Title(concealed.FaceId), interrupt.Description);
        Sequence.Answer(world, Cards, runner, first,
            Decision.Take(interrupt.Id, [], [payment.ObjectId]), events);

        Assert.True(replacement.FaceUp);
        Assert.Equal(DeckType.RevealingArea, replacement.Area.Type);
        Assert.False(concealed.FaceUp);
        Assert.Equal(DeckType.EncounterDiscardPile, original.Area.Type);
        Assert.Equal([replacement.ObjectId], Assert.Single(events.OfType<CardsFlipped>()).Cards);
        Assert.Single(new Resolution(world, null, events).Information,
            signal => signal.Kind == InformationKind.Reveal);
        events.Clear();

        Assert.Null(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(DeckType.EngagedEnemiesArea, replacement.Area.Type);
        Assert.False(scheme.Tokens.ContainsKey("k_threat"));
        Assert.DoesNotContain(events, gameEvent => gameEvent is CardsFlipped);
        Assert.DoesNotContain(new Resolution(world, null, events).Information,
            signal => signal.Kind == InformationKind.Reveal);
        events.Clear();

        Assert.Null(Sequence.Work(world, Cards, runner, events));
        Assert.Empty(events);
        Assert.Empty(new Resolution(world, null, events).Information);
        Assert.False(concealed.FaceUp);
        Assert.DoesNotContain(events, gameEvent => gameEvent is CardsFlipped);
    }
}
