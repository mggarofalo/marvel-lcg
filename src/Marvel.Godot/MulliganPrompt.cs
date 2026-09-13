using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Godot;

/// <summary>Identifies the one opening-hand prompt that has a dedicated table surface.</summary>
internal static class MulliganPrompt
{
    internal static bool IsOpening(Prompt? prompt) => prompt is { Affordances.Count: 1 }
        && string.Equals(prompt.Affordances[0].Verb, Game.ResolveMulligans, StringComparison.Ordinal);
}
