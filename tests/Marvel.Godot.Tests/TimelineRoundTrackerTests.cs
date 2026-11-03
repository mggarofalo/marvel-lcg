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

    [Fact]
    public void ARestoredSummaryAfterABoundaryMakesTheRoundUnknown()
    {
        // Seat B synchronizes during round 2: the summary stands for occurrences, boundaries among them, it lacks.
        TimelineOccurrence[] entries =
        [
            Boundary(1, "Villain"),
            new(4, new EventPresentation("Spider-Man attacks.", "Earlier action summary", [], EventMotionKind.State))
            {
                Restored = true,
            },
            Other("Spider-Man thwarts."),
        ];

        Assert.Equal((1, "Villain"), TimelineRoundTracker.At(entries, 0));
        Assert.Null(TimelineRoundTracker.At(entries, 1));
        Assert.Null(TimelineRoundTracker.At(entries, 2));
        Assert.Equal((2, "Player"), TimelineRoundTracker.At([.. entries, Boundary(2, "Player")], 3));
    }

    [Fact]
    public void WhileFollowingTheTrackerReadsTheBeatPlayingNowNotTheNewestEntry()
    {
        TimelineOccurrence reveal = Other("Rhino reveals Hard to Keep Down.", 1, "Villain");
        TimelineOccurrence[] entries = [Boundary(1, "Villain"), reveal, Boundary(2, "Player")];

        int? playing = TimelineRoundTracker.ShownIndex(entries, following: true, selected: 2, active: reveal.Event);

        Assert.Equal(1, playing);
        Assert.Equal((1, "Villain"), TimelineRoundTracker.At(entries, playing!.Value));
        Assert.Equal(2, TimelineRoundTracker.ShownIndex(entries, following: true, selected: 0, active: null));
        Assert.Equal(0, TimelineRoundTracker.ShownIndex(entries, following: false, selected: 0, active: reveal.Event));
        // A beat the archive does not hold keeps the shown round until playback settles.
        Assert.Null(TimelineRoundTracker.ShownIndex(entries, following: true, selected: 2,
            active: new EventPresentation("Not archived.", "", [], EventMotionKind.State)));
    }

    [Theory]
    [InlineData(400, true, true)]
    [InlineData(300, false, true)]
    [InlineData(200, false, false)]
    public void TheTrackerYieldsThePhaseChipThenItselfToFitTheSeam(float width, bool phase, bool tracker)
    {
        // Minimum widths: both 350, the tracker alone 250, neither 150.
        float Minimum(bool withPhase, bool withTracker) => 150 + (withTracker ? 100 : 0) + (withPhase ? 100 : 0);

        Assert.Equal((phase, tracker), TimelineRoundTracker.Yield(Minimum, width));
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
