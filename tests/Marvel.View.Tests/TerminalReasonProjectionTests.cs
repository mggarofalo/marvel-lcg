using Marvel.Rules.Play;
using Xunit;

namespace Marvel.View.Tests;

public sealed class TerminalReasonProjectionTests
{
    [Theory]
    [InlineData(Outcome.PlayersLose, PlayerLossReason.AllPlayersEliminated, "All players were eliminated.")]
    [InlineData(Outcome.PlayersLose, PlayerLossReason.EncounterCardsExhausted, "The encounter deck and discard pile are both empty.")]
    [InlineData(Outcome.VillainWins, PlayerLossReason.Unspecified, "The final main scheme was completed.")]
    public void BothViewerPoliciesRetainThePublicTerminalCause(Outcome outcome, PlayerLossReason reason, string expected)
    {
        var world = VisibilityFixture.Board();
        world.Finish(outcome, reason);
        foreach (IVisibilityPolicy policy in new IVisibilityPolicy[]
                 { new PermissiveVisibilityPolicy(), new RestrictedVisibilityPolicy(0) })
        {
            var visible = WorldProjection.For(world, null, [], policy.Authorize(null, world.Players)).World;
            Assert.Equal(reason, visible.LossReason);
            Assert.Equal(expected, PendingSituationPresentation.Context(visible));
            Assert.Equal(expected, OutcomeDescription.Result(visible.Outcome, visible.LossReason));
            Assert.Equal(expected, Assert.Single(EventCuePlanner.Plan([], visible, Outcome.Unfinished).Cues).Summary);
            var action = new ActionHistoryFacts(1, "Spider-Man", "end_phase", "PlayerTurn",
                Game.EndPhaseVerb, "End turn", null, [], [], outcome);
            Assert.EndsWith(expected, ActionHistoryPresenter.PresentEntry(action, [], visible).Summary);
        }
    }
}
