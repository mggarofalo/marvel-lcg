using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
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
