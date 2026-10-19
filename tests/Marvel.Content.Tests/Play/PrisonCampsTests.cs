using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class PrisonCampsTests : CoreCardTestBase
{
    [Theory]
    [Rule("rr:play-put-into-play.4")]
    [InlineData(0, DeckType.PlayerDeck)]
    [InlineData(1, DeckType.PlayerDeck)]
    [InlineData(0, DeckType.DiscardPile)]
    [InlineData(1, DeckType.DiscardPile)]
    public void DefeaterChoosesOwnAllyAndPutsItIntoPlayWithoutPaying(int player, DeckType area)
    {
        // "When a card is put into play, its resource cost is ignored."
        var world = Hero("01001a", 2);
        var runner = Runner();
        world.Abilities = runner;
        var scheme = world.CreateCard("04141", world.AreaOf(DeckType.SideSchemesArea));
        var ally = world.CreateCard("01011", world.AreaOf(area, PlayArea.Of(player), cardOwner: player));
        var other = world.CreateCard("01011", world.Seats[1 - player].Deck);
        var sentinel = world.CreateCard("01088", world.Seats[player].Deck);
        var events = new List<Marvel.Rules.Events.GameEvent>();
        Defeat.Scheme(world, Cards, scheme, "test", events, by: player,
            recordOn: new Occurrence(1, []));
        var choice = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(player, choice.Player);
        var offer = Assert.Single(choice.Affordances);
        Assert.Equal(ally.ObjectId, offer.AnchorId);
        Sequence.Answer(world, Cards, runner, choice, Decision.Take(offer.Id), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(PlayArea.Of(player), ally.Area.PlayArea);
        Assert.Equal(player, ally.Owner);
        Assert.Equal(DeckType.PlayerDeck, other.Area.Type);
        Assert.Same(world.Seats[player].Deck, sentinel.Area);
        Assert.Equal(DeckType.EncounterDiscardPile, scheme.Area.Type);
        Assert.Contains(events.OfType<Marvel.Rules.Events.CardsMoved>(), item =>
            item.Verb == "Put_Into_Play" && item.Cards.Any(card => card.Card == ally.ObjectId));
    }

    [Fact]
    public void UniqueCopyInPlayCannotBeSelectedAndStaysInTheDiscardPile()
    {
        var world = Hero("01001a");
        var runner = Runner();
        world.Abilities = runner;
        var scheme = world.CreateCard("04141", world.AreaOf(DeckType.SideSchemesArea));
        world.CreateCard("01011", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var duplicate = world.CreateCard("01011", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01088", world.Seats[0].Deck);
        Defeat.Scheme(world, Cards, scheme, "test", [], by: 0, recordOn: new Occurrence(1, []));
        Assert.Null(Sequence.Work(world, Cards, runner, []));
        Assert.Equal(DeckType.DiscardPile, duplicate.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, scheme.Area.Type);
    }

    [Fact]
    [Rule("rr:ally-limit")]
    public void AllyLimitChoiceCompletesBeforeTheSchemeLeavesPlay()
    {
        // "They must immediately choose and discard from play ally cards they control"
        // until the limit is met, before entering-play abilities.
        var world = Hero("01001a");
        var runner = Runner();
        world.Abilities = runner;
        var scheme = world.CreateCard("04141", world.AreaOf(DeckType.SideSchemesArea));
        var allies = world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
        var departing = world.CreateCard("01002", allies);
        world.CreateCard("01041", allies);
        world.CreateCard("01083", allies);
        var entering = world.CreateCard("01011", world.Seats[0].Deck);
        world.CreateCard("01088", world.Seats[0].Deck);
        Defeat.Scheme(world, Cards, scheme, "test", [], by: 0, recordOn: new Occurrence(1, []));
        var choice = Sequence.Work(world, Cards, runner, [])!;
        Sequence.Answer(world, Cards, runner, choice,
            Decision.Take(Assert.Single(choice.Affordances).Id), []);
        var limit = Sequence.Work(world, Cards, runner, [])!;
        Assert.Equal(Steps.ChooseAllyForLimit, limit.Trigger);
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        Assert.Equal(4, limit.Affordances.Count);
        Sequence.Answer(world, Cards, runner, limit,
            Decision.Take(Assert.Single(limit.Affordances, item => item.AnchorId == departing.ObjectId).Id), []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.Equal(DeckType.DiscardPile, departing.Area.Type);
        Assert.Equal(DeckType.AlliesArea, entering.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, scheme.Area.Type);
    }

    [Fact]
    public void EnterPlayResponseResolvesBeforeTheDefeatedSchemeLeaves()
    {
        var world = Hero("01001a", 2);
        var runner = Runner();
        world.Abilities = runner;
        var scheme = world.CreateCard("04141", world.AreaOf(DeckType.SideSchemesArea));
        var ally = world.CreateCard("01067", world.Seats[1].Deck);
        foreach (var seat in world.Seats)
        {
            world.CreateCard("01088", seat.Deck);
            world.CreateCard("01089", seat.Deck);
            world.CreateCard("01090", seat.Deck);
        }
        Defeat.Scheme(world, Cards, scheme, "test", [], by: 1, recordOn: new Occurrence(1, []));
        var choice = Sequence.Work(world, Cards, runner, [])!;
        Sequence.Answer(world, Cards, runner, choice,
            Decision.Take(Assert.Single(choice.Affordances).Id), []);
        var response = Sequence.Work(world, Cards, runner, [])!;
        Assert.Equal(DeckType.SideSchemesArea, scheme.Area.Type);
        Assert.Equal(1, response.Player);
        var offer = Assert.Single(response.Affordances, item => item.AnchorId == ally.ObjectId);
        Sequence.Answer(world, Cards, runner, response, Decision.Take(offer.Id), []);
        Sequence.Finish(world, Cards, runner, []);
        Assert.All(world.Seats, seat => Assert.Single(seat.Hand.Cards));
        // Maria Hill draws before Prison Camps' final shuffle, so the known top
        // card is still first; the search itself has not reordered the deck.
        Assert.All(world.Seats, seat => Assert.Equal("01090", seat.Hand.Cards[0].FaceId));
        Assert.Equal(DeckType.EncounterDiscardPile, scheme.Area.Type);
    }

    internal static AbilityRunner Runner() => AuthoredCards.Runner();
}

