using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>The rule-neutral physical counters represented by typed pools.</summary>
public sealed class AllPurposeCounterTests
{
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:initiating-abilities.step.3")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void ExactCounterCostCanSpendSeveralCountersFromYourIdentity()
    {
        // Step 3 determines both the costs and the player's ability to pay;
        // step 5 says "Pay the cost(s)." The exact two-counter payment is one
        // state change before the damage effect begins.
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard(
                AuthoredCards.AuntMay,
                board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            board.Seats[0].IdentityCard.PlaceTokens("c_charge", 3);
        }, Runner(
            AuthoredCards.AuntMay,
            """{ "removeCounters": { "card": "you", "counter": "charge", "count": 2 } }""",
            """{ "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }"""));

        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens["c_charge"]);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:initiating-abilities.step.3")]
    [Fact]
    public void ExactCounterCostIsNotOfferedWhenTheFullCountCannotBePaid()
    {
        // Step 3 determines "the cost (or costs) ... and the player's ability
        // to pay them." One counter cannot pay an exact cost of two, so this
        // action never becomes an affordance and no partial payment occurs.
        Card? source = null;
        var (game, _) = Playing(board =>
        {
            source = board.CreateCard(
                AuthoredCards.AuntMay,
                board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            source.PlaceTokens("c_charge", 1);
        }, Runner(
            AuthoredCards.AuntMay,
            """{ "removeCounters": { "card": "this", "counter": "charge", "count": 2 } }""",
            """{ "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }"""));

        Assert.DoesNotContain(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        Assert.Equal(1, source!.Tokens["c_charge"]);
    }

    [Rule("rr:cost.5")]
    [Fact]
    public void SimultaneousCounterCostsArePricedTogether()
    {
        // "If multiple costs for a single card or ability require payment,
        // those costs must be paid simultaneously." Two counters cannot pay
        // a one-plus-two cost, even though either component is payable alone.
        Card? source = null;
        var (game, _) = Playing(board =>
        {
            source = board.CreateCard(
                AuthoredCards.AuntMay,
                board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            source.PlaceTokens("c_charge", 2);
        }, Runner(
            AuthoredCards.AuntMay,
            """
            { "seq": [
              { "removeCounters": { "card": "this", "counter": "charge", "count": 1 } },
              { "removeCounters": { "card": "this", "counter": "charge", "count": 2 } }
            ] }
            """,
            """{ "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }"""));

        Assert.DoesNotContain(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        Assert.Equal(2, source!.Tokens["c_charge"]);
    }

    [Rule("rr:cost.5")]
    [Fact]
    public void NestedSimultaneousCounterCostsArePricedTogether()
    {
        // "If multiple costs for a single card or ability require payment,
        // those costs must be paid simultaneously." Structural nesting does
        // not divide one cost into separate payments that may partially land.
        Card? source = null;
        var (game, _) = Playing(board =>
        {
            source = board.CreateCard(
                AuthoredCards.AuntMay,
                board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            source.PlaceTokens("c_charge", 3);
        }, Runner(
            AuthoredCards.AuntMay,
            """
            { "seq": [
              { "removeCounters": { "card": "this", "counter": "charge", "count": 2 } },
              { "seq": [
                { "removeCounters": { "card": "this", "counter": "charge", "count": 2 } }
              ] }
            ] }
            """,
            """{ "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }"""));

        Assert.DoesNotContain(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        Assert.Equal(3, source!.Tokens["c_charge"]);
    }

    [Rule("rr:initiating-abilities.step.3")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void CounterCostIsRevalidatedBeforeAStaleActionPays()
    {
        // Step 3 checks the ability to pay before step 5 pays. If the board
        // changes after the offer, the engine repeats that check and neither
        // removes a partial cost nor begins the effect.
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard(
                AuthoredCards.AuntMay,
                board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            source.PlaceTokens("c_charge", 2);
        }, Runner(
            AuthoredCards.AuntMay,
            """{ "removeCounters": { "card": "this", "counter": "charge", "count": 2 } }""",
            """{ "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }"""));
        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        source!.PlaceTokens("c_charge", -1);

        Assert.Throws<RulesNotImplementedException>(
            () => game.Resolve(Decision.Take(action.Id)));

        Assert.Equal(1, source.Tokens["c_charge"]);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Theory]
    [InlineData("""{ "removeCounters": "charge" }""", "cost/removeCounters")]
    [InlineData("""{ "removeCounters": { "card": "this", "counter": "charge", "count": 0 } }""", "positive")]
    [InlineData("""{ "removeCounters": { "card": "this", "counter": "charge", "count": 1, "extra": 1 } }""", "extra")]
    public void CounterCostsRefuseImplicitMalformedOrNonPositiveForms(
        string cost, string says)
    {
        var refused = Assert.Throws<AbilityException>(() => Playing(board =>
        {
            var source = board.CreateCard(
                AuthoredCards.AuntMay,
                board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            source.PlaceTokens("c_charge", 3);
        }, Runner(
            AuthoredCards.AuntMay,
            cost,
            """{ "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }""")));

        Assert.Contains(says, refused.Message, StringComparison.Ordinal);
    }

    [Rule("rr:all-purpose-counter.1")]
    [Rule("rr:all-purpose-counter.2")]
    [Fact]
    public void AllPurposeReferenceSpendsTheOnlyTypedCounter()
    {
        // "All-purpose counters are considered tokens for all game purposes",
        // and an ability naming one can refer to one "regardless of what other
        // types that counter might have." The web counter occupies the card's
        // token inventory, and the generic cost can spend it by that identity.
        Card? shooter = null;
        var (game, world) = Playing(board =>
        {
            shooter = board.CreateCard(
                "01008",
                board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            shooter.PlaceTokens("c_web", 1);
        }, Runner(
            "01008",
            """{ "removeCounters": { "card": "this", "counter": "allPurpose", "count": 1 } }""",
            """{ "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }"""));

        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb
                && option.AnchorId == shooter!.ObjectId);
        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(0, shooter!.Tokens.GetValueOrDefault("c_web"));
        Assert.Equal(DeckType.DiscardPile, shooter.Area.Type);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:all-purpose-counter.2")]
    [Fact]
    public void AllPurposeAmountCountsEveryTypedPool()
    {
        // A generic reference can see both physical all-purpose counters even
        // though the card has defined different types for them.
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard(
                AuthoredCards.AuntMay,
                board.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
            source.PlaceTokens("c_arrow", 2);
            source.PlaceTokens("c_web", 3);
        }, Runner(
            AuthoredCards.AuntMay,
            null,
            """
            { "dealDamage": {
              "cards": { "query": "villain" },
              "amount": { "countersOn": { "card": "this", "counter": "allPurpose" } }
            } }
            """));

        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb
                && option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(5, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:uses-x-type.1")]
    [Fact]
    public void UsesCardRemainsWhileAnyAllPurposeCounterRemains()
    {
        // "If there are no all-purpose counters on this card, discard this
        // card." Removing the last web counter does not satisfy that condition
        // while an arrow counter remains in the same token inventory.
        Card? shooter = null;
        var (game, _) = Playing(board =>
        {
            shooter = board.CreateCard(
                "01008",
                board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            shooter.PlaceTokens("c_web", 1);
            shooter.PlaceTokens("c_arrow", 1);
        }, Runner(
            "01008",
            """{ "removeCounters": { "card": "this", "counter": "web", "count": 1 } }""",
            """{ "draw": { "player": "you", "count": 1 } }"""));

        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.Verb == Game.ActionVerb
                && option.AnchorId == shooter!.ObjectId);
        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(0, shooter!.Tokens.GetValueOrDefault("c_web"));
        Assert.Equal(1, shooter.Tokens["c_arrow"]);
        Assert.Equal(DeckType.UpgradesArea, shooter.Area.Type);
    }

    [Rule("rr:victory-x.1.3")]
    [Rule("rr:victory-x.4")]
    [Fact]
    public void AUsesCardWithVictoryZeroGoesToTheDisplayAtItsLastCounter()
    {
        // The uses-provided instruction replaces the ordinary last-counter
        // discard with the victory display when the card also has Victory X.
        // X may be zero; keyword presence and point value are different facts.
        Card? shooter = null;
        var (game, world) = Playing(board =>
        {
            shooter = board.CreateCard(
                "01008",
                board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            shooter.PlaceTokens("c_web", 1);
            board.Effects.Register(new ContinuousEffect(
                EffectSource.ConstantAbility,
                "victory",
                Amount: 0,
                Card: shooter.ObjectId,
                Affects: shooter.ObjectId,
                Lasts: Duration.WhileInPlay));
        }, Runner(
            "01008",
            """{ "removeCounters": { "card": "this", "counter": "web", "count": 1 } }""",
            """{ "draw": { "player": "you", "count": 1 } }"""));

        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == shooter!.ObjectId);
        var resolved = game.Resolve(Decision.Take(action.Id));

        Assert.Equal(DeckType.VictoryDisplay, shooter!.Area.Type);
        Assert.Single(resolved.Events.OfType<CardsMoved>(), moved =>
            moved.Verb == "Victory"
            && moved.Cards.Any(card => card.Card == shooter.ObjectId));
    }

    [Rule("rr:in-play-and-out-of-play.4")]
    [Rule("rr:victory-x.1.3")]
    [Fact]
    public void LaterEffectCannotDiscardAUsesCardFromTheVictoryDisplay()
    {
        // Paying the cost removes the final counter and sends this card to the
        // Victory display. The retained `this` binding is then out of play and
        // the later discard effect cannot bring it back into the game.
        Card? shooter = null;
        var (game, world) = Playing(board =>
        {
            shooter = board.CreateCard(
                "01008",
                board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            shooter.PlaceTokens("c_web", 1);
            board.Effects.Register(new ContinuousEffect(
                EffectSource.ConstantAbility,
                "victory",
                Amount: 0,
                Card: shooter.ObjectId,
                Affects: shooter.ObjectId,
                Lasts: Duration.WhileInPlay));
        }, Runner(
            "01008",
            """{ "removeCounters": { "card": "this", "counter": "web", "count": 1 } }""",
            """{ "discard": "this" }"""));
        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == shooter!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(DeckType.VictoryDisplay, shooter!.Area.Type);
        Assert.Contains(shooter, world.AreaOf(DeckType.VictoryDisplay).Cards);
    }

    [Rule("rr:all-purpose-counter.2")]
    [Fact]
    public void RemovingOneOfSeveralTypesRaisesBeforeChoosingForThePlayer()
    {
        World? world = null;
        Card? source = null;
        Assert.Throws<RulesNotImplementedException>(
            () => Playing(board =>
            {
                world = board;
                source = board.CreateCard(
                    AuthoredCards.AuntMay,
                    board.AreaOf(
                        DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
                source.PlaceTokens("c_arrow", 1);
                source.PlaceTokens("c_web", 1);
            }, Runner(
                AuthoredCards.AuntMay,
                """{ "removeCounters": { "card": "this", "counter": "allPurpose", "count": 1 } }""",
                """{ "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }""")));

        Assert.Equal(1, source!.Tokens["c_arrow"]);
        Assert.Equal(1, source.Tokens["c_web"]);
        Assert.Equal(0, world!.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    private static AbilityRunner Runner(string card, string? cost, string effect) =>
        new(AbilityCatalog.Parse(
            $$"""
            { "cards": [ { "card": "{{card}}", "abilities": [ {
              "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
              {{(cost is null ? string.Empty : $"\"cost\": {cost},")}}
              "effect": {{effect}}
            } ] } ] }
            """));

    private static (Game Game, World World) Playing(
        Action<World> prepare, ICardAbilities abilities)
    {
        var world = WorldSetup.DealWithoutCardAbilities(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            [Setup.Hero("spider_man").Name],
            12345);
        prepare(world);

        var game = Game.Begin(world, Cards, abilities);
        while (game.Pending is { } asked
            && asked.Affordances.Any(option => option.Verb == Game.ResolveMulligans))
        {
            game.Resolve(Decision.Decline);
        }

        return (game, world);
    }
}
