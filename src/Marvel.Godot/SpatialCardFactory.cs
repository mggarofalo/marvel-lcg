using Marvel.View;

namespace Marvel.Godot;

/// <summary>Creates physical table faces at one reading scale during an adjacent render transfer.</summary>
internal sealed class SpatialCardFactory(InterfaceScale scale, ICardArtProvider? art, BoardCardFaceReuse? reuse,
    InterfaceScale readingScale = InterfaceScale.Standard)
{
    internal InterfaceScale Scale => scale;
    internal InterfaceScale ReadingScale => readingScale;
    internal ICardArtProvider? Art => art;

    internal CardControl Create(BoardCardPresentation card, CardDisplaySize size) =>
        CardControl.CreateReconciled(card, size, scale, art, reuse, readingScale);

    internal CardControl CreateFitted(BoardCardPresentation card, float factor) =>
        CardControl.CreateFitted(card, scale, art, factor, reuse, readingScale);
}
