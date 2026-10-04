using Marvel.Rules.Play;
using Marvel.View;
using Xunit;

namespace Marvel.View.Tests;

public sealed class HandCommitmentHistoryTests
{
    [Theory]
    [InlineData(Game.ResolveMulligans, "Mulligan", "Spider-Man chose their opening hand.")]
    [InlineData(Game.EndPhaseVerb, "EndPhase", "Spider-Man finished choosing hand discards.")]
    public void HandChoicesNameTheCompletedCommitment(string verb, string phase, string expected)
    {
        var facts = new ActionHistoryFacts(1, "Spider-Man", "phase_step", phase,
            verb, verb, null, [], []);

        Assert.Equal(expected, ActionHistoryPresenter.Present(facts));
    }
}
