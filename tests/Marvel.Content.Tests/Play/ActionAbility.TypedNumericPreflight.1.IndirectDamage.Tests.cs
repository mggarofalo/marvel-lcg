using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;
public sealed class ActionAbilityTypedNumericPreflightIndirectDamageTests
{
    [Fact]
    public void IndirectDamageCanExposeALaterPlayerFrameBeforeTheCostIsPaid()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"eachPlayer":{"effect":{"if":{
              "test":{"inForm":{"player":"firstPlayer","form":"hero"}},
              "then":{"if":{"test":{"inForm":{"player":"you","form":"hero"}},
                "then":{"indirectDamage":{"among":"you","amount":1}}}},
              "else":{"attack":{"target":{"query":"villain"},
                "effect":{"enemyAttacks":{"enemies":{"query":"villain"}}}}}
            }}}}
            """, cost: """{"exhaust":"this"}""");
        Card? source = null;
        World? world = null;
        // The first player's only recipient can be eliminated by the assignment.
        // Preflight must retain the next player's newly reachable branch; the
        // engine cannot suspend for an enemy activation inside its labeled power.
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", refused.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Theory]
    [InlineData("dealDamage")]
    [InlineData("dealAttackDamage")]
    [InlineData("removeThreat")]
    public void ChoicePreviewUsesTheSameCompiledAmountAsResolution(string operation)
    {
        bool threat = operation == "removeThreat";
        var(runner, fields) = MutableNumericRunner(operation, threat ? """{"scheme":"chosen","amount":2}""" : """{"cards":"chosen","amount":2}""", choice: false, cards: threat ? "mainScheme" : "villain");
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 3);
        }, hero: true, abilities: runner);
        var target = world.TheCardIn(threat ? DeckType.MainSchemesArea : DeckType.VillainArea)!;
        long maximum = threat ? world.Facts.PrintedValue(target.FaceId, "TargetThreat", world.Players) : Damage.Health(world, world.Facts, target);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        fields["amount"] = new AbilityValue.Number(0);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(Question.Element, game.Pending!.Asking);
        var selected = Assert.Single(game.Pending.Affordances);
        string expected = threat ? $"3/{maximum} → 1/{maximum} threat" : $"{maximum}/{maximum} → {maximum - 2}/{maximum} HP";
        Assert.Contains(expected, selected.Description, StringComparison.Ordinal);
        game.Resolve(Decision.Take(selected.Id));
        Assert.Equal(threat ? 1 : 2, threat ? target.Tokens.GetValueOrDefault("k_threat") : target.Damage);
        Assert.False(source!.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OmittedLastingAmountDoesNotInventGuardDuringPreflight(bool power)
    {
        // Engine choice: an omitted lasting-modifier amount is zero. Inventing
        // Guard would hide the villain damage that can eliminate the first
        // player and expose a continuation this engine cannot suspend inside.
        const string effects = """
            {"grantUntil":{"card":{"titled":"Sandman"},"keyword":"guard","until":"EndOfRound"}},
            {"dealDamage":{"cards":{"query":"attackableEnemies"},"amount":1}},
            {"moveDamage":{"from":{"query":"villain"},"to":{"titled":"Spider-Man"},"amount":1}}
            """;
        string effect = power ? $$"""
            {"attack":{"target":{"query":"villain"},"effect":{"seq":[{{effects}},
              {"if":{"test":{"inForm":{"player":"firstPlayer","form":"hero"} },
                "then":{"draw":{"player":"you","count":1} },
                "else":{"enemyAttacks":{"enemies":{"query":"villain"} } }
              } }
            ]} } }
            """ : $$"""
            {"eachPlayer":{"effect":{"if":{
              "test":{"inForm":{"player":"firstPlayer","form":"hero"} },
              "then":{"if":{"test":{"inForm":{"player":"you","form":"hero"} },
                "then":{"seq":[{{effects}}]} } },
              "else":{"attack":{"target":{"query":"villain"},
                "effect":{"enemyAttacks":{"enemies":{"query":"villain"} } } } }
            } } } }
            """;
        var runner = Runner(AuthoredCards.AuntMay, "Action", effect, cost: """{"exhaust":"this"}""");
        Card? source = null;
        Card? minion = null;
        World? world = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard("01102", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("suspends inside a labelled power", refused.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(0, minion!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(0, StateFields.Modified(world, minion, "guard", world.Facts, world.Players));
    }

    [Theory]
    [InlineData("heal", false)]
    [InlineData("heal", true)]
    [InlineData("dealDamage", false)]
    [InlineData("dealDamage", true)]
    [InlineData("dealAttackDamage", false)]
    [InlineData("dealAttackDamage", true)]
    [InlineData("placeThreat", false)]
    [InlineData("placeThreat", true)]
    [InlineData("removeThreat", false)]
    [InlineData("removeThreat", true)]
    public void CompiledNumericAmountsGovernActionAndOptionEligibility(string operation, bool choice)
    {
        string target = operation switch
        {
            "heal" => "\"card\":\"you\"",
            "placeThreat" or "removeThreat" => "\"scheme\":{\"query\":\"mainScheme\"}",
            _ => "\"cards\":{\"query\":\"villain\"}",
        };
        var(runner, fields) = MutableNumericRunner(operation, $$"""
            { {{target}},"amount":{"if":{"test":{"inForm":{"player":"you","form":"hero"} },
              "then":{"add":[1,1]},"else":0} } }
            """, choice);
        // Engine contract: edits to caller-owned syntax cannot turn the
        // compiled amount into a no-op before either eligibility check.
        fields["amount"] = new AbilityValue.Number(0);
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(5);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 3);
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        if (choice)
        {
            Assert.Equal(Question.Option, game.Pending!.Asking);
            Assert.Equal(2, game.Pending.Affordances.Count);
            game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances, option => option.Id == 0).Id));
        }

        Assert.Equal(operation == "heal" ? 3 : 5, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(operation is "dealDamage" or "dealAttackDamage" ? 2 : 0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(operation == "placeThreat" ? 5 : operation == "removeThreat" ? 1 : 3, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
        Assert.False(source!.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }
}
