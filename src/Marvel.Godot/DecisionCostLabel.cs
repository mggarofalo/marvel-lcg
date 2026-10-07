using Godot;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Shows engine-provided costs with canonical symbols for resource restrictions.</summary>
internal static class DecisionCostLabel
{
    internal static HBoxContainer Create(CostOption cost, WorldDescriptor world, string prefix = "", bool showTarget = true)
    {
        var row = new HBoxContainer();
        row.Name = "PaymentCost";
        row.AddChild(Requirements(cost, prefix));
        if (showTarget && cost.Target != 0)
        {
            Label target = DecisionPanel.Text($"· {PromptPresentation.Describe(cost.Target, world)}",
                GodotThemeVariations.Body, wrap: true);
            target.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(target);
        }
        return row;
    }

    internal static VBoxContainer Requirements(CostOption cost, string prefix = "")
    {
        var rows = new VBoxContainer { Name = "PaymentRequirements" };
        foreach (ResourceCost component in cost.ResourceCosts)
            rows.AddChild(ResourceIconRendering.Row($"{prefix}Pay {component.Cost}",
                string.Concat(component.Rule ?? []), GodotThemeVariations.Body));
        if (cost.Components is null && cost.HasAlternative)
            rows.AddChild(ResourceIconRendering.Row($"OR {cost.OrCost}",
                string.Concat(cost.OrRule ?? []), GodotThemeVariations.Body));
        return rows;
    }
    internal static string Accessible(CostOption cost) =>
        $"Pay {cost.Cost} {CardRulesMarkup.ResourceNames(string.Concat(cost.Rule ?? []))}"
        + (cost.HasAlternative
            ? $" or {cost.OrCost} {CardRulesMarkup.ResourceNames(string.Concat(cost.OrRule ?? []))}"
            : string.Empty);

}
