using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingActivationOtherwiseTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    [Rule("rr:otherwise.1.2")]
    public void ActionOtherwiseUsesCompletedActivationReceipt(bool target, bool stunned)
    {
        // Synthetic action isolates otherwise admission and its actual suspended receipt.
        // rr:otherwise.1.2 requires the preceding effect to have "not resolved at all".
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01007","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "effect":{"otherwise":{"effect":{"enemySchemes":{"enemies":{"query":"minions"},"against":"you"}},
                "otherwise":{"placeCounters":{"card":"you","counter":"observed","count":1}}}}
            }]}]}
            """));
        Card? source = null;
        var (_, world) = Playing(board =>
        {
            source = InPlay(board, "01007");
            if (!target) return;
            var minion = board.CreateCard("01102", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            if (stunned) Statuses.Give(board, minion, Statuses.Confused);
        }, abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source!.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(!target || stunned ? 1 : 0, world.Seats[0].IdentityCard.Tokens.GetValueOrDefault("c_observed"));
    }
}
