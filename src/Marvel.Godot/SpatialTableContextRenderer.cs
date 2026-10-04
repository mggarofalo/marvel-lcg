using Godot;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Keeps the current cause, editable answer and latest result distinct on the table.</summary>
internal static class SpatialTableContextRenderer
{
    internal static void Add(
        Control surface,
        BoardRenderResult result,
        AstraTableGeometry geometry,
        WorldDescriptor? world,
        Prompt? prompt)
    {
        if (world is null)
        {
            return;
        }
        result.RegisterContextualWorld(world);
        Rect2 rect = geometry.Context;
        var panel = new PanelContainer
        {
            Name = "ContextualDecision",
            Position = rect.Position,
            Size = rect.Size,
            CustomMinimumSize = rect.Size,
            ThemeTypeVariation = GodotThemeVariations.SpatialDecision,
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 12,
            ClipContents = true,
        };
        var body = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddChild(body);
        surface.AddChild(panel);
        Vector2 innerSize = rect.Size - panel.GetThemeStylebox("panel").GetMinimumSize();
        float actionsWidth = Math.Min(400, innerSize.X * 0.32f);
        float copyWidth = innerSize.X - actionsWidth - 42;
        PromptPresentation? presentation = prompt is null ? null : PromptPresentation.From(prompt, world);
        string heading = presentation?.Heading ?? PendingSituationPresentation.Heading(world);
        if (presentation is not null) heading += $" · {presentation.Context}";
        var causeScroll = TableScrollNavigation.Create(body, "CausalContext", "details",
            new Rect2(14, 3, copyWidth, innerSize.Y - 6));
        var cause = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        causeScroll.AddChild(cause);
        cause.AddChild(Copy(heading, GodotThemeVariations.Body));
        string context = presentation is null
            ? PendingSituationPresentation.Context(world)
            : presentation.Resolution.Replace("\n", " · ");
        cause.AddChild(Copy(context, GodotThemeVariations.Caption));
        Label summary = Copy(string.Empty, GodotThemeVariations.Caption);
        summary.Name = "DraftProgress";
        cause.AddChild(summary);
        result.RegisterContextualSummary(summary, string.Empty);
        var actionScroll = TableScrollNavigation.Create(body, "ContextualActionScroll", "choices",
            new Rect2(innerSize.X - actionsWidth - 14, 3, actionsWidth, innerSize.Y - 6));
        var actions = new VBoxContainer
        {
            Name = "ContextualActionObjects",
            ThemeTypeVariation = GodotThemeVariations.TightStack,
            MouseFilter = Control.MouseFilterEnum.Pass,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        };
        actionScroll.AddChild(actions);
        result.RegisterContextualActions(actions);
    }

    private static Label Copy(string text, string variation) => new()
    {
        Text = text, ThemeTypeVariation = variation,
        AutowrapMode = TextServer.AutowrapMode.WordSmart,
        SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

}
