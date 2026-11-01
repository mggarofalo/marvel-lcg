namespace Marvel.Godot;

/// <summary>Divides one game-area column between its text and its cards; every size is a presentation choice.</summary>
/// <remarks>
/// The heading and markers always get their measured height, so they are never clipped. Below them
/// a margin keeps activity badges drawn at a card's corner off the markers. The cards then keep
/// <see cref="MinimumCards"/> above a strip reserved for the attachments hung below them; the
/// explanation takes what remains up to its full height, and the cards take anything left over.
/// An explanation that does not fit whole shows whole lines only and scrolls, so its viewport never
/// cuts a line in half; one with no room for a line is hidden and stays in the column's tooltip.
/// </remarks>
internal sealed record GameAreaColumnLayout(float CardsTop, float CardsHeight, float DetailsTop, float DetailsHeight)
{
    internal const float BadgeMargin = 18;
    internal const float MinimumCards = 72;

    /// <summary>Arranges a column of the given height from the measured heights of its parts.</summary>
    internal static GameAreaColumnLayout Arrange(float height, GameAreaColumnText text, float attachments)
    {
        float cardsTop = 4 + text.Heading + 2 + text.Markers + BadgeMargin;
        float available = Math.Max(0, height - cardsTop - 4);
        float details = Math.Clamp(available - attachments - MinimumCards, 0, Math.Max(0, text.Details));
        if (details < text.Details && text.Line > 0) details = MathF.Floor(details / text.Line) * text.Line;
        float cards = Math.Max(0, available - attachments - details);
        return new GameAreaColumnLayout(cardsTop, cards, height - details - 2, details);
    }
}
