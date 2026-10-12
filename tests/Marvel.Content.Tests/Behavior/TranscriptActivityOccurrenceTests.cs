using Marvel.Behavior.Run;
using Marvel.Rules.Events;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Behavior;

public sealed class TranscriptActivityOccurrenceTests
{
    [Rule("rr:discard.4")]
    [Fact]
    public void DiscardCountObservesCardOccurrencesAndRetainsTheInstructionReceipt()
    {
        // "Place those cards in the appropriate discard pile one at a time
        // (without changing the order)." The transcript asserts three occurrences.
        // Engine-choice activity receipt describes that instruction independently.
        var suite = new CoreTranscriptSuite(RepositoryPaths.Root);
        TranscriptResult result = suite.RunScenario("specs/behavior/core/discard.feature",
            "A singular effect discards deck cards one at a time without reordering them");
        Assert.Equal(3, result.Events.OfType<CardsMoved>().Count(item => item.Verb == "Discard"));
        ActivityCompleted completed = Assert.Single(result.Events.OfType<ActivityCompleted>());
        Assert.Equal("Discard", completed.Verb);
        Assert.Equal(3, Assert.Single(completed.Participants).Count);
    }
}
