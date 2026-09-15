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
    public void ForcedDiscardChoicesUseExplicitDestructiveCues()
    {
        var composer = Composer(new Affordance(
            3, "Choose", 55, 0, "support", Description: "Discard Avengers Mansion"));
        PromptPresentation prompt = Prompt(
            composer.Prompt, [Visible(3, 55) with { Description = "Discard Avengers Mansion" }]);

        Assert.Equal(
            CardInteractionCue.DestructiveChoice,
            BoardInteractionCueProjection.From(composer, prompt)[55]);

        composer.SelectAffordance(3);

        Assert.Equal(
            CardInteractionCue.DestructiveChoice
                | CardInteractionCue.SelectedDestructiveChoice,
            BoardInteractionCueProjection.From(composer, prompt)[55]);
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
    public void AutomaticTargetKeepsItsCueWithoutARedundantAttachedControl()
    {
        var composer = Composer(new Affordance(3, "Play", 19, 0, "Visible",
            new TargetRequest([1], 1, 1)));
        composer.SelectAffordance(3);
        PromptPresentation prompt = Prompt(composer.Prompt, [Visible(3, 19)],
            target: new TargetRequest([1], 1, 1));

        Assert.True(composer.UsesAutomaticTargetSelection);
        Assert.DoesNotContain(BoardInteractionControlProjection.From(composer, prompt),
            control => control.Intent == CardInteractionIntent.Target);
        Assert.Equal(CardInteractionCue.LegalTarget | CardInteractionCue.SelectedTarget,
            BoardInteractionCueProjection.From(composer, prompt)[1]);
    }

    [Fact]
    public void GroupedTargetsKeepTheirCuesForTheOrderedFallbackWithoutAttachedToggles()
    {
        var target = new TargetRequest([1, 2, 3, 4], 2, 2, Groups: [[1, 2], [3, 4]]);
        var composer = Composer(new Affordance(3, "Play", 19, 0, "Visible", target));
        composer.SelectAffordance(3);
        PromptPresentation prompt = Prompt(composer.Prompt, [Visible(3, 19)], target: target);

        Assert.DoesNotContain(BoardInteractionControlProjection.From(composer, prompt),
            control => control.Intent == CardInteractionIntent.Target);
        IReadOnlyDictionary<int, CardInteractionCue> cues =
            BoardInteractionCueProjection.From(composer, prompt);
        Assert.All(target.Legal, id => Assert.Equal(CardInteractionCue.LegalTarget, cues[id]));
    }

    [Fact]
    public void RepeatedTargetsKeepTheirCuesForTheOrderedFallbackWithoutAttachedToggles()
    {
        var target = new TargetRequest([1, 2], 1, 3, AllowRepeated: true,
            MaximumOccurrences: new Dictionary<int, int> { [1] = 2, [2] = 1 });
        var composer = Composer(new Affordance(3, "Play", 19, 0, "Visible", target));
        composer.SelectAffordance(3);
        PromptPresentation prompt = Prompt(composer.Prompt, [Visible(3, 19)], target: target);

        Assert.DoesNotContain(BoardInteractionControlProjection.From(composer, prompt),
            control => control.Intent == CardInteractionIntent.Target);
        IReadOnlyDictionary<int, CardInteractionCue> cues =
            BoardInteractionCueProjection.From(composer, prompt);
        Assert.All(target.Legal, id => Assert.Equal(CardInteractionCue.LegalTarget, cues[id]));
    }

    [Fact]
    public void TargetScopedGeneratorsAppearOnlyAfterTheirSelectedTargetMakesTheCostApply()
    {
        var target = new TargetRequest([11, 12], 1, 1);
        var cost = new CostOption(11, "1", Sources: [new ResourceSource(41, "Y")]);
        var composer = Composer(new Affordance(3, "Play", 19, 0, "Visible", target, [cost]));
        composer.SelectAffordance(3);
        PromptPresentation prompt = Prompt(composer.Prompt, [Visible(3, 19)], target, [cost]);

        Assert.False(composer.CostApplies(cost));
        Assert.DoesNotContain(BoardInteractionControlProjection.From(composer, prompt),
            control => control.Intent == CardInteractionIntent.Generator);
        Assert.Equal(CardInteractionCue.LegalGenerator,
            BoardInteractionCueProjection.From(composer, prompt)[41]);

        composer.SelectTargets([11]);

        Assert.True(composer.CostApplies(cost));
        CardInteractionControlDescriptor generator = Assert.Single(
            BoardInteractionControlProjection.From(composer, prompt),
            control => control.Intent == CardInteractionIntent.Generator);
        Assert.Equal(41, generator.CardId);
    }

    [Fact]
    public void RelationshipPlannerUsesDirectPathOrRoutesAroundBlockedCards()
    {
        Rect2 source = new(0, 0, 20, 20);
        Rect2 target = new(100, 0, 20, 20);

        Vector2[]? direct = RelationshipRoutePlanner.Route(source, target, []);
        Assert.NotNull(direct);
        Assert.Equal(2, direct.Length);
        Assert.Equal(new Vector2(20, 10), direct[0]);
        Assert.Equal(new Vector2(100, 10), direct[1]);

        Rect2[] blockers = [new Rect2(45, -10, 30, 40), new Rect2(45, 10, 30, 40)];
        Vector2[] detour = Assert.IsType<Vector2[]>(
            RelationshipRoutePlanner.Route(source, target, blockers));
        Assert.Equal(4, detour.Length);
        Assert.All(detour.Skip(1).SkipLast(1), point =>
            Assert.DoesNotContain(blockers, blocker => blocker.Grow(2).HasPoint(point)));
    }

    [Fact]
    public void RelationshipPlannerLeavesCardContentFromTheFacingEdges()
    {
        Rect2 source = new(0, 0, 20, 20);
        Rect2 target = new(0, 100, 20, 20);

        Vector2[] direct = Assert.IsType<Vector2[]>(RelationshipRoutePlanner.Route(source, target, []));

        Assert.Equal([new Vector2(10, 20), new Vector2(10, 100)], direct);
    }

    [Fact]
    public void RelationshipEndpointMustRemainInsideEveryClippingAncestor()
    {
        var viewport = new Rect2(0, 0, 320, 180);
        Vector2 center = new(110, 90);

        Assert.True(RelationshipOverlayVisibility.EndpointIsVisible(center, viewport,
            [new Rect2(0, 0, 200, 180), new Rect2(100, 0, 220, 180)]));
        Assert.False(RelationshipOverlayVisibility.EndpointIsVisible(center, viewport,
            [new Rect2(0, 0, 100, 180)]));
        Assert.False(RelationshipOverlayVisibility.EndpointIsVisible(center, viewport,
            [new Rect2(100, 0, 220, 80)]));
    }

    [Fact]
    public void HiddenRelationshipEndpointHasNoPromptRelationshipToDraw()
    {
        var composer = Composer(new Affordance(3, "Play", 19, 0, "Visible"));
        composer.SelectAffordance(3);
        PromptPresentation prompt = Prompt(composer.Prompt, [Visible(3, 19)]);

        Assert.Empty(BoardInteractionRelationshipProjection.From(composer, prompt));
    }

    [Fact]
    public void OpeningMulliganHasNoAttachedTargetControlsOrRelationshipFan()
    {
        var targets = new TargetRequest([1, 2], 0, 2);
        var composer = new DecisionComposer(new Prompt(0, Question.TurnOption,
            TimingPriority.Untimed, "test", "Choose", false,
            [new Affordance(3, Game.ResolveMulligans, 19, 0, "Mulligan", targets)]));
        composer.SelectAffordance(3);
        PromptPresentation prompt = Prompt(composer.Prompt,
            [Visible(3, 19) with
            {
                TargetRequest = targets,
                Relationships =
                [
                    new TableRelationshipDescriptor(RelationshipKind.OfferedTarget, 19, 1),
                    new TableRelationshipDescriptor(RelationshipKind.OfferedTarget, 19, 2),
                ],
            }], targets);

        Assert.DoesNotContain(BoardInteractionControlProjection.From(composer, prompt),
            control => control.Intent == CardInteractionIntent.Target);
        Assert.Empty(BoardInteractionRelationshipProjection.From(composer, prompt));
    }

    [Fact]
    public void SelectedPromptDrawsOnlyActionableTargetAndGeneratorLinks()
    {
        var composer = Composer(new Affordance(3, "Play", 19, 0, "Visible"));
        composer.SelectAffordance(3);
        PromptPresentation prompt = Prompt(composer.Prompt,
        [
            Visible(3, 19) with
            {
                Relationships =
                [
                    new TableRelationshipDescriptor(RelationshipKind.Attachment, 19, 20),
                    new TableRelationshipDescriptor(RelationshipKind.Result, 19, 21),
                    new TableRelationshipDescriptor(RelationshipKind.OfferedTarget, 19, 22),
                    new TableRelationshipDescriptor(RelationshipKind.OfferedGenerator, 19, 23),
                    new TableRelationshipDescriptor(RelationshipKind.Engagement, 19, null, 0),
                ],
            },
        ]);

        Assert.Equal(
        [
            new TableRelationshipDescriptor(RelationshipKind.OfferedTarget, 19, 22),
            new TableRelationshipDescriptor(RelationshipKind.OfferedGenerator, 19, 23),
        ], BoardInteractionRelationshipProjection.From(composer, prompt));
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
