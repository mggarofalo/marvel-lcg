using Godot;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Shows engine-provided costs with canonical symbols for resource restrictions.</summary>
internal static class DecisionCostLabel
{
    internal static HBoxContainer Create(CostOption cost, WorldDescriptor world, string prefix = "", bool showTarget = true)
    {
        HBoxContainer row = ResourceIconRendering.Row($"{prefix}Pay {cost.Cost}",
            string.Concat(cost.Rule ?? []), GodotThemeVariations.Body);
        row.Name = "PaymentCost";
        if (cost.HasAlternative)
            row.AddChild(ResourceIconRendering.Row($"OR {cost.OrCost}",
                string.Concat(cost.OrRule ?? []), GodotThemeVariations.Body));
        if (showTarget && cost.Target != 0)
        {
            Label target = DecisionPanel.Text($"· {PromptPresentation.Describe(cost.Target, world)}",
                GodotThemeVariations.Body, wrap: true);
            target.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(target);
        }
        return row;
    }
    internal static string Accessible(CostOption cost) =>
        $"Pay {cost.Cost} {CardRulesMarkup.ResourceNames(string.Concat(cost.Rule ?? []))}"
        + (cost.HasAlternative
            ? $" or {cost.OrCost} {CardRulesMarkup.ResourceNames(string.Concat(cost.OrRule ?? []))}"
            : string.Empty);

}
