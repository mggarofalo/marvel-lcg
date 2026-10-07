using Godot;

namespace Marvel.Godot;

/// <summary>Reserves one readable row of full cards and fixed decision controls.</summary>
internal static class SearchChoiceLayout
{
    internal static Rect2 Frame(Vector2 viewport)
    {
        var size = new Vector2(Math.Min(1760, viewport.X - 48), Math.Min(900, viewport.Y - 48));
        return new Rect2((viewport - size) / 2, size);
    }

    internal static InterfaceScale CardScale(Vector2 viewport) =>
        Frame(viewport).Size.Y >= 900 ? InterfaceScale.Standard : InterfaceScale.Percent80;

    internal static int Capacity(Vector2 viewport)
    {
        float width = VisualSystem.Card(CardDisplaySize.Full, CardScale(viewport)).Width;
        return Math.Max(1, (int)((Frame(viewport).Size.X - 64) / (width + 24)));
    }
}
