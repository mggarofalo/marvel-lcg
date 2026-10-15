using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Places authorized arrivals in their actual table regions at the movement's impact.</summary>
internal sealed class EventActivityTableArrivals(Main main)
{
    private readonly List<CardControl> retained = [];
    private readonly Dictionary<int, BoardCardPresentation> exposed = [];
    private readonly Dictionary<int, string> pendingDestinations = [];

    internal void Impact(EventPresentation entry)
    {
        if (main.CurrentGame?.World is not { } world || main.boardRender is not { } board
            || main.boardAreas.FindChild("AstraTableSurface", true, false) is not Control surface) return;
        BoardPresentation final = BoardPresentation.From(world);
        int seat = surface.GetMeta("expanded_seat", -1).AsInt32();
        var context = DecisionContextCards.MissingFromTable(main.CurrentGame.Prompt is { } prompt
            ? PromptPresentation.From(prompt, world) : null, final);
        var geometry = SpatialTableSurfaceRenderer.ActivityGeometry(surface.Size, main.interfaceScale, final, seat, context);
        foreach (EventPresentation fact in EventActivityCardFacts.Facts(entry))
        {
            if (fact.Activity is not { } activity) continue;
            foreach (ActivitySubject subject in activity.Subjects.Where(subject => subject.Role == ActivitySubjectRole.Subject))
                ApplySubject(subject, activity, final, board, surface, geometry, seat);
        }
    }

    private void ApplySubject(ActivitySubject subject, EventActivityDescriptor activity, BoardPresentation final,
        BoardRenderResult board, Control surface, AstraTableGeometry geometry, int seat)
    {
        if (subject.Reference is { Concealed: false } granted) exposed[subject.Card] = granted;
        if (activity.DestinationZone.Length > 0) pendingDestinations[subject.Card] = activity.DestinationZone;
        BoardAreaPresentation? area = final.Areas.FirstOrDefault(area => area.Cards.Any(card => card.TargetId == subject.Card));
        if (!Eligible(subject.Card, activity, area, seat)) return;
        BoardCardPresentation? face = GrantedFace(subject, board);
        if (face is null || face.Concealed) return;
        Place(face with { TargetId = subject.Card }, area!, final, board, surface, geometry);
    }

    private BoardCardPresentation? GrantedFace(ActivitySubject subject, BoardRenderResult board) =>
        subject.Reference ?? exposed.GetValueOrDefault(subject.Card)
            ?? (board.ControlFor(subject.Card) as CardControl)?.DisplayedPresentation;

    private bool Eligible(int id, EventActivityDescriptor activity, BoardAreaPresentation? area, int seat) =>
        area is not null && (area.Seat < 0 || area.Seat == seat)
        && IsDestination(id, activity, area.Zone) && !retained.Any(card => card.TargetId == id);

    private bool IsDestination(int id, EventActivityDescriptor activity, string zone) => Arrives(activity, zone)
        || activity.FaceChanged && pendingDestinations.GetValueOrDefault(id) == zone && Supported(zone);

    internal static bool Arrives(EventActivityDescriptor activity, string zone) =>
        Supported(zone) && (activity.DestinationZone == zone || activity.Kind == EventActivityKind.Installation);

    private static bool Supported(string zone) => zone is "AlliesArea" or "SupportsArea" or "UpgradesArea"
        or "EngagedEnemiesArea" or "SideSchemesArea" or "VillainArea" or "MainSchemesArea";

    private void Place(BoardCardPresentation face, BoardAreaPresentation area, BoardPresentation final,
        BoardRenderResult board, Control surface, AstraTableGeometry geometry)
    {
        InterfaceScale scale = SpatialCardMetrics.TableScale(main.interfaceScale, surface.Size.Y);
        if (area.Host >= 0 && area.Seat < 0 && board.ControlFor(area.Host) is CardControl host)
        {
            SpatialTableHostAttachments.Add(host, [face], board, main.interfaceScale, main.art);
            if (board.ControlFor(face.TargetId!.Value) is CardControl attachment)
            {
                attachment.SetMeta("activity_table_arrival", true);
                attachment.SetMeta("activity_destination", area.Zone);
                retained.Add(attachment);
            }
            return;
        }
        bool source = area.Zone is "SupportsArea" or "UpgradesArea" && area.Seat >= 0;
        var layout = new SourceTableauLayout(geometry.SourceTableau.Size.X, geometry.SourceTableau.Size.Y);
        CardControl card = source ? SourceTableauTile.CreateCard(face, layout.TileWidth, main.interfaceScale, "")
            : CardControl.CreateReconciled(face, CardDisplaySize.Board, scale, main.art,
                reuse: null, readingScale: main.interfaceScale);
        card.Name = "ActivityTableArrival" + face.TargetId;
        card.MouseFilter = Control.MouseFilterEnum.Ignore;
        card.FocusMode = Control.FocusModeEnum.None;
        card.SetMeta("activity_table_arrival", true);
        card.SetMeta("activity_destination", area.Zone);
        card.SetMeta("activity_title", face.Title);
        surface.AddChild(card);
        card.Position = Position(face, area, final, geometry, card.Size);
        card.ZIndex = 15;
        board.Register(face.TargetId!.Value, card);
        if (!source) SpatialCardSidecar.State(card, face);
        retained.Add(card);
    }

    private static Vector2 Position(BoardCardPresentation face, BoardAreaPresentation area,
        BoardPresentation final, AstraTableGeometry geometry, Vector2 size)
    {
        if (area.Zone is "SupportsArea" or "UpgradesArea" && area.Seat >= 0)
        {
            BoardCardPresentation[] cards = SourcePresentationOrder.Arrange(area.Seat, CardSourceGroups.Controlled(final.Areas, area.Seat));
            var layout = new SourceTableauLayout(geometry.SourceTableau.Size.X, geometry.SourceTableau.Size.Y, cards.Length);
            BoardCardPresentation?[] slots = SourcePresentationOrder.Slots(area.Seat, cards, layout.Capacity);
            return geometry.SourceTableau.Position + layout.Position(Array.FindIndex(slots, card => card?.TargetId == face.TargetId));
        }
        Rect2 region = area.Zone switch
        {
            "AlliesArea" => geometry.Allies,
            "EngagedEnemiesArea" => geometry.EngagedEnemies,
            "SideSchemesArea" => geometry.SideSchemes,
            "VillainArea" => geometry.Villain,
            "MainSchemesArea" => geometry.MainScheme,
            _ => geometry.Villain,
        };
        BoardCardPresentation[] ordered = area.Cards.Where(card => !card.Concealed).ToArray();
        return geometry.Slot(region, Math.Max(0, Array.FindIndex(ordered, card => card.TargetId == face.TargetId)),
            Math.Max(1, ordered.Length), size);
    }

    internal void Clear()
    {
        foreach (CardControl card in retained)
            if (InteractionControl.IsUsable(card)) { card.Hide(); card.QueueFree(); }
        retained.Clear();
        exposed.Clear();
        pendingDestinations.Clear();
    }
}
