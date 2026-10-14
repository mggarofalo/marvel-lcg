using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Reveals only receipt-named, authorized arrivals together into the final hand slots.</summary>
internal sealed class EventActivityArrivals(Main main)
{
    private readonly List<(CardControl Card, Vector2 From, SpatialCardPlacement To, double Delay)> active = [];
    private readonly List<CardControl> retained = [];
    private readonly HashSet<int> arriving = [];
    private int dealt;

    internal void Begin(EventPresentation? entry)
    {
        Advance(1, .16);
        active.Clear();
        dealt = 0;
        if (entry is null || main.CurrentGame?.World is not { } world || main.boardRender is not { } board) return;
        RegisterArrivals(entry);
        if (arriving.Count == 0 || main.boardAreas.FindChild("AstraTableSurface", true, false) is not Control surface) return;
        retained.RemoveAll(card => !InteractionControl.IsUsable(card));
        PopulateHand(BoardPresentation.From(world), surface, board);
    }

    private void PopulateHand(BoardPresentation final, Control surface, BoardRenderResult board)
    {
        int seat = surface.GetMeta("expanded_seat", -1).AsInt32();
        BoardAreaPresentation? hand = final.Areas.FirstOrDefault(area => area.Zone == "HandsArea" && area.Seat == seat);
        if (hand is null) return;
        BoardCardPresentation[] ordered = HandPresentationOrder.Arrange(seat, hand.Cards);
        var context = DecisionContextCards.MissingFromTable(main.CurrentGame?.Prompt is { } prompt
            && main.CurrentGame.World is { } world ? PromptPresentation.From(prompt, world) : null, final);
        var geometry = SpatialTableSurfaceRenderer.ActivityGeometry(surface.Size, main.interfaceScale, final, seat, context);
        InterfaceScale scale = SpatialCardMetrics.TableScale(main.interfaceScale, surface.Size.Y);
        float width = VisualSystem.Card(CardDisplaySize.Hand, scale).Width;
        for (int index = 0; index < ordered.Length; index++)
            PlaceArrival(ordered[index], geometry.HandCard(index, ordered.Length, width), board, surface, scale);
    }

    private void RegisterArrivals(EventPresentation entry)
    {
        arriving.UnionWith(EventActivityCardFacts.Facts(entry)
            .Where(fact => fact.Motion == EventMotionKind.HandGain || fact.Activity?.Operation == "Draw")
            .SelectMany(fact => fact.Activity?.Subjects ?? []).Select(subject => subject.Card));
        if (entry.Activity?.Operation == "Draw") arriving.UnionWith(entry.Activity.Subjects.Select(subject => subject.Card));
    }

    private void PlaceArrival(BoardCardPresentation shown, SpatialCardPlacement placement,
        BoardRenderResult board, Control surface, InterfaceScale scale)
    {
        if (shown.TargetId is not { } id || shown.Concealed) return;
        if (board.ControlFor(id) is CardControl existing && !existing.HasMeta("activity_departed"))
        {
            active.Add((existing, existing.Position, placement, 0));
            return;
        }
        if (!arriving.Contains(id) || retained.Any(prior => prior.TargetId == id)) return;
        CardControl card = CardControl.CreateReconciled(shown, CardDisplaySize.Hand, scale, main.art,
            reuse: null, readingScale: main.interfaceScale);
        card.Name = "ActivityArrival" + id;
        card.ZIndex = placement.ZIndex;
        card.MouseFilter = Control.MouseFilterEnum.Pass;
        card.PivotOffset = card.Size / 2;
        surface.AddChild(card);
        Vector2 from = placement.Position + new Vector2(0, 34);
        card.Position = from;
        card.Modulate = Colors.Transparent;
        card.SetMeta("activity_arrival", true);
        // A short visual overlap preserves the engine's grouped draw without serial reading pauses.
        active.Add((card, from, placement, Math.Min(.22, dealt++ * .055)));
        retained.Add(card);
    }

    internal void Advance(double elapsed, double onset)
    {
        foreach (var (card, from, destination, delay) in active)
        {
            if (!InteractionControl.IsUsable(card)) continue;
            float progress = Mathf.Clamp((float)((elapsed - onset - delay) / .24), 0, 1);
            float eased = 1 - Mathf.Pow(1 - progress, 3);
            card.Position = from.Lerp(destination.Position, eased);
            card.Rotation = destination.Rotation;
            if (card.HasMeta("activity_arrival")) card.Modulate = Colors.White with { A = eased };
        }
    }

    internal void Clear()
    {
        foreach (CardControl card in retained)
            if (InteractionControl.IsUsable(card)) { card.Hide(); card.QueueFree(); }
        active.Clear();
        retained.Clear();
        arriving.Clear();
    }
}
