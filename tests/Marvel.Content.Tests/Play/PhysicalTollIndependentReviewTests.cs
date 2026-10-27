using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PhysicalTollIndependentReviewTests
{
    // Synthetic event programs isolate reservation and completion; they do not
    // author these printed cards or admit an expansion product.
    private static AbilityRunner Runner() => new(AbilityCatalog.Parse(
        """
        {"cards":[{"card":"05003","abilities":[{
          "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this","form":"hero"},
          "effect":{"draw":{"player":"you","count":1}}
        }]},{"card":"05005","abilities":[{
          "trigger":{"event":"WhenEventPlayInitiated","timing":"Interrupt","subject":"you","form":"hero"},
          "effect":{"draw":{"player":"you","count":1}}
        }]},{"card":"05010","abilities":[{
          "trigger":{"event":"WhenEventPlayInitiated","timing":"ForcedInterrupt","subject":"you","form":"hero"},
          "effect":{"cancelOccurrence":"trigger"}
        }]},{"card":"05011","abilities":[{
          "trigger":{"event":"WhenCardPlayed","timing":"Response","subject":"you","form":"hero"},
          "cost":{"exhaust":"this"},"effect":{"draw":{"player":"you","count":1}}
        }]}]}
        """));

    [Fact]
    [Rule("rr:initiating-abilities.step.6")]
    public void NestedPlayedEventCannotConsumeTheEnclosingEventsReservation()
    {
        // "The card commences being played" follows payment: the nested event
        // begins after the enclosing event has consumed the one-use increase.
        var runner = Runner();
        Card source = null!;
        Card outer = null!;
        Card inner = null!;
        int[] paying = [];
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("09027", board.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0)));
            NextEventCost.Register(board, source, 0, 3);
            outer = board.CreateCard("05003", board.Seats[0].Hand);
            inner = board.CreateCard("05005", board.Seats[0].Hand);
            paying = Enumerable.Range(0, 3).Select(_ => board.CreateCard("01088", board.Seats[0].Hand).ObjectId).ToArray();
        }, hero: true, abilities: runner);
        game.Resolve(PaidDecision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == outer.ObjectId), paying));
        Assert.Equal(DeckType.RevealingArea, outer.Area.Type);
        Assert.Equal(DeckType.ObligationsArea, source.Area.Type);
        Assert.Equal(0, CardPayment.CostOf(world, world.Facts, world.Seats[0], inner).Amount);
        Assert.Single(world.Effects.Registered, effect => effect.Card == source.ObjectId);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == inner.ObjectId).Id));
        Assert.Equal(DeckType.DiscardPile, inner.Area.Type);
        Assert.Equal(DeckType.DiscardPile, outer.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Card == source.ObjectId);
        Assert.True(game.IsRootPrompt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:event.2")]
    public void ObligationCompletionPrecedesCardPlayedResponseEvenAfterForcedCancellation(bool cancel)
    {
        // "the card is still considered to have been played, and its costs remain paid."
        var runner = Runner();
        Card source = null!;
        Card outer = null!;
        Card response = null!;
        int[] paying = [];
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("09027", board.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0)));
            NextEventCost.Register(board, source, 0, 3);
            outer = board.CreateCard("05003", board.Seats[0].Hand);
            response = board.CreateCard("05011", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            if (cancel) board.CreateCard("05010", response.Area);
            paying = Enumerable.Range(0, 3).Select(_ => board.CreateCard("01088", board.Seats[0].Hand).ObjectId).ToArray();
        }, hero: true, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        game.Resolve(PaidDecision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == outer.ObjectId), paying));
        Assert.Equal(DeckType.DiscardPile, outer.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.Equal(held - 4 + (cancel ? 0 : 1), world.Seats[0].Hand.Cards.Count);
        Assert.All(paying, id => Assert.Equal(DeckType.DiscardPile, world.Cards[id].Area.Type));
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Card == source.ObjectId);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == response.ObjectId).Id));
        Assert.Equal(held - 3 + (cancel ? 0 : 1), world.Seats[0].Hand.Cards.Count);
        Assert.True(game.IsRootPrompt);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReconstructedEffectDataPreservesFutureCostAndCompletion(bool reserved)
    {
        // This is an effect-data round trip on independently reconstructed
        // nonpublic boards, not public save/session admission or journal replay.
        var left = PhysicalTollFixture.Reveal();
        var right = PhysicalTollFixture.Reveal();
        var card = left.World.Seats[1].Hand.Cards.Single(item => item.FaceId == "09005");
        if (reserved)
        {
            foreach (var board in new[] { left, right })
            {
                var played = board.World.Cards[card.ObjectId];
                var cost = CardPayment.CostOf(board.World, board.World.Facts, board.World.Seats[1], played);
                NextEventCost.Reserve(board.World, played, cost);
                CardPayment.UseCostModifiers(board.World, cost);
            }
        }
        string data = JsonSerializer.Serialize(right.World.Effects.Registered);
        foreach (var effect in right.World.Effects.Registered) Assert.True(right.World.Effects.Use(effect));
        Assert.Empty(right.World.Effects.Registered);
        foreach (var effect in JsonSerializer.Deserialize<ContinuousEffect[]>(data)!) right.World.Effects.Register(effect);
        Assert.Equal(data, JsonSerializer.Serialize(right.World.Effects.Registered));
        Assert.Equal(reserved ? 1 : 4,
            CardPayment.CostOf(right.World, right.World.Facts, right.World.Seats[1], right.World.Cards[card.ObjectId]).Amount);
        var leftEvents = new List<Marvel.Rules.Events.GameEvent>();
        var rightEvents = new List<Marvel.Rules.Events.GameEvent>();
        if (reserved)
        {
            NextEventCost.Complete(left.World, card, leftEvents);
            NextEventCost.Complete(right.World, right.World.Cards[card.ObjectId], rightEvents);
        }
        else
        {
            int[] paying = left.World.Seats[1].Hand.Cards.Where(item => item.FaceId == "01088").Take(2)
                .Select(item => item.ObjectId).ToArray();
            leftEvents.AddRange(PhysicalTollFixture.Play(left.World, left.Runner, card, paying));
            rightEvents.AddRange(PhysicalTollFixture.Play(right.World, right.Runner, right.World.Cards[card.ObjectId], paying));
        }
        Assert.Equal(DeckType.EncounterDiscardPile, right.Source.Area.Type);
        Assert.Equal(JsonSerializer.Serialize(leftEvents), JsonSerializer.Serialize(rightEvents));
        Assert.Equal(SpellMasteryFixture.Snapshot(left.World), SpellMasteryFixture.Snapshot(right.World));
        Assert.Equal(JsonSerializer.Serialize(left.World.Effects.Registered), JsonSerializer.Serialize(right.World.Effects.Registered));
    }
}
