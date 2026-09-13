using Godot;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Adds only prompt-supplied actions and ordinary targets beside a visible card.</summary>
internal static class BoardLocalInteractionRenderer
{
    internal static void Add(
        Control destination,
        BoardCardPresentation card,
        BoardRenderContext context,
        CardLayoutMetrics geometry)
    {
        int row = 0;
        foreach (Affordance action in context.Interaction.ActionsFor(card.TargetId))
        {
            var button = new Button
            {
                Name = $"LocalAffordance{action.Id}",
                Text = action.IsLegal
                    ? $"◇ {PromptPresentation.Words(action.Verb)}"
                    : $"— UNAVAILABLE · {PromptPresentation.Words(action.Verb)}",
                Disabled = !action.IsLegal,
                TooltipText = action.Illegal ?? action.Description,
                CustomMinimumSize = new Vector2(
                    Math.Min(geometry.Width, VisualSystem.Controls(context.Scale).MinimumButtonWidth),
                    VisualSystem.Controls(context.Scale).MinimumHeight),
                ThemeTypeVariation = action.IsLegal
                    ? GodotThemeVariations.LegalTargetButton
                    : GodotThemeVariations.UnavailableButton,
            };
            button.Pressed += () => context.Result.RequestAffordance(action.Id);
            Place(button, geometry, row++);
            destination.AddChild(button);
        }

        if (!context.Interaction.CanTarget(card.TargetId) || card.TargetId is not { } target)
        {
            return;
        }
        bool selected = context.Interaction.IsSelected(target);
        var choose = new Button
        {
            Name = $"LocalTarget{target}",
            Text = selected ? "✓ SELECTED" : "◇ TARGET",
            ToggleMode = true,
            ButtonPressed = selected,
            CustomMinimumSize = new Vector2(
                Math.Min(geometry.Width, VisualSystem.Controls(context.Scale).MinimumButtonWidth),
                VisualSystem.Controls(context.Scale).MinimumHeight),
            ThemeTypeVariation = selected
                ? GodotThemeVariations.SelectedTargetButton
                : GodotThemeVariations.LegalTargetButton,
        };
        choose.Pressed += () => context.Result.RequestTarget(target);
        Place(choose, geometry, row);
        destination.AddChild(choose);
    }

    private static void Place(Button button, CardLayoutMetrics geometry, int row)
    {
        float height = button.CustomMinimumSize.Y;
        float width = button.CustomMinimumSize.X;
        button.Position = new Vector2(
            geometry.Width - width,
            Math.Max(0, geometry.MinimumHeight - height * (row + 1)));
        button.Size = new Vector2(width, height);
        button.ZIndex = 2;
    }
}
