using Godot;

namespace Marvel.Godot;

/// <summary>Synthetic native tooltip authoring examples; no gameplay authority.</summary>
public partial class TooltipPolicyProbe : Control
{
    public override void _Ready()
    {
        var clipped = new Button
        {
            Name = "ClippedReference", Text = "A very long card title that cannot fit",
            Position = new Vector2(20, 20), Size = new Vector2(120, 44), ClipText = true,
            TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis,
            Disabled = true, AccessibilityName = "Event reference: A very long card title that cannot fit",
        };
        clipped.Size = new Vector2(120, 44);
        TooltipPolicy.ClippedCaption(clipped, clipped.Text);
        AddChild(clipped);
        var readable = new Button
        {
            Name = "ReadableReference", Text = "Rhino", Position = new Vector2(20, 80),
            Size = new Vector2(120, 44), Disabled = true,
            AccessibilityName = "Event reference: Rhino",
        };
        TooltipPolicy.ClippedCaption(readable, readable.Text);
        AddChild(readable);
        var inspection = new Button
        {
            Name = "PinInspection", Text = "Rhino", Position = new Vector2(20, 140),
            Size = new Vector2(120, 44),
        };
        TooltipPolicy.Inspect(inspection, inspection.Text);
        AddChild(inspection);
    }
}
