namespace Marvel.Godot;

/// <summary>Assigns known control responsibilities without interpreting visible game prose.</summary>
internal static class ButtonHierarchy
{
    internal static string Variation(string name, string declared) => name switch
    {
        "ContextualDecline" => GodotThemeVariations.ChoiceButton,
        "ToggleHistory" or "Earlier" or "More" or "CopyInvitation" or "CopyReport"
            or "SaveReport" or "Skip" or "Synchronize" or "UndoLast" or "CloseHistory"
            or "CloseChoiceSheet" or "CompleteChoiceSheet" => GodotThemeVariations.TertiaryButton,
        _ => declared,
    };
}
