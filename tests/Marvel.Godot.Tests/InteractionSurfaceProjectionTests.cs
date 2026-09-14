using Godot;
using Marvel.Decisions;
using Marvel.Godot;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class InteractionSurfaceProjectionTests
{
    [Fact]
    public void CuesComeOnlyFromTheVisiblePromptAndCurrentDraft()
    {
        var composer = Composer(new Affordance(7, "Play", 19, 0, "Visible",
            new TargetRequest([10], 1, 1),
            [new CostOption(19, "1", Sources: [new ResourceSource(1, "Y")])]),
            new Affordance(8, "Use", 20, 0, "Unavailable", Illegal: "Exhausted"));
        composer.SelectAffordance(7);
        composer.AddTarget(10);
        composer.ToggleResource(1);
        PromptPresentation prompt = Prompt(composer.Prompt, [
            Visible(7, 19), Visible(8, 20, illegal: "Exhausted")],
            target: new TargetRequest([10], 1, 1),
            costs: [new CostOption(19, "1", Sources: [new ResourceSource(1, "Y")])]);

        IReadOnlyDictionary<int, CardInteractionCue> cues =
            BoardInteractionCueProjection.From(composer, prompt);

        Assert.Equal(CardInteractionCue.OfferedAction, cues[19]);
        Assert.Equal(CardInteractionCue.Unavailable, cues[20]);
        Assert.Equal(CardInteractionCue.LegalTarget | CardInteractionCue.SelectedTarget, cues[10]);
        Assert.Equal(CardInteractionCue.LegalGenerator | CardInteractionCue.SelectedGenerator, cues[1]);
        Assert.DoesNotContain(99, cues.Keys);
    }

    [Fact]
    public void MultipleActionsRemainAnExplicitChoiceInsteadOfUsingPromptOrder()
    {
        var composer = Composer(new Affordance(3, "Use", 55, 0, "First"),
            new Affordance(4, "Use", 55, 0, "Second"));
        PromptPresentation prompt = Prompt(composer.Prompt, [Visible(3, 55), Visible(4, 55)]);

        CardInteractionControlDescriptor action = Assert.Single(
            BoardInteractionControlProjection.From(composer, prompt));

        Assert.Equal(CardInteractionIntent.Action, action.Intent);
        Assert.Equal("◇ CHOOSE ACTION (2)", action.Text);
        Assert.Null(composer.Selected);
    }

    [Fact]
    public void SelectedPromptTargetsReceiveAttachedControlsFromTheEngineOffer()
    {
        var composer = Composer(new Affordance(3, "Play", 19, 0, "Visible",
            new TargetRequest([1, 49], 1, 1)));
        composer.SelectAffordance(3);
        PromptPresentation prompt = Prompt(composer.Prompt, [Visible(3, 19)],
            target: new TargetRequest([1, 49], 1, 1));

        CardInteractionControlDescriptor[] targets = [.. BoardInteractionControlProjection
            .From(composer, prompt).Where(control => control.Intent == CardInteractionIntent.Target)];

        Assert.Equal([1, 49], targets.Select(control => control.CardId));
        Assert.All(targets, control => Assert.Equal("◇ TARGET", control.Text));
    }

    [Fact]
    public void RelationshipPlannerUsesDirectPathOrOmitsBlockedRoute()
    {
        Rect2 source = new(0, 0, 20, 20);
        Rect2 target = new(100, 0, 20, 20);

        Vector2[]? direct = RelationshipRoutePlanner.Route(source, target, []);
        Assert.NotNull(direct);
        Assert.Equal(2, direct.Length);

        Rect2[] blockers = [new Rect2(45, -10, 30, 40), new Rect2(45, 10, 30, 40)];
        Assert.Null(RelationshipRoutePlanner.Route(source, target, blockers));
    }

    [Fact]
    public void HiddenRelationshipEndpointHasNoPromptRelationshipToDraw()
    {
        var composer = Composer(new Affordance(3, "Play", 19, 0, "Visible"));
        composer.SelectAffordance(3);
        PromptPresentation prompt = Prompt(composer.Prompt, [Visible(3, 19)]);

        Assert.Empty(BoardInteractionRelationshipProjection.From(composer, prompt));
    }

    private static DecisionComposer Composer(params Affordance[] offers) => new(new Prompt(
        0, Question.TurnOption, TimingPriority.Untimed, "test", "Choose", false, offers));

    private static PromptPresentation Prompt(
        Marvel.Rules.Prompts.Prompt source,
        IReadOnlyList<AffordancePresentation> affordances,
        TargetRequest? target = null,
        IReadOnlyList<CostOption>? costs = null) => new("", "", "", "", "", affordances.Select(view => view with
        {
            TargetRequest = target,
            CostOptions = costs ?? [],
        }).ToArray());

    private static AffordancePresentation Visible(int id, int card, string? illegal = null) => new(
        id, "Visible", null, "Play", "Visible", card, 0, illegal, "", [])
    {
        Source = new AffordanceSourceDescriptor(AffordanceAnchorKind.Card, card, null, 0),
    };
}
