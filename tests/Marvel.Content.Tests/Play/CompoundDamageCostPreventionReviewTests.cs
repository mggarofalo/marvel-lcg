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

public sealed class CompoundDamageCostPreventionReviewTests
{
    [Theory]
    [InlineData(-1, false)]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(-1, true)]
    [InlineData(0, true)]
    [InlineData(1, true)]
    [Rule("rr:cost.5")]
    [Rule("rr:cost.12")]
    [Rule("rr:initiating-abilities.1")]
    public void FailedCompoundTakingRetainsActualAttemptsWithoutPayingSiblings(int preventAt, bool nested)
    {
        // Synthetic cost composition: costs "must be paid simultaneously" and
        // taking is unpaid "unless all of that damage was taken". Interrupts
        // can initiate "just before that triggering condition becomes true".
        // An earlier actual damage placement is an attempted payment, not
        // evidence that any component of the aborted combined cost was paid.
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        var row = book["cards"]!.AsArray().Single(r => r!["card"]!.GetValue<string>() == "01027")!;
        row["abilities"] = JsonNode.Parse("""
            [{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"seq":[{"exhaust":"this"},{"spend":"Y"},
                {"takeDamage":{"cards":"you","amount":1}},
                {"takeDamage":{"cards":"you","amount":1}}]},
              "effect":{"draw":{"player":"you","count":1}}}]
            """);
        if (nested)
        {
            var costs = row["abilities"]![0]!["cost"]!["seq"]!.AsArray();
            costs[3] = new JsonObject { ["seq"] = new JsonArray(costs[3]!.DeepClone()) };
        }
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        Card? rage = null, barrier = null, payment = null;
        var (game, world) = Playing(board =>
        {
            var upgrades = board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0);
            rage = board.CreateCard("01027", upgrades);
            barrier = board.CreateCard("05017", upgrades);
            barrier.PlaceTokens("c_reflection", 1);
            payment = board.CreateCard("01088", board.Seats[0].Hand);
        }, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        long damage = world.Seats[0].IdentityCard.Damage;
        var action = Assert.Single(game.Pending!.Affordances,
            a => a.Verb == Game.ActionVerb && a.AnchorId == rage!.ObjectId);
        game.Resolve(PaidDecision.Take(action, [payment!.ObjectId]));
        for (int component = 0; component < 2; component++)
        {
            var offer = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == barrier!.ObjectId);
            Assert.True(rage!.Ready);
            Assert.Equal(DeckType.HandsArea, payment.Area.Type);
            if (component == preventAt)
            {
                game.Resolve(Decision.Take(offer.Id));
                Assert.Equal(Question.Element, game.Pending!.Asking);
                game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances).Id));
                break;
            }
            game.Resolve(Decision.Decline);
        }
        bool paid = preventAt < 0;
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(!paid, rage!.Ready);
        Assert.Equal(paid ? DeckType.DiscardPile : DeckType.HandsArea, payment.Area.Type);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(damage + (paid ? 2 : preventAt), world.Seats[0].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
    }
}
