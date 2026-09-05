using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RepetitionReadsPublishAtTheirLiveBoundary(bool alteration)
    {
        // The repeat count and the per-card alteration condition are live
        // reads, distinct from the previews that decide whether to offer them.
        string effect = alteration
            ? """
              {"eachTime":{"effect":{"discardTop":{"from":"encounterDeck","count":1}},
                "when":{"exists":{"cardsIn":{"area":"yourDeck"}}},
                "then":{"placeCounters":{"card":"this","counter":"test","count":1}}}}
              """
            : """
              {"forEach":{"count":{"if":{"test":{"exists":{"cardsIn":{"area":"yourDeck"}}},"then":1,"else":0}},
                "effect":{"dealDamage":{"cards":{"query":"villain"},"amount":1}}}}
              """;
        var runner = Runner(AuthoredCards.AuntMay, "Action", effect);
        Card? source = null;
        var (game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        string before = world.Digest().Canonical();
        Assert.Contains(runner.Actions(world, 0), option => option.Card == source!.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        long damage = villain.Damage;
        long words = world.Random.Generator.WordsConsumed;

        var result = game.Resolve(Decision.Take(action.Id));

        Assert.Single(result.Information, signal => signal.Kind == InformationKind.Search);
        Assert.Equal(alteration ? 1 : 0, source!.Tokens.GetValueOrDefault("c_test"));
        Assert.Equal(damage + (alteration ? 0 : 1), villain.Damage);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NumericEffectReadsPublishOnlyExecutedObservations(bool readsDeck)
    {
        // Frontier metadata belongs to the live numeric expression, even
        // when its result is used directly instead of inside a condition.
        const string read = """{"count":{"cardsIn":{"area":"yourDeck"}}}""";
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$$$$$"""
            {"placeCounters":{"card":"this","counter":"test","count":{
              "if":{"test":{"exists":{"query":"villain"}},
                "then":{{{{{{(readsDeck ? read : "1")}}}}}},
                "else":{{{{{{(readsDeck ? "1" : read)}}}}}}}
            }}}
            """);
        Card? source = null;
        var (game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        string before = world.Digest().Canonical();
        Assert.Contains(runner.Actions(world, 0), option => option.Card == source!.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        int expected = readsDeck ? world.Seats[0].Deck.Cards.Count : 1;
        long words = world.Random.Generator.WordsConsumed;

        var result = game.Resolve(Decision.Take(action.Id));

        Assert.Equal(readsDeck ? 1 : 0, result.Information.Count(signal => signal.Kind == InformationKind.Search));
        Assert.Equal(expected, source!.Tokens.GetValueOrDefault("c_test"));
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }

    [Theory]
    [InlineData("addToHand")]
    [InlineData("returnToHand")]
    public void SingleAndPluralEffectReadsPublishTheirObservations(string operation)
    {
        // Both cardinalities of a direct effect selection expose the search.
        // Previewing the same selection must not publish that exposure.
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$$$$$"""
            {"{{{{{{operation}}}}}}":{"cardsIn":{"area":"yourDeck","title":"Genius"}}}
            """);
        Card? source = null;
        var (game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        var genius = Assert.Single(world.Cards, card => world.Facts.Title(card.FaceId) == "Genius");
        World.MoveToTop(genius, world.Seats[0].Deck);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        string before = world.Digest().Canonical();
        Assert.Contains(runner.Actions(world, 0), option => option.Card == source!.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        int hand = world.Seats[0].Hand.Cards.Count;
        long words = world.Random.Generator.WordsConsumed;

        var result = game.Resolve(Decision.Take(action.Id));

        Assert.Single(result.Information, signal => signal.Kind == InformationKind.Search);
        Assert.Equal(world.Seats[0].Hand, genius.Area);
        Assert.Equal(hand + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EvaluationRetainsTheSourceIncarnationAcrossAChoice(bool choice)
    {
        // A query captures the source binding established by the live ability.
        // Reading again after it reenters play cannot bind a new incarnation.
        string before = choice
            ? """{"choose":{"options":[{"heal":{"card":"you","amount":1}},{"heal":{"card":"you","amount":2}}]}},"""
            : "";
        var runner = Runner(AuthoredCards.Shocker, "Action", $$$$$$"""
            {"seq":[{{{{{{before}}}}}}{"discard":"this"},
              {"putIntoPlay":{"card":"this","where":"engagedWithYou"}},
              {"if":{"test":{"exists":"this"},
                "then":{"placeCounters":{"card":"this","counter":"test","count":1}},
                "else":{"heal":{"card":"you","amount":1}}}}]}
            """);
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard(AuthoredCards.Shocker,
                board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.Seats[0].IdentityCard.TakeDamage(3);
        }, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        if (choice)
        {
            Assert.Equal(Question.Option, game.Pending!.Asking);
            game.Resolve(Decision.Take(0));
        }

        Assert.Equal(DeckType.EngagedEnemiesArea, source!.Area.Type);
        Assert.DoesNotContain("c_test", source.Tokens.Keys);
        Assert.Equal(choice ? 1 : 2, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("""{"exists":{"cardsIn":{"area":"yourDeck"}}}""", 1)]
    [InlineData("""{"and":[{"exists":{"query":"villain"}},{"exists":{"cardsIn":{"area":"yourDeck"}}}]}""", 1)]
    [InlineData("""{"or":[{"exists":{"query":"villain"}},{"exists":{"cardsIn":{"area":"yourDeck"}}}]}""", 0)]
    [InlineData("""{"atLeast":{"value":{"if":{"test":{"exists":{"query":"villain"}},"then":{"count":{"cardsIn":{"area":"yourDeck"}}},"else":0}},"count":0}}""", 1)]
    public void OnlyTheExecutedConditionPublishesItsQueryObservations(string condition, int searches)
    {
        // Observations belong to the live expression that was evaluated.
        // Repeated previews and short-circuited operands publish nothing.
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$$$$$"""
            {"if":{"test":{{{{{{condition}}}}}},
              "then":{"placeCounters":{"card":"this","counter":"test","count":1}},
              "else":{"placeCounters":{"card":"this","counter":"test","count":2}}}}
            """);
        Card? source = null;
        var (game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        string before = world.Digest().Canonical();
        Assert.Contains(runner.Actions(world, 0), option => option.Card == source!.ObjectId);
        Assert.Contains(runner.Actions(world, 0), option => option.Card == source!.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        long words = world.Random.Generator.WordsConsumed;

        var result = game.Resolve(Decision.Take(action.Id));

        Assert.Equal(searches, result.Information.Count(signal => signal.Kind == InformationKind.Search));
        Assert.Equal(1, source!.Tokens.GetValueOrDefault("c_test"));
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("minionsEngagedWithYou")]
    [InlineData("sideSchemes")]
    [InlineData("schemes")]
    [InlineData("thwartableSchemes")]
    [InlineData("charactersYouControl")]
    [InlineData("alliesYouControl")]
    [InlineData("identitiesWithTechInDiscard")]
    [InlineData("dronesEngagedWithYou")]
    [InlineData("canMakeTheCall")]
    public void QueryingAnEmptyAreaDoesNotAllocateIt(string query)
    {
        // Evaluation is an engine-owned read boundary. Even empty areas and
        // their future ids must be unchanged by repeatedly asking for actions.
        string condition = query == "canMakeTheCall"
            ? """{"canMakeTheCall":"game"}"""
            : $$$$$$"""{"exists":{"query":"{{{{{{query}}}}}}"}}""";
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$$$$$"""
            {"if":{"test":{{{{{{condition}}}}}},
              "then":{"placeCounters":{"card":"this","counter":"test","count":1}},
              "else":{"placeCounters":{"card":"this","counter":"test","count":2}}}}
            """);
        var (world, source) = FixedCountBoard(runner, players: 2);
        var areas = world.Areas.ToArray();
        string digest = world.Digest().Canonical();

        Assert.Contains(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        Assert.Contains(runner.Actions(world, 0), action => action.Card == source.ObjectId);

        Assert.Equal(areas, world.Areas);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.DoesNotContain("c_test", source.Tokens.Keys);
        Assert.Equal(0, world.Random.Generator.WordsConsumed);
        Assert.False(world.Agenda.IsBusy);
        var next = world.CreateArea(DeckType.EncounterDeck);
        Assert.Equal(areas.Length, next.Id);
    }
}
