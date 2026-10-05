using Marvel.Decisions;
using Marvel.Godot;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

/// <summary>Synthetic contracts distinguish blank play areas from expressed object targets.</summary>
public sealed class HandPlayDropTests
{
    [Fact]
    public void InvalidAttachmentTargetInsideThePlayerAreaPreservesThePriorDraft()
    {
        var composer = Composer(new Affordance(1, "Play", 10, 0, "Attach",
            new TargetRequest([20], 1, 1)), new Affordance(2, "Play", 11, 0, "Other"));
        composer.SelectAffordance(2);
        var play = Interaction(composer);
        Assert.Empty(play.Matches(10, 21, true, true));
        Assert.False(play.TrySelect(1, 10, 21, true, true));
        Assert.Equal(2, composer.Selected!.Id);
        Assert.Empty(composer.Targets);
        Assert.Single(play.Matches(10, null, true, true));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void OnlyExplicitDeferredTargetingCanBeginAPlayOnAnObject(bool deferred)
    {
        var composer = Composer(new Affordance(1, "Action", 10, 0, "Effect")
        {
            PlaysCard = true, DeferredTargetSelection = deferred,
        });
        var play = Interaction(composer);
        Assert.Equal(deferred, play.TrySelect(1, 10, 20, true, false));
        Assert.Equal(deferred, composer.Selected is not null);
        Assert.Empty(composer.Targets);
    }

    [Fact]
    public void ACurrentTargetRequestCannotBeBypassedByTheDeferredMarker()
    {
        var composer = Composer(new Affordance(1, "Play", 10, 0, "Effect",
            new TargetRequest([20], 1, 1)) { DeferredTargetSelection = true });
        Assert.Empty(Interaction(composer).Matches(10, 21, true, true));
        Assert.Null(composer.Selected);
    }

    [Fact]
    public void MultiplePlayOffersWaitForAnExactChoice()
    {
        var composer = Composer(new Affordance(1, "Play", 10, 0, "First"),
            new Affordance(2, "Play", 10, 0, "Second"));
        var play = Interaction(composer);
        Assert.Equal([1, 2], play.Matches(10, null, true, true).Select(offer => offer.Id));
        Assert.Null(composer.Selected);
        Assert.True(play.TrySelect(2, 10, null, true, true));
        Assert.Equal(2, composer.Selected!.Id);
    }

    [Fact]
    public void NonHandAndUnprojectedSourcesCannotBeginPlay()
    {
        var composer = Composer(new Affordance(1, "Play", 10, 0, "Effect"));
        Assert.Empty(Interaction(composer).Matches(10, null, false, true));
        var hidden = new BoardHandPlayInteraction(composer,
            new TableDraftBinding(composer, 1, 1, (_, _) => true), []);
        Assert.Empty(hidden.Matches(10, null, true, true));
        Assert.False(hidden.TrySelect(1, 10, null, true, true));
        Assert.Null(composer.Selected);
    }

    private static DecisionComposer Composer(params Affordance[] offers) => new(new Prompt(
        0, Question.TurnOption, TimingPriority.Untimed, "test", "Choose", false, offers));

    private static BoardHandPlayInteraction Interaction(DecisionComposer composer) => new(composer,
        new TableDraftBinding(composer, 1, 1, (_, _) => true),
        [.. composer.Prompt.Affordances.Select(offer => new AffordancePresentation(offer.Id,
            offer.Label, null, offer.Verb, offer.Label, offer.AnchorId, offer.AnchorPlayer,
            offer.Illegal, "", []) { AnchorKind = AffordanceAnchorKind.Card, TargetRequest = offer.Targets })]);
}
