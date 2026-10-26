using Marvel.Behavior.Run;
using Marvel.Rules.Events;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;

public sealed class TreacheryCancellationTranscriptTests
{
    [Fact]
    public void EnhancedSpiderSenseCancelsPrintedAndKeywordWhenRevealedEffects()
    {
        var suite = new CoreTranscriptSuite(RepositoryPaths.Root);
        TranscriptResult result = suite.RunScenario(
            "specs/behavior/core/treachery-cancellation.feature",
            "Enhanced Spider-Sense cancels Exhaustion and its Surge");

        Assert.Single(result.Events.OfType<WhenRevealedCanceled>());
    }
}
