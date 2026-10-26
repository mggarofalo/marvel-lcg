using System.Text.Json;
using static Marvel.Content.Tests.Play.SpellMasteryFixture;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Nonpublic real-card composition; no production pack admission.</summary>
public sealed class SpellMasteryTests
{
    [Fact]
    [Rule("rr:player-deck.1")]
    [Rule("rr:player-deck.2")]
    public void ActualWindsRecyclesBothDecksButOnlyPlayerDeckDealsAnEncounterCard()
    {
        // A player deck reset "immediately deals themself one facedown encounter card"
        // and drawing "continues ... up to the specified number". The mc08 insert
        // separately says Invocation recycling has "no penalty".
        var (world, game, deck, _) = Board(1, last: true);
        var seat = world.Seats[1];
        var discard = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1);
        foreach (var card in seat.Deck.Cards.Skip(1).ToArray()) World.MoveToTop(card, discard);
        var encounter = world.AreaOf(DeckType.EncounterDeck);
        world.CreateCard("01148", encounter);
        var penalty = world.CreateCard("01148", encounter);
        int hand = seat.Hand.Cards.Count;
        int recycled = discard.Cards.Count;
        var top = deck.PublicTop!;

        var result = game.Resolve(Decision.Take(Mastery(game, seat.IdentityCard).Id, [top.ObjectId], []));

        Assert.Equal(hand + 3, seat.Hand.Cards.Count);
        Assert.Equal(recycled - 2, seat.Deck.Cards.Count);
        Assert.Empty(discard.Cards);
        Assert.Equal(DeckType.DealtEncounterCardsDeck, penalty.Area.Type);
        Assert.Equal(PlayArea.Of(1), penalty.Area.PlayArea);
        Assert.False(penalty.FaceUp);
        Assert.Single(encounter.Cards);
        Assert.Equal(5, deck.Deck.Cards.Count);
        Assert.Empty(deck.Discard.Cards);
        Assert.Single(deck.Deck.Cards, card => card.FaceUp);
        Assert.Equal(2, result.Events.OfType<AreaReordered>().Count());
        Assert.False(seat.IdentityCard.Ready);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Rule("rr:special")]
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(0, true)]
    public void ActualOfferedWindsExhaustsDrawsThreeAndDiscardsOrRecycles(int player, bool last)
    {
        // rr:special: "only ... explicit instruction of another card ability".
        // 09001a pays/exhausts; actual 09036 draws three then enters its own discard.
        var (world, game, deck, runner) = Board(player, last);
        var top = deck.PublicTop!;
        var hero = world.Seats[player].IdentityCard;
        int hand = world.Seats[player].Hand.Cards.Count;
        var offer = Mastery(game, hero);
        Assert.Equal("09036", top.FaceId);
        Assert.Equal([top.ObjectId], offer.Targets!.Legal);
        Assert.Equal("0", Assert.Single(offer.CostOptions).Cost);
        Assert.Contains("Exhaust Doctor Strange", offer.CostDescription);
        var result = game.Resolve(Decision.Take(offer.Id, [top.ObjectId], []));
        Assert.False(hero.Ready);
        Assert.Equal(hand + 3, world.Seats[player].Hand.Cards.Count);
        Assert.Equal(last ? 5 : 4, deck.Deck.Cards.Count);
        Assert.Equal(last ? 0 : 1, deck.Discard.Cards.Count);
        Assert.Single(deck.Deck.Cards, card => card.FaceUp);
        Assert.Equal(last ? 1 : 0, result.Events.OfType<AreaReordered>().Count());
        Assert.Empty(world.Agenda.Outstanding);
        Assert.DoesNotContain(result.Events, happened => happened.Verb == CardPlay.Verb);
        Assert.DoesNotContain(runner.Actions(world, player), pending => pending.Card == hero.ObjectId);
    }

    [Theory]
    [InlineData("deeper")]
    [InlineData("foreign")]
    [InlineData("stale")]
    [InlineData("exhausted")]
    [InlineData("form")]
    public void RejectionPreservesPromptCardsRngAndPendingWork(string invalid)
    {
        var (world, game, deck, runner) = Board(1);
        var hero = world.Seats[1].IdentityCard;
        var offer = Mastery(game, hero);
        var top = deck.PublicTop!;
        int target = top.ObjectId;
        switch (invalid)
        {
            case "deeper": target = deck.Deck.Cards[0].ObjectId; break;
            case "foreign": target = world.Seats[0].Hand.Cards[0].ObjectId; break;
            case "stale": deck.DiscardTop(top, "test", []); break;
            case "exhausted": hero.Exhaust(); break;
            case "form": hero.TurnTo("09001b"); break;
        }
        var prompt = game.Pending;
        string before = Snapshot(world);
        if (invalid is "deeper" or "foreign")
            Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(offer.Id, [target], [])));
        else
            Assert.Throws<RulesNotImplementedException>(() => game.Resolve(Decision.Take(offer.Id, [target], [])));
        Assert.Equal(before, Snapshot(world));
        Assert.Same(prompt, game.Pending);
        if (invalid is "form" or "exhausted")
            Assert.DoesNotContain(runner.Actions(world, 1), pending => pending.Card == hero.ObjectId);
    }

    [Fact]
    public void QueryPurityIncludesSubsequentActualCompletionAgainstAnUnprobedControl()
    {
        var (world, game, deck, runner) = Board(1);
        var (control, controlGame, controlDeck, _) = Board(1);
        string before = Snapshot(world);
        for (int i = 0; i < 5; i++)
            foreach (var pending in runner.Actions(world, 1)) _ = runner.Describe(world, pending);
        Assert.Equal(before, Snapshot(world));
        var result = game.Resolve(Decision.Take(Mastery(game, world.Seats[1].IdentityCard).Id, [deck.PublicTop!.ObjectId], []));
        var expected = controlGame.Resolve(Decision.Take(Mastery(controlGame, control.Seats[1].IdentityCard).Id, [controlDeck.PublicTop!.ObjectId], []));
        Assert.Equal(Snapshot(control), Snapshot(world));
        Assert.Equal(JsonSerializer.Serialize(expected.Events), JsonSerializer.Serialize(result.Events));
    }

}
