using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>One full B1 face and its measured authorized state, owned by the containing scene.</summary>
internal sealed record CardInspectionContent(CardControl Face, Control Body)
{
    internal static CardInspectionContent Create(BoardCardPresentation card, InterfaceScale scale,
        ICardArtProvider? art, bool beside, Action<BoardCardPresentation>? inspect = null)
    {
        CardControl face = CardControl.Create(card, CardDisplaySize.Full, scale, art);
        return new(face, CardStateDetails.Wrap(face, card, beside, inspect));
    }
}
