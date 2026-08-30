using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
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
            """{ "removeCounters": "allPurpose" }""",
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
            """{ "removeCounters": "web" }""",
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
            """{ "removeCounters": "web" }""",
            """{ "draw": { "player": "you", "count": 1 } }"""));

        var action = Assert.Single(game.Pending!.Affordances, option =>
            option.Verb == Game.ActionVerb && option.AnchorId == shooter!.ObjectId);
        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(DeckType.VictoryDisplay, shooter!.Area.Type);
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
                """{ "removeCounters": "allPurpose" }""",
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
        var world = WorldSetup.Deal(
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
