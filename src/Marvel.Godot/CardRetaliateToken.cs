using System.Globalization;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Shows the supplied live retaliation value separately from printed rules.</summary>
internal static class CardRetaliateToken
{
    internal static string Caption(BoardCardPresentation card) =>
        !card.Concealed && card.Retaliate is > 0
            ? $"Retaliate {card.Retaliate.Value.ToString(CultureInfo.InvariantCulture)}" : string.Empty;

}
