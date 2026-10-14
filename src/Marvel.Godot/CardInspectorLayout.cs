using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Measures and places card detail independently of inspection lifetime.</summary>
internal sealed partial class CardInspectorLayout(Main main) : Node
{
    internal float AvailableHeight() => CardPaymentWorkspaceLayout.Active(main)
        ? HandCards().Select(card => card.Position.Y - 12).DefaultIfEmpty(main.Size.Y).Min()
        : main.Size.Y;

    internal bool PreservesSituation() => FitsContent()
        && VisibleSituation().All(region => !main.cardInspectorFrame.GetGlobalRect().Intersects(region));

    private bool FitsContent()
    {
        Control detail = (Control)main.cardInspectorContent.GetChild(0);
        HBoxContainer header = main.cardInspectorTitle.GetParent<HBoxContainer>();
        float required = detail.GetCombinedMinimumSize().Y
            + (header.Visible ? header.GetCombinedMinimumSize().Y + 12 : 0);
        return required <= main.cardInspectorFrame.Size.Y;
    }

    private Action? positionCurrent;

    internal void Track(Control detail, BoardCardPresentation card, Control? source, bool pinned)
    {
        Vector2 previousSize = Vector2.Zero;
        Vector2 previousViewport = Vector2.Zero;
        Rect2 previousSource = new();
        Rect2[] previousSituation = [];
        positionCurrent = () =>
        {
            if (!IsCurrentDetail(detail)) return;
            Vector2 size = detail.GetCombinedMinimumSize();
            Rect2 sourceBounds = CurrentSource(source, card.TargetId)?.GetGlobalRect() ?? new Rect2();
            Rect2[] situation = VisibleSituation();
            if (size == previousSize && main.Size == previousViewport && sourceBounds == previousSource
                && situation.SequenceEqual(previousSituation)) return;
            previousSize = size;
            previousViewport = main.Size;
            previousSource = sourceBounds;
            previousSituation = situation;
            Position(card, source, pinned);
        };
    }

    private bool IsCurrentDetail(Control detail) => InteractionControl.IsUsable(detail)
        && main.cardInspector.Visible && main.cardInspectorContent.GetChildCount() == 1
        && main.cardInspectorContent.GetChild(0) == detail;

    public override void _Process(double delta)
    {
        if (main.cardInspector.Visible) positionCurrent?.Invoke();
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
        Rect2 frame = CardInspectorPlacement.Fit(new Vector2(main.Size.X, AvailableHeight()), sourceRect,
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
        .Select(control => control.GetGlobalRect()).Concat(HandCards()).ToArray();

    private IEnumerable<Rect2> HandCards() => main.FindChildren("ProceduralCard*", "Control", true, false)
        .OfType<Control>()
        .Where(control => control.HasMeta("spatial_hand_index") && control.IsVisibleInTree())
        .Select(RestingHandFace);

    private static Rect2 RestingHandFace(Control card)
    {
        if (!card.HasMeta("spatial_resting_position")) return SpatialCardFootprint.Face(card);
        Vector2 position = card.GetMeta("spatial_resting_position").AsVector2();
        float rotation = (float)card.GetMeta("spatial_resting_rotation").AsDouble();
        var pose = new Transform2D(rotation, card.Scale, 0, Vector2.Zero);
        pose.Origin = position + card.PivotOffset - pose * card.PivotOffset;
        return SpatialCardFootprint.Bounds(card.GetParent<Control>().GetGlobalTransform() * pose, card.Size);
    }

    private static bool HasSituation(Control control) => control.Name.ToString() switch
    {
        "CausalSituation" => control.FindChildren("*", "Label", true, false).OfType<Label>()
            .Any(label => label.IsVisibleInTree() && !string.IsNullOrWhiteSpace(label.Text)),
        "ContextualDecision" or "EventTimeline" or "AbilityWindow" or "PaymentWorkspace" => true,
        _ => false,
    };
}
