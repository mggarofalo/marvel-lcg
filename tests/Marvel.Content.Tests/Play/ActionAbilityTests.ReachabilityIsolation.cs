using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed partial class ActionAbilityTests
{
    [Rule("rr:choose-option.2.2")]
    [Rule("rr:target.2.2")]
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ARejectedSiblingProbeDoesNotReplaceTheOuterSelection(bool reverseOptions)
    {
        // Player-card options cannot "require one or more targets" when
        // "there are no valid targets." The enemy option cannot supply the
        // player required by the suffix. Probing it must not replace the
        // outer identity retained by the explicit decline option.
        const string enemy = """{"chooseCard":{"from":{"query":"attackableEnemies"},"effect":{"seq":[]}}}""";
        const string retain = """{"seq":[]}""";
        string options = reverseOptions ? $"{retain},{enemy}" : $"{enemy},{retain}";
        var runner = Runner(AuthoredCards.AuntMay, "Action", $$$$$$"""
            {"seq":[
              {"chooseCard":{"from":{"query":"identities"},"effect":{"seq":[]}}},
              {"choose":{"options":[{{{{{{options}}}}}}]}},
              {"draw":{"player":"chosenPlayer","count":1}}
            ]}
            """, cost: """{"exhaust":"this"}""");
        Card? source = null;
        var (game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        string before = world.Digest().Canonical();
        Assert.Contains(runner.Actions(world, 0), option => option.Card == source!.ObjectId);
        Assert.Contains(runner.Actions(world, 0), option => option.Card == source!.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        int firstHand = world.Seats[0].Hand.Cards.Count;
        int secondHand = world.Seats[1].Hand.Cards.Count;

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(world.Seats[1].IdentityCard.ObjectId));

        var permitted = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(reverseOptions ? 0 : 1, permitted.Id);
        game.Resolve(Decision.Take(permitted.Id));

        Assert.Equal(firstHand, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(secondHand + 1, world.Seats[1].Hand.Cards.Count);
        Assert.False(source!.Ready);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.False(world.Agenda.IsBusy);
    }
}
