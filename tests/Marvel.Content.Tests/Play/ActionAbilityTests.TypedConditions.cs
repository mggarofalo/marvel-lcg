using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Fact]
    public void AFormChangeExposesThePlayerBindingNeededByTheContinuation()
    {
        // On the initial board the else branch selects a scenario-owned card.
        // The earlier form change exposes identities, whose owners can draw.
        // Preflight must include that reachable binding before offering the action.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            {"seq":[
              {"changeForm":{"player":"you","to":"hero"}},
              {"if":{"test":{"inForm":{"player":"you","form":"hero"}},
                "then":{"chooseCard":{"from":{"query":"identities"},"effect":{"seq":[]}}},
                "else":{"chooseCard":{"from":{"query":"villain"},"effect":{"seq":[]}}}
              }},
              {"draw":{"player":"chosenPlayer","count":1}}
            ]}
            """, cost: """{"exhaust":"this"}""");
        Card? source = null;
        var (game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        int firstHeld = world.Seats[0].Hand.Cards.Count;
        int secondHeld = world.Seats[1].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.True(Forms.In(world, world.Seats[0], world.Facts, "hero"));
        var second = Assert.Single(game.Pending!.Affordances,
            option => option.Id == world.Seats[1].IdentityCard.ObjectId);
        game.Resolve(Decision.Take(second.Id));

        Assert.Equal(firstHeld, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(secondHeld + 1, world.Seats[1].Hand.Cards.Count);
        Assert.False(source!.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("direct", 2)]
    [InlineData("and", 2)]
    [InlineData("or", 2)]
    [InlineData("not", 1)]
    [InlineData("count", 2)]
    [InlineData("add", 2)]
    [InlineData("mul", 2)]
    [InlineData("min", 2)]
    [InlineData("comparison-count", 1)]
    public void ConditionalBindsTheSelectedPlayerBeforeTesting(string wrapper, int drawn)
    {
        const string form = """{"inForm":{"player":"chosenPlayer","form":"hero"}}""";
        const string count = """{"count":{"query":"enemiesEngagedWithChosenPlayer"}}""";
        string amount = wrapper switch
        {
            "add" => $$"""{"add":[0,{{count}}]}""",
            "mul" => $$"""{"mul":[1,{{count}}]}""",
            "min" => $$"""{"min":[1,{{count}}]}""",
            _ => count,
        };
        string condition = wrapper switch
        {
            "direct" => form,
            "not" => $$"""{"not":{{form}} }""",
            "and" or "or" => $$"""{"{{wrapper}}":[{{form}}]}""",
            "comparison-count" => $$"""{"atLeast":{"value":1,"count":{{amount}} } }""",
            _ => $$"""{"atLeast":{"value":{{amount}},"count":1} }""",
        };
        bool hasCost = wrapper is "direct" or "and" or "or" or "not";
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$"""
            {"chooseCard":{"from":{"query":"identities"},"effect":{"if":{
              "test":{{condition}},
              "then":{"draw":{"player":"chosenPlayer","count":1} },
              "else":{"draw":{"player":"chosenPlayer","count":2} }
            } } } }
            """, cost: hasCost ? """{"exhaust":"this"}""" : null);
        Card? source = null;
        var (game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        int firstHeld = world.Seats[0].Hand.Cards.Count;
        int secondHeld = world.Seats[1].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var option = Assert.Single(game.Pending!.Affordances,
            candidate => candidate.Id == world.Seats[1].IdentityCard.ObjectId);
        game.Resolve(Decision.Take(option.Id));

        Assert.Equal(firstHeld, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(secondHeld + drawn, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(!hasCost, source!.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void ConditionalKeepsItsCompiledTestAcrossChoices(
        bool hero, bool repeated, bool changeWhilePending)
    {
        // Compiling the book snapshots the condition, not its truth value.
        // Changing the caller's syntax cannot select a different branch.
        var (runner, fields) = MutableEffectRunner("if", """
            {"test":{"inForm":{"player":"you","form":"hero"}},
             "then":{"choose":{"options":[
               {"heal":{"card":"you","amount":1}},
               {"draw":{"player":"you","count":1}}
             ]}},
             "else":{"choose":{"options":[
               {"heal":{"card":"you","amount":2}},
               {"draw":{"player":"you","count":2}}
             ]}}}
            """, repeated);
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(5);
        }, hero: hero, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        void ChangeTest() => fields["test"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
        {
            ["inForm"] = new AbilityValue.Map(new Dictionary<string, AbilityValue>
            {
                ["player"] = new AbilityValue.Word("you"),
                ["form"] = new AbilityValue.Word("alter-ego"),
            }),
        });
        if (!changeWhilePending) ChangeTest();

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(Question.Option, game.Pending!.Asking);
        if (changeWhilePending) ChangeTest();
        game.Resolve(Decision.Take(0));
        int healed = hero ? 1 : 2;
        Assert.Equal(5 - healed, world.Seats[0].IdentityCard.Damage);
        if (repeated)
        {
            Assert.Equal(Question.Option, game.Pending!.Asking);
            game.Resolve(Decision.Take(0));
            Assert.Equal(5 - 2 * healed, world.Seats[0].IdentityCard.Damage);
        }
        Assert.False(source!.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }
}
