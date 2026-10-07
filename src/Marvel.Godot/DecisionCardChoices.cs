using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Routes card-choice surfaces without changing their distinct draft operations.</summary>
internal sealed class DecisionCardChoices(DecisionPanel panel)
{
    private readonly SearchChoiceGallery search = new(panel);
    private readonly MinionOrderGallery order = new(panel);

    internal static bool IsChoice(Prompt? prompt) => SearchChoiceGallery.IsChoice(prompt)
        || prompt?.PublicKind == PublicDecisionKind.MinionActivationOrder;

    internal static string Heading(Prompt? prompt) => prompt?.PublicKind == PublicDecisionKind.MinionActivationOrder
        ? "Choose activation order"
        : SearchChoiceGallery.IsChoice(prompt) ? "Choose a card" : "Complete choices";

    internal void RefreshLayout()
    {
        search.RefreshLayout();
        order.RefreshLayout();
    }

    internal void AddSearch(PromptPresentation prompt, int generation) => search.Add(prompt, generation);

    internal bool AddOrderedTargets(int generation)
    {
        if (!panel.CompleteChoicesOpen
            || panel.composer?.Prompt.PublicKind != PublicDecisionKind.MinionActivationOrder) return false;
        order.Add(generation);
        return true;
    }
}
