using Marvel.Decisions;
using Marvel.Godot;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

/// <summary>Synthetic offer shapes exercise gestures without asserting expansion playability.</summary>
public sealed class SourceTargetInteractionTests
{
    [Fact]
    public void SourceDestinationStagesExactOfferAndTargetWithoutAcceptingAnAnswer()
    {
        var composer = Composer(Offer(1, 10, [20, 21]));
        var interaction = Interaction(composer);

        Assert.True(interaction.CanDrag(10));
        Assert.Single(interaction.Matches(10, 21));
        Assert.True(interaction.TrySelect(1, 10, 21));
        Assert.Equal(1, composer.Selected!.Id);
        Assert.Equal([21], composer.Targets);
        Assert.True(composer.TryBuild(out _, out _));
    }

    [Fact]
    public void NoMatchOrUnavailableSourcePreservesTheExistingDraft()
    {
        var composer = Composer(Offer(1, 10, [20]), Offer(2, 11, [21]) with { Illegal = "Unavailable" });
        composer.SelectAffordance(1);
        var interaction = Interaction(composer);

        Assert.Empty(interaction.Matches(10, 22));
        Assert.Empty(interaction.Matches(12, 20));
        Assert.False(interaction.CanDrag(11));
        Assert.False(interaction.TrySelect(2, 11, 21));
        Assert.Equal(1, composer.Selected!.Id);
        Assert.Equal([20], composer.Targets);
    }

    [Fact]
    public void SeveralExactMatchesRemainUnselectedUntilThePlayerChoosesOne()
    {
        var composer = Composer(Offer(1, 10, [20]), Offer(2, 10, [20]));
        var interaction = Interaction(composer);

        Assert.Equal([1, 2], interaction.Matches(10, 20).Select(offer => offer.Id));
        Assert.Null(composer.Selected);
        Assert.True(interaction.TrySelect(2, 10, 20));
        Assert.Equal(2, composer.Selected!.Id);
    }

    [Fact]
    public void AllocationDropCanBeginAnIncompleteDraftThroughComposerBounds()
    {
        Affordance offer = Offer(1, 10, [20]) with
        {
            Targets = new TargetRequest([20], 3, 3, AllowRepeated: true),
        };
        var composer = Composer(offer);

        Assert.True(Interaction(composer).TrySelect(1, 10, 20));
        Assert.Equal([20], composer.Targets);
        Assert.False(composer.TryBuild(out _, out _));
        Assert.Equal(1, composer.Progress().Targets.Selected);
        Assert.True(Interaction(composer).TrySelect(1, 10, 20));
        Assert.Equal([20, 20], composer.Targets);
        Assert.True(Interaction(composer).TrySelect(1, 10, 20));
        Assert.Equal([20, 20, 20], composer.Targets);
        Assert.True(composer.TryBuild(out _, out _));
        Assert.False(Interaction(composer).TrySelect(1, 10, 20));
        Assert.Equal([20, 20, 20], composer.Targets);
    }

    [Fact]
    public void MultipleTargetDropsPreserveSelectionsAndRevalidatePayment()
    {
        var offer = Offer(1, 10, [20, 21, 22]) with
        {
            Targets = new TargetRequest([20, 21, 22], 2, 3),
            Costs = [new CostOption(10, "1", Sources: [new ResourceSource(30, "E")])],
        };
        var composer = Composer(offer);
        var interaction = Interaction(composer);
        Assert.True(interaction.TrySelect(1, 10, 20));
        composer.ToggleResource(30);
        Assert.True(interaction.TrySelect(1, 10, 20));
        Assert.Equal([30], composer.Resources);
        Assert.True(interaction.TrySelect(1, 10, 21));
        Assert.Equal([20, 21], composer.Targets);
        // The shared composer requires paying again after changing targets.
        Assert.Empty(composer.Resources);
        Assert.Equal(0, composer.SelectedCost);
    }

    [Fact]
    public void StaleGestureCannotReplaceTheDraft()
    {
        var composer = Composer(Offer(1, 10, [20]), Offer(2, 11, [21]));
        composer.SelectAffordance(1);
        var interaction = new BoardSourceTargetInteraction(composer,
            new TableDraftBinding(composer, 1, 1, (_, _) => false), Views(composer));

        Assert.False(interaction.TrySelect(2, 11, 21));
        Assert.Equal(1, composer.Selected!.Id);
        Assert.Equal([20], composer.Targets);
    }

    private static Affordance Offer(int id, int source, int[] targets) =>
        new(id, "Synthetic action", source, 0, "Use source", new TargetRequest(targets, 1, 1));

    private static DecisionComposer Composer(params Affordance[] offers) => new(new Prompt(
        0, Question.TurnOption, TimingPriority.Untimed, "test", "Choose", false, offers));

    private static BoardSourceTargetInteraction Interaction(DecisionComposer composer) => new(composer,
        new TableDraftBinding(composer, 1, 1, (_, _) => true), Views(composer));

    private static AffordancePresentation[] Views(DecisionComposer composer) =>
        [.. composer.Prompt.Affordances.Select(offer => new AffordancePresentation(offer.Id,
            offer.Label, null, offer.Verb, offer.Label, offer.AnchorId, offer.AnchorPlayer,
            offer.Illegal, "", []) { AnchorKind = AffordanceAnchorKind.Card, TargetRequest = offer.Targets })];
}
