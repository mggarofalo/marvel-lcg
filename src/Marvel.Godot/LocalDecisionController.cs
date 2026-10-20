using Godot;
using Marvel.Client;
using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Hosts the existing prompt editor near a menu intention without owning a second draft.</summary>
internal sealed class LocalDecisionController(DecisionPanel panel) : IDisposable
{
    private readonly LocalDecisionReceipt receipt = new(panel);
    internal LocalDecisionContinuity Continuity { get; } = new();
    private CanvasLayer? layer;
    private PanelContainer? frame;
    private Control? opener;
    private Rect2 anchor;
    private bool dismissed;
    private Viewport? viewport;
    private Control? inspector;
    private Node? inspectorParent;
    private string notice = string.Empty;
    internal VBoxContainer? Content { get; private set; }
    internal bool IsOpen => Content is not null && !dismissed;
    internal bool Active => Continuity.Active;

    internal void Begin(CardPointerGesture gesture, AffordancePresentation offer)
        => Begin(gesture.Source, gesture.Card.TargetId!.Value, offer);

    internal void Begin(Control source, int sourceId, AffordancePresentation offer)
    {
        receipt.Dispose();
        Continuity.Begin(sourceId, panel.composer!.Prompt.Player,
            panel.CurrentRevision, DecisionCopy.Choice(offer), panel.composer.Prompt.PublicKind);
        panel.CurrentBoard?.ClearPendingChoices();
        anchor = source.GetGlobalRect();
        opener = source;
        dismissed = false;
        notice = string.Empty;
    }

    internal void Refresh(Marvel.Rules.Prompts.Prompt? prompt, long revision)
    {
        bool wasActive = Active;
        bool submitted = Continuity.AcceptedRevision == revision;
        string origin = Continuity.Origin;
        if (Continuity.Refresh(prompt?.Player, prompt?.PublicKind, revision)) dismissed = false;
        else if (wasActive)
        {
            ClearFrame();
            receipt.Present(anchor, origin, submitted);
        }
    }

    internal void PresentProgress(GameProgressPresentation progress)
    {
        notice = CompleteDecisionSheet.RecoveryCopy(progress);
        if (progress.Kind is GameProgressKind.DecisionNotSent or GameProgressKind.DecisionRejected)
            Continuity.Reject();
        if (frame?.FindChild("LocalRecovery", true, false) is Label label)
        {
            label.Text = notice;
            label.Visible = notice.Length > 0;
        }
    }

    internal void Open(PromptPresentation prompt)
    {
        if (!Active || dismissed || panel.AcknowledgementPending) return;
        Main main = ClientSceneHost.MainFor(panel);
        layer = new CanvasLayer { Name = "LocalDecisionLayer", Layer = 90 };
        main.AddChild(layer);
        frame = new PanelContainer { Name = "LocalDecisionWorkspace", Theme = panel.Theme,
            ThemeTypeVariation = GodotThemeVariations.SurfacePanel };
        layer.AddChild(frame);
        inspector = main.cardInspector;
        inspectorParent = inspector.GetParent();
        inspector.Reparent(layer);
        Content = new VBoxContainer { Name = "LocalDecisionContent",
            ThemeTypeVariation = GodotThemeVariations.TightStack };
        frame.AddChild(Content);
        AddHeader(prompt);
        Callable.From(() => Fit(main)).CallDeferred();
        frame.MinimumSizeChanged += () => Callable.From(() => Fit(main)).CallDeferred();
        viewport = main.GetViewport();
        viewport.SizeChanged += Resize;
        panel.CurrentBoard?.SetCompleteChoicesOpen(true);
    }

    private string Requirement(PromptPresentation prompt) =>
        $"Player {panel.composer!.Prompt.Player + 1} · " +
        (panel.composer.Selected is not null && panel.composer.Prompt.PublicKind == Marvel.Rules.Prompts.PublicDecisionKind.PlayerAction
            ? "Draft — not committed" : prompt.Requirement);

    private void AddHeader(PromptPresentation prompt)
    {
        AddSource();
        Content!.AddChild(DecisionPanel.Text(Requirement(prompt),
            GodotThemeVariations.Caption, wrap: true));
        if (Continuity.Committed)
            Content.AddChild(DecisionPanel.Text("Previous choice committed · no refund on close.",
                GodotThemeVariations.Caption, wrap: true));
        if (panel.composer is { } current
            && CardPaymentWorkspaceHeader.ShowsResolution(current.Prompt.PublicKind, current.Prompt.CauseCardIds.Count > 0)
            && !string.IsNullOrWhiteSpace(prompt.Resolution))
            Content.AddChild(DecisionPanel.Text(prompt.Resolution, GodotThemeVariations.Body, wrap: true));
        var recovery = DecisionPanel.Text(notice, GodotThemeVariations.Body, wrap: true);
        recovery.Name = "LocalRecovery";
        recovery.Visible = notice.Length > 0;
        Content.AddChild(recovery);
        var row = new HBoxContainer();
        var back = new Button { Name = "LocalChooseAction", Text = "Clear current draft", Disabled = panel.submitting };
        var draft = panel.composer;
        int generation = panel.GetRenderGeneration();
        back.Pressed += () => { if (draft is not null && panel.IsCurrentDraft(draft, generation)) ResetDraft(); };
        back.Visible = panel.composer?.Selected is not null;
        row.AddChild(back);
        var close = new Button { Name = "LocalReturnToTable", Text = "Return to table" };
        close.Pressed += () => { if (ReferenceEquals(draft, panel.composer)
            && generation == panel.GetRenderGeneration()) Dismiss(); };
        row.AddChild(close);
        Content.AddChild(row);
    }

    private void AddSource()
    {
        var source = new Button
        {
            Name = "LocalDecisionSource", Text = Continuity.Origin, Flat = true,
            Alignment = HorizontalAlignment.Left, AutowrapMode = TextServer.AutowrapMode.WordSmart,
            AccessibilityName = $"Inspect {Continuity.Origin}",
        };
        CardPaymentInspection.Bind(panel, source, Continuity.Source);
        source.Pressed += () =>
        {
            if (panel.CurrentBoard is { } board && board.Inspector.Current(Continuity.Source) is { } card)
                board.InspectCard(card, source);
        };
        Content!.AddChild(source);
    }

    private void Resize() => Fit(ClientSceneHost.MainFor(panel));

    private void Fit(Main main)
    {
        if (!InteractionControl.IsUsable(frame) || Content is null) return;
        Control? hand = main.FindChild("HandDropArea", true, false) as Control;
        Rect2 currentAnchor = panel.CurrentBoard?.ControlFor(Continuity.Source)?.GetGlobalRect() ?? anchor;
        Rect2 bounds = LocalDecisionLayout.Fit(main.GetViewportRect().Size, currentAnchor,
            hand?.GetGlobalRect().Position.Y ?? main.GetViewportRect().Size.Y - 250, 640);
        frame!.Position = bounds.Position;
        frame.Size = bounds.Size;
    }

    internal static void DescribeChoice(DecisionPanel panel, Button button, AffordancePresentation offer)
    {
        if (!panel.LocalDecision.IsOpen) return;
        button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        var engagement = panel.world?.Relationships.FirstOrDefault(relation =>
            relation.Kind == RelationshipKind.Engagement && relation.Subject == offer.CardAnchorId);
        if (engagement?.Seat is { } seat) button.Text = $"Player {seat + 1} · {button.Text}";
        string detail = DecisionCopy.CompactActionSummary(offer);
        if (offer.Description?.StartsWith(offer.Anchor + " · ", StringComparison.Ordinal) == true)
            button.Text = button.Text.Replace(DecisionCopy.Choice(offer), detail, StringComparison.Ordinal);
        else if (!string.IsNullOrWhiteSpace(detail)) button.Text += "\n" + detail;
    }

    internal void Preview(int? id)
    { if (frame is not null) panel.CurrentBoard?.PreviewMenuCard(id, frame); }

    internal void LeaveForTableAction()
    {
        if (panel.composer is { } composer && Continuity.LeaveForTableAction(composer.Prompt.PublicKind))
        {
            panel.CurrentBoard?.ClearPendingChoices();
            Dispose();
        }
    }

    internal void Resume(Control source)
    {
        if (!Active) return;
        opener = source;
        dismissed = false;
        panel.CurrentBoard?.ClearPendingChoices();
        panel.Rebuild(focusFirst: true);
    }

    private void ResetDraft()
    {
        if (panel.submitting || panel.composer is not { } draft) return;
        panel.composer = new DecisionComposer(draft.Prompt);
        panel.Rebuild(focusFirst: true);
    }

    private void Dismiss()
    {
        dismissed = true;
        ClearFrame();
        panel.Rebuild();
        panel.CurrentBoard?.PresentPendingChoices("Resume current choice", Resume);
        if (InteractionControl.IsUsable(opener)) opener!.GrabFocus();
    }

    internal void Input(InputEvent input)
    {
        if (!IsOpen || !input.IsActionPressed("ui_cancel")) return;
        Dismiss();
        panel.GetViewport().SetInputAsHandled();
    }

    public void Dispose() { ClearFrame(); receipt.Dispose(); }

    internal void ClearFrame()
    {
        if (GodotObject.IsInstanceValid(inspector) && GodotObject.IsInstanceValid(inspectorParent))
            inspector!.Reparent(inspectorParent!);
        inspector = null;
        inspectorParent = null;
        if (GodotObject.IsInstanceValid(viewport)) viewport!.SizeChanged -= Resize;
        viewport = null;
        Content = null;
        frame = null;
        if (GodotObject.IsInstanceValid(layer)) { layer!.Visible = false; layer.QueueFree(); }
        layer = null;
        panel.CurrentBoard?.SetCompleteChoicesOpen(false);
    }
}
