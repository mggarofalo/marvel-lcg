using Godot;
using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Renders one current prompt and composes its typed decision.</summary>
public sealed partial class DecisionPanel : VBoxContainer
{
    private InterfaceScale interfaceScale = ClientTheme.ConfiguredScale();
    private InterfaceScale requestedScale = ClientTheme.ConfiguredScale();
    internal ControlMetrics ControlMetrics => VisualSystem.Controls(interfaceScale);
    internal DecisionComposer? composer;
    private VBoxContainer? content;
    private VBoxContainer? commit;
    internal bool submitting;
    internal bool mulliganChoiceSheetOpen;
    private bool compactMulliganChrome;
    private BoardRenderResult? mulliganBoard;
    private readonly DecisionPanelLifecycle lifecycle;
    internal WorldDescriptor? world;
    public DecisionPanel() => lifecycle = new DecisionPanelLifecycle(this);
    /// <summary>Raised with one answer built from the current prompt.</summary>
    public event Action<EngineDecision>? Submitted;
    /// <summary>Raised when an affordance or target points at a board object.</summary>
    public event Action<IReadOnlyList<int>>? AnchorFocused;
    /// <summary>Raised with the visible card whose action or target is under the pointer.</summary>
    public event Action<int?>? CardHovered;
    /// <summary>Raised whenever the visible draft's count-only progress changes.</summary>
    public event Action<DecisionProgressPresentation?>? ProgressChanged;
    /// <summary>Raised when a player opens one action's target and payment editor.</summary>
    public event Action? DraftStarted;
    /// <summary>Raised when prompt-authorized card cues need to reflect the current draft.</summary>
    internal event Action<DecisionComposer?, PromptPresentation?>? DraftChanged;
    /// <summary>Applies the current presentation-only desktop scale.</summary>
    public void SetInterfaceScale(InterfaceScale scale)
    {
        requestedScale = scale;
        // The tabletop owns the full desktop height. Its dock deliberately
        // keeps one compact metric so a scale preference never moves Commit.
        InterfaceScale effectiveScale = EffectiveScale(
            scale, compactMulliganChrome, MulliganPrompt.IsOpening(composer?.Prompt));
        if (interfaceScale == effectiveScale)
        {
            if (composer is not null)
            {
                Rebuild();
            }
            return;
        }

        interfaceScale = effectiveScale;
        if (composer is not null)
        {
            Rebuild();
        }
    }
    /// <summary>Discards the old draft and renders the response's current prompt.</summary>
    public void Render(Prompt? prompt, WorldDescriptor currentWorld, long revision)
    {
        interfaceScale = EffectiveScale(
            requestedScale, compactMulliganChrome, MulliganPrompt.IsOpening(prompt));
        lifecycle.Render(prompt, currentWorld, revision);
    }

    internal static InterfaceScale EffectiveScale(
        InterfaceScale requested, bool compactTableChrome, bool opening) =>
        compactTableChrome && (opening || requested < InterfaceScale.Standard)
            ? InterfaceScale.Standard
            : requested;

    internal void SetCompactMulliganChrome(bool value)
    {
        if (compactMulliganChrome == value)
        {
            return;
        }

        compactMulliganChrome = value;
        SetInterfaceScale(requestedScale);
    }
    /// <summary>Reopens a prompt only after the client proved its request was not sent.</summary>
    public void AllowRetry(long revision) => lifecycle.AllowRetry(revision);

    /// <summary>Reopens the prompt after an authoritative table synchronization.</summary>
    internal void AuthoritativeSynchronization(long revision) =>
        lifecycle.AuthoritativeSynchronization(revision);
    /// <summary>Prevents a second mutation while one response is outstanding.</summary>
    public void SetSubmitting(bool value)
    {
        submitting = value;
        Rebuild();
    }
    internal void NotifyAnchorFocused(IReadOnlyList<int> ids) =>
        AnchorFocused?.Invoke(ids);

    internal int GetRenderGeneration() => lifecycle.RenderGeneration;

    internal void NotifySubmitted(EngineDecision decision, int generation) =>
        lifecycle.NotifySubmitted(decision, generation);

    internal void SelectAffordance(int id, int generation) => lifecycle.SelectAffordance(id, generation);

    internal void BindMulliganTargets(BoardRenderResult? board)
    {
        mulliganBoard = board;
        MulliganBinding.Bind(this, board);
        BoardInteractionBinder.Bind(this, board);
        board?.PresentInteraction(composer,
            composer is null || world is null ? null : PromptPresentation.From(composer.Prompt, world));
    }

    internal TableDraftBinding BindTableDraft(DecisionComposer draft, int generation) =>
        lifecycle.Bind(draft, generation);

    internal void RefreshMulliganTargets(DecisionComposer draft, int target)
    {
        mulliganBoard?.SetMulliganTargets(draft.Targets);
        NotifyAnchorFocused([target]);
        Rebuild();
    }

    internal bool IsCurrentDraft(DecisionComposer expected, int generation) =>
        ReferenceEquals(composer, expected)
        && lifecycle.CanMutate(generation, lifecycle.Revision);

    internal void RaiseSubmitted(EngineDecision decision) => Submitted?.Invoke(decision);

    internal void RaiseDraftStarted() => DraftStarted?.Invoke();

    internal void Rebuild(bool focusFirst = false)
    {
        BoardActionChoiceSurface.Close();
        int generation = lifecycle.NextRenderGeneration();
        Control? focused = GetViewport()?.GuiGetFocusOwner();
        string? focusName = focused is not null && IsAncestorOf(focused)
            ? FocusKey(focused)
            : null;
        ClearPanel();
        if (!RenderPrompt())
        {
            DraftChanged?.Invoke(null, null);
            RenderNoDecision();
            return;
        }
        Callable.From(() => lifecycle.RestoreFocus(focusName, focusFirst, generation)).CallDeferred();
    }

    private bool RenderPrompt()
    {
        if (composer is null || world is null)
        {
            return false;
        }
        PromptPresentation prompt = PromptPresentation.From(composer.Prompt, world);
        DraftChanged?.Invoke(composer, prompt);
        DecisionPanelPromptRenderer.CreateLayout(this, composer, prompt);
        DecisionPanelPromptRenderer.AddAffordances(this, prompt, lifecycle.RenderGeneration);
        AddSelectedDraft();
        AddDecline();
        ProgressChanged?.Invoke(composer.Progress());
        return true;
    }

    private void ClearPanel()
    {
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
        ThemeTypeVariation = GodotThemeVariations.TightStack;
    }

    private void RenderNoDecision()
    {
        ProgressChanged?.Invoke(null);
        (string heading, string detail) = world?.Outcome switch
        {
            Outcome.Unfinished => (
                "WAITING FOR ANOTHER PLAYER",
                "Another player has the current decision."),
            Outcome.PlayersWin => ("VICTORY", "The players won. No further decision is waiting."),
            Outcome.VillainWins => ("DEFEAT", "The villain won. No further decision is waiting."),
            Outcome.PlayersLose => ("DEFEAT", "The players lost. No further decision is waiting."),
            _ => ("NO DECISION", "No decision is available."),
        };
        AddChild(Text(heading, GodotThemeVariations.Eyebrow));
        AddChild(Text(detail, GodotThemeVariations.Body, wrap: true));
    }


    private void AddSelectedDraft()
    {
        if (composer!.Selected is not { } selected)
        {
            return;
        }
        DecisionProgressPresentation progress = composer.Progress();
        if (MulliganPrompt.IsOpening(composer.Prompt))
        {
            MulliganDecisionSurface.AddDraft(this, selected, progress);
            return;
        }
        AddContent(new HSeparator());
        AddContent(Text(DecisionPanelCopy.TargetProgress(composer, progress.Targets), GodotThemeVariations.Eyebrow));
        int generation = lifecycle.RenderGeneration;
        new DecisionDraftRenderer(this, composer, world!, submitting, generation)
            .AddTargets(selected, progress.Targets);
        var payment = new DecisionPaymentRenderer(this, composer, world!, submitting, generation);
        payment.AddCosts(selected);
        payment.AddSubmit(composer.Progress());
    }

    private void AddDecline()
    {
        if (!composer!.Prompt.Cancellable)
        {
            return;
        }
        var pass = new Button
        {
            Name = "Decline",
            Text = submitting ? "— UNAVAILABLE  ·  Pass / decline" : "Pass / decline",
            Disabled = submitting,
        };
        StyleButton(pass, submitting
            ? InteractiveVisualState.Unavailable
            : InteractiveVisualState.Resting);
        int generation = lifecycle.RenderGeneration;
        pass.Pressed += () =>
        {
            if (lifecycle.CanMutate(generation, lifecycle.Revision)
                && composer!.TryDecline(out EngineDecision? decision, out _))
            {
                NotifySubmitted(decision!, generation);
            }
        };
        AddCommit(pass);
    }

    internal void InstallLayout(VBoxContainer body, VBoxContainer commitBar)
    {
        content = body;
        commit = commitBar;
    }

    internal void AddContent(Control control) =>
        (content ?? throw new InvalidOperationException("decision content is unavailable"))
            .AddChild(control);

    internal void AddCommit(Control control) =>
        (commit ?? throw new InvalidOperationException("decision commit bar is unavailable"))
            .AddChild(control);

    private string? FocusKey(Control focused) => DecisionFocus.Key(this, focused);

    internal void BindAnchors(Control control, params int[] ids) => DecisionAnchorBinding.Bind(this, control, ids);

    internal void NotifyCardHovered(int? id) => CardHovered?.Invoke(id);

    internal static string NodeKey(string value) => new(
        value.Select(character => char.IsLetterOrDigit(character) ? character : '_').ToArray());

    internal static Label Text(string text, string variation, bool wrap = false)
    {
        return new Label
        {
            Text = text,
            AutowrapMode = wrap
                ? TextServer.AutowrapMode.WordSmart
                : TextServer.AutowrapMode.Off,
            ThemeTypeVariation = variation,
        };
    }

    internal void StyleButton(
        Button button,
        InteractiveVisualState state,
        bool compact = false)
    {
        InteractiveStyle style = VisualSystem.For(state);
        button.ThemeTypeVariation = style.ThemeVariation;
        button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        button.CustomMinimumSize = new Vector2(
            Math.Max(
                button.CustomMinimumSize.X,
                compact
                    ? ControlMetrics.MinimumPointerTarget
                    : ControlMetrics.MinimumButtonWidth),
            ControlMetrics.MinimumHeight);
    }

}
