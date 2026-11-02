using Godot;

namespace Marvel.Godot;

/// <summary>Divides one game-area column between its header and its cards; every size is a presentation choice.</summary>
/// <remarks>
/// The header is one line tall at the reading scale. Below it a margin keeps activity badges drawn at a
/// card's corner off the header. The cards take the rest above a strip reserved for the attachments hung
/// below them. The central column also stands the shared encounter piles side by side under its stage.
/// </remarks>
/// <param name="CardsTop">The cards' top, from the column's top.</param>
/// <param name="CardsHeight">The height the cards may take.</param>
internal sealed record GameAreaColumnLayout(float CardsTop, float CardsHeight)
{
    internal const float BadgeMargin = 18;
    internal const float Padding = 4;

    /// <summary>The most of the central column's height the shared piles take.</summary>
    internal const float PileShare = 0.55f;

    /// <summary>A pile's width-to-height ratio, the shape of a card.</summary>
    internal const float PileAspect = 70f / 112f;

    /// <summary>Arranges a column of the given height under a header, above an attachment strip.</summary>
    internal static GameAreaColumnLayout Arrange(float height, float header, float attachments)
    {
        float top = Padding + header + BadgeMargin;
        return new GameAreaColumnLayout(top, Math.Max(0, height - top - attachments - Padding));
    }

    /// <summary>
    /// Splits the central column's card room: the shared piles stand side by side along its bottom, as large as
    /// fits in <see cref="PileShare"/> of the room, and the stage takes what is above them.
    /// </summary>
    internal static (Rect2 Stage, Rect2 Discard, Rect2 Deck) Central(Rect2 cards, float gap)
    {
        float width = Math.Max(0, (cards.Size.X - gap) / 2);
        float height = Math.Min(cards.Size.Y * PileShare, width / PileAspect);
        Vector2 pile = new(height * PileAspect, height);
        float left = cards.Position.X + (cards.Size.X - 2 * pile.X - gap) / 2;
        float top = cards.End.Y - pile.Y;
        return (new Rect2(cards.Position, cards.Size.X, Math.Max(0, top - gap - cards.Position.Y)),
            new Rect2(new Vector2(left, top), pile), new Rect2(new Vector2(left + pile.X + gap, top), pile));
    }
}
