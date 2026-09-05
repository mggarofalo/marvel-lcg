using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Theory]
    [InlineData("add")]
    [InlineData("mul")]
    [InlineData("min")]
    public void MutableCountUnderArithmeticIsRejectedBeforeCost(string operation)
    {
        // This engine boundary refuses a board-dependent count across payment.
        // An arithmetic wrapper does not make the count stable.
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$"""
            {"forEach":{"count":{"{{operation}}":[1,{"damageOn":"you"}]},
              "effect":{"draw":{"player":"you","count":1} } } }
            """, cost: """{"exhaust":"this"}""");
        Card? source = null;
        World? world = null;

        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(2);
        }, abilities: runner));

        Assert.Contains("count after state may change", refused.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(2, world!.Seats[0].IdentityCard.Damage);
    }

    [Theory]
    [InlineData("{\"powerAmount\":\"cardsDiscarded\"}")]
    [InlineData("{\"add\":[0,{\"powerAmount\":\"cardsDiscarded\"}]}")]
    [InlineData("{\"mul\":[1,{\"powerAmount\":\"cardsDiscarded\"}]}")]
    [InlineData("{\"min\":[{\"powerAmount\":\"cardsDiscarded\"},3]}")]
    [InlineData("{\"if\":{\"test\":{\"atLeast\":{\"value\":{\"powerAmount\":\"cardsDiscarded\"},\"count\":1}},\"then\":2,\"else\":-1}}")]
    [InlineData("{\"if\":{\"test\":{\"and\":[{\"or\":[{\"not\":{\"atLeast\":{\"value\":1,\"count\":{\"powerAmount\":\"cardsDiscarded\"}}}}]}]},\"then\":2,\"else\":-1}}")]
    public void RepetitionCountBindsSelectedPaymentBeforeEvaluation(string count)
    {
        // The engine's unbound sentinel is not an authored negative count.
        // Two selected cards supply the power amount before the count runs.
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$"""
            {"legalPractice":{"schemes":{"query":"thwartableSchemes"},
              "power":{"thwart":{"target":"chosen","effect":{"forEach":{
                "count":{{count}},"effect":{"removeThreat":{"scheme":"chosen","amount":1} }
              } } } } } }
            """);
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 4);
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        var option = Assert.Single(game.Pending!.Affordances, candidate => candidate.AnchorId == scheme.ObjectId);
        var discarded = world.Seats[0].Hand.Cards.Take(2).ToList();
        Assert.Equal(2, discarded.Count);

        game.Resolve(Decision.Take(option.Id, discarded.Select(card => card.ObjectId).ToList(), []));

        Assert.All(discarded, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        Assert.Equal(2, scheme.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    public void StableArithmeticCountDoesNotBecomeMutableAfterCompilation()
    {
        var (runner, fields) = MutableEffectRunner("forEach", """
            {"count":{"add":[1,{"mul":[1,{"min":[3,{"perPlayer":2}]}]}]},
             "effect":{"draw":{"player":"you","count":1}}}
            """, false);
        Card? source = null;
        var (game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        int held = world.Seats[0].Hand.Cards.Count;
        fields["count"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
        {
            ["damageOn"] = new AbilityValue.Word("you"),
        });

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(held + 4, world.Seats[0].Hand.Cards.Count);
        Assert.False(source!.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RepetitionKeepsItsCompiledCountAcrossChoices(bool changeWhilePending)
    {
        var (runner, fields) = MutableEffectRunner("forEach", """
            {"count":2,"effect":{"choose":{"options":[
              {"heal":{"card":"you","amount":1}},
              {"draw":{"player":"you","count":1}}
            ]}}}
            """, false);
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(3);
        }, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        if (!changeWhilePending) fields["count"] = new AbilityValue.Number(0);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(Question.Option, game.Pending!.Asking);
        if (changeWhilePending) fields["count"] = new AbilityValue.Number(0);
        game.Resolve(Decision.Take(0));
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(Question.Option, game.Pending!.Asking);
        game.Resolve(Decision.Take(0));
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.False(source!.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("dealDamage")]
    [InlineData("removeThreat")]
    public void CombinedRepetitionUsesItsCompiledMultiplier(string operation)
    {
        string arguments = operation == "dealDamage"
            ? """{"cards":{"query":"villain"},"amount":1}"""
            : """{"scheme":{"query":"mainScheme"},"amount":1}""";
        var (runner, fields) = MutableEffectRunner("forEach",
            $$"""{"count":3,"effect":{"{{operation}}": {{arguments}} } }""", false);
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 4);
        }, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        fields["count"] = new AbilityValue.Number(1);

        var result = game.Resolve(Decision.Take(action.Id));

        if (operation == "dealDamage")
            Assert.Equal(3, world.TheCardIn(DeckType.VillainArea)!.Damage);
        else
            Assert.Equal(1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
        var changed = operation == "dealDamage"
            ? world.TheCardIn(DeckType.VillainArea)!
            : world.TheCardIn(DeckType.MainSchemesArea)!;
        Assert.Single(result.Events.OfType<FieldSet>(), change => change.Card == changed.ObjectId);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EachTimeKeepsCompiledDiscardAndPredicateAcrossChoices(bool changeWhilePending)
    {
        var (runner, fields) = MutableEffectRunner("eachTime", """
            {"effect":{"discardTop":{"from":"encounterDeck","count":2}},
             "when":{"isKind":{"card":"that","kind":"minion"}},
             "then":{"choose":{"options":[
               {"heal":{"card":"you","amount":1}},
               {"draw":{"player":"you","count":1}}
             ]}}}
            """, false);
        Card? source = null;
        Card? first = null;
        Card? second = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(3);
            var deck = board.AreaOf(DeckType.EncounterDeck);
            second = board.CreateCard(AuthoredCards.Shocker, deck);
            first = board.CreateCard(AuthoredCards.Shocker, deck);
        }, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        void ChangeArguments()
        {
            fields["effect"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
            {
                ["discardTop"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
                {
                    ["from"] = new AbilityValue.Word("yourDeck"),
                    ["count"] = new AbilityValue.Number(1),
                }),
            });
            fields["when"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
            {
                ["isKind"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
                {
                    ["card"] = new AbilityValue.Word("that"),
                    ["kind"] = new AbilityValue.Word("treachery"),
                }),
            });
        }
        if (!changeWhilePending) ChangeArguments();

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(Question.Option, game.Pending!.Asking);
        Assert.Equal(DeckType.EncounterDiscardPile, first!.Area.Type);
        Assert.Equal(DeckType.EncounterDeck, second!.Area.Type);
        if (changeWhilePending) ChangeArguments();
        game.Resolve(Decision.Take(0));
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(Question.Option, game.Pending!.Asking);
        Assert.Equal(DeckType.EncounterDiscardPile, second.Area.Type);
        game.Resolve(Decision.Take(0));
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.False(source!.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }
}
