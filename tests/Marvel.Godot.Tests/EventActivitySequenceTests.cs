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
    [InlineData(1, 0.5)]
    [InlineData(4, 2.6)]
    [InlineData(30, 3.0)]
    public void PlaybackBudgetKeepsDecisionsResponsiveWithoutDroppingBeats(int count, double limit)
    {
        var sequence = new EventActivitySequence();
        sequence.Start(Enumerable.Range(0, count).Select(index => Cue($"Event {index}")).ToArray(), true);
        var observed = new HashSet<int>();
        double seconds = 0;
        while (sequence.Active && seconds < limit)
        {
            observed.Add(sequence.Index);
            sequence.Advance(1.0 / 120);
            seconds += 1.0 / 120;
        }
        Assert.False(sequence.Active);
        Assert.Equal(count, observed.Count);
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
        Assert.False(sequence.Advance(.19));
        Assert.True(sequence.Advance(.02));
        Assert.Same(damage, sequence.Attention);
        sequence.Advance(.21);
        Assert.False(sequence.Active);
        var concealed = world with { Areas = [new(1, "HeroArea", 0, -1, [hero, villain with { Face = null }], [])] };
        Assert.Equal([damage], EventPlaybackPlan.From([damage], receipt, concealed));
    }

    private static EventPresentation Cue(string summary) => new(summary, "Result", [], EventMotionKind.Threat);
}
