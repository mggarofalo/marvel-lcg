using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

// The timeline's round and phase come from the engine's phase-boundary events, never from their sentences.
public sealed class TimelineRoundTrackerTests
{
    private static TimelineOccurrence Boundary(int round, string phase) => new(0,
        new EventPresentation($"Round {round}: {phase} phase.", "", [], EventMotionKind.State)
        {
            Activity = new EventActivityDescriptor([], []) { PhaseBoundary = true, Round = round, Phase = phase },
        });

    private static TimelineOccurrence Other(string summary, int? round = null, string phase = "") => new(0,
        new EventPresentation(summary, "", [], EventMotionKind.State)
        {
            Activity = new EventActivityDescriptor([], []) { Round = round, Phase = phase },
        });

    [Fact]
    public void TheShownPointTakesTheLastPhaseBoundaryAtOrBeforeIt()
    {
        TimelineOccurrence[] entries =
        [
            Boundary(1, "Player"), Other("Spider-Man attacks."), Boundary(1, "Villain"),
            Other("Rhino attacks.", 1, "Villain"), Boundary(2, "Player"),
        ];

        Assert.Equal((1, "Player"), TimelineRoundTracker.At(entries, 1));
        Assert.Equal((1, "Villain"), TimelineRoundTracker.At(entries, 3));
        Assert.Equal((2, "Player"), TimelineRoundTracker.At(entries, 4));
        Assert.Equal((2, "Player"), TimelineRoundTracker.At(entries, 99));
    }

    [Fact]
    public void ARoundIsUnknownWithoutABoundaryEvenWhenASentenceNamesIt()
    {
        TimelineOccurrence[] entries =
        [
            new(3, new EventPresentation("Round 4: Player phase.", "Earlier action summary", [], EventMotionKind.State)),
            Other("A step names its phase", 4, "Player"),
        ];

        Assert.Null(TimelineRoundTracker.At(entries, 1));
        Assert.Null(TimelineRoundTracker.At([], -1));
    }

    [Theory]
    [InlineData("Player", 1)]
    [InlineData("Villain", 2)]
    public void ThePhaseFillsItsPipsInRoundOrder(string phase, int filled)
    {
        TrackerState state = TimelineRoundTracker.State(3, phase);

        Assert.Equal(("Round 3", filled, 2), (state.Label, state.Filled, state.Total));
        Assert.False(state.Numbered);
        Assert.Contains($"{phase.ToLowerInvariant()} phase", state.Tooltip);
    }
}
