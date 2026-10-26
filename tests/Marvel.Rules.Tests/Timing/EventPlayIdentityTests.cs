using System.Text.Json;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Rules.Tests.Timing;

public sealed class EventPlayIdentityTests
{
    [Fact]
    [Rule("rr:triggering-condition.1")]
    public void CompletingAnEventPlayDoesNotReopenAPermanentAbility()
    {
        // Each interrupt/response may trigger "only once per occurrence".
        // Ruling b144656def387a1e treats a recovered event's replay separately.
        var origin = new Occurrence(4, "WhenDamageWouldBeDealt");
        var permanent = new PendingAbility(7, AbilityType.Interrupt, 0);
        var played = new PendingAbility(8, AbilityType.Interrupt, 0);
        Assert.True(origin.Trigger(WindowKind.Interrupt, permanent));
        Assert.True(origin.Trigger(WindowKind.Interrupt, played));
        origin.CompleteEventPlay(played.Card);
        var replay = played with { EventPlay = origin.EventPlay(played.Card) };
        Assert.False(origin.MayTrigger(WindowKind.Interrupt, permanent));
        Assert.False(origin.MayTrigger(WindowKind.Interrupt, played));
        Assert.True(origin.Trigger(WindowKind.Interrupt, replay));
        Assert.False(origin.Trigger(WindowKind.Interrupt, replay));
    }

    [Fact]
    public void EventPlayResolutionIdentitySurvivesItsDataRoundtrip()
    {
        // Session saves reconstruct these runtime facts from accepted decisions.
        // Their diagnostic JSON retains distinct per-play resolution addresses.
        var first = new PendingAbility(8, AbilityType.Interrupt, 0);
        var second = first with { EventPlay = 1 };
        second = JsonSerializer.Deserialize<PendingAbility>(JsonSerializer.Serialize(second));
        Assert.Equal(1, second.EventPlay);
        var origin = new Occurrence(4, "WhenDamageWouldBeDealt");
        origin.Resolution.BeginCard(first.Card, [first]);
        origin.Resolution.Resolve(first);
        origin.Resolution.Complete(first);
        origin.Resolution.BeginCard(second.Card, [second]);
        origin.Resolution.Complete(second);
        var entries = JsonSerializer.Deserialize<ResolutionEntry[]>(JsonSerializer.Serialize(origin.Resolutions))!;
        var restored = new Occurrence(4, ["WhenDamageWouldBeDealt"], Resolutions: entries);
        Assert.Equal(ResolutionStatus.Resolved, restored.Resolution.StatusOf(first));
        Assert.Equal(ResolutionStatus.Unresolved, restored.Resolution.StatusOf(second));
        Assert.Equal(ResolutionStatus.Unresolved, restored.Resolution.CardStatus(second.Card));
        Assert.Equal(4, restored.Resolutions.Count);
        Assert.DoesNotContain("EventPlay", JsonSerializer.Serialize(first), StringComparison.Ordinal);
    }
}
