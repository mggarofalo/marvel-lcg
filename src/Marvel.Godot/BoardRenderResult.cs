using Godot;
using Marvel.Decisions;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>The controls addressable by prompt anchor and target ids.</summary>
public sealed class BoardRenderResult
{
    private readonly Dictionary<int, List<CardControl>> controls = [];
    internal readonly BoardInspectorSequences Inspector = new();
    private readonly Dictionary<Control, Action> areaExpanders = [];
    private readonly Dictionary<int, Button> mulliganToggles = [];
    private readonly BoardCardInteractionControls interactionControls;
    private readonly BoardPointerInteractions pointer;
    private readonly BoardControlReveal reveal;
    private DecisionComposer? currentComposer;
    private PromptPresentation? currentPrompt;
    private Label? contextualSummary;
    private string contextualFallback = string.Empty;
    public BoardRenderResult()
    {
        reveal = new BoardControlReveal(this);
        interactionControls = new BoardCardInteractionControls(IsCurrentRender);
        pointer = new BoardPointerInteractions(
            IsCurrentRender,
            (card, control) => CardActivated?.Invoke(card, control),
            (card, control) => CardPreviewEntered?.Invoke(card, control),
            control => CardPreviewExited?.Invoke(control));
    }

    /// <summary>Identifies whether this render remains the board currently shown by its owner.</summary>
    internal Func<bool>? IsCurrent { get; set; }

    /// <summary>Raised with the card under the pointer, or null when it leaves.</summary>
    public event Action<BoardCardPresentation, Control>? CardActivated;

    /// <summary>Raised after a readable card body becomes the pointer's preview source.</summary>
    internal event Action<BoardCardPresentation, Control>? CardPreviewEntered;

    /// <summary>Raised when the pointer leaves a readable card body.</summary>
    internal event Action<Control>? CardPreviewExited;

    internal void Register(int id, CardControl control)
    {
        if (!controls.TryGetValue(id, out List<CardControl>? matches))
        {
            matches = [];
            controls.Add(id, matches);
        }

        matches.Add(control);
    }

    internal void RegisterArea(Control body, Action expand) =>
        areaExpanders.Add(body, expand);

    internal void RegisterDropTarget(int seat, Control control) =>
        pointer.RegisterDropTarget(seat, control);

    internal void RegisterContextualActions(Container host) =>
        interactionControls.RegisterContextualHost(host);

    internal void RegisterContextualSummary(Label label, string fallback)
    {
        contextualSummary = label;
        contextualFallback = fallback;
    }

    internal void TrackCard(Control control, BoardCardPresentation card, bool isHandCard = false)
    {
        if (control is CardControl rendered) interactionControls.Track(rendered, card, isHandCard);
        pointer.Track(control, card, isHandCard);
    }

    /// <summary>Records the authored table pose after spatial placement is complete.</summary>
    internal void UpdateRestingPose(Control control) =>
        pointer.UpdateRestingPose(control);

    /// <summary>Routes a captured card gesture from the root input path.</summary>
    internal bool RoutePointer(InputEvent input) => pointer.Route(input);

    /// <summary>Replaces card-attached controls and cues from the current authorized draft.</summary>
    internal void PresentInteraction(DecisionComposer? composer, PromptPresentation? prompt)
    {
        currentComposer = composer;
        currentPrompt = prompt;
        if (InteractionControl.IsUsable(contextualSummary))
        {
            string summary = TableDraftSummary.From(composer, prompt) ?? contextualFallback;
            contextualSummary!.Text = summary;
            contextualSummary.TooltipText = summary;
        }
        interactionControls.Present(controls, composer, prompt);
        InteractionRelationshipsChanged?.Invoke(
            BoardInteractionRelationshipProjection.From(composer, prompt));
    }

    internal void RefreshInteraction() => PresentInteraction(currentComposer, currentPrompt);

    internal event Action<IReadOnlyList<TableRelationshipDescriptor>>? InteractionRelationshipsChanged;
    private bool IsCurrentRender() => IsCurrent?.Invoke() == true;

    internal void BindDirectInteractions(
        Func<CardPointerGesture, bool> dragAvailable,
        Func<CardPointerGesture, bool> activate,
        Func<CardPointerGesture, bool> drag)
        => pointer.BindDirect(dragAvailable, activate, drag);

    internal void BindExplicitInteraction(Func<CardPointerGesture, bool> activate) =>
        interactionControls.Bind(activate);

    internal void BindContextualInteraction(Action<int> activate) =>
        interactionControls.BindContextual(activate);

    internal bool IsDroppedOnLivePlayerLane(int seat, Vector2 position) =>
        pointer.IsDroppedOnLivePlayerLane(seat, position);

    internal void RegisterMulliganToggle(int id, Button toggle) => mulliganToggles[id] = toggle;

    internal void RegisterMulliganCard(int id, CardControl card) =>
        pointer.RegisterMulliganCard(id, card);

    internal void RegisterMulliganDiscard(Control discard) =>
        pointer.RegisterMulliganDiscard(discard);

    internal void BindMulliganTargets(
        IReadOnlyCollection<int> legal, IReadOnlyCollection<int> selected, Action<int> choose)
    {
        pointer.BindMulliganTargets(legal, choose);
        foreach ((int id, Button toggle) in mulliganToggles)
        {
            bool available = legal.Contains(id);
            toggle.Visible = available;
            toggle.Disabled = !available;
            SetMulliganToggle(toggle, selected.Contains(id));
        }
    }

    internal void RequestMulliganTarget(int id)
    {
        pointer.RequestMulliganTarget(id);
    }

    internal void SetMulliganTargets(IReadOnlyCollection<int> selected)
    {
        foreach ((int id, Button toggle) in mulliganToggles)
        {
            SetMulliganToggle(toggle, selected.Contains(id));
        }
    }

    private static void SetMulliganToggle(Button toggle, bool selected)
    {
        toggle.SetPressedNoSignal(selected);
        toggle.Text = selected ? "✓ DISCARD" : "□ DISCARD";
        toggle.ThemeTypeVariation = selected
            ? GodotThemeVariations.SelectedTargetButton
            : GodotThemeVariations.LegalTargetButton;
    }

    /// <summary>Returns the visible control for an engine-provided card id.</summary>
    public Control? ControlFor(int id) =>
        controls.TryGetValue(id, out List<CardControl>? matches)
            ? matches.LastOrDefault(InteractionControl.IsUsable)
            : null;

    internal IReadOnlyList<CardControl> VisibleCardControls() =>
        [.. controls.Values.SelectMany(matches => matches).Where(InteractionControl.IsUsable)];

    /// <summary>Highlights every visible control matching server-provided ids.</summary>
    public void Highlight(IEnumerable<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        HashSet<int> highlighted = ids.ToHashSet();
        foreach ((int key, List<CardControl> matches) in controls)
        {
            foreach (CardControl match in matches.Where(InteractionControl.IsUsable))
            {
                match.SetHighlighted(highlighted.Contains(key));
                if (highlighted.Contains(key))
                {
                    reveal.Ensure(key);
                }
            }
        }
    }

    /// <summary>Marks cards for a transient event cue without disturbing prompt focus.</summary>
    public void Present(IEnumerable<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        HashSet<int> presented = ids.ToHashSet();
        foreach ((int key, List<CardControl> matches) in controls)
        {
            foreach (CardControl match in matches.Where(InteractionControl.IsUsable))
            {
                match.SetPresented(presented.Contains(key));
                if (presented.Contains(key))
                {
                    reveal.Ensure(key);
                }
            }
        }
    }

    internal void ExpandArea(Control areaBody)
    {
        if (areaExpanders.TryGetValue(areaBody, out Action? expand))
        {
            expand();
        }
    }

    internal bool TryCurrentControl(int key, out Control? control)
    {
        control = IsCurrent?.Invoke() == true ? ControlFor(key) : null;
        return control is not null;
    }

}
