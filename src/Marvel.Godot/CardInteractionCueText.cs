namespace Marvel.Godot;

/// <summary>Pairs each table cue with its visible symbol and accessible meaning.</summary>
internal static class CardInteractionCueText
{
    internal static string Symbol(CardInteractionCue cue) =>
        (cue & (CardInteractionCue.SelectedTarget | CardInteractionCue.SelectedGenerator
            | CardInteractionCue.SelectedDestructiveChoice)) != 0 ? "✓"
        : cue.HasFlag(CardInteractionCue.LegalTarget) ? "◎"
        : cue.HasFlag(CardInteractionCue.OfferedAction) ? "↗"
        : cue.HasFlag(CardInteractionCue.Unavailable) ? "×" : "";

    internal static string Description(CardInteractionCue cue) =>
        cue.HasFlag(CardInteractionCue.SelectedTarget) ? "Selected target"
        : cue.HasFlag(CardInteractionCue.SelectedGenerator) ? "Selected payment source"
        : cue.HasFlag(CardInteractionCue.SelectedDestructiveChoice) ? "Staged discard"
        : cue.HasFlag(CardInteractionCue.LegalTarget) ? "Available target"
        : cue.HasFlag(CardInteractionCue.OfferedAction) ? "Available action"
        : cue.HasFlag(CardInteractionCue.Unavailable) ? "Unavailable" : "";
}
