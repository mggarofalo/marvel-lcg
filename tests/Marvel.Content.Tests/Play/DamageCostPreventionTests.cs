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

public sealed class DamageCostPreventionTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:initiating-abilities.1")]
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:cost.12")]
    public void ActualFocusedRageOffersActualEnergyBarrierBeforePayingOrDrawing(bool prevent)
    {
        // Interrupts may initiate "just before that triggering condition becomes true".
        // Step 5 aborts "without paying any costs" when payment fails. Taking damage
        // requires "all of that damage was taken". Vendored FAQ 01027 confirms that
        // Energy Barrier prevention leaves Focused Rage's draw unpaid (2020-08-28).
        Card? rage = null, barrier = null;
        var (game, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TurnTo("01019a");
            rage = Upgrade(board, "01027");
            barrier = Upgrade(board, "05017");
            barrier.PlaceTokens("c_reflection", 1);
        }, heroes: ["she_hulk"]);
        int held = world.Seats[0].Hand.Cards.Count;
        long damage = world.Seats[0].IdentityCard.Damage;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        long villainDamage = villain.Damage;
        game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances,
            a => a.Verb == Game.ActionVerb && a.AnchorId == rage!.ObjectId).Id));
        var opportunity = game.Pending!;
        var offered = Assert.Single(opportunity.Affordances, a => a.AnchorId == barrier!.ObjectId);
        Assert.True(rage!.Ready);
        Assert.Equal(damage, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        var baseline = (world.Digest().Canonical(), world.Random.Generator.WordsConsumed,
            world.Agenda.Count, world.Agenda.Occurrence!.DirectDamage!.Remaining);
        Assert.Throws<ArgumentException>(() => game.Resolve(Decision.Take(int.MaxValue)));
        Assert.Same(opportunity, game.Pending);
        Assert.Equal(baseline, (world.Digest().Canonical(), world.Random.Generator.WordsConsumed,
            world.Agenda.Count, world.Agenda.Occurrence!.DirectDamage!.Remaining));
        game.Resolve(prevent ? Decision.Take(offered.Id) : Decision.Decline);
        if (prevent)
        {
            Assert.Equal(Question.Element, game.Pending!.Asking);
            Assert.True(rage.Ready);
            Assert.Equal(damage, world.Seats[0].IdentityCard.Damage);
            Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
            game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances).Id));
        }
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(damage + (prevent ? 0 : 1), world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held + (prevent ? 0 : 1), world.Seats[0].Hand.Cards.Count);
        Assert.Equal(prevent, rage.Ready);
        Assert.Equal(villainDamage + (prevent ? 1 : 0), villain.Damage);
        Assert.Equal(prevent ? DeckType.DiscardPile : DeckType.UpgradesArea, barrier!.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("takeDamage", 1, true, false)]
    [InlineData("takeDamage", 2, true, false)]
    [InlineData("takeDamage", 2, false, false)]
    [InlineData("dealDamage", 1, true, false)]
    [InlineData("dealDamage", 2, true, false)]
    [InlineData("takeDamage", 2, true, true)]
    [InlineData("takeDamage", 2, false, true)]
    [Rule("rr:cost.11")]
    [Rule("rr:cost.12")]
    [Rule("rr:cost.5")]
    public void PreventionDistinguishesFullAndPartialTakingFromDealing(string kind, int amount, bool prevent, bool nested)
    {
        // Dealing is paid "even if some or all of that damage is prevented";
        // taking is paid only when "all of that damage was taken". Sibling costs
        // "must be paid simultaneously", so failed taking cannot exhaust or spend.
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        var row = book["cards"]!.AsArray().Single(r => r!["card"]!.GetValue<string>() == "01027")!;
        row["abilities"] = JsonNode.Parse($$$$"""
            [{"trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"seq":[{"exhaust":"this"},{"spend":"Y"},{"{{{{kind}}}}":{"cards":"you","amount":{{{{amount}}}}}}]},
              "effect":{"placeCounters":{"card":"this","counter":"test","count":1}}}]
            """);
        if (nested)
        {
            var costs = row["abilities"]![0]!["cost"]!["seq"]!.AsArray();
            costs[2] = new JsonObject { ["seq"] = new JsonArray(costs[2]!.DeepClone()) };
        }
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        Card? source = null, barrier = null, payment = null;
        var (game, world) = Playing(board =>
        {
            source = Upgrade(board, "01027");
            barrier = Upgrade(board, "05017");
            barrier.PlaceTokens("c_reflection", 1);
            payment = board.CreateCard("01088", board.Seats[0].Hand);
        }, abilities: runner);
        long damage = world.Seats[0].IdentityCard.Damage;
        var action = Assert.Single(game.Pending!.Affordances,
            a => a.Verb == Game.ActionVerb && a.AnchorId == source!.ObjectId);
        game.Resolve(PaidDecision.Take(action, [payment!.ObjectId]));
        var offer = Assert.Single(game.Pending!.Affordances, a => a.AnchorId == barrier!.ObjectId);
        game.Resolve(prevent ? Decision.Take(offer.Id) : Decision.Decline);
        if (prevent) game.Resolve(Decision.Take(Assert.Single(game.Pending!.Affordances).Id));
        DeclineOtherResponses(game, barrier!);
        bool paid = kind == "dealDamage" || !prevent;
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Equal(!paid, source!.Ready);
        Assert.Equal(paid ? 1 : 0, source.Tokens.GetValueOrDefault("c_test"));
        Assert.Equal(paid ? DeckType.DiscardPile : world.Seats[0].Hand.Type, payment.Area.Type);
        Assert.Equal(damage + amount - (prevent ? 1 : 0), world.Seats[0].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    private static Card Upgrade(World world, string code) => world.CreateCard(code,
        world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));

    private static void DeclineOtherResponses(Game game, Card barrier)
    {
        for (int i = 0; i < 5 && game.Pending!.Asking == Question.Opportunity; i++)
        {
            Assert.DoesNotContain(game.Pending.Affordances, a => a.AnchorId == barrier.ObjectId);
            game.Resolve(Decision.Decline);
        }
    }
}
