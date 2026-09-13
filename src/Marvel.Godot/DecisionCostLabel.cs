using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Formats engine-provided payment options for the presentation layer.</summary>
internal static class DecisionCostLabel
{
    internal static string For(CostOption cost, WorldDescriptor world)
    {
        string primary = $"Pay {cost.Cost}";
        if (cost.Rule is { Count: > 0 })
        {
            primary += $" [{string.Join(", ", cost.Rule)}]";
        }
        if (cost.HasAlternative)
        {
            primary += $"  OR  {cost.OrCost}";
            if (cost.OrRule is { Count: > 0 })
            {
                primary += $" [{string.Join(", ", cost.OrRule)}]";
            }
        }
        if (cost.Target != 0)
        {
            primary += $"  ·  {PromptPresentation.Describe(cost.Target, world)}";
        }
        return primary;
    }
}
