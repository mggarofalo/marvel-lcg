using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MediaCoverageIndependentReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActualEnhancedSpiderSensePaymentCancelsAllCopies(bool cancel)
    {
        var f = Fixture(syntheticTreachery: true);
        var world = f.World;
        var seat = world.Seats[0];
        foreach (var held in seat.Hand.Cards.ToArray()) World.MoveToTop(held, seat.Deck);
        var spiderSense = world.CreateCard("01004", seat.Hand);
        var payment = world.CreateCard("01003", seat.Hand);
        var source = f.Reveal("01188");
        var events = new List<GameEvent>();
        var prompt = Sequence.Work(world, world.Facts, f.Runner, events);
        Assert.NotNull(prompt);
        var option = Assert.Single(prompt.Affordances, option => option.AnchorId == spiderSense.ObjectId);
        Sequence.Answer(world, world.Facts, f.Runner, prompt,
            cancel ? Decision.Take(option.Id, [], [payment.ObjectId]) : Decision.Decline, events);
        events.AddRange(f.Finish());
        Assert.Equal(cancel ? 0 : 2, seat.IdentityCard.Damage);
        Assert.Equal(cancel ? DeckType.DiscardPile : DeckType.HandsArea, spiderSense.Area.Type);
        Assert.Equal(cancel ? DeckType.DiscardPile : DeckType.HandsArea, payment.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.Equal(cancel ? 1 : 0, events.OfType<WhenRevealedCanceled>().Count());
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void ActualRedSkullPromotionDealsOneEncounterPerPlayer()
    {
        var f = Fixture();
        var world = f.World;
        var prior = f.Villain;
        var next = Assert.Single(world.Cards, card => card.FaceId == "04126");
        var dealtBefore = world.Seats.Select(seat => world.AreaOf(
            DeckType.DealtEncounterCardsDeck, PlayArea.Of(seat.Index)).Cards.Count).ToArray();
        var events = new List<GameEvent>();
        // The Red House's damage prohibition is absent in this prepared promotion state.
        foreach (var scheme in world.Cards.Where(card => card.FaceId == "04139"
            && card.Area.Type == DeckType.SideSchemesArea).ToArray())
            Discard.Card(world, scheme, "review fixture", events);
        var receipt = DamagePlacement.Resolve(world, world.Facts,
            new(world.Seats[0].IdentityCard, prior, 999, "review", "Damage"), events);
        events.AddRange(f.Finish());
        Assert.True(receipt.IsComplete);
        Assert.Equal(DeckType.RemovedArea, prior.Area.Type);
        Assert.Same(next, f.Villain);
        foreach (var seat in world.Seats)
            Assert.Equal(dealtBefore[seat.Index] + 1, world.AreaOf(
                DeckType.DealtEncounterCardsDeck, PlayArea.Of(seat.Index)).Cards.Count);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static RedSkullFixture Fixture(bool syntheticTreachery = false)
    {
        var f = RedSkullFixture.Create(players: 2);
        var root = JsonNode.Parse(AuthoredCards.Text)!;
        var cards = root["cards"]!.AsArray();
        cards.Add(JsonNode.Parse("""
        {"card":"02049","abilities":[{"trigger":{"timing":"Constant","subject":"this"},
          "effect":{"grant":{"card":"attachedTo","keyword":"additionalWhenRevealed","amount":1}}}]}
        """));
        if (syntheticTreachery)
        {
            // A synthetic damage body distinguishes both repetitions; cancellation uses the canonical event.
            var source = cards.Single(card => card!["card"]!.GetValue<string>() == "01188")!;
            source["abilities"] = JsonNode.Parse("""
            [{"trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
              "effect":{"dealDamage":{"cards":"you","amount":1}}}]
            """);
        }
        var runner = new AbilityRunner(AbilityCatalog.Parse(root.ToJsonString()));
        f.World.Abilities = runner;
        f.World.CreateCard("02049", f.World.AreaOf(DeckType.UpgradesArea,
            PlayArea.Of(0), f.World.Seats[0].IdentityCard.ObjectId));
        return new(f.World, runner);
    }
}


