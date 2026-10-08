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
        var body = new VBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        panel.AddChild(body);
        surface.AddChild(panel);
        Vector2 innerSize = rect.Size - panel.GetThemeStylebox("panel").GetMinimumSize();
        float actionsWidth = Math.Min(580, innerSize.X * 0.38f);
        var columns = new HBoxContainer
        {
            Name = "DecisionColumns", MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsVertical = Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
        };
        body.AddChild(columns);
        PromptPresentation? presentation = prompt is null ? null : PromptPresentation.From(prompt, world);
        string heading = presentation?.Heading ?? PendingSituationPresentation.Heading(world);
        if (presentation is not null) heading += $" · {presentation.Context}";
        Label headingLabel = Copy(heading, GodotThemeVariations.Body);
        headingLabel.Name = "ContextualHeading";
        body.AddChild(headingLabel);
        body.MoveChild(headingLabel, 0);
        var causeScroll = TableScrollNavigation.Create(columns, "CausalContext", "details");
        var cause = new VBoxContainer
        {
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        causeScroll.AddChild(cause);
        string context = presentation is null
            ? PendingSituationPresentation.Context(world)
            : presentation.Resolution.Replace("\n", " · ");
        Label resolution = Copy(context, GodotThemeVariations.Caption);
        cause.AddChild(resolution);
        Label summary = Copy(string.Empty, GodotThemeVariations.Caption);
        summary.Name = "DraftProgress";
        cause.AddChild(summary);
        result.RegisterContextualSummary(summary, string.Empty, resolution);
        VBoxContainer actions = TableActionPages.Create(columns, actionsWidth, result.OpenCompleteChoices);
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
