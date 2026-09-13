using Godot;

namespace Marvel.Godot;

/// <summary>Semantic inputs for one themed button family.</summary>
internal sealed record ButtonStyle(
    string Variation,
    Color Background,
    Color Border,
    int LeftBorder = 1,
    string Basis = "Button");
