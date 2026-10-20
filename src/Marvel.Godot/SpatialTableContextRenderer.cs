using Godot;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>A compact current question, with one nearby path to the complete draft.</summary>
internal static class SpatialTableContextRenderer
{
    internal static void Add(Control surface, BoardRenderResult result, AstraTableGeometry geometry,
        WorldDescriptor? world, Prompt? prompt)
    {
        if (world is null) return;
        result.RegisterContextualWorld(world);
        EventActivitySurface.Add(surface, new Rect2(geometry.SourceTableau.Position.X,
            geometry.Revealing.Position.Y, geometry.SourceTableau.Size.X,
            geometry.SourceTableau.Position.Y - geometry.Revealing.Position.Y - 4));
        Rect2 bounds = geometry.Context;
        var panel = new HBoxContainer { Name = "ContextualDecision", Position = bounds.Position,
            Size = bounds.Size, CustomMinimumSize = bounds.Size, MouseFilter = Control.MouseFilterEnum.Ignore,
            ZIndex = 60 };
        surface.AddChild(panel);
        TableCanvasReflow.AnchorBottom(surface, panel);
        var copy = new VBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            MouseFilter = Control.MouseFilterEnum.Ignore };
        var summaryViewport = new ScrollContainer { Name = "DraftSummaryViewport",
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Reserve,
            FocusMode = Control.FocusModeEnum.All, FollowFocus = true };
        summaryViewport.AddChild(copy);
        panel.AddChild(summaryViewport);
        Label resolution = AddSituation(surface, geometry, world, prompt);
        Label summary = Copy("");
        summary.Name = "DraftProgress";
        summary.AutowrapMode = TextServer.AutowrapMode.Word;
        summary.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        copy.AddChild(summary);
        summary.VisibilityChanged += () => summaryViewport.Visible = summary.Visible;
        summaryViewport.Visible = summary.Visible;
        result.RegisterContextualSummary(summary, "", resolution);
        var scroll = new ScrollContainer { Name = "ContextualActionScroll", CustomMinimumSize = new Vector2(330, 0),
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled, VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
            FollowFocus = true };
        panel.AddChild(scroll);
        var actions = new VBoxContainer { Name = "ContextualActionObjects", SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        scroll.AddChild(actions);
        result.RegisterContextualActions(actions);
        var complete = new Button { Name = "CompleteChoiceSheet", Text = ChoiceDisclosure.Label(prompt),
            CustomMinimumSize = new Vector2(148, 44), Visible = prompt is not null };
        panel.AddChild(complete);
        result.RegisterCompleteChoices(complete);
        TableCompactButtonStyle.Apply(complete);
    }

    private static Label AddSituation(Control surface, AstraTableGeometry geometry,
        WorldDescriptor world, Prompt? prompt)
    {
        PromptPresentation? presentation = prompt is null ? null : PromptPresentation.From(prompt, world);
        float referenceWidth = surface.HasNode("CausalReferenceStack") ? 180 : 0;
        var cause = new VBoxContainer { Name = "CausalSituation", Position = geometry.Revealing.Position + new Vector2(referenceWidth, 0),
            Size = new Vector2(Math.Max(150, geometry.SourceTableau.Position.X
                - geometry.Revealing.Position.X - referenceWidth - 12), geometry.Revealing.Size.Y),
            MouseFilter = Control.MouseFilterEnum.Ignore };
        cause.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        surface.AddChild(cause);
        var prose = new VBoxContainer { Name = "CausalSituationCopy",
            MouseFilter = Control.MouseFilterEnum.Ignore };
        prose.Ready += () => AbilitySituationOwnership.Apply(ClientSceneHost.MainFor(prose), prose);
        cause.AddChild(prose);
        Label heading = Copy(prompt?.PublicKind == PublicDecisionKind.PlayerAction ? ""
            : presentation?.Heading ?? PendingSituationPresentation.Heading(world));
        heading.AutowrapMode = TextServer.AutowrapMode.Word;
        heading.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        heading.Name = "ContextualHeading";
        prose.AddChild(heading);
        heading.AddThemeFontSizeOverride("font_size", 18);
        Label resolution = AddResolution(prose, presentation, world, prompt);
        Label operation = Copy("");
        operation.Name = "SpatialOperationalNotice";
        operation.AutowrapMode = TextServer.AutowrapMode.Word;
        operation.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        operation.Visible = false;
        cause.AddChild(operation);
        return resolution;
    }

    private static Label AddResolution(VBoxContainer cause, PromptPresentation? presentation,
        WorldDescriptor world, Prompt? prompt)
    {
        bool ordinaryTurn = prompt?.PublicKind == PublicDecisionKind.PlayerAction;
        Label resolution = Copy(ordinaryTurn ? ""
            : presentation?.Resolution.Replace("\n", " · ") ?? PendingSituationPresentation.Context(world));
        resolution.Name = "CausalContext";
        resolution.Visible = !string.IsNullOrWhiteSpace(resolution.Text);
        resolution.AutowrapMode = TextServer.AutowrapMode.Word;
        resolution.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        cause.AddChild(resolution);
        if (presentation is not null && !ordinaryTurn)
        {
            Label answerer = Copy(presentation.Context + " · " + presentation.Requirement);
            answerer.AutowrapMode = TextServer.AutowrapMode.Word;
            answerer.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
            cause.AddChild(answerer);
        }
        return resolution;
    }

    private static Label Copy(string text)
    {
        var label = new Label { Text = text, ThemeTypeVariation = GodotThemeVariations.Caption,
            MouseFilter = Control.MouseFilterEnum.Ignore, SizeFlagsHorizontal = Control.SizeFlags.ExpandFill,
            AutowrapMode = TextServer.AutowrapMode.Off, TextOverrunBehavior = TextServer.OverrunBehavior.TrimEllipsis };
        label.AddThemeFontSizeOverride("font_size", 15);
        return label;
    }
}
