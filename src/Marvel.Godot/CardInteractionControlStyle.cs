namespace Marvel.Godot;

/// <summary>Copy and layering for a card-local decision control.</summary>
internal static class CardInteractionControlStyle
{
    internal static string Symbol(CardInteractionControlDescriptor descriptor) =>
        (descriptor.Cue & (CardInteractionCue.SelectedTarget | CardInteractionCue.SelectedGenerator
            | CardInteractionCue.SelectedDestructiveChoice)) != 0 ? "✓"
        : descriptor.Intent switch
        {
            CardInteractionIntent.Action => "↗",
            CardInteractionIntent.Target => "◎",
            CardInteractionIntent.Generator => "+",
            _ => descriptor.Text,
        };

    internal static string Tooltip(CardInteractionIntent intent) => intent switch
    {
        CardInteractionIntent.Target => "Choose this offered target.",
        CardInteractionIntent.Generator => "Use this offered resource generator.",
        CardInteractionIntent.Cost => "Choose this engine-offered payment option.",
        CardInteractionIntent.Submit => "Confirm this composed action.",
        CardInteractionIntent.Decline => "Pass this optional decision.",
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
