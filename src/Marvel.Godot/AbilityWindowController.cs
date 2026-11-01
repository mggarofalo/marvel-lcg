using Marvel.Rules.Play;
using Godot;
using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Presents current ability choices through one explicit, revision-bound commitment surface.</summary>
internal sealed class AbilityWindowController(DecisionPanel panel) : IDisposable
{
    private AbilityModalFrame? frame;
    internal bool IsOpen => frame is not null;
    internal bool Paused { get; private set; }
    internal ForcedAbilityNotification? RequiredNotification { get; private set; }
    internal VBoxContainer? Content => frame?.Content;
    internal VBoxContainer? Footer => frame?.Footer;

    internal void SetNotification(bool paused, ForcedAbilityNotification? required)
    {
        if (Paused == paused && RequiredNotification == required) return;
        Paused = paused;
        RequiredNotification = required;
        panel.Rebuild(focusFirst: true);
    }

    internal bool Open(PromptPresentation prompt)
    {
        if (panel.composer is not { } draft || !CanOpen(draft)) return false;
        string[] titles = SourceTitles(prompt, draft);
        CreateFrame(prompt, draft, titles);
        if (draft.Selected is not null) return false;
        int generation = panel.GetRenderGeneration();
        foreach (AffordancePresentation offer in prompt.Affordances.Where(offer => offer.Illegal is null))
            AddOffer(draft, offer, generation, titles.Length > 1);
        AddTableEntry(draft, prompt, generation);
        AddPass(draft, prompt, generation);
        Callable.From(() =>
        {
            if (frame is { } current && InteractionControl.IsUsable(current.Frame))
                current.Frame.FindChildren("*", "Button", true, false).OfType<Button>()
                    .FirstOrDefault(button => !button.Disabled)?.GrabFocus();
        }).CallDeferred();
        return true;
    }

    private bool CanOpen(DecisionComposer draft) => !Paused && !panel.LocalDecision.Active && !panel.CompleteChoicesOpen
        && !CardPaymentPresentation.UsesModal(draft, panel.submitting)
        && (UsesModal(draft.Prompt) || RequiredNotification is not null);

    private static string[] SourceTitles(PromptPresentation prompt, DecisionComposer draft) =>
        [.. prompt.Affordances.Where(offer => offer.Illegal is null)
            .Select(offer => $"{offer.SourceName ?? offer.Anchor} {PromptPresentation.Words(offer.Timing?.ToString() ?? draft.Prompt.PublicKind.ToString())}")
            .Distinct()];

    private void CreateFrame(PromptPresentation prompt, DecisionComposer draft, string[] titles)
    {
        int choices = prompt.Affordances.Where(offer => offer.Illegal is null)
            .Sum(offer => Math.Max(1, offer.WindowChoices.Count));
        frame = new AbilityModalFrame(ClientSceneHost.MainFor(panel), "AbilityWindow", panel.Theme, choices);
        frame.AddText("AbilityWindowHeading", RequiredNotification?.Title
            ?? (titles.Length == 1 ? titles[0] : prompt.Heading), GodotThemeVariations.Heading);
        frame.AddText("AbilityWindowCause", RequiredNotification?.Summary ?? prompt.Resolution(), GodotThemeVariations.Body);
        string answerer = panel.world?.Players.FirstOrDefault(player => player.Seat == draft.Prompt.Player)?.Name
            ?? $"Player {draft.Prompt.Player + 1}";
        frame.AddText("AbilityWindowAnswerer", answerer, GodotThemeVariations.Caption);
    }

    private void AddOffer(DecisionComposer draft, AffordancePresentation offer, int generation, bool showSource)
    {
        if (showSource) frame!.AddText($"WindowSource{offer.Id}", offer.SourceName ?? offer.Anchor, GodotThemeVariations.Caption);
        if (offer.WindowChoices.Count == 0)
        {
            Button button = frame!.AddButton($"WindowAction{offer.Id}", DecisionCopy.AbilityLabel(offer, offer.DisplayLabel ?? offer.Label),
                () => panel.SelectAffordance(offer.Id, generation), panel.submitting);
            button.SetMeta("table_mutation", true);
            AttachPreview(button, offer);
            return;
        }
        foreach (var choice in offer.WindowChoices)
        {
            Button button = frame!.AddButton($"WindowChoice{offer.Id}_{choice.TargetId?.ToString() ?? "Immediate"}", DecisionCopy.AbilityLabel(offer, choice.Label, choice.Description),
                () => Commit(draft, generation, offer.Id, choice.TargetId), panel.submitting);
            button.SetMeta("table_mutation", true);
            AttachPreview(button, offer, choice.Description);
        }
    }

    private static void AttachPreview(Button button, AffordancePresentation offer, string? choice = null)
    {
        button.TooltipText = DecisionCopy.AbilityDetail(offer, button.Text, choice);
        button.AccessibilityDescription = button.TooltipText;
    }

    private void Commit(DecisionComposer draft, int generation, int offer, int? target)
    {
        if (panel.IsCurrentDraft(draft, generation) && draft.SelectWindowChoice(offer, target)
            && draft.TryBuild(out Decision? decision, out _))
            panel.NotifySubmitted(decision!, generation);
    }

    private void AddPass(DecisionComposer draft, PromptPresentation prompt, int generation)
    {
        if (!draft.Prompt.Cancellable) return;
        Button pass = frame!.AddButton("WindowPass", $"{prompt.DeclineLabel}  ·  Esc",
            () => Decline(draft, generation), panel.submitting);
        pass.SetMeta("table_mutation", true);
    }

    private void AddTableEntry(DecisionComposer draft, PromptPresentation prompt, int generation)
    {
        int? source = prompt.Affordances.FirstOrDefault(offer => offer.Illegal is null)?.CardAnchorId;
        if (source is not { } id || panel.CurrentBoard?.VisibleCardControls()
                .Any(card => card.TargetId == id) != true) return;
        frame!.AddButton("WindowUseTable", "Use card on table · right-click or drag", () =>
        {
            if (panel.IsCurrentDraft(draft, generation))
                panel.CurrentBoard?.VisibleCardControls()
                    .FirstOrDefault(card => card.TargetId == id)?.GrabFocus();
        }, panel.submitting);
    }

    private void Decline(DecisionComposer draft, int generation)
    {
        if (panel.IsCurrentDraft(draft, generation) && draft.TryDecline(out Decision? decision, out _))
            panel.NotifySubmitted(decision!, generation);
    }

    internal void Input(InputEvent input)
    {
        if (frame is null || panel.composer is not { } draft) return;
        Control? focused = panel.GetViewport().GuiGetFocusOwner();
        if (focused is not null && focused != frame.Frame && !frame.Frame.IsAncestorOf(focused)) return;
        if (AbilityModalFocus.Route(frame.Frame, input)) return;
        if (!input.IsActionPressed("ui_cancel") || !draft.Prompt.Cancellable) return;
        Decline(draft, panel.GetRenderGeneration());
        panel.GetViewport().SetInputAsHandled();
    }

    internal void ClearFrame()
    {
        frame?.Dispose();
        frame = null;
    }

    public void Dispose() => ClearFrame();

    internal static bool UsesModal(Prompt? prompt) =>
        prompt?.PublicKind is PublicDecisionKind.Interrupt or PublicDecisionKind.Response;
}
