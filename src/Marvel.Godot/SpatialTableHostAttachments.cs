using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>One current source tab under a host, with complete title access to denser stacks.</summary>
internal static class SpatialTableHostAttachments
{
    internal static void AddOther(CardControl host, BoardAreaPresentation area,
        BoardRenderResult result, InterfaceScale scale, ICardArtProvider? art)
    {
        BoardCardPresentation[] cards = [.. area.Cards.Where(card => !CardSourceGroups.IsLocalSource(card))];
        if (cards.Length == 0) return;
        var inspect = new MenuButton { Name = $"InspectAttachments{area.Host}", Text = Caption(area, cards),
            CustomMinimumSize = new Vector2(0, 44), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.WordSmart };
        TabletopInspectionMenu.Bind(inspect, [area with { Cards = cards, Removed = [] }], result, scale, art);
        SpatialCardSidecar.For(host).AddChild(inspect);
        TableCompactButtonStyle.Apply(inspect);
    }

    internal static string Caption(BoardAreaPresentation area, BoardCardPresentation[] cards) =>
        cards.All(card => card.Concealed)
            ? $"{TabletopAreaNames.Region(area.Zone)} · {cards.Sum(card => card.Count)}\nFace down"
            : cards.Length == 1 ? cards[0].Title : $"{TabletopAreaNames.Region(area.Zone)}\n{cards.Length}";

    internal static void Add(CardControl host, BoardCardPresentation[] cards,
        BoardRenderResult result, InterfaceScale scale, ICardArtProvider? art = null)
    {
        if (cards.Length == 0) return;
        var overlay = new Control { Name = "HostSources", MouseFilter = Control.MouseFilterEnum.Ignore };
        host.AddChild(overlay);
        Callable.From(() =>
        {
            if (!InteractionControl.IsUsable(host)) return;
            Rect2 face = SpatialCardFootprint.Face(host);
            VBoxContainer tabs = CardSourceCollection.Create(cards, face.Size.X, result, scale, "Attached");
            overlay.AddChild(tabs);
            tabs.Rotation = -host.Rotation;
            tabs.Position = overlay.GetGlobalTransform().AffineInverse() * new Vector2(face.Position.X, face.End.Y + 4);
            tabs.MinimumSizeChanged += () => Callable.From(() => CollapseIfOccupied(host, tabs, cards, result, scale, art)).CallDeferred();
            host.VisibilityChanged += () => Callable.From(() => CollapseIfOccupied(host, tabs, cards, result, scale, art)).CallDeferred();
            Callable.From(() => CollapseIfOccupied(host, tabs, cards, result, scale, art)).CallDeferred();
        }).CallDeferred();
    }

    private static void CollapseIfOccupied(CardControl host, VBoxContainer tabs,
        BoardCardPresentation[] cards, BoardRenderResult result, InterfaceScale scale, ICardArtProvider? art)
    {
        if (!InteractionControl.IsUsable(tabs) || tabs.IsQueuedForDeletion()) return;
        if (!Occupied(host, tabs.GetGlobalRect(), result)) return;
        Control? focused = tabs.GetViewport().GuiGetFocusOwner();
        bool restoreFocus = focused is not null && tabs.IsAncestorOf(focused);
        tabs.GetParent().RemoveChild(tabs);
        tabs.QueueFree();
        var picker = new MenuButton { Name = "InspectBoundedSources", Text = $"Attached · {cards.Length}",
            CustomMinimumSize = new Vector2(0, 44), SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        var area = new BoardAreaPresentation(-1, "Attached cards", "", cards, []) { Zone = "UpgradesArea" };
        TabletopInspectionMenu.Bind(picker, [area], result, scale, art);
        SpatialCardSidecar.For(host).AddChild(picker);
        TableCompactButtonStyle.Apply(picker);
        if (restoreFocus) picker.GrabFocus();
    }

    private static bool Occupied(CardControl host, Rect2 bounds, BoardRenderResult result) =>
        result.VisibleCardControls().Any(card => card != host && !host.IsAncestorOf(card)
            && card.IsVisibleInTree() && bounds.Intersects(SpatialCardFootprint.Face(card)))
        || host.GetParent().GetChildren().OfType<PanelContainer>()
            .Any(pile => pile.Name.ToString().StartsWith("Pile", StringComparison.Ordinal)
                && pile.IsVisibleInTree() && bounds.Intersects(pile.GetGlobalRect()));
}
