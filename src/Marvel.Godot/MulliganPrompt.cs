using Godot;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Godot;

/// <summary>Identifies the one opening-hand prompt that has a dedicated table surface.</summary>
internal static class MulliganPrompt
{
    internal static bool IsOpening(Prompt? prompt) => prompt is { Affordances.Count: 1 }
        && prompt.Affordances[0].Targets is not null
        && string.Equals(prompt.Affordances[0].Verb, Game.ResolveMulligans, StringComparison.Ordinal);

    internal static bool UsesDesktopTable(Prompt? prompt, Vector2 viewport) =>
        IsOpening(prompt) && DesktopTabletop.Uses(viewport);
}
