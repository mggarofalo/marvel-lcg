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
        // Encounter groups and separated game areas use the entire far mat. Their timeline occupies
        // the seam below it, beside the causal references, rather than the source-tableau column.
        Rect2 activity = geometry.HasScenarioColumns
            ? geometry.ColumnActivity
            : new Rect2(geometry.SourceTableau.Position.X,
                geometry.Revealing.Position.Y, geometry.SourceTableau.Size.X,
                geometry.SourceTableau.Position.Y - geometry.Revealing.Position.Y - 4);
        EventActivitySurface.Add(surface, activity);
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
        Control resolution = AddSituation(surface, geometry, world, prompt);
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

    private static Control AddSituation(Control surface, AstraTableGeometry geometry,
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
        Control resolution = AddQuestion(prose, presentation, world, prompt, geometry.ReadingScale);
        Label operation = Copy("");
        operation.Name = "SpatialOperationalNotice";
        operation.AutowrapMode = TextServer.AutowrapMode.Word;
        operation.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        operation.Visible = false;
        cause.AddChild(operation);
        return resolution;
    }

    // The question stays one line; who answers, whether it is required, its causes and the attack are chips,
    // and the engine's whole instruction is the question's tooltip and the complete-choices sheet.
    private static Control AddQuestion(VBoxContainer prose, PromptPresentation? presentation, WorldDescriptor world,
        Prompt? prompt, InterfaceScale scale)
    {
        bool ordinaryTurn = prompt?.PublicKind == PublicDecisionKind.PlayerAction;
        Label heading = AddHeading(prose, ordinaryTurn ? ""
            : presentation?.Heading ?? PendingSituationPresentation.Heading(world), scale);
        if (presentation is null || ordinaryTurn)
            return AddPendingContext(prose, ordinaryTurn ? "" : PendingSituationPresentation.Context(world));
        heading.MouseFilter = Control.MouseFilterEnum.Stop;
        heading.TooltipText = TableWording.DecisionDetail(presentation.Frame, presentation.DeclineLabel);
        HFlowContainer facts = AddFacts(prose, presentation, scale);
        if (TableWording.UnstructuredConsequence(presentation.Frame) is { Length: > 0 } consequence)
            AddConsequence(prose, consequence);
        return facts;
    }

    // A decision whose consequence the engine states only in words keeps that statement on one trimmed line,
    // whole on hover, so its known effect stays in view.
    private static void AddConsequence(VBoxContainer cause, string text)
    {
        Label line = Copy(text);
        line.Name = "CausalConsequence";
        line.TooltipText = text;
        line.MouseFilter = Control.MouseFilterEnum.Stop;
        cause.AddChild(line);
    }

    // The question reads a step larger than its chips.
    private static Label AddHeading(VBoxContainer cause, string text, InterfaceScale scale)
    {
        Label heading = Copy(text);
        heading.AutowrapMode = TextServer.AutowrapMode.Word;
        heading.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        heading.Name = "ContextualHeading";
        cause.AddChild(heading);
        heading.AddThemeFontSizeOverride("font_size", Math.Max(18, VisualSystem.Type(scale).Heading + 2));
        return heading;
    }

    private static Label AddPendingContext(VBoxContainer cause, string text)
    {
        Label resolution = Copy(text);
        resolution.Name = "CausalContext";
        resolution.Visible = !string.IsNullOrWhiteSpace(resolution.Text);
        resolution.AutowrapMode = TextServer.AutowrapMode.Word;
        resolution.TextOverrunBehavior = TextServer.OverrunBehavior.NoTrimming;
        cause.AddChild(resolution);
        return resolution;
    }

    /// <summary>The decision's facts as one wrapping row of chips, read at the situation's text size.</summary>
    private static HFlowContainer AddFacts(VBoxContainer cause, PromptPresentation presentation, InterfaceScale scale)
    {
        var facts = new HFlowContainer { Name = "CausalContext", MouseFilter = Control.MouseFilterEnum.Ignore,
            SizeFlagsHorizontal = Control.SizeFlags.ExpandFill };
        int gap = VisualSystem.Spacing(scale).ExtraSmall;
        facts.AddThemeConstantOverride("h_separation", gap);
        facts.AddThemeConstantOverride("v_separation", gap);
        Wording[] wordings = DecisionSheetHead.Facts(presentation.Frame, presentation.DeclineLabel);
        int size = Math.Max(15, VisualSystem.Type(scale).Heading);
        for (int index = 0; index < wordings.Length; index++)
        {
            PanelContainer chip = Chip.Create($"CausalFact{index}", wordings[index], null, scale);
            if (chip.FindChild("Text", true, false) is Label label) label.AddThemeFontSizeOverride("font_size", size);
            facts.AddChild(chip);
        }
        cause.AddChild(facts);
        return facts;
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
