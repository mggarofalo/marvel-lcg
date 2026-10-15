using Marvel.Godot;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class EventActivitySequenceTests
{
    [Fact]
    public void SlowFramesDoNotSkipCommittedConsequences()
    {
        // Synthetic visibility-safe response: the animation consumes descriptors, not game state.
        EventPresentation[] entries = [Cue("Threat increased."), Cue("A boost resolved."), Cue("Rhino schemed.")];
        var sequence = new EventActivitySequence();
        sequence.Start(entries, animate: true);
        Assert.Equal(entries[0], sequence.Current);
        Assert.False(sequence.Advance(0.1));
        Assert.True(sequence.Advance(60));
        Assert.Equal(entries[1], sequence.Current);
        Assert.True(sequence.Active);
        Assert.False(sequence.Advance(0.1));
        sequence.Advance(60);
        Assert.Equal(entries[2], sequence.Current);
        Assert.True(sequence.Active);
        sequence.Advance(60);
        Assert.False(sequence.Active);
        Assert.Equal(entries, sequence.Entries);
    }

    [Fact]
    public void ReducedMotionRetainsEveryOrderedResultWithoutWaiting()
    {
        EventPresentation[] entries = [Cue("Threat increased."), Cue("Rhino schemed.")];
        var sequence = new EventActivitySequence();
        sequence.Start(entries, animate: false);
        Assert.False(sequence.Active);
        Assert.Equal(entries, sequence.Entries);
        Assert.False(sequence.Advance(10));
        sequence.Start(entries, animate: true);
        sequence.Complete();
        Assert.False(sequence.Active);
        Assert.Equal(entries, sequence.Entries);
    }

    [Fact]
    public void ReplacementAndSynchronizationDiscardPendingActivity()
    {
        var sequence = new EventActivitySequence();
        sequence.Start([Cue("Old damage."), Cue("Old boost.")], animate: true);
        EventPresentation replacement = Cue("New result.");
        sequence.Start([replacement], animate: true);
        Assert.Equal(replacement, sequence.Current);
        Assert.Single(sequence.Entries);
        sequence.Clear();
        Assert.Null(sequence.Current);
        Assert.Empty(sequence.Entries);
        Assert.False(sequence.Active);
        Assert.False(sequence.Advance(100));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(4)]
    [InlineData(8)]
    public void MeaningfulResultsKeepReadableTimeWhenTheResponseGrows(int count)
    {
        var sequence = new EventActivitySequence();
        sequence.Start(Enumerable.Range(0, count).Select(index => Cue($"Event {index}")).ToArray(), true);
        var observed = new HashSet<int>();
        double seconds = 0;
        while (sequence.Active && seconds < count)
        {
            observed.Add(sequence.Index);
            sequence.Advance(1.0 / 120);
            seconds += 1.0 / 120;
        }
        Assert.False(sequence.Active);
        Assert.Equal(count, observed.Count);
        Assert.InRange(seconds, count * .65, count * .9);
    }

    [Theory]
    [InlineData(30)]
    [InlineData(60)]
    public void TypedCleanupReceiptsKeepThePlaybackBudgetAndEveryOrderedFact(int count)
    {
        var sequence = new EventActivitySequence();
        EventPresentation[] cleanup = [.. Enumerable.Range(0, count).Select(Movement)];
        EventPresentation terminal = new("The players lost the game.", "Game outcome", [], EventMotionKind.Terminal);
        sequence.Start([.. cleanup, terminal], true);
        Assert.Contains(count.ToString(), sequence.Current!.Summary, StringComparison.Ordinal);
        Assert.Equal(cleanup, sequence.Current.Details);
        Assert.Equal(cleanup, EventActivityCardFacts.Facts(sequence.Current));
        var observed = new List<EventPresentation>();
        for (int frame = 0; frame < 360 && sequence.Active; frame++)
        {
            if (observed.Count == 0 || observed[^1] != sequence.Current) observed.Add(sequence.Current!);
            sequence.Advance(1.0 / 120);
        }
        Assert.False(sequence.Active);
        Assert.Equal(2, observed.Count);
        Assert.Equal(2, sequence.BeatCount);
        Assert.Equal(sequence.BeatCount - 1, sequence.Index);
        Assert.Same(terminal, observed[^1]);
        Assert.Equal([.. cleanup, terminal], sequence.Entries);
    }

    [Fact]
    public void CleanupDoesNotGroupAcrossMeaningfulResultsOrWithoutAnOutcome()
    {
        EventPresentation damage = Cue("Rhino takes damage") with { Motion = EventMotionKind.Damage,
            Activity = new([new(7, "Rhino", ActivitySubjectRole.Subject)], [new("health", 4, 0)]) };
        EventPresentation reveal = Cue("A new encounter is revealed") with { Motion = EventMotionKind.Flip,
            Activity = new([], []) { Operation = "Reveal" } };
        EventPresentation status = Movement(8) with { Motion = EventMotionKind.Status };
        EventPresentation ready = Movement(9) with
            { Activity = new([], []) { PhaseStep = true, Batch = 4, Operation = "Ready" } };
        EventPresentation terminal = new("Game over", "", [], EventMotionKind.Terminal);
        EventPresentation[] events = [Movement(1), Movement(2), damage, reveal, status, ready, Movement(3), Movement(4)];
        Assert.Equal(events, EventActivityBeats.From(events));
        var beats = EventActivityBeats.From([.. events, terminal]);
        Assert.Equal(events.Take(6), beats.Take(6));
        Assert.Equal(events.TakeLast(2), beats[^2].Details);
        Assert.Same(terminal, beats[^1]);
    }

    [Fact]
    public void CompletingDecorativePlaybackDoesNotReplayOrDiscardTheAccount()
    {
        EventPresentation[] events = [Cue("First result"), Cue("Second result")];
        var sequence = new EventActivitySequence();
        sequence.Start(events, true);
        sequence.Advance(.3);
        sequence.Complete();
        Assert.False(sequence.Active);
        Assert.Null(sequence.Attention);
        Assert.Same(events[^1], sequence.Current);
        Assert.Equal(events, sequence.Entries);
        Assert.False(sequence.Advance(60));
    }

    [Fact]
    public void TerminalCleanupUsesMovementEndpointsWithoutGuessingFromCopy()
    {
        EventPresentation moved = Movement(1) with
        {
            Summary = "An arbitrary localized label",
            Activity = new([new(1, "Card", ActivitySubjectRole.Subject)], [])
                { From = "player deck", To = "removed" },
        };
        EventPresentation terminal = new("Game over", "", [], EventMotionKind.Terminal);
        var beats = EventActivityBeats.From([moved, moved, terminal]);
        Assert.Equal(2, beats.Count);
        Assert.Equal([moved, moved], beats[0].Details);
        Assert.Contains("1 card moved", beats[0].Summary, StringComparison.Ordinal);
        var unspecified = moved with { Activity = moved.Activity! with { From = "", To = "" } };
        Assert.Equal([unspecified, unspecified, terminal], EventActivityBeats.From([unspecified, unspecified, terminal]));
        var unknown = moved with { Activity = moved.Activity! with { Operation = "FutureReveal" } };
        Assert.Equal([unknown, unknown, terminal], EventActivityBeats.From([unknown, unknown, terminal]));
        var eliminated = moved with { Activity = moved.Activity! with { Operation = "Eliminate" } };
        Assert.Equal(2, EventActivityBeats.From([eliminated, eliminated, terminal]).Count);
        Assert.InRange(EventActivityTiming.Duration(moved), .3, .45);
        Assert.InRange(EventActivityTiming.Duration(moved with
            { Activity = moved.Activity! with { Operation = "Discard" } }), .65, .9);
    }

    [Fact]
    public void AttackCausePrecedesDamageAndNoBoardAttentionSurvivesCompletion()
    {
        var damage = new EventPresentation("Spider-Man HP 10 → 8", "", [2], EventMotionKind.Damage);
        var completed = new EventPresentation("Rhino attack ended", "", [1, 2], EventMotionKind.Attack)
        { Activity = new EventActivityDescriptor([
            new(1, "Rhino", ActivitySubjectRole.Source), new(2, "Spider-Man", ActivitySubjectRole.Target)],
            [new("attack_damage", null, 2)]) { Operation = "Attack_Completed" } };
        var sequence = new EventActivitySequence();
        sequence.Start(EventPlaybackPlan.From([damage, completed]), true);
        Assert.Equal("Rhino → Spider-Man", sequence.Attention!.Summary);
        Assert.Empty(sequence.Attention.Activity!.Quantities);
        sequence.Advance(1);
        Assert.Same(damage, sequence.Attention);
        sequence.Advance(1);
        Assert.Same(completed, sequence.Current);
        Assert.Null(sequence.Attention);
        sequence.Advance(1);
        Assert.False(sequence.Active);
        Assert.Null(sequence.Attention);
        Assert.Equal(3, sequence.Entries.Count);
        var grouped = damage with { Details = [damage, completed] };
        Assert.Equal("Rhino → Spider-Man", EventPlaybackPlan.From([grouped])[0].Summary);
    }

    [Fact]
    public void ReducedMotionDoesNotRetainAttackOverTheNextDecision()
    {
        var sequence = new EventActivitySequence();
        sequence.Start([new("Attack ended", "", [], EventMotionKind.Attack)], false);
        Assert.Null(sequence.Attention);
        Assert.Single(sequence.Entries);
    }

    [Fact]
    public void BasicAttackUsesExplicitReceiptRolesAndOnlyPriorAuthorizedFaces()
    {
        var hero = new CardDescriptor(1, CardBack.Player, true, true, -1,
            new CardFaceDescriptor("hero", "Spider-Man", "", Marvel.Rules.State.CardKind.Hero, new Dictionary<string, long>()));
        var villain = new CardDescriptor(2, CardBack.Encounter, true, true, -1,
            new CardFaceDescriptor("enemy", "Rhino", "", Marvel.Rules.State.CardKind.EncounterVillain, new Dictionary<string, long>()));
        var world = new WorldDescriptor([], [new(1, "HeroArea", 0, -1, [hero, villain], [])], [],
            Marvel.Rules.Play.Outcome.Unfinished);
        var receipt = new DecisionReceiptContext("Attack", [1, 2, 99])
            { ActionVerb = "Attack", SourceCardId = 1, TargetCardIds = [2] };
        EventPresentation damage = Cue("Rhino takes damage");
        var intro = EventPlaybackPlan.From([damage], receipt, world)[0];
        Assert.Equal("Spider-Man → Rhino", intro.Summary);
        Assert.Equal([1, 2], intro.Anchors);
        Assert.Empty(intro.Activity!.Quantities);
        var sequence = new EventActivitySequence();
        sequence.Start(EventPlaybackPlan.From([damage], receipt, world), true);
        Assert.False(sequence.Advance(.45));
        Assert.True(sequence.Advance(.04));
        Assert.Same(damage, sequence.Attention);
        Assert.False(sequence.Advance(.6));
        sequence.Advance(.2);
        Assert.False(sequence.Active);
        var concealed = world with { Areas = [new(1, "HeroArea", 0, -1, [hero, villain with { Face = null }], [])] };
        Assert.Equal([damage], EventPlaybackPlan.From([damage], receipt, concealed));
    }

    private static EventPresentation Cue(string summary) => new(summary, "Result", [], EventMotionKind.Threat);

    private static EventPresentation Movement(int card) => new($"Moved {card}", "", [card], EventMotionKind.Move)
    { Activity = new([new(card, "Card", ActivitySubjectRole.Subject)], []) { Operation = "Remove" } };
}
