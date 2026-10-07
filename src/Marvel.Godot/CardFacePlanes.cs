using Godot;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Paints the angled B1 fields independently of text, symbols, and interactions.</summary>
internal static class CardFacePlanes
{
    internal static void Add(Control face, BoardCardPresentation card, CardFaceRegions r, Vector2 size)
    {
        var planes = new Control { Name = "FacePlanes", MouseFilter = Control.MouseFilterEnum.Ignore };
        face.AddChild(planes);
        face.MoveChild(planes, 0);
        Color accent = CardFaceStyle.Accent(card);
        AddPolygon(planes, "PaperPlane", [Vector2.Zero, new(size.X, 0), size, new(0, size.Y)], CardFaceStyle.Paper);
        float cut = (r.Full ? 24 : 10) * r.Density;
        AddPolygon(planes, "InkPlane", [Vector2.Zero, new(size.X, 0), new(size.X, r.InkEnd - cut),
            new(0, r.InkEnd)], CardFaceStyle.Ink);
        if (r.Illustration.Size.Y > 0)
        {
            Rect2 art = r.Illustration;
            AddPolygon(planes, "AspectPlane", [new(art.Position.X - cut, art.Position.Y + cut),
                new(size.X, art.Position.Y - cut), new(size.X, r.InkEnd - cut),
                new(art.Position.X + cut, r.InkEnd)], accent);
        }
        if (r.Stats.Size.Y > 0)
            AddPolygon(planes, "ProtectedStatsBand", [new(0, r.Stats.Position.Y),
                new(size.X, r.Stats.Position.Y), new(size.X, r.InkEnd - cut / 2),
                new(0, r.InkEnd + cut / 2)], CardFaceStyle.Ink);
        float iconSize = (r.Full ? 20 : 10) * r.Density;
        planes.AddChild(CardGlyphRendering.Create(CardGlyphRendering.Aspect(card.Classification,
            VisualSystem.CardFrame(card.Kind).Family), "AspectIcon",
            new Rect2(size.X - iconSize - r.Kind.Position.X,
                r.Kind.Position.Y, iconSize, iconSize), Colors.White));
    }

    private static void AddPolygon(Control face, string name, Vector2[] points, Color color) =>
        face.AddChild(new Polygon2D { Name = name, Polygon = points, Color = color });
}
