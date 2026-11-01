using Godot;

namespace Marvel.Godot;

/// <summary>Stable far-to-near bands. These coordinates are presentation choices, independent of prompts.</summary>
internal sealed record AstraTableGeometry(
    float Width, float Height, bool LargeText, bool HasRevealingCard = false,
    Vector2? PhysicalCardSize = null, bool HasSeatSummaries = false, bool HasSourceTableau = false)
{
    internal bool HasAllies { get; init; }
    internal bool HasEncounterGroups { get; init; }
    internal bool HasGameAreas { get; init; }
    internal bool HasScenarioColumns => HasEncounterGroups || HasGameAreas;
    internal const float ReferenceWidth = 1320;
    internal const float ReferenceHeight = 962;
    internal Rect2 VillainMat => new(8, 8, Width - 16, HasScenarioColumns ? 344 : ScaleY(294));
    internal Rect2 PlayerMat => new(8, ScaleY(404), Width - 16, Height - ScaleY(404) - 8);
    internal Rect2 EncounterDiscard => Scale(new Rect2(22, 60, 70, 112));
    internal Rect2 EncounterDeck => Scale(new Rect2(104, 60, 70, 112));
    internal Rect2 MainScheme => Scale(new Rect2(194, 34, 230, 236));
    internal Rect2 Villain => Scale(new Rect2(434, 34, 178, 248));
    internal Rect2 SideSchemes => Scale(new Rect2(626, 34, 300, 248));
    internal Rect2 EngagedEnemies => Scale(new Rect2(944, 34, 354, 248));
    internal Rect2 SeatStrip => Scale(new Rect2(22, 214, 158, 164));
    // A column-filled mat leaves a seam below it: causal references, then the current situation up
    // to the source-tableau column, then the activity line above that column. The source tableau
    // starts below the seam rather than under it.
    internal Rect2 Revealing => HasScenarioColumns
        ? new Rect2(MainScheme.Position.X, SeamTop, 180, 44)
        : Scale(new Rect2(194, 304, 474, 94));
    internal Rect2 ColumnActivity => new(SourceTableau.Position.X, SeamTop, SourceTableau.Size.X, 44);
    private float SeamTop => VillainMat.End.Y + 6;
    internal bool HasSeparateRevealSlot => Width > 0;
    internal Rect2 PlayerDiscard => Scale(new Rect2(22, 408, 70, 112));
    internal Rect2 PlayerDeck => Scale(new Rect2(104, 408, 70, 112));
    internal Rect2 Identity => Scale(new Rect2(194, 408, 196, 248));
    internal Rect2 Allies
    {
        get
        {
            Rect2 baseline = Scale(new Rect2(406, 408, 378, 248));
            Vector2 face = Identity.Size;
            float left = Math.Max(baseline.Position.X, Identity.Position.X + Math.Max(face.X, face.Y) + ScaleX(12));
            return new Rect2(left, baseline.Position.Y, baseline.End.X - left, baseline.Size.Y);
        }
    }
    internal Rect2 Assets => SourceTableau;
    internal Rect2 Upgrades => SourceTableau;
    internal Rect2 SourceTableau => new(ScaleX(808), TableauTop, ScaleX(490), HandTop - TableauTop - 24);
    private float TableauTop => HasScenarioColumns ? Math.Max(ScaleY(350), SeamTop + 48) : ScaleY(350);
    internal Rect2 Hand => new(ScaleX(194), HandTop, Width - ScaleX(194) - 22,
        Context.Position.Y - HandTop - 14);
    internal Rect2 Context => new(ScaleX(194), Height - 58, Width - ScaleX(194) - 22, 54);
    internal Rect2 Overflow => Scale(new Rect2(22, 560, 150, 44));
    internal Rect2 PendingEncounters => Scale(new Rect2(22, 610, 150, 68));

    internal SpatialCardPlacement HandCard(int index, int count, float cardWidth)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, count);
        float step = HandStep(count, cardWidth);
        float start = Hand.Position.X + (Hand.Size.X - step * (count - 1) - cardWidth) / 2;
        float distance = index - (count - 1) / 2f;
        return new SpatialCardPlacement(new Vector2(start + index * step, HandTop - Math.Abs(distance) * 2),
            Mathf.DegToRad(Mathf.Clamp(distance * 1.2f, -4, 4)), 20 + index, step < cardWidth);
    }

    internal float HandExposedWidth(int index, int count, float cardWidth) =>
        index == count - 1 ? cardWidth : Math.Min(cardWidth, HandStep(count, cardWidth));

    private float HandStep(int count, float cardWidth) => count == 1 ? 0
        : Math.Min(cardWidth + ScaleX(8), Math.Max(0, Hand.Size.X - cardWidth) / (count - 1));

    internal Vector2 Slot(Rect2 region, int index, int count, Vector2 objectSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        float step = count == 1 ? 0 : Math.Min(objectSize.X + ScaleX(14), Math.Max(0, region.Size.X - objectSize.X) / (count - 1));
        return region.Position + new Vector2(index * step, 0);
    }

    private float HandTop => Height - 58 - (int)SpatialCardMetrics.TableScale(InterfaceScale.Percent100, Height) / 100f * 218 - 14;
    private Rect2 Scale(Rect2 value) => new(ScaleX(value.Position.X), ScaleY(value.Position.Y), ScaleX(value.Size.X), ScaleY(value.Size.Y));
    private float ScaleX(float value) => value * Width / ReferenceWidth;
    private float ScaleY(float value) => value * Height / ReferenceHeight;
}
