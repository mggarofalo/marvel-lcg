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
            var button = new Button
            {
                Name = $"Resource{source.Id}",
                Text = $"{(selected ? "✓ " : "")}{verb} {source.Name}",
                Alignment = HorizontalAlignment.Left,
                ToggleMode = true,
                ButtonPressed = selected,
                Disabled = panel.submitting,
                TooltipText = selected ? "Remove this source from the payment." : "Add this source to the payment.",
            };
            ResourceIconRendering.Apply(button, source.Resources);
            panel.StyleButton(button, selected ? InteractiveVisualState.Selected : InteractiveVisualState.Resting);
            button.Pressed += () =>
            {
                if (operations.TryToggleGenerator(source.Id)) panel.Rebuild();
            };
            var row = new HBoxContainer { ThemeTypeVariation = GodotThemeVariations.CompactRow };
            button.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            row.AddChild(button);
            row.AddChild(CardPaymentInspection.Button(panel, CardPaymentWorkspaceLayout.MainFor(panel), source.Id));
            panel.AddContent(row);
            if (!source.DiscardsCard && !string.IsNullOrWhiteSpace(source.Reference))
            {
                CardRulesMarkup.ResourceFont();
                panel.AddContent(new RichTextLabel
                {
                    Name = "ResourceAbilityReference", BbcodeEnabled = true, FitContent = true,
                    ScrollActive = false, MouseFilter = Control.MouseFilterEnum.Ignore,
                    Text = CardRulesMarkup.ToBbCode(source.ReferenceMarkup, source.Reference),
                });
            }
        }
    }
}
