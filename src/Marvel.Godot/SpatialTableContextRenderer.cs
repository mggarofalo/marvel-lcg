using Godot;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Places decision and outcome context beside its related table objects.</summary>
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
        if (prompt is null)
        {
            AddOutcome(surface, geometry, world.Outcome);
            return;
        }
        PromptPresentation presentation = PromptPresentation.From(prompt, world);
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
        var copy = new Control { MouseFilter = Control.MouseFilterEnum.Ignore };
        AddLabel(copy, "CURRENT DECISION", GodotThemeVariations.Eyebrow,
            new Rect2(12, 10, rect.Size.X - 24, 26), 1);
        AddLabel(copy, presentation.Heading, GodotThemeVariations.Heading,
            new Rect2(12, 38, rect.Size.X - 24, 62), 2);
        AddLabel(copy, CompactContext(presentation.Context), GodotThemeVariations.Caption,
            new Rect2(12, 102, rect.Size.X - 24, 24), 1, wrap: false);
        string fallback = string.IsNullOrWhiteSpace(presentation.Resolution)
            ? "Select an action on a card."
            : WrapResolution(presentation.Resolution);
        Label summary = AddLabel(copy, fallback, GodotThemeVariations.Caption,
            new Rect2(12, 128, rect.Size.X - 24, 88), 3, trim: false);
        var actions = new HBoxContainer
        {
            Name = "ContextualActionObjects",
            ThemeTypeVariation = GodotThemeVariations.TightStack,
            MouseFilter = Control.MouseFilterEnum.Pass,
            Position = new Vector2(12, rect.Size.Y - 44),
            Size = new Vector2(rect.Size.X - 24, 40),
        };
        copy.AddChild(actions);
        panel.AddChild(copy);
        surface.AddChild(panel);
        result.RegisterContextualActions(actions);
        result.RegisterContextualSummary(summary, fallback);
    }

    private static void AddOutcome(
        Control surface, AstraTableGeometry geometry, Outcome outcome)
    {
        if (outcome == Outcome.Unfinished)
        {
            return;
        }
        bool victory = outcome == Outcome.PlayersWin;
        Rect2 rect = geometry.Context;
        var panel = new PanelContainer
        {
            Name = "TableOutcome",
            Position = rect.Position,
            Size = rect.Size,
            CustomMinimumSize = rect.Size,
            ThemeTypeVariation = GodotThemeVariations.SpatialDecision,
            ZIndex = 40,
        };
        var copy = new VBoxContainer { ThemeTypeVariation = GodotThemeVariations.TightStack };
        copy.AddChild(Label("GAME COMPLETE", GodotThemeVariations.Eyebrow));
        copy.AddChild(Label(victory ? "Victory" : "Defeat", GodotThemeVariations.DisplayTitle));
        copy.AddChild(Label(victory
            ? "The players won. The settled table remains available for inspection."
            : "The players lost. The settled table remains available for inspection.",
            GodotThemeVariations.Body, true));
        copy.AddChild(Label("Open History for the complete ordered record.",
            GodotThemeVariations.Caption, true));
        panel.AddChild(copy);
        surface.AddChild(panel);
    }

    private static Label Label(string text, string variation, bool wrap = false) => new()
    {
        Text = text,
        ThemeTypeVariation = variation,
        AutowrapMode = wrap ? TextServer.AutowrapMode.WordSmart : TextServer.AutowrapMode.Off,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

    private static string WrapResolution(string text)
    {
        string normalized = text.Replace('\r', ' ').Replace('\n', ' ');
        string[] segments = [.. normalized
            .Split('·', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => segment.Trim())];
        if (segments.Length >= 4)
        {
            string step = segments[1].Replace("Step ", string.Empty, StringComparison.Ordinal)
                .Replace(" of ", "/", StringComparison.Ordinal);
            string detail = segments[^1].StartsWith("Resolve ", StringComparison.Ordinal)
                ? segments[^1]["Resolve ".Length..]
                : segments[^1];
            if (detail.StartsWith("step ", StringComparison.OrdinalIgnoreCase))
            {
                detail = detail["step ".Length..];
            }
            return $"{segments[0]} · {step}\n{WrapWords(detail, 28)}";
        }
        return WrapWords(normalized, 22);
    }

    private static string WrapWords(string text, int lineLength)
    {
        var lines = new List<string>();
        string current = string.Empty;
        foreach (string word in text.Split(' ', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current.Length > 0 && current.Length + word.Length + 1 > lineLength)
            {
                lines.Add(current);
                current = word;
            }
            else
            {
                current = current.Length == 0 ? word : $"{current} {word}";
            }
        }
        if (current.Length > 0)
        {
            lines.Add(current);
        }
        return string.Join('\n', lines);
    }

    private static string CompactContext(string text)
    {
        string normalized = text.Replace('\r', ' ').Replace('\n', ' ');
        return string.Join(" · ", normalized
            .Split('·', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Trim())
            .Take(2));
    }

    private static Label AddLabel(
        Control parent,
        string text,
        string variation,
        Rect2 bounds,
        int lines,
        bool trim = true,
        bool wrap = true)
    {
        var label = new Label
        {
            Text = text,
            TooltipText = text,
            ThemeTypeVariation = variation,
            Position = bounds.Position,
            Size = bounds.Size,
            AutowrapMode = wrap
                ? TextServer.AutowrapMode.WordSmart
                : TextServer.AutowrapMode.Off,
            TextOverrunBehavior = trim
                ? TextServer.OverrunBehavior.TrimEllipsis
                : TextServer.OverrunBehavior.NoTrimming,
            MaxLinesVisible = lines,
            ClipText = true,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        parent.AddChild(label);
        return label;
    }
}
