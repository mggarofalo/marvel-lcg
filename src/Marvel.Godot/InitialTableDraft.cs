using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Godot;

/// <summary>Opens required hand-staging tasks without accepting an engine answer.</summary>
internal static class InitialTableDraft
{
    internal static DecisionComposer Create(Prompt prompt)
    {
        var composer = new DecisionComposer(prompt);
        if (MulliganPrompt.IsOpening(prompt) || IsRequiredEndPhase(prompt))
            composer.SelectAffordance(prompt.Affordances[0].Id);
        return composer;
    }

    internal static bool IsRequiredEndPhase(Prompt prompt) => !prompt.Cancellable
        && prompt.Affordances.Count == 1
        && prompt.Affordances[0] is { Verb: Game.EndPhaseVerb, IsLegal: true, Targets: not null };
}
