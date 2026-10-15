using Godot;

namespace Marvel.Godot;

/// <summary>Installs native pointers at their actual pixel size for the application lifetime.</summary>
internal static class TableCursorTheme
{
    private static readonly Dictionary<string, Input.CursorShape> shapes = new()
    {
        ["arrow"] = Input.CursorShape.Arrow,
        ["point"] = Input.CursorShape.PointingHand,
        ["grab"] = Input.CursorShape.Move,
        ["drag"] = Input.CursorShape.Drag,
        ["drop"] = Input.CursorShape.CanDrop,
        ["forbidden"] = Input.CursorShape.Forbidden,
        ["text"] = Input.CursorShape.Ibeam,
    };
    private static readonly List<ImageTexture> textures = [];
    private static Window? lifetime;
    private static int installedSize;

    internal static void Apply(Control owner, InterfaceScale scale)
    {
        int size = TableCursorArtwork.PixelSize(scale);
        if (installedSize == size) return;
        ReleaseImages();
        if (lifetime is null)
        {
            lifetime = owner.GetTree().Root;
            lifetime.TreeExiting += Release;
        }
        foreach ((string name, Input.CursorShape shape) in shapes)
        {
            using var image = new Image();
            Error result = image.LoadSvgFromString(TableCursorArtwork.Svg(name), size / 32f);
            if (result != Error.Ok) throw new InvalidOperationException($"Cursor {name}: {result}");
            ImageTexture texture = ImageTexture.CreateFromImage(image);
            textures.Add(texture);
            (float x, float y) = TableCursorArtwork.Hotspot(name, size);
            Input.SetCustomMouseCursor(texture, shape, new Vector2(x, y));
        }
        installedSize = size;
    }

    private static void ReleaseImages()
    {
        foreach (Input.CursorShape shape in shapes.Values) Input.SetCustomMouseCursor(null, shape);
        foreach (ImageTexture texture in textures) texture.Dispose();
        textures.Clear();
        installedSize = 0;
    }

    private static void Release()
    {
        if (lifetime is not null) lifetime.TreeExiting -= Release;
        lifetime = null;
        ReleaseImages();
    }
}
