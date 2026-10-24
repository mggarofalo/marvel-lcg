using Marvel.Rules.Timing;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CaptainAmericaQuinjetTests
{
    [Theory]
    [InlineData(0, true, true)]
    [InlineData(0, false, false)]
    [InlineData(1, true, true)]
    [InlineData(1, false, false)]
    [Rule("rr:player-phase")]
    public void OptionalCounterIsOfferedOnlyBeforeItsControllersTurnActions(int owner, bool hero, bool accept)
    {
        // "Each player (in player order) takes one turn"; Quinjet responds after "your turn begins".
        Card? quinjet = null;
        var (game, world) = Playing(board =>
            quinjet = board.CreateCard("03019", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(owner), cardOwner: owner)),
            hero: hero, heroes: ["spider_man", "captain_marvel"]);
        if (owner == 1)
        {
            Assert.Equal(Question.TurnOption, game.Pending!.Asking);
            Assert.Equal(0, quinjet!.Tokens.GetValueOrDefault("c_time"));
            game.Resolve(Decision.Decline);
        }
        Assert.Equal(owner, game.Active);
        var response = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == quinjet!.ObjectId);
        Assert.False(response.PlaysCard);
        if (accept) game.Resolve(Decision.Take(response.Id));
        else game.Resolve(Decision.Decline);
        Assert.Equal(accept ? 1 : 0, quinjet!.Tokens.GetValueOrDefault("c_time"));
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.DoesNotContain(game.Pending.Affordances, offer => offer.AnchorId == quinjet.ObjectId && offer.Timing == AbilityType.Response);
    }

    [Theory]
    [InlineData(true, 2)]
    [InlineData(false, 3)]
    public void EntryChoosesOnlyAffordablePrintedAvengerAllyThenDiscardsSource(bool hero, int counters)
    {
        Card? quinjet = null;
        Card? affordable = null;
        Card? expensive = null;
        Card? outsider = null;
        Card? foreign = null;
        var (game, world) = Playing(board =>
        {
            quinjet = board.CreateCard("03019", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            quinjet.PlaceTokens("c_time", counters);
            CardPayment.ReduceNextCardCost(board, quinjet, 0, 2);
            affordable = board.CreateCard("03014", board.Seats[0].Hand);
            expensive = board.CreateCard("03011", board.Seats[0].Hand);
            outsider = board.CreateCard("01002", board.Seats[0].Hand);
            foreign = board.CreateCard("03013", board.Seats[1].Hand);
        }, hero: hero, heroes: ["spider_man", "captain_marvel"]);
        game.Resolve(Decision.Decline);
        var action = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == quinjet!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        var choice = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(affordable!.ObjectId, choice.AnchorId);
        Assert.DoesNotContain(game.Pending.Affordances, offer => offer.AnchorId == expensive!.ObjectId);
        Assert.DoesNotContain(game.Pending.Affordances, offer => offer.AnchorId == outsider!.ObjectId);
        Assert.DoesNotContain(game.Pending.Affordances, offer => offer.AnchorId == foreign!.ObjectId);
        game.Resolve(Decision.Take(choice.Id));
        Assert.Equal(DeckType.AlliesArea, affordable.Area.Type);
        Assert.Equal(0, affordable.Area.PlayArea.Player);
        Assert.Equal(DeckType.DiscardPile, quinjet!.Area.Type);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(2, true)]
    public void NoActionWhenPrintedCostOrUniquenessPreventsEntry(int counters, bool duplicate)
    {
        Card? quinjet = null;
        var (game, _) = Playing(board =>
        {
            foreach (var held in board.Seats[0].Hand.Cards.ToArray())
                World.MoveToTop(held, board.AreaOf(DeckType.RemovedArea));
            quinjet = board.CreateCard("03019", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            quinjet.PlaceTokens("c_time", counters);
            board.CreateCard("03014", board.Seats[0].Hand);
            if (duplicate) board.CreateCard("03014", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        });
        game.Resolve(Decision.Decline);
        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.AnchorId == quinjet!.ObjectId);
        Assert.Equal(DeckType.SupportsArea, quinjet!.Area.Type);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void EntryResponseCompletesBeforeDependentDiscard(bool accept, bool atLimit)
    {
        Card? quinjet = null;
        Card? ally = null;
        Card? departing = null;
        var (game, world) = Playing(board =>
        {
            quinjet = board.CreateCard("03019", board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            quinjet.PlaceTokens("c_time", 2);
            ally = board.CreateCard("03013", board.Seats[0].Hand);
            if (atLimit)
            {
                var allies = board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0);
                departing = board.CreateCard("01002", allies);
                board.CreateCard("03014", allies);
                board.CreateCard("03020", allies);
            }
        });
        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == quinjet!.ObjectId).Id));
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            offer => offer.AnchorId == ally!.ObjectId).Id));
        Assert.Equal(DeckType.AlliesArea, ally!.Area.Type);
        Assert.Equal(DeckType.SupportsArea, quinjet!.Area.Type);
        if (atLimit)
        {
            Assert.Equal(Steps.ChooseAllyForLimit, game.Pending!.Trigger);
            game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances,
                offer => offer.AnchorId == departing!.ObjectId).Id));
            Assert.Equal(DeckType.DiscardPile, departing!.Area.Type);
            Assert.Equal(DeckType.SupportsArea, quinjet.Area.Type);
        }
        var response = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == ally.ObjectId);
        game.Resolve(accept ? Decision.Take(response.Id) : Decision.Decline);
        Assert.Equal(accept ? 1 : 0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.DiscardPile, quinjet.Area.Type);
        Assert.True(game.IsRootPrompt);
    }

    [Fact]
    public void EveryParticipatingTurnBeginsExactlyOnceAcrossTheRoundBoundary()
    {
        Card[] jets = [];
        var (game, world) = Playing(board =>
        {
            jets = [.. Enumerable.Range(0, 2).Select(player => board.CreateCard("03019",
                board.AreaOf(DeckType.SupportsArea, PlayArea.Of(player), cardOwner: player)))];
            var deck = board.AreaOf(DeckType.EncounterDeck);
            foreach (var card in deck.Cards.ToArray()) World.MoveToTop(card, board.AreaOf(DeckType.RemovedArea));
            for (int index = 0; index < 20; index++) board.CreateCard("01098", deck);
        }, heroes: ["spider_man", "captain_marvel"]);
        TakeResponse(game, jets[0]);
        game.Resolve(Decision.Decline);
        TakeResponse(game, jets[1]);
        game.Resolve(Decision.Decline);
        for (int count = 0; game.Round < 2 && count < 80; count++)
        {
            var prompt = Assert.IsType<Prompt>(game.Pending);
            var offer = prompt.Affordances[0];
            game.Resolve(prompt.Cancellable ? Decision.Decline : new Decision(offer.Id,
                offer.Targets is { } targets ? [.. targets.Legal.Take(targets.Min)] : []));
        }
        Assert.Equal(2, game.Round);
        Assert.Equal(1, game.Active);
        Assert.All(jets, jet => Assert.Equal(1, jet.Tokens.GetValueOrDefault("c_time")));
        TakeResponse(game, jets[1]);
        game.Resolve(Decision.Decline);
        TakeResponse(game, jets[0]);
        Assert.All(jets, jet => Assert.Equal(2, jet.Tokens.GetValueOrDefault("c_time")));
        Assert.True(game.IsRootPrompt);
    }

    private static void TakeResponse(Game game, Card quinjet)
    {
        var response = Assert.Single(game.Pending!.Affordances, offer => offer.AnchorId == quinjet.ObjectId);
        Assert.Equal(AbilityType.Response, response.Timing);
        game.Resolve(Decision.Take(response.Id));
        Assert.True(game.IsRootPrompt);
    }

}
