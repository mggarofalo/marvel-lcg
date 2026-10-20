using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Timing;

public sealed class NativeAbilityIdentityTests
{
    [Rule("rr:retaliate-x.1")]
    [Rule("rr:triggering-condition.1")]
    [Fact]
    public void RetaliateAndPrintedForcedResponseRemainDistinctOnTheSameCard()
    {
        // rr:retaliate-x.1: "equivalent to the following triggered ability".
        // rr:triggering-condition.1: "only once per occurrence".
        // The engine chooses an explicit identity namespace for native keywords.
        var occurrence = new Occurrence(1, "AttackEnds");
        var printed = new PendingAbility(7, AbilityType.ForcedResponse, -1);
        var retaliate = printed with { Native = NativeAbilityKind.Retaliate };
        Assert.True(occurrence.Trigger(WindowKind.Response, printed));
        Assert.True(occurrence.MayTrigger(WindowKind.Response, retaliate));
        Assert.True(occurrence.Trigger(WindowKind.Response, retaliate));
        Assert.False(occurrence.Trigger(WindowKind.Response, retaliate));
        Assert.False(occurrence.MayTrigger(WindowKind.Response, printed));
        occurrence.Resolution.Begin(printed);
        occurrence.Resolution.Begin(retaliate);
        occurrence.Resolution.Resolve(retaliate);
        occurrence.Resolution.Complete(retaliate);
        Assert.Equal(ResolutionStatus.Pending, occurrence.Resolution.StatusOf(printed));
        Assert.Equal(ResolutionStatus.Resolved, occurrence.Resolution.StatusOf(retaliate));
        occurrence.Resolution.Cancel(printed);
        Assert.Equal(ResolutionStatus.Unresolved, occurrence.Resolution.StatusOf(printed));
        Assert.Equal(ResolutionStatus.Resolved, occurrence.Resolution.StatusOf(retaliate));
        Assert.Equal(2, occurrence.Resolutions.Count);
    }
}
