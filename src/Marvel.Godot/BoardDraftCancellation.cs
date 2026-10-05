using Marvel.Decisions;
using Marvel.Rules.Prompts;

namespace Marvel.Godot;

/// <summary>Clears staged choices while retaining the current opening-hand task.</summary>
internal static class BoardDraftCancellation
{
    internal static DecisionComposer Clear(DecisionComposer current)
    {
        var fresh = InitialTableDraft.Create(current.Prompt);
        if (MulliganPrompt.IsOpening(current.Prompt) && current.Selected is { } selected)
            fresh.SelectAffordance(selected.Id);
        return fresh;
    }
}
