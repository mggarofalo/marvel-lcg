using Godot;
using Marvel.Decisions;
using Marvel.Godot;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class TaskCommitmentProjectionTests
{
    [Fact]
    public void SingleActionEntryNamesTheExactOfferedChoiceWithoutSubmittingIt()
    {
        var composer = Composer(new Affordance(3, "Use", 55, 0, "First"));
        var offer = Visible(3, 55) with { Description = "Draw a card with Avengers Mansion" };

        CardInteractionControlDescriptor control = Assert.Single(
            BoardInteractionControlProjection.From(composer, Prompt(composer.Prompt, [offer])));

        Assert.Equal("Visible", control.Text);
        Assert.Equal("Visible\nDraw a card with Avengers Mansion", control.Description);
        Assert.Equal(CardInteractionIntent.Action, control.Intent);
        Assert.Null(composer.Selected);
    }

    [Fact]
    public void StructuredCardChoiceUsesTheAuthorizedDisplayNameAndPreservesItsWireIdentity()
    {
        var composer = Composer(new Affordance(3, "Choose option", 55, 0, "01094")
        {
            DisplayLabel = "Rhino",
        });
        var offer = Visible(3, 55) with
        {
            Verb = "Choose option", Label = "01094", DisplayLabel = "Rhino",
        };
        CardInteractionControlDescriptor source = Assert.Single(
            BoardInteractionControlProjection.From(composer, Prompt(composer.Prompt, [offer])));

        Assert.Equal("Rhino", source.Text);
        Assert.DoesNotContain("01094", DecisionCopy.Choice(offer));
        composer.SelectAffordance(3);
        Assert.Equal("Choose Rhino", DecisionCopy.GenericCommit(composer.Selected!.Verb,
            composer.Selected.DisplayLabel ?? composer.Selected.Label, "Rhino"));
        Assert.Equal("01094", composer.Selected.Label);
    }

    [Theory]
    [InlineData("Attack Rhino")]
    [InlineData("Discard Surveillance Team")]
    public void AcceptanceNamesTheEngineOperationAndAffectedObject(string commitment)
    {
        // Synthetic contract: opaque command identity cannot substitute for the
        // engine's description of what accepting this particular option does.
        var composer = Composer(new Affordance(3, "Choose_Option", 55, 0, "01094")
        {
            DisplayLabel = "Rhino", CommitLabel = commitment,
        });
        composer.SelectAffordance(3);

        Assert.Equal(commitment, DecisionPanelCopy.SubmitAction(composer,
            new WorldDescriptor([], [], [], Outcome.Unfinished)));
        Assert.Equal("01094", composer.Selected!.Label);
    }

    [Fact]
    public void ReadySelectionHasNoCardLocalCommitmentOrCostButtons()
    {
        var composer = Composer(new Affordance(3, "Use", 55, 0, "First"));
        composer.SelectAffordance(3);

        IReadOnlyList<CardInteractionControlDescriptor> controls =
            BoardInteractionControlProjection.From(composer, Prompt(composer.Prompt, [Visible(3, 55)]));

        Assert.True(composer.Progress().IsReady);
        Assert.DoesNotContain(controls, control => control.Intent is
            CardInteractionIntent.Submit or CardInteractionIntent.Decline or CardInteractionIntent.Cost);
        Assert.Empty(controls);
    }

    [Fact]
    public void ClearingMulliganReplacementsKeepsTheRequiredOpeningHandDecisionOperable()
    {
        var targets = new TargetRequest([1, 2], 0, 2);
        var composer = new DecisionComposer(new Prompt(0, Question.TurnOption,
            TimingPriority.Untimed, "test", "Choose", false,
            [new Affordance(3, Game.ResolveMulligans, 19, 0, "Mulligan", targets)]));
        composer.SelectAffordance(3);
        composer.SelectTargets([1, 2]);

        DecisionComposer cleared = BoardDraftCancellation.Clear(composer);

        Assert.Same(composer.Prompt, cleared.Prompt);
        Assert.Equal(3, cleared.Selected!.Id);
        Assert.Empty(cleared.Targets);
        Assert.True(cleared.TryBuild(out EngineDecision? answer, out _));
        Assert.Empty(answer!.Targets);
        Assert.Equal([1, 2], composer.Targets);
    }

    [Fact]
    public void EndPhaseSelectionsExplainDiscardStagingAndItsOptionalBounds()
    {
        var targets = new TargetRequest([1, 2, 3], 0, 3)
        {
            Details = new Dictionary<int, string> { [1] = "Discard this card after confirmation." },
        };
        var composer = Composer(new Affordance(3, Game.EndPhaseVerb, 19, 0, "End Phase", targets));
        composer.SelectAffordance(3);
        PromptPresentation prompt = Prompt(composer.Prompt, [Visible(3, 19)], targets);

        CardInteractionControlDescriptor before = Assert.Single(
            BoardInteractionControlProjection.From(composer, prompt), control => control.CardId == 1);
        Assert.Equal("Stage discard", before.Text);
        Assert.Equal("Discard this card after confirmation.", before.Description);
        Assert.Contains("Optional, up to 3", DecisionPanelCopy.TargetProgress(composer, composer.Progress().Targets));

        composer.SelectTargets([1]);

        CardInteractionControlDescriptor selected = Assert.Single(
            BoardInteractionControlProjection.From(composer, prompt), control => control.CardId == 1);
        Assert.Equal("✓ Discard staged", selected.Text);
        Assert.Equal([1], composer.Targets);
    }

    [Fact]
    public void RequiredEndPhaseHandChoiceOpensAReversibleDraftWithoutAcceptingAnAnswer()
    {
        var prompt = new Prompt(0, Question.TurnOption, TimingPriority.Untimed,
            "test", "End Phase", false,
            [new Affordance(3, Game.EndPhaseVerb, 19, 0, "End Phase", new TargetRequest([1, 2], 0, 2))]);

        DecisionComposer composer = InitialTableDraft.Create(prompt);

        Assert.Equal(3, composer.Selected!.Id);
        Assert.Empty(composer.Targets);
        Assert.True(composer.TryBuild(out EngineDecision? proposed, out _));
        Assert.Empty(proposed!.Targets);
        composer.SelectTargets([1]);
        DecisionComposer cleared = BoardDraftCancellation.Clear(composer);
        Assert.Equal(3, cleared.Selected!.Id);
        Assert.Empty(cleared.Targets);
    }

    [Fact]
    public void SoleOptionalActionDoesNotBecomeARequiredHandTask()
    {
        var prompt = new Prompt(0, Question.TurnOption, TimingPriority.Untimed,
            "test", "End Phase", true,
            [new Affordance(3, Game.EndPhaseVerb, 19, 0, "End Phase", new TargetRequest([1, 2], 0, 2))]);

        Assert.False(InitialTableDraft.IsRequiredEndPhase(prompt));
        Assert.Null(InitialTableDraft.Create(prompt).Selected);
    }

    [Theory]
    [InlineData("Attack", true, true)]
    [InlineData(Game.EndPhaseVerb, false, true)]
    [InlineData(Game.EndPhaseVerb, true, false)]
    public void UnrelatedIllegalOrTargetlessOffersRemainUnselected(string verb, bool legal, bool targets)
    {
        var prompt = new Prompt(0, Question.TurnOption, TimingPriority.Untimed,
            "test", "End Phase", false,
            [new Affordance(3, verb, 19, 0, "End Phase",
                targets ? new TargetRequest([1, 2], 0, 2) : null,
                Illegal: legal ? null : "Unavailable")]);

        Assert.Null(InitialTableDraft.Create(prompt).Selected);
    }

    [Fact]
    public void MultipleRequiredOffersKeepTheActionChoiceExplicit()
    {
        var prompt = new Prompt(0, Question.TurnOption, TimingPriority.Untimed,
            "test", "Choose", false,
            [new Affordance(3, Game.EndPhaseVerb, 19, 0, "End Phase", new TargetRequest([1, 2], 0, 2)),
             new Affordance(4, "Attack", 19, 0, "Attack")]);

        Assert.Null(InitialTableDraft.Create(prompt).Selected);
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
        id, "Visible", null, "Use", "Visible", card, 0, illegal, "", [])
    {
        Source = new AffordanceSourceDescriptor(AffordanceAnchorKind.Card, card, null, 0),
    };
}
