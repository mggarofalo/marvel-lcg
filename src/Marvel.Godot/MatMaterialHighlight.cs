using Godot;

namespace Marvel.Godot;

/// <summary>Paints a quiet top highlight and local encounter/player signature.</summary>
internal static class MatMaterialHighlight
{
    internal static void Attach(PanelContainer mat, VisualColor signature)
    {
        mat.Draw += () =>
        {
            float end = Math.Max(12, mat.Size.X - 12);
            mat.DrawLine(new Vector2(12, 1), new Vector2(end, 1), new Color(1, 1, 1, 0.05f), 1, true);
            mat.DrawLine(new Vector2(12, 3), new Vector2(Math.Min(end, 52), 3),
                new Color(ClientTheme.ToGodot(signature), 0.65f), 2, true);
        };
    }
}
