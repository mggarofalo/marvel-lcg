using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic ally isolates the entry interrupt without admitting Hank Pym.
public sealed class EntryResourceExcessReviewTests
{
    [Fact]
    [Rule("rr:cost.4.1")]
    [Rule("rr:play-put-into-play.3")]
    public void PaidPutUsesItsOwnExcessInPendingEntryInterruptAcrossOwners()
    {
        // Excess resources were not paid for the cost; putting a card into play is not playing it.
        var root = JsonNode.Parse(AuthoredCards.Text)!;
        var cards = root["cards"]!.AsArray();
        var previous = cards.SingleOrDefault(row => (string?)row!["card"] == "06012");
        if (previous is not null) cards.Remove(previous);
        cards.Add(JsonNode.Parse("""
            {"card":"06012","abilities":[{
              "trigger":{"event":"WhenCardEntersPlay","timing":"Interrupt","subject":"this"},
              "effect":{"placeCounters":{"card":"this","counter":"excess",
                "count":{"min":[4,{"entryResourceExcess":"this"}]}}}}]}
            """));
        var runner = new AbilityRunner(AbilityCatalog.Parse(root.ToJsonString()));
        Card? source = null;
        Card? ally = null;
        Card? eventResource = null;
        Card? first = null;
        Card? second = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("01071", board.Seats[0].Hand);
            ally = board.CreateCard("06012", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(1), cardOwner: 1));
            eventResource = board.CreateCard("01003", board.Seats[0].Hand);
            first = board.CreateCard("06022", board.Seats[0].Hand);
            second = board.CreateCard("01088", board.Seats[0].Hand);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var play = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source!.ObjectId);
        int[] eventPayment = [eventResource!.ObjectId];
        game.Resolve(new Decision(play.Id, [], eventPayment,
            Allocations: ResourcePayment.Allocate(Assert.Single(play.Costs!), eventPayment)));
        var choice = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == ally!.ObjectId);
        int[] allyPayment = [first!.ObjectId, second!.ObjectId];
        game.Resolve(new Decision(choice.Id, [], allyPayment,
            Allocations: ResourcePayment.Allocate(Assert.Single(choice.CostOptions), allyPayment)));
        var offer = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == ally!.ObjectId);
        Assert.Equal(DeckType.DiscardPile, ally!.Area.Type);
        Assert.Equal(1, ally.Owner);
        game.Resolve(Decision.Take(offer.Id));
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(1, ally.Tokens.GetValueOrDefault("c_excess"));
        Assert.DoesNotContain(world.Agenda.Outstanding, item => item.What == Steps.CardEntersPlay);
    }
}
