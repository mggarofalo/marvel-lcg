using Marvel.Cards.Dsl;
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
    [InlineData("heal")]
    [InlineData("giveStatus")]
    [InlineData("grantUntil")]
    public void RepeatedProtectionKeepsItsCompiledRecipient(string operation)
    {
        string arguments = operation switch
        {
            "heal" => """{"card":{"titled":"Spider-Man"},"amount":1}""",
            "giveStatus" => """{"card":{"titled":"Spider-Man"},"status":"tough"}""",
            _ => """{"card":{"titled":"Spider-Man"},"keyword":"health","amount":1,"until":"EndOfRound"}""",
        };
        var (runner, fields) = MutableRepeatedTraceRunner($$$"""
            [{"{{{operation}}}":{{{arguments}}}},
             {"dealDamage":{"cards":{"titled":"Spider-Man"},"amount":1}}]
            """, 0);
        fields["card"] = new AbilityValue.Word("this");
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var identity = world.Seats[0].IdentityCard;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);

        Assert.Equal(9, identity.Damage);
        Assert.Equal(10, Damage.Health(world, world.Facts, identity));
        Assert.Equal(0, Statuses.Count(world, identity, Statuses.Tough));
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(Question.Order, game.Pending!.Asking);
        var order = Assert.Single(game.Pending.Affordances);
        int[] players = [identity.ObjectId, world.Seats[1].IdentityCard.ObjectId];
        Assert.Equal(players, order.Targets!.Legal);
        game.Resolve(new Decision(order.Id, players));

        Assert.Equal(operation == "grantUntil" ? 11 : 9, identity.Damage);
        Assert.Equal(operation == "grantUntil" ? 12 : 10, Damage.Health(world, world.Facts, identity));
        Assert.Equal(0, Statuses.Count(world, identity, Statuses.Tough));
        Assert.Equal(0, world.FirstPlayer);
        Assert.False(source!.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("trait")]
    [InlineData("field")]
    public void RepeatedDynamicSelectionUsesTheCompiledGrant(string grantKind)
    {
        string grant = grantKind == "trait"
            ? """{"card":{"titled":"Shocker"},"trait":"AERIAL","until":"EndOfRound"}"""
            : """{"card":{"titled":"Shocker"},"keyword":"attack","amount":3,"until":"EndOfRound"}""";
        string selection = grantKind == "trait"
            ? """{"withTrait":{"cards":{"query":"minions"},"trait":"AERIAL"}}"""
            : """{"maxBy":{"of":{"query":"enemies"},"by":"attack"}}""";
        var (runner, fields) = MutableRepeatedTraceRunner($$$"""
            [{"grantUntil":{{{grant}}}},
             {"dealDamage":{"cards":{{{selection}}},"amount":1}},
             {"moveDamage":{"from":{"titled":"Shocker"},"to":{"titled":"Spider-Man"},"amount":1}}]
            """, 0);
        fields["card"] = new AbilityValue.Word("this");
        if (grantKind == "trait") fields["trait"] = new AbilityValue.Word("AVENGER");
        else fields["keyword"] = new AbilityValue.Word("scheme");
        Card? source = null;
        Card? minion = null;
        World? world = null;

        // The compiled grant adds Shocker to the later selector. Its damage
        // can then eliminate the first player, exposing the unsupported branch.
        var failure = Assert.Throws<RulesNotImplementedException>(() => Playing(board =>
        {
            world = board;
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard("01103", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            if (grantKind == "field")
                board.CreateCard("01102", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner));

        Assert.Contains("suspends inside a labelled power", failure.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
        Assert.Equal(0, minion!.Damage);
        Assert.Equal(9, world!.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    [Theory]
    [InlineData("titled")]
    [InlineData("minBy")]
    public void RepeatedSelectorsReevaluateTheCompiledRelationAfterAStageChange(string selectionKind)
    {
        string selection = selectionKind == "titled"
            ? """{"titled":"Rhino"}"""
            : """{"minBy":{"of":{"query":"enemies"},"by":"attack"}}""";
        var (runner, fields) = MutableRepeatedTraceRunner($$$"""
            [{"dealDamage":{"cards":{"query":"villain"},"amount":100}},
             {"dealDamage":{"cards":{{{selection}}},"amount":1}},
             {"moveDamage":{"from":{"query":"villain"},"to":{"titled":"Spider-Man"},"amount":1}}]
            """, 1);
        fields["cards"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
        {
            ["query"] = new AbilityValue.Word("villain"),
        });
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            if (selectionKind == "titled") board.CreateCard("01136", board.AreaOf(DeckType.VillainDeck));
            board.CreateCard("01103", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.Seats[0].IdentityCard.TakeDamage(9);
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);

        // Rhino's title must not follow Ultron, and the lowest-attack set must
        // drop Rhino II. Neither compiled selector damages the new stage.
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
        Assert.Equal(9, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(0, world.FirstPlayer);
    }

    private static (AbilityRunner Runner, Dictionary<string, AbilityValue> Fields) MutableRepeatedTraceRunner(
        string sequence, int index)
    {
        var parsed = AbilityCatalog.Parse($$$$$$"""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"exhaust":"this"},
              "effect":{"eachPlayer":{"effect":{"if":{
                "test":{"inForm":{"player":"firstPlayer","form":"hero"}},
                "then":{"seq":{{{{{{sequence}}}}}}},
                "else":{"attack":{"target":{"query":"villain"},
                  "effect":{"enemyAttacks":{"enemies":{"query":"villain"}}}}}
              }}}}
            }]}]}
            """);
        var conditional = AbilityNode.Of(parsed.Abilities[0].Effect.Require("effect"));
        var conditionFields = ((AbilityValue.Map)conditional.Argument).Entries.ToDictionary(pair => pair.Key, pair => pair.Value);
        var steps = ((AbilityValue.List)AbilityNode.Of(conditionFields["then"]).Argument).Values.ToList();
        var selected = AbilityNode.Of(steps[index]);
        var fields = ((AbilityValue.Map)selected.Argument).Entries.ToDictionary(pair => pair.Key, pair => pair.Value);
        steps[index] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
        {
            [selected.Kind] = new AbilityValue.Map(fields),
        });
        conditionFields["then"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
        {
            ["seq"] = new AbilityValue.List(steps),
        });
        var effect = new AbilityNode("eachPlayer", new AbilityValue.Map(new Dictionary<string, AbilityValue>
        {
            ["effect"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
            {
                ["if"] = new AbilityValue.Map(conditionFields),
            }),
        }));
        return (new AbilityRunner(new AbilityBook(
            [parsed.Abilities[0] with { Effect = effect }], parsed.Authored)), fields);
    }
}
