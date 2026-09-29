using Godot;
using Marvel.Decisions;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Offers reversible hand discards and resource abilities as distinct payment choices.</summary>
internal sealed class DecisionPaymentSources
{
    private readonly DecisionPanel panel;
    private readonly DecisionComposer composer;
    private readonly TableDraftBinding operations;

    internal DecisionPaymentSources(DecisionPanel panel, DecisionComposer composer, int generation)
    {
        this.panel = panel;
        this.composer = composer;
        operations = panel.BindTableDraft(composer, generation);
    }

    internal void Add(CostOption cost)
    {
        PaymentSourcePresentation[] sources = [.. cost.Generators
            .Select(source => PaymentSourcePresentation.From(source, panel.world!))];
        AddGroup(sources.Where(source => source.DiscardsCard).ToArray(), "Discard cards from hand");
        AddGroup(sources.Where(source => !source.DiscardsCard).ToArray(), "Resource abilities");
    }

    private void AddGroup(PaymentSourcePresentation[] sources, string heading)
    {
        if (sources.Length == 0) return;
        panel.AddContent(DecisionPanel.Text(heading, GodotThemeVariations.Heading));
        foreach (PaymentSourcePresentation source in sources)
        {
            bool selected = composer.Resources.Contains(source.Id);
            string verb = source.DiscardsCard ? "Discard" : "Use";
            string resources = string.Join(" + ", source.Resources.Select(DecisionResourceName.For));
            var button = new Button
            {
                Name = $"Resource{source.Id}",
                Text = $"{(selected ? "✓ " : "")}{verb} {source.Name}   ·   {resources}",
                Alignment = HorizontalAlignment.Left,
                ToggleMode = true,
                ButtonPressed = selected,
                Disabled = panel.submitting,
                TooltipText = selected ? "Remove this source from the payment." : "Add this source to the payment.",
            };
            panel.StyleButton(button, selected ? InteractiveVisualState.Selected : InteractiveVisualState.Resting);
            button.Pressed += () =>
            {
                if (operations.TryToggleGenerator(source.Id)) panel.Rebuild();
            };
            panel.AddContent(button);
            if (!source.DiscardsCard && !string.IsNullOrWhiteSpace(source.Reference))
            {
                panel.AddContent(DecisionPanel.Text(source.Reference, GodotThemeVariations.Caption, wrap: true));
            }
        }
    }
}
