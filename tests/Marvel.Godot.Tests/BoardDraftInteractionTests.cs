using Godot;
using Marvel.Decisions;
using Marvel.Godot;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class BoardDraftInteractionTests
{
    [Fact]
    public void DraggingOneOfDuplicateHandTitlesUsesItsExactStableAffordanceAndDraft()
    {
        var composer = Composer(
            new Affordance(3, "Play", 19, 0, "Web-Shooter",
                new TargetRequest([10], 1, 1),
                [new CostOption(19, "1", Sources: [new ResourceSource(1, "Y")])]),
            new Affordance(4, "Play", 20, 0, "Web-Shooter"));
        var interaction = Interaction(composer);

        Assert.Equal(BoardDraftMutation.Affordance, interaction.TryPlay(19, true, true));
        Assert.Equal(3, composer.Selected!.Id);
        Assert.Equal(BoardDraftMutation.Target, interaction.TryActivate(10, false));
        Assert.Equal(BoardDraftMutation.Generator, interaction.TryActivate(1, false));
        Assert.Equal([1], composer.Resources);
    }

    [Fact]
    public void HandClickAndOutsideDropLeaveTheDraftUntouched()
    {
        var composer = Composer(new Affordance(3, "Play", 19, 0, "Visible"));
        var interaction = Interaction(composer);

        Assert.Equal(BoardDraftMutation.None, interaction.TryActivate(19, true));
        Assert.Equal(BoardDraftMutation.None, interaction.TryPlay(19, true, false));
        Assert.Null(composer.Selected);
    }

    [Fact]
    public void OnlyOneLegalAnchoredActionCanBePreparedDirectly()
    {
        var one = Composer(new Affordance(3, "Use", 55, 0, "One action"));
        var oneInteraction = Interaction(one);
        Assert.Equal(BoardDraftMutation.Affordance, oneInteraction.TryActivate(55, false));
        Assert.Equal(3, one.Selected!.Id);

        var several = Composer(
            new Affordance(3, "Use", 55, 0, "First action"),
            new Affordance(4, "Use", 55, 0, "Second action"));
        var severalInteraction = Interaction(several);
        Assert.Equal(BoardDraftMutation.None, severalInteraction.TryActivate(55, false));
        Assert.Null(several.Selected);
    }

    [Fact]
    public void IllegalAndUnregisteredCardIdsCannotPrepareAnAction()
    {
        var composer = Composer(new Affordance(3, "Use", 55, 0, "Unavailable", Illegal: "Guard"));
        var interaction = Interaction(composer);

        Assert.Equal(BoardDraftMutation.None, interaction.TryActivate(55, false));
        Assert.Equal(BoardDraftMutation.None, interaction.TryActivate(56, false));
        Assert.Null(composer.Selected);
    }

    [Fact]
    public void PointerAndKeyboardCardActivationUseTheSameTargetAndGeneratorOperation()
    {
        DecisionComposer pointer = TargetAndGeneratorComposer();
        DecisionComposer keyboard = TargetAndGeneratorComposer();

        Assert.Equal(BoardDraftMutation.Target,
            Interaction(pointer).TryActivate(10, false));
        Assert.Equal(BoardDraftMutation.Target,
            Interaction(keyboard).TryActivate(10, false));
        Assert.Equal(BoardDraftMutation.Generator,
            Interaction(pointer).TryToggleGenerator(1));
        Assert.Equal(BoardDraftMutation.Generator,
            Interaction(keyboard).TryToggleGenerator(1));
        Assert.Equal(pointer.Targets, keyboard.Targets);
        Assert.Equal(pointer.Resources, keyboard.Resources);
    }

    [Fact]
    public void ExplicitHandGeneratorControlCanPayWithoutMakingHandBodyClickPlay()
    {
        DecisionComposer composer = TargetAndGeneratorComposer();
        var interaction = Interaction(composer);

        Assert.Equal(BoardDraftMutation.None, interaction.TryActivate(1, true));
        Assert.Equal(BoardDraftMutation.Generator, interaction.TryToggleGenerator(1));
        Assert.Equal([1], composer.Resources);
    }

    [Theory]
    [InlineData(9.99f, false)]
    [InlineData(10f, true)]
    public void PointerThresholdSeparatesClickFromDrag(float distance, bool expectedDrag) =>
        Assert.Equal(expectedDrag, CardPointerGestureRouter.IsDrag(Vector2.Zero, new Vector2(distance, 0)));

    private static DecisionComposer TargetAndGeneratorComposer()
    {
        var composer = Composer(new Affordance(3, "Play", 19, 0, "Visible",
            new TargetRequest([10], 1, 1),
            [new CostOption(19, "1", Sources: [new ResourceSource(1, "Y")])]));
        composer.SelectAffordance(3);
        return composer;
    }

    private static DecisionComposer Composer(params Affordance[] offers) => new(new Prompt(
        0, Question.TurnOption, TimingPriority.Untimed, "test", "Choose", false, offers));

    private static TableDraftBinding Binding(DecisionComposer composer) =>
        new(composer, 1, 1, (generation, revision) => generation == 1 && revision == 1);

    private static BoardDraftInteraction Interaction(DecisionComposer composer) =>
        new(composer, Binding(composer), composer.Prompt.Affordances.Select(Visible).ToArray());

    private static AffordancePresentation Visible(Affordance offer) => new(
        offer.Id, offer.Label, null, offer.Verb, offer.Label, offer.AnchorId,
        offer.AnchorPlayer, offer.Illegal, string.Empty, [])
    {
        Source = new AffordanceSourceDescriptor(
            AffordanceAnchorKind.Card, offer.AnchorId, null, offer.AnchorPlayer),
    };
}
