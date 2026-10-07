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
    private CardPaymentWorkspace? paymentModal;
    private readonly CompleteDecisionSheetController choices;
    internal readonly DecisionCardChoices CardChoices;
    internal bool CompleteChoicesOpen => choices.IsOpen;
    internal void ShowCompleteChoices(Control source) => choices.Open(source);
    internal void CompleteChoicesClosed() => choices.Closed();
    internal void PresentOperationalNotice(Marvel.Client.GameProgressPresentation progress) =>
        choices.PresentProgress(progress);
    internal void SetCompleteChoicesVisibility(bool open) => mulliganBoard?.SetCompleteChoicesOpen(open);
    internal bool PaymentModalOpen => paymentModal is not null;
    internal Node LayoutHost => paymentModal?.Content ?? (Node)this;
    internal Control FocusHost => paymentModal?.Content ?? (Control)this;
    internal void RouteDecisionSurfaceInput(InputEvent input)
    {
        paymentModal?.Input(input);
        choices.Input(input);
    }
    internal void ClearDecision()
    {
        choices.Close();
        composer = null;
        world = null;
        Rebuild();
    }
    private VBoxContainer? content;
    private VBoxContainer? commit;
    internal bool submitting;
    internal bool mulliganChoiceSheetOpen;
    private bool compactMulliganChrome;
    private BoardRenderResult? mulliganBoard;
    private readonly CardPreviewOwnership cardPreview = new();
    internal CardPreviewOwnership CardPreview => cardPreview;
    private readonly DecisionPanelLifecycle lifecycle;
    internal WorldDescriptor? world;
    public DecisionPanel()
    {
        lifecycle = new DecisionPanelLifecycle(this);
        choices = new CompleteDecisionSheetController(this);
        CardChoices = new DecisionCardChoices(this);
        cardPreview.Changed += id => CardHovered?.Invoke(id);
    }
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
        Theme = ClientTheme.Create(scale);
        // The table's physical cards keep their own bounded geometry. The
        // expanded editor uses the player's requested reading scale.
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
        choices.Close();
        lifecycle.Render(prompt, currentWorld, revision);
        choices.OpenCardChoiceAfterRender();
    }

    internal static InterfaceScale EffectiveScale(
        InterfaceScale requested, bool compactTableChrome, bool opening) =>
        requested;

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
        string? focusName = DecisionFocus.CurrentKey(this);
        int? paymentScroll = paymentModal?.ScrollPosition(composer);
        ClearPanel();
        if (!RenderPrompt())
        {
            DraftChanged?.Invoke(null, null);
            RenderNoDecision();
            return;
        }
        Callable.From(() => lifecycle.RestoreFocus(focusName, focusFirst || PaymentModalOpen, generation, paymentScroll)).CallDeferred();
    }

    private bool RenderPrompt()
    {
        if (composer is null || world is null)
        {
            return false;
        }
        PresentAutomaticTarget(composer);
        PromptPresentation prompt = PromptPresentation.From(composer.Prompt, world);
        MulliganBinding.Bind(this, mulliganBoard);
        BoardInteractionBinder.Bind(this, mulliganBoard);
        DraftChanged?.Invoke(composer, prompt);
        if (CardPaymentPresentation.UsesModal(composer, submitting))
        {
            choices.Close();
            paymentModal = new CardPaymentWorkspace(this, prompt);
        }
        DecisionPanelPromptRenderer.CreateLayout(this, composer, prompt);
        if (paymentModal is null)
            DecisionPanelPromptRenderer.AddAffordances(this, prompt, lifecycle.RenderGeneration);
        DecisionPanelSurface.AddSelectedDraft(this);
        if (paymentModal is not null) paymentModal.AddCancel();
        else DecisionPanelSurface.AddDecline(this);
        ProgressChanged?.Invoke(composer.Progress());
        return true;
    }

    private void PresentAutomaticTarget(DecisionComposer draft)
    {
        SetMeta("automatic_target_selection", draft.UsesAutomaticTargetSelection);
        SetMeta("automatic_target_id", draft.UsesAutomaticTargetSelection && draft.Targets.Count == 1
            ? draft.Targets[0] : -1);
    }

    private void ClearPanel()
    {
        cardPreview.Clear();
        paymentModal?.Dispose();
        paymentModal = null;

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
        AddChild(Text(heading, GodotThemeVariations.Caption));
        AddChild(Text(detail, GodotThemeVariations.Body, wrap: true));
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

    internal void BindAnchors(Control control, params int[] ids)
    {
        DecisionAnchorBinding.Bind(this, control, ids);
    }

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
