using Marvel.Rules.Play;
using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Owns prompt-revision submission and deferred-focus lifetime for one decision panel.</summary>
internal sealed class DecisionPanelLifecycle
{
    private readonly DecisionPanel panel;
    private readonly PromptSubmissionLatch submission = new();
    private int renderGeneration;
    private long revision;

    internal DecisionPanelLifecycle(DecisionPanel panel)
    {
        this.panel = panel;
    }

    internal int RenderGeneration => renderGeneration;

    internal long Revision => revision;

    internal void Render(Prompt? prompt, WorldDescriptor world, long currentRevision)
    {
        panel.world = world ?? throw new ArgumentNullException(nameof(world));
        panel.composer = prompt is null ? null : InitialTableDraft.Create(prompt);
        panel.mulliganChoiceSheetOpen = false;
        revision = currentRevision;
        submission.Render(currentRevision);
        panel.submitting = submission.IsSubmitted;
        panel.Rebuild(focusFirst: true);
    }

    internal void AllowRetry(long currentRevision)
    {
        submission.AllowRetry(currentRevision);
        panel.submitting = submission.IsSubmitted;
        panel.Rebuild();
    }

    internal void AuthoritativeSynchronization(long currentRevision)
    {
        submission.AuthoritativeSynchronization(currentRevision);
        panel.submitting = submission.IsSubmitted;
        panel.Rebuild();
    }

    internal bool TrySubmit()
    {
        if (panel.composer is null || !submission.TrySubmit(revision))
        {
            return false;
        }

        panel.LocalDecision.Continuity.Submit();
        panel.submitting = true;
        panel.Rebuild();
        return true;
    }

    internal bool CanMutate(int generation, long expectedRevision) => expectedRevision == revision
        && generation == renderGeneration
        && !panel.AcknowledgementPending
        && !panel.submitting && !submission.IsSubmitted && panel.composer is not null;

    internal TableDraftBinding Bind(DecisionComposer composer, int generation) =>
        new(composer, generation, revision, CanMutate);

    internal void NotifySubmitted(Decision decision, int generation)
    {
        if (CanMutate(generation, revision) && TrySubmit())
        {
            panel.RaiseSubmitted(decision);
        }
    }

    internal void SelectAffordance(int affordanceId, int generation)
    {
        DecisionComposer? composer = panel.composer;
        if (composer is null || !Bind(composer, generation).TrySelectAffordance(affordanceId))
        {
            return;
        }

        Affordance option = composer.Selected!;
        panel.LocalDecision.LeaveForTableAction();
        panel.RaiseDraftStarted();
        panel.NotifyAnchorFocused([option.AnchorId]);
        if (!DecisionDialogLayout.Automatic(composer.Prompt) && DirectCommitment.IsReady(composer)
            && composer.TryBuild(out Decision? decision, out _))
        {
            NotifySubmitted(decision!, generation);
            return;
        }
        panel.Rebuild();
    }

    internal int NextRenderGeneration() => checked(++renderGeneration);

    internal void RestoreFocus(string? requested, bool focusFirst, int generation, int? paymentScroll)
    {
        if (generation == renderGeneration)
        {
            DecisionFocus.Restore(panel, requested, focusFirst, generation, paymentScroll);
        }
    }
}
