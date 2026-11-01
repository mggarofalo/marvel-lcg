using Marvel.Rules.Events;
using Xunit;

namespace Marvel.Session.Tests;

public sealed class TopologyEventJournalTests
{
    [Fact]
    public void TheJournalOmitsOccurrenceTimeMembership()
    {
        // Membership is presentation evidence, like subjects: recording it would change every
        // saved event list that holds a topology change.
        var joined = JournalJson.Event(new PlayAreaJoined(0, 3) { Members = [0, 1], Trigger = "join" });
        var detached = JournalJson.Event(new PlayAreaDetached(-1, 0) { Members = [0], Trigger = "split" });

        Assert.Equal(JournalJson.Event(new PlayAreaJoined(0, 3) { Trigger = "join" }).GetRawText(), joined.GetRawText());
        Assert.Equal(JournalJson.Event(new PlayAreaDetached(-1, 0) { Trigger = "split" }).GetRawText(), detached.GetRawText());
        Assert.DoesNotContain("members", joined.GetRawText(), StringComparison.Ordinal);
    }
}
