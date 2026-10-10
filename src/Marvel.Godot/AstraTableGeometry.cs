using Godot;

namespace Marvel.Godot;

/// <summary>The fixed 1920x1080 spatial grammar for the Astra desktop table.</summary>
/// <remarks>
/// These coordinates are a presentation choice. They describe a physical reading
/// order inside the table column and contain no gameplay or legality decisions.
/// </remarks>
internal sealed record AstraTableGeometry(
    float Width, float Height, bool LargeText, bool HasRevealingCard = false,
    Vector2? PhysicalCardSize = null, bool HasSeatSummaries = false, bool HasSourceTableau = false)
{
    internal bool HasAllies { get; init; }
    internal const float ReferenceWidth = 1320;
    internal const float ReferenceHeight = 962;

    internal Rect2 VillainMat => Scale(new Rect2(8, 8, 1304, 282));
    internal Rect2 PlayerMat => Scale(new Rect2(8, 254, 1304, 672));
    internal Rect2 EncounterDiscard => Scale(new Rect2(36, 72, 108, 142));
    internal Rect2 EncounterDeck => Scale(new Rect2(164, 72, 108, 142));
    internal Rect2 MainScheme => new(EncounterDeck.End.X + 22, ScaleY(76), FootprintWidth, ScaleY(176));
    internal Rect2 Villain => new(MainScheme.End.X + 16, ScaleY(54), FootprintWidth, ScaleY(220));
    internal Rect2 SideSchemes => new(Villain.End.X + 16, ScaleY(76),
        Math.Max(0, (HasSeatSummaries ? SeatStrip.Position.X - 16 : Width - 20) - Villain.End.X - 16), ScaleY(176));
    internal Rect2 SeatStrip => Scale(new Rect2(1060, 46, 224, 212));
    internal Rect2 EngagedEnemies => new(ScaleX(36), ScaleY(280), FootprintWidth, ScaleY(160));
    internal Rect2 PlayerDiscard => Scale(new Rect2(36, 496, 108, 142));
    internal Rect2 PlayerDeck => Scale(new Rect2(164, 496, 108, 142));
    internal Rect2 Context => Scale(new Rect2(20, 800, 1280, 156));
    internal bool HasSeparateRevealSlot => HasRevealingCard && !HasSourceTableau
        && Assets.End.X + 2 * (FootprintWidth + 16) + 16 + SpatialRegionDrawerLayout.MinimumWidth <= Width - 20;
    internal Rect2 Identity => new(HasSourceTableau ? (HasRevealingCard ? Assets.End.X + 16 : Assets.Position.X) : (HasSeparateRevealSlot ? Revealing.End.X : Assets.End.X) + 16,
        ScaleY(310), FootprintWidth, ScaleY(189));
    internal Rect2 Allies => new(Identity.End.X + 16, ScaleY(310),
        Math.Max(0, (HasSourceTableau && !CompactSourceTableau ? SourceTableau.Position.X - 16 : Width - 20) - Identity.End.X - 16), ScaleY(180));
    internal Rect2 Assets => new(Math.Max(ScaleX(302), EngagedEnemies.End.X + 16), ScaleY(310), FootprintWidth, ScaleY(180));
    internal Rect2 Upgrades => new(Width - FootprintWidth - 20,
        Math.Max(ScaleY(530), PlayerRowBottom + ScaleY(24)), FootprintWidth, ScaleY(230));
    internal Rect2 Revealing => new(HasSeparateRevealSlot ? Assets.End.X + 16 : Assets.Position.X,
        ScaleY(310), FootprintWidth, ScaleY(180));
    private bool CompactSourceTableau => HasRevealingCard || HasAllies || Width < 1500;
    private float SourceWidth => Math.Min(744, Width * 0.45f);
    private float SourceLeft => CompactSourceTableau ? Math.Max(Width - SourceWidth - 20, Assets.Position.X + 220)
        : Math.Max(Width - SourceWidth - 20, Identity.End.X + 170);
    private float SourceTop => CompactSourceTableau ? Math.Max(ScaleY(530), PlayerRowBottom + ScaleY(24)) : ScaleY(300);
    internal Rect2 SourceTableau => new(SourceLeft, SourceTop,
        Width - SourceLeft - 20, Math.Max(44, ScaleY(790) - SourceTop));
    internal Rect2 Hand => new(Assets.Position.X, HandTop,
        Math.Max(0, (HasSourceTableau ? SourceTableau.Position.X : Upgrades.Position.X) - Assets.Position.X - 20),
        Math.Max(0, Context.Position.Y - HandTop - ScaleY(40)));
    internal Rect2 Overflow => Scale(new Rect2(36, 660, 164, 44));
    internal Rect2 PendingEncounters => Scale(new Rect2(36, 718, 236, 70));

    internal SpatialCardPlacement HandCard(int index, int count, float cardWidth)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, count);

        Rect2 hand = Hand;
        float step = HandStep(count, cardWidth);
        float spread = step * (count - 1);
        float start = hand.Position.X + (hand.Size.X - spread - cardWidth) / 2;
        float middle = (count - 1) / 2f;
        float distance = index - middle;
        float rotation = Mathf.DegToRad(Mathf.Clamp(distance * 2.2f, -7, 7));
        float drop = -Mathf.Abs(distance) * 3.5f;
        return new SpatialCardPlacement(
            new Vector2(start + index * step, hand.Position.Y + drop),
            rotation,
            20 + index,
            step < cardWidth);
    }

    internal float HandExposedWidth(int index, int count, float cardWidth) =>
        index == count - 1 ? cardWidth : Math.Min(cardWidth, HandStep(count, cardWidth));

    private float HandStep(int count, float cardWidth) => count == 1 ? 0
        : Math.Min(cardWidth + ScaleX(8), Math.Max(0, Hand.Size.X - cardWidth) / (count - 1));

    internal Vector2 Slot(Rect2 region, int index, int count, Vector2 objectSize)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        float available = Math.Max(0, region.Size.X - objectSize.X);
        float step = count == 1 ? 0 : Math.Min(objectSize.X + ScaleX(14), available / (count - 1));
        return new Vector2(region.Position.X + index * step, region.Position.Y);
    }

    private Rect2 Scale(Rect2 value) => new(
        value.Position.X * Width / ReferenceWidth,
        value.Position.Y * Height / ReferenceHeight,
        value.Size.X * Width / ReferenceWidth,
        value.Size.Y * Height / ReferenceHeight);

    private float ScaleX(float value) => value * Width / ReferenceWidth;
    private float ScaleY(float value) => value * Height / ReferenceHeight;
    private float HandTop => Math.Max(ScaleY(510), PlayerRowBottom + ScaleY(24));
    private float PlayerRowBottom => Identity.Position.Y + OccupiedCardSize.Y;
    private Vector2 OccupiedCardSize => SpatialCardFootprint.OccupiedSize(
        PhysicalCardSize ?? new Vector2(176, 190));
    private float FootprintWidth => OccupiedCardSize.X;
}
