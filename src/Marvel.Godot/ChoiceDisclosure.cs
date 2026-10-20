using Marvel.Rules.Prompts;

namespace Marvel.Godot;

/// <summary>Names navigation to the complete prompt-bound composer.</summary>
internal static class ChoiceDisclosure
{
    internal static string Label(Prompt? prompt) =>
        prompt?.PublicKind == PublicDecisionKind.PlayerAction ? "All actions"
            : DecisionDialogLayout.Automatic(prompt) ? "Resume current choice" : "All choices";
}
