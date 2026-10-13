using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Measures and places card detail independently of inspection lifetime.</summary>
internal sealed class CardInspectorLayout(Main main)
{
    internal bool PreservesSituation() => VisibleSituation()
        .All(region => !main.cardInspectorFrame.GetGlobalRect().Intersects(region));

    internal void BindMeasurement(Control detail, BoardCardPresentation card, Control? source, bool pinned)
    {
        detail.MinimumSizeChanged += () => Callable.From(() =>
        {
            if (InteractionControl.IsUsable(detail) && main.cardInspector.Visible
                && main.cardInspectorContent.GetChildCount() == 1
                && main.cardInspectorContent.GetChild(0) == detail)
            {
                Position(card, source, pinned);
            }
        }).CallDeferred();
    }

    internal void Position(BoardCardPresentation card, Control? source, bool pinned)
    {
        Control detail = (Control)main.cardInspectorContent.GetChild(0);
        Vector2 detailSize = detail.GetCombinedMinimumSize();
        HBoxContainer header = main.cardInspectorTitle.GetParent<HBoxContainer>();
        float headerHeight = pinned ? header.GetCombinedMinimumSize().Y + 12 : 0;
        float contentWidth = Math.Max(detailSize.X + 20,
            pinned ? header.GetCombinedMinimumSize().X : 0);
        Control? currentSource = CurrentSource(source, card.TargetId);
        Rect2 sourceRect = currentSource is not null ? SpatialCardFootprint.Face(currentSource)
            : new Rect2(main.GetViewport().GetMousePosition(), Vector2.Zero);
        Rect2 frame = CardInspectorPlacement.Fit(main.Size, sourceRect,
            new Vector2(contentWidth, detailSize.Y + headerHeight),
            OccupiedCards(pinned, currentSource), VisibleActions(pinned), VisibleSituation());
        main.cardInspectorFrame.CustomMinimumSize = Vector2.Zero;
        main.cardInspectorFrame.Size = frame.Size;
        main.cardInspectorFrame.Position = frame.Position;
    }

    private Control? CurrentSource(Control? source, int? targetId) =>
        InteractionControl.IsUsable(source) ? source
            : targetId is { } target ? main.boardRender?.ControlFor(target) : null;

    private Rect2[]? OccupiedCards(bool pinned, Control? source) => pinned ? null
        : main.boardRender?.VisibleCardControls()
            .Where(control => control != source && control.IsVisibleInTree())
            .Select(control => SpatialCardFootprint.Face(control)).ToArray();

    private Rect2[]? VisibleActions(bool pinned) => pinned ? null
        : main.FindChildren("*", "BaseButton", true, false).OfType<BaseButton>()
            .Where(button => InteractionControl.IsUsable(button)
                && button.IsVisibleInTree() && !main.cardInspector.IsAncestorOf(button))
            .Select(button => button.GetGlobalRect()).ToArray();

    private Rect2[] VisibleSituation() => main.FindChildren("*", "Control", true, false)
        .OfType<Control>()
        .Where(control => control.IsVisibleInTree() && HasSituation(control))
        .Select(control => control.GetGlobalRect()).ToArray();

    private static bool HasSituation(Control control) => control.Name.ToString() switch
    {
        "CausalSituation" => control.FindChildren("*", "Label", true, false).OfType<Label>()
            .Any(label => label.IsVisibleInTree() && !string.IsNullOrWhiteSpace(label.Text)),
        "ContextualDecision" or "EventTimeline" or "AbilityWindow" => true,
        _ => false,
    };
}
