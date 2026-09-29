using Godot;

namespace Marvel.Godot;

/// <summary>The fixed 1920x1080 spatial grammar for the Astra desktop table.</summary>
/// <remarks>
/// These coordinates are a presentation choice. They describe a physical reading
/// order inside the table column and contain no gameplay or legality decisions.
/// </remarks>
internal sealed record AstraTableGeometry(float Width, float Height, bool LargeText)
{
    internal const float ReferenceWidth = 1320;
    internal const float ReferenceHeight = 930;

    internal Rect2 VillainMat => Scale(new Rect2(8, 8, 1304, 282));
    internal Rect2 PlayerMat => Scale(new Rect2(8, 318, 1304, 596));
    internal Rect2 EncounterDiscard => Scale(new Rect2(36, 72, 108, 142));
    internal Rect2 EncounterDeck => Scale(new Rect2(164, 72, 108, 142));
    internal Rect2 MainScheme => Scale(new Rect2(330, 76, 190, 176));
    internal Rect2 Villain => Scale(new Rect2(602, 54, 176, 220));
    internal Rect2 SideSchemes => Scale(new Rect2(812, 76, 230, 176));
    internal Rect2 SeatStrip => Scale(new Rect2(1060, 46, 224, 212));
    internal Rect2 EngagedEnemies => Scale(new Rect2(582, 294, 520, 126));
    internal Rect2 PlayerDiscard => Scale(new Rect2(36, 506, 108, 142));
    internal Rect2 PlayerDeck => Scale(new Rect2(164, 506, 108, 142));
    internal Rect2 Context => Scale(new Rect2(302, 350, 250, 260));
    internal Rect2 Identity => Scale(new Rect2(586, 421, 208, 189));
    internal Rect2 Allies => Scale(new Rect2(824, 446, 454, 200));
    internal Rect2 Assets => Scale(new Rect2(318, 650, 836, 116));
    internal Rect2 Hand => Scale(new Rect2(248, 630, 824, 264));
    internal Rect2 Overflow => Scale(new Rect2(1124, 702, 164, 72));

    internal SpatialCardPlacement HandCard(int index, int count, float cardWidth)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(index);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, count);

        Rect2 hand = Hand;
        float usable = Math.Max(cardWidth, hand.Size.X - cardWidth);
        float step = count == 1 ? 0 : Math.Min(cardWidth * 0.68f, usable / (count - 1));
        float spread = step * (count - 1);
        float start = hand.Position.X + (hand.Size.X - spread - cardWidth) / 2;
        float middle = (count - 1) / 2f;
        float distance = index - middle;
        float rotation = Mathf.DegToRad(Mathf.Clamp(distance * 2.2f, -7, 7));
        float drop = Mathf.Abs(distance) * 3.5f;
        return new SpatialCardPlacement(
            new Vector2(start + index * step, hand.Position.Y + drop),
            rotation,
            20 + index,
            step < cardWidth);
    }

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
}
