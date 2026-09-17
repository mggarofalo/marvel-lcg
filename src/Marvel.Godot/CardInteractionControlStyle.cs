namespace Marvel.Godot;

/// <summary>Copy and layering for a card-local decision control.</summary>
internal static class CardInteractionControlStyle
{
    internal static string Tooltip(CardInteractionIntent intent) => intent switch
    {
        CardInteractionIntent.Target => "Choose this offered target.",
        CardInteractionIntent.Generator => "Use this offered resource generator.",
        CardInteractionIntent.Cost => "Choose this engine-offered payment option.",
        CardInteractionIntent.Submit => "Execute the composed engine-authorized action.",
        CardInteractionIntent.Decline => "Pass this engine-authorized decision.",
        _ => "Choose this card's offered action.",
    };

    internal static int Layer(CardInteractionIntent intent) => intent switch
    {
        CardInteractionIntent.Submit => 720,
        CardInteractionIntent.Decline => 710,
        CardInteractionIntent.Cost => 700,
        CardInteractionIntent.Target => 680,
        CardInteractionIntent.Generator => 660,
        _ => 640,
    };
}
