using Godot;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Shows engine-provided costs with canonical symbols for resource restrictions.</summary>
internal static class DecisionCostLabel
{
    internal static void AttachTo(Button button, AffordancePresentation offer, bool compact = false)
    {
        if (offer.CostOptions is not [var cost] || cost.HasAlternative
            || cost.ResourceCosts is not [var component]) return;
        string label = button.Text;
        int end = label.IndexOf('\n', StringComparison.Ordinal);
        HBoxContainer row = ResourceIconRendering.Row(OfferCaption(end < 0 ? label : label[..end], component),
            Symbols(component), GodotThemeVariations.Caption);
        if (compact && row.GetChild(0) is Label caption)
        {
            caption.AutowrapMode = TextServer.AutowrapMode.Off;
            caption.AddThemeFontSizeOverride("font_size", 14);
        }
        string accessible = $"{label}. {Accessible(cost)}. {button.TooltipText}";
        button.Text = string.Empty;
        // Only the caption row carries control weight; the explanation reads as body copy.
        ResourceIconRendering.ButtonContent(button, end < 0 ? row : WithExplanation(row, label[(end + 1)..]), accessible, row);
    }

    // The price stays on the choice's first line, so explanation lines below it
    // cannot push it out of a compact caption.
    private static VBoxContainer WithExplanation(HBoxContainer row, string explanation)
    {
        var content = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        content.AddChild(row);
        content.AddChild(DecisionPanel.Text(explanation, GodotThemeVariations.Caption, wrap: true));
        return content;
    }

    internal static string OfferCaption(string label, ResourceCost component) => $"{label} · Pay {component.Cost}";

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
                Symbols(component), GodotThemeVariations.Body));
        if (cost.Components is null && cost.HasAlternative)
            rows.AddChild(ResourceIconRendering.Row($"OR {cost.OrCost}",
                string.Concat(cost.OrRule ?? []), GodotThemeVariations.Body));
        return rows;
    }
    internal static string Accessible(CostOption cost) =>
        string.Join("; ", cost.ResourceCosts.Select(component =>
            $"Pay {component.Cost} {CardRulesMarkup.ResourceNames(Symbols(component))}"))
        + (cost.HasAlternative
            ? $" or {cost.OrCost} {CardRulesMarkup.ResourceNames(string.Concat(cost.OrRule ?? []))}"
            : string.Empty);

    private static string Symbols(ResourceCost component) =>
        component.RepeatedResource?.ToString() ?? string.Concat(component.Rule ?? []);

}
