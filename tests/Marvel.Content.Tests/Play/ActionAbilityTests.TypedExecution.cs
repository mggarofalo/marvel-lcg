using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CounterRemovalChoiceUsesCompiledEligibilityAndExecution(bool mutateBeforeAction)
    {
        var parsed = AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"exhaust":"this"},"effect":{"choose":{"options":[
                {"removeCounters":{"card":"this","counter":"marker","count":1}},
                {"draw":{"player":"you","count":1}}
              ]}}
            }]}]}
            """);
        var options = (AbilityValue.List)parsed.Abilities[0].Effect.Require("options");
        var original = AbilityNode.Of(options.Values[0]);
        var fields = ((AbilityValue.Map)original.Argument).Entries
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        var changedOption = new AbilityValue.Map(new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
        {
            ["removeCounters"] = new AbilityValue.Map(fields),
        });
        var choice = new AbilityNode("choose", new AbilityValue.Map(new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
        {
            ["options"] = new AbilityValue.List([changedOption, options.Values[1]]),
        }));
        var runner = new Marvel.Cards.Run.AbilityRunner(new AbilityBook(
            [parsed.Abilities[0] with { Effect = choice }], parsed.Authored));
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            source.PlaceTokens("c_marker", 2);
        }, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        int held = world.Seats[0].Hand.Cards.Count;

        // Engine choice: eligibility and execution read the same compiled
        // amount even if the caller edits the option while it is pending.
        if (mutateBeforeAction) fields["count"] = new AbilityValue.Number(99);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(Question.Option, game.Pending!.Asking);
        Assert.Contains(game.Pending.Affordances, option => option.Id == 0);
        fields["count"] = new AbilityValue.Number(99);
        game.Resolve(Decision.Take(0));

        Assert.False(source!.Ready);
        Assert.Equal(1, source.Tokens.GetValueOrDefault("c_marker"));
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("heal")]
    [InlineData("giveStatus")]
    [InlineData("grantUntil")]
    [InlineData("grantUntilExpiry")]
    public void MaintenanceEffectsUseCompiledValuesAndExpiry(string changedOperation)
    {
        var parsed = AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"exhaust":"this"},"effect":{"seq":[
                {"heal":{"card":"you","amount":2}},
                {"giveStatus":{"card":"you","status":"tough"}},
                {"grantUntil":{"card":"you","keyword":"attack","amount":2,"until":"EndOfRound"}}
              ]}
            }]}]}
            """);
        var nodes = ((AbilityValue.List)parsed.Abilities[0].Effect.Argument).Values.Select(AbilityNode.Of).ToList();
        var fields = nodes.ToDictionary(node => node.Kind,
            node => ((AbilityValue.Map)node.Argument).Entries.ToDictionary(
                pair => pair.Key, pair => pair.Value, StringComparer.Ordinal), StringComparer.Ordinal);
        var effect = new AbilityNode("seq", new AbilityValue.List(nodes.Select(node => (AbilityValue)
            new AbilityValue.Map(new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
            {
                [node.Kind] = new AbilityValue.Map(fields[node.Kind]),
            })).ToList()));
        var runner = new Marvel.Cards.Run.AbilityRunner(new AbilityBook(
            [parsed.Abilities[0] with { Effect = effect }], parsed.Authored));
        Card? source = null;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(4);
        }, hero: true, abilities: runner);
        var identity = world.Seats[0].IdentityCard;
        long attack = StateFields.Modified(world, identity, "attack", Cards, world.Players);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);

        // Engine-choice fixture: changing caller-owned argument maps cannot
        // alter the compiled heal, status, modifier amount or expiry.
        if (changedOperation == "giveStatus")
        {
            fields[changedOperation]["status"] = new AbilityValue.Word("confused");
        }
        else if (changedOperation == "grantUntilExpiry")
        {
            fields["grantUntil"]["until"] = new AbilityValue.Word("EndOfPhase");
        }
        else
        {
            fields[changedOperation]["amount"] = new AbilityValue.Number(9);
        }
        game.Resolve(Decision.Take(action.Id));

        Assert.False(source!.Ready);
        Assert.Equal(2, identity.Damage);
        Assert.True(Statuses.Has(world, identity, Statuses.Tough));
        Assert.False(Statuses.Has(world, identity, Statuses.Confused));
        Assert.Equal(attack + 2, StateFields.Modified(world, identity, "attack", Cards, world.Players));
        world.Effects.Expire(TimingPoints.EndOfPhase);
        Assert.Equal(attack + 2, StateFields.Modified(world, identity, "attack", Cards, world.Players));
        world.Effects.Expire(TimingPoints.EndOfRound);
        Assert.Equal(attack, StateFields.Modified(world, identity, "attack", Cards, world.Players));
    }

    [Rule("rr:printed")]
    [Theory]
    [InlineData("drawToHandSize", 9)]
    [InlineData("drawToPrintedHandSize", 6)]
    public void HandSizeDrawDistinguishesPrintedAndModifiedValues(string operation, int expected)
    {
        // "Printed" is the value "physically printed on the card". This
        // synthetic ability has the same player binding in both cases; only
        // whether the registered +3 modifier applies differs.
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$"""{"{{operation}}":"you"}""",
            cost: """{"exhaust":"this"}""");
        Card? source = null;
        var (game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        Hand(world, Physicals, 1);
        var identity = world.Seats[0].IdentityCard;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: "hand_size", Amount: 3, Card: source!.ObjectId, Affects: identity.ObjectId));
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.False(source.Ready);
        Assert.Equal(expected, world.Seats[0].Hand.Cards.Count);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void ImmediateEffectsResumeWithCompiledArguments(bool mutateBeforeAction)
    {
        var parsed = AbilityCatalog.Parse("""
            {"cards":[{"card":"01006","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "cost":{"exhaust":"this"},"effect":{"draw":{"player":"you","count":1}}
            }]}]}
            """);
        static AbilityValue.Map Operation(string name, AbilityValue argument) =>
            new(new Dictionary<string, AbilityValue>(StringComparer.Ordinal) { [name] = argument });
        var firstDraw = new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
        {
            ["player"] = new AbilityValue.Word("you"), ["count"] = new AbilityValue.Number(1),
        };
        var lastDraw = new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
        {
            ["player"] = new AbilityValue.Word("you"), ["count"] = new AbilityValue.Number(2),
        };
        var counters = new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
        {
            ["card"] = new AbilityValue.Word("this"),
            ["counter"] = new AbilityValue.Word("marker"), ["count"] = new AbilityValue.Number(2),
        };
        var effect = new AbilityNode("seq", new AbilityValue.List([
            Operation("draw", new AbilityValue.Map(firstDraw)),
            Operation("choose", new AbilityValue.Map(new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
            {
                ["options"] = new AbilityValue.List([
                    Operation("draw", new AbilityValue.Map(firstDraw)),
                    Operation("draw", new AbilityValue.Map(lastDraw)),
                ]),
            })),
            Operation("placeCounters", new AbilityValue.Map(counters)),
            Operation("draw", new AbilityValue.Map(lastDraw)),
        ]));
        var ability = parsed.Abilities[0] with { Effect = effect };
        var runner = new Marvel.Cards.Run.AbilityRunner(new AbilityBook([ability], parsed.Authored));
        Card? source = null;
        var (game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);

        // Engine choice: compiled arguments remain fixed before execution and
        // across a suspended choice. The final draw also occurs as an unchosen
        // option, so identical syntax must not conflate continuation addresses.
        void ChangeArguments()
        {
            firstDraw["count"] = new AbilityValue.Number(4);
            counters["count"] = new AbilityValue.Number(6);
            lastDraw["count"] = new AbilityValue.Number(5);
        }
        if (mutateBeforeAction) ChangeArguments();
        game.Resolve(Decision.Take(action.Id));

        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(0, source.Tokens.GetValueOrDefault("c_marker"));
        Assert.Equal(2, game.Pending!.Affordances.Count);
        ChangeArguments();
        game.Resolve(Decision.Take(0));

        Assert.Equal(2, source.Tokens.GetValueOrDefault("c_marker"));
        Assert.Equal(held + 4, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }
}
