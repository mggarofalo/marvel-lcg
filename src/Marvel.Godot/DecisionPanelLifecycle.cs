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

    internal void Render(Prompt? prompt, WorldDescriptor world, long currentRevision)
    {
        panel.world = world ?? throw new ArgumentNullException(nameof(world));
        panel.composer = prompt is null ? null : new DecisionComposer(prompt);
        if (panel.composer is not null && MulliganPrompt.IsOpening(prompt))
        {
            // This is a client presentation choice. The engine offered exactly
            // one mulligan affordance; selecting it starts the same typed draft
            // the ordered renderer and final commit use.
            panel.composer.SelectAffordance(prompt!.Affordances[0].Id);
        }
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

        panel.submitting = true;
        panel.Rebuild();
        return true;
    }

    internal bool CanMutate(int generation) => generation == renderGeneration
        && !panel.submitting && !submission.IsSubmitted && panel.composer is not null;

    internal void NotifySubmitted(EngineDecision decision, int generation)
    {
        if (CanMutate(generation) && TrySubmit())
        {
            panel.RaiseSubmitted(decision);
        }
    }

    internal void SelectAffordance(int affordanceId, int generation)
    {
        if (panel.composer is null || !CanMutate(generation))
        {
            return;
        }

        Affordance option = panel.composer.Prompt.Affordances.Single(candidate =>
            candidate.Id == affordanceId);
        panel.composer.SelectAffordance(option.Id);
        panel.RaiseDraftStarted();
        panel.NotifyAnchorFocused([option.AnchorId]);
        if (panel.composer.Prompt.Asking == Question.Element
            && panel.composer.Prompt.Affordances.Count == 1
            && panel.composer.TryBuild(out EngineDecision? automatic, out _))
        {
            NotifySubmitted(automatic!, generation);
            return;
        }

        panel.Rebuild();
    }

    internal int NextRenderGeneration() => checked(++renderGeneration);

    internal void RestoreFocus(string? requested, bool focusFirst, int generation)
    {
        if (generation == renderGeneration)
        {
            DecisionFocus.Restore(panel, requested, focusFirst, generation);
        }
    }
}
