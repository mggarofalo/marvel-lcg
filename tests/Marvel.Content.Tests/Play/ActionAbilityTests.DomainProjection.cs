using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Rule("rr:player-elimination.step.1")]
    [Rule("rr:player-elimination.step.2")]
    [Rule("rr:player-elimination.5")]
    [Rule("rr:initiating-abilities.3")]
    [Fact]
    public void ProjectedEliminationRetainsTheMinionsCardsAndFinishesThePaidAbility()
    {
        // Elimination transfers minions "retaining any ... attached cards ...
        // and status cards on them". If it happens within an ability, "resolve
        // the entire ability", including the threat removal after the defeat.
        var runner = Runner(AuthoredCards.AuntMay, "Action",
            """
            {"thwart":{"target":{"query":"mainScheme"},"effect":{"seq":[
              {"dealDamage":{"cards":"you","amount":10}},
              {"removeThreat":{"scheme":{"query":"mainScheme"},"amount":1}}
            ]}}}
            """, cost: """{"discard":"this"}""");
        Card? source = null;
        Card? minion = null;
        Card? tracer = null;
        Card? tough = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard("01101", board.AreaOf(
                DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            tracer = board.CreateCard("01007", board.AreaOf(
                DeckType.UpgradesArea, PlayArea.Of(0), minion.ObjectId, cardOwner: 0));
            tough = Statuses.Give(board, minion, Statuses.Tough);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 2);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        long threat = scheme.Tokens["k_threat"];
        string before = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;

        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);

        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.False(world.Seats[0].Eliminated);
        Assert.Equal(PlayArea.Of(0), tracer!.Area.PlayArea);
        var offered = Assert.Single(game.Pending!.Affordances,
            option => option.AnchorId == action.Card);

        game.Resolve(Decision.Take(offered.Id));

        Assert.True(world.Seats[0].Eliminated);
        Assert.Equal(1, world.FirstPlayer);
        Assert.Equal(DeckType.RemovedArea, source!.Area.Type);
        Assert.Equal(PlayArea.Of(1), minion!.Area.PlayArea);
        Assert.Equal(PlayArea.Of(1), tracer.Area.PlayArea);
        Assert.Equal(minion.ObjectId, tracer.Area.Host);
        Assert.Equal(PlayArea.Of(1), tough!.Area.PlayArea);
        Assert.True(Statuses.Has(world, minion, Statuses.Tough));
        Assert.Equal(threat - 1, scheme.Tokens["k_threat"]);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:initiating-abilities.3")]
    [Rule("rr:choose-option")]
    [Rule("rr:tough.2")]
    [Rule("rr:heal")]
    [Theory]
    [InlineData(0, 2)]
    [InlineData(1, 3)]
    public void ProjectedChoicesRemainPlayableAfterSourcePaymentAndDamagePrevention(
        int choice, long finalDamage)
    {
        // A started ability "does not stop from completing if that card leaves
        // play". The player, not preflight, chooses between tough ("prevent all
        // of that damage") and healing one damage before taking two.
        var runner = Runner(AuthoredCards.AuntMay, "Action",
            """
            {"seq":[
              {"choose":{"options":[
                {"giveStatus":{"card":"you","status":"tough"}},
                {"heal":{"card":"you","amount":1}}
              ]}},
              {"dealDamage":{"cards":"you","amount":2}},
              {"attack":{"target":{"query":"villain"},
                "effect":{"dealAttackDamage":{"cards":{"query":"villain"},"amount":1}}}}
            ]}
            """, cost: """{"discard":"this"}""");
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(2);
        }, hero: true, abilities: runner);
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        string before = world.Digest().Canonical();
        long rng = world.Random.Generator.WordsConsumed;

        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(rng, world.Random.Generator.WordsConsumed);
        Assert.Equal(2, hero.Damage);
        Assert.False(Statuses.Has(world, hero, Statuses.Tough));

        var offered = Assert.Single(game.Pending!.Affordances,
            option => option.AnchorId == action.Card);
        game.Resolve(Decision.Take(offered.Id));

        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        Assert.Equal(Question.Option, game.Pending!.Asking);
        Assert.Equal([0, 1], game.Pending.Affordances.Select(option => option.Id));
        Assert.Equal(2, hero.Damage);
        Assert.Equal(0, villain.Damage);
        Assert.False(Statuses.Has(world, hero, Statuses.Tough));

        game.Resolve(Decision.Take(choice));

        Assert.Equal(finalDamage, hero.Damage);
        Assert.False(Statuses.Has(world, hero, Statuses.Tough));
        Assert.Equal(1, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(rng, world.Random.Generator.WordsConsumed);
    }
}
