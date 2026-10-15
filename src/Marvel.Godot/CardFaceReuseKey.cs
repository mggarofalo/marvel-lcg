using System.Text.Json;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Exact authorized facts and geometry that determine one procedural face.</summary>
internal sealed record CardFaceReuseKey(string Presentation, CardDisplaySize Size,
    InterfaceScale Scale, CardLayoutMetrics Layout)
{
    // Presentation chooses exact serialized value equality, including nested collections,
    // so a newly supplied visibility, live value or explanation cannot retain an old face.
    internal static CardFaceReuseKey From(BoardCardPresentation card, CardDisplaySize size,
        InterfaceScale scale, CardLayoutMetrics layout) =>
        new(JsonSerializer.Serialize(card), size, scale, layout);
}
