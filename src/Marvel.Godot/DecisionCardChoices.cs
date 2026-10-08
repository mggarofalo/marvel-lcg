using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Routes card-choice surfaces without changing their distinct draft operations.</summary>
internal sealed class DecisionCardChoices(DecisionPanel panel)
{
    private readonly SearchChoiceGallery search = new(panel);
    private readonly VisibleTargetCardGallery targets = new(panel);
    internal static bool IsChoice(Prompt? prompt) => SearchChoiceGallery.IsChoice(prompt)
        || VisibleTargetCardGallery.IsChoice(prompt);

    internal static string Heading(Prompt? prompt) => IsChoice(prompt)
        ? prompt?.DisplayQuestion ?? "Choose a card" : "Complete choices";

    internal void RefreshLayout()
    {
        search.RefreshLayout();
        targets.RefreshLayout();
    }

    internal void AddSearch(PromptPresentation prompt, int generation) => search.Add(prompt, generation);

    internal bool AddVisibleTargets(int generation)
    {
        if (!panel.CompleteChoicesOpen || !VisibleTargetCardGallery.IsChoice(panel.composer?.Prompt)) return false;
        targets.Add(generation);
        return true;
    }
}
