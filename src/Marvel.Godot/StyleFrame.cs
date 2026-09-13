namespace Marvel.Godot;

/// <summary>Symmetric content and border measurements for a themed frame.</summary>
internal sealed record StyleFrame(
    int BorderWidth,
    int CornerRadius,
    float ContentHorizontal,
    float ContentVertical,
    float? ContentBottom = null,
    int? LeftBorder = null,
    int? BottomBorder = null);
