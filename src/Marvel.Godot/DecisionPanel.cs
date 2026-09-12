using Godot;
using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Renders one current prompt and composes its typed decision.</summary>
public sealed partial class DecisionPanel : VBoxContainer
{
    private InterfaceScale interfaceScale = ClientTheme.ConfiguredScale();
    internal ControlMetrics ControlMetrics => VisualSystem.Controls(interfaceScale);
    private DecisionComposer? composer;
    private VBoxContainer? content;
    private VBoxContainer? commit;
    private bool submitting;
    private WorldDescriptor? world;

    /// <summary>Raised with one answer built from the current prompt.</summary>
    public event Action<EngineDecision>? Submitted;

    /// <summary>Raised when an affordance or target points at a board object.</summary>
    public event Action<IReadOnlyList<int>>? AnchorFocused;

    /// <summary>Raised with the visible card whose action or target is under the pointer.</summary>
    public event Action<int?>? CardHovered;

    /// <summary>Raised whenever the visible draft's count-only progress changes.</summary>
    public event Action<DecisionProgressPresentation?>? ProgressChanged;

    /// <summary>Raised when a player opens one action's target and payment editor.</summary>
    public event Action? DraftStarted;

    /// <summary>Applies the current presentation-only desktop scale.</summary>
    public void SetInterfaceScale(InterfaceScale scale)
    {
        if (interfaceScale == scale)
        {
            return;
        }

        interfaceScale = scale;
        if (composer is not null)
        {
            Rebuild();
        }
    }

    /// <summary>Discards the old draft and renders the response's current prompt.</summary>
    public void Render(Prompt? prompt, WorldDescriptor currentWorld)
    {
        world = currentWorld ?? throw new ArgumentNullException(nameof(currentWorld));
        composer = prompt is null ? null : new DecisionComposer(prompt);
        submitting = false;
        Rebuild(focusFirst: true);
    }

    /// <summary>Prevents a second mutation while one response is outstanding.</summary>
    public void SetSubmitting(bool value)
    {
        submitting = value;
        Rebuild();
    }


    internal void NotifyAnchorFocused(IReadOnlyList<int> ids) =>
        AnchorFocused?.Invoke(ids);

    internal void NotifySubmitted(EngineDecision decision) =>
        Submitted?.Invoke(decision);

    internal void Rebuild(bool focusFirst = false)
    {
        Control? focused = GetViewport()?.GuiGetFocusOwner();
        string? focusName = focused is not null && IsAncestorOf(focused)
            ? FocusKey(focused)
            : null;
        ClearPanel();
        if (composer is null || world is null)
        {
            RenderNoDecision();
            return;
        }

        PromptPresentation prompt = PromptPresentation.From(composer.Prompt, world);
        CreateDecisionLayout(prompt);
        AddAffordances(prompt);
        AddSelectedDraft();
        AddDecline();
        ProgressChanged?.Invoke(composer.Progress());
        Callable.From(() => RestoreFocus(focusName, focusFirst)).CallDeferred();
    }

    private void ClearPanel()
    {
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }
        ThemeTypeVariation = GodotThemeVariations.TightStack;
    }

    private void RenderNoDecision()
    {
        ProgressChanged?.Invoke(null);
        (string heading, string detail) = world?.Outcome switch
        {
            Outcome.Unfinished => (
                "WAITING FOR ANOTHER PLAYER",
                "Another player has the current decision."),
            Outcome.PlayersWin => ("VICTORY", "The players won. No further decision is waiting."),
            Outcome.VillainWins => ("DEFEAT", "The villain won. No further decision is waiting."),
            Outcome.PlayersLose => ("DEFEAT", "The players lost. No further decision is waiting."),
            _ => ("NO DECISION", "No decision is available."),
        };
        AddChild(Text(heading, GodotThemeVariations.Eyebrow));
        AddChild(Text(detail, GodotThemeVariations.Body, wrap: true));
    }

    private void AddAffordances(PromptPresentation prompt)
    {
        var basicCharacters = new HashSet<int>();
        foreach (AffordancePresentation view in prompt.Affordances)
        {
            if (view.Verb is "Attack" or "Thwart" or "Recover"
                && basicCharacters.Add(view.AnchorId))
            {
                AddContent(Text(
                    $"BASIC ACTIONS  ·  {view.Anchor}",
                    GodotThemeVariations.Eyebrow,
                    wrap: true));
            }
            AddAffordance(view);
        }
    }

    private void AddAffordance(AffordancePresentation view)
    {
        Affordance option = composer!.Prompt.Affordances.Single(candidate => candidate.Id == view.Id);
        bool unavailable = submitting || !option.IsLegal;
        bool selected = composer.Selected?.Id == option.Id;
        bool resolving = submitting && selected;
        string action = DecisionCopy.Choice(view);
        var choose = new Button
        {
            Name = $"Affordance{option.Id}",
            Text = AffordanceText(action, unavailable, selected, resolving),
            Alignment = HorizontalAlignment.Left,
            Disabled = unavailable,
            ToggleMode = true,
            ButtonPressed = selected,
            TooltipText = option.Illegal ?? $"Anchor {option.AnchorId}, player {option.AnchorPlayer}",
        };
        StyleButton(choose, AffordanceState(option, selected, resolving));
        choose.Pressed += () => SelectAffordance(option);
        BindAnchors(choose, option.AnchorId);
        AddContent(choose);
        if (option.Illegal is not null)
        {
            AddContent(Text($"! {option.Illegal}", GodotThemeVariations.DangerText, wrap: true));
        }
    }

    private static string AffordanceText(
        string action, bool unavailable, bool selected, bool resolving) =>
        resolving
            ? $"✓ {action}  ·  resolving"
            : unavailable
                ? $"— Unavailable  ·  {action}"
                : selected ? $"✓ {action}" : action;

    private InteractiveVisualState AffordanceState(
        Affordance option, bool selected, bool resolving) =>
        resolving
            ? InteractiveVisualState.Selected
            : !option.IsLegal || submitting
                ? InteractiveVisualState.Unavailable
                : selected ? InteractiveVisualState.Selected : InteractiveVisualState.Resting;

    private void SelectAffordance(Affordance option)
    {
        composer!.SelectAffordance(option.Id);
        DraftStarted?.Invoke();
        AnchorFocused?.Invoke([option.AnchorId]);
        if (composer.Prompt.Asking == Question.Element
            && composer.Prompt.Affordances.Count == 1
            && composer.TryBuild(out EngineDecision? automatic, out _))
        {
            Submitted?.Invoke(automatic!);
            return;
        }
        Rebuild();
    }

    private void AddSelectedDraft()
    {
        if (composer!.Selected is not { } selected)
        {
            return;
        }
        DecisionProgressPresentation progress = composer.Progress();
        AddContent(new HSeparator());
        AddContent(Text(TargetProgressText(progress.Targets), GodotThemeVariations.Eyebrow));
        new DecisionDraftRenderer(this, composer, world!, submitting)
            .AddTargets(selected, progress.Targets);
        var payment = new DecisionPaymentRenderer(this, composer, world!, submitting);
        payment.AddCosts(selected);
        payment.AddSubmit(composer.Progress());
    }

    private void AddDecline()
    {
        if (!composer!.Prompt.Cancellable)
        {
            return;
        }
        var pass = new Button
        {
            Name = "Decline",
            Text = submitting ? "— UNAVAILABLE  ·  Pass / decline" : "Pass / decline",
            Disabled = submitting,
        };
        StyleButton(pass, submitting
            ? InteractiveVisualState.Unavailable
            : InteractiveVisualState.Resting);
        pass.Pressed += () =>
        {
            if (composer.TryDecline(out EngineDecision? decision, out _))
            {
                Submitted?.Invoke(decision!);
            }
        };
        AddCommit(pass);
    }

    private void CreateDecisionLayout(PromptPresentation prompt)
    {
        SizeFlagsHorizontal = SizeFlags.ExpandFill;
        SizeFlagsVertical = SizeFlags.ExpandFill;
        AffordancePresentation? selected = composer!.Selected is { } option
            ? prompt.Affordances.Single(view => view.Id == option.Id)
            : null;
        if (selected is not null)
        {
            var summary = new VBoxContainer
            {
                Name = "ActionSummary",
                ThemeTypeVariation = GodotThemeVariations.TightStack,
            };
            summary.AddChild(Text("Preparing", GodotThemeVariations.Eyebrow));
            summary.AddChild(Text(
                DecisionCopy.ActionSummary(selected),
                selected.Consequence is null
                    ? GodotThemeVariations.StatusText
                    : GodotThemeVariations.DangerText,
                wrap: true));
            AddChild(summary);
            AddChild(new HSeparator());
        }

        var scroll = new ScrollContainer
        {
            Name = "DecisionBodyScroll",
            CustomMinimumSize = composer.Selected?.CostOptions.Any(cost =>
                cost.Generators.Count > 0) == true
                    ? new Vector2(0, ControlMetrics.MinimumPointerTarget)
                    : Vector2.Zero,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            SizeFlagsVertical = SizeFlags.ExpandFill,
            FollowFocus = true,
            HorizontalScrollMode = ScrollContainer.ScrollMode.Disabled,
            VerticalScrollMode = ScrollContainer.ScrollMode.Auto,
        };
        content = new VBoxContainer
        {
            Name = "DecisionBody",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        scroll.AddChild(content);
        AddChild(scroll);

        commit = new VBoxContainer
        {
            Name = "CommitBar",
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            ThemeTypeVariation = GodotThemeVariations.TightStack,
        };
        AddChild(new HSeparator());
        AddChild(commit);
    }

    internal void AddContent(Control control) =>
        (content ?? throw new InvalidOperationException("decision content is unavailable"))
            .AddChild(control);

    internal void AddCommit(Control control) =>
        (commit ?? throw new InvalidOperationException("decision commit bar is unavailable"))
            .AddChild(control);

    internal string TargetProgressText(TargetSelectionProgress progress) =>
        composer?.Selected?.Verb == "Resolve Mulligans"
            ? $"DISCARD AND REDRAW  ·  {progress.Selected} CHOSEN"
                + (progress.IsSatisfied ? "  ·  READY" : "  ·  INCOMPLETE")
            : progress.Mode switch
            {
                TargetSelectionMode.None => "NO TARGET SELECTION",
                TargetSelectionMode.Grouped => $"GROUP  ·  {progress.Selected}/1 CHOSEN"
                    + (progress.IsSatisfied ? "  ·  COMPLETE" : "  ·  INCOMPLETE"),
                _ => $"TARGETS  ·  {progress.Selected} CHOSEN"
                    + (progress.Minimum == progress.Maximum
                        ? $"  ·  REQUIRED {progress.Minimum}"
                        : $"  ·  REQUIRED {progress.Minimum}–{progress.Maximum}")
                    + (progress.IsSatisfied ? "  ·  COMPLETE" : "  ·  INCOMPLETE"),
            };

    internal string TargetAction(bool selected) => composer!.Selected?.Verb switch
    {
        "Resolve Mulligans" => "DISCARD AND REDRAW",
        "End Phase" => "DISCARD",
        "Attack" => "ATTACK",
        "Thwart" => "THWART",
        _ => selected ? "CHOSEN" : "TARGET",
    };

    internal string SubmitAction()
    {
        Affordance selected = composer!.Selected!;
        int count = composer.Targets.Count;
        return selected.Verb switch
        {
            "Resolve Mulligans" when count == 0 => "Keep hand",
            "Resolve Mulligans" => $"Discard {count} and redraw",
            "End Phase" when count == 0 => "End player phase",
            "End Phase" => $"Discard {count} and end player phase",
            "Play" => $"Play {PromptPresentation.Describe(selected.AnchorId, world!)}",
            "Attack" when count == 1 =>
                $"Attack {PromptPresentation.Describe(composer.Targets[0], world!)}",
            "Thwart" when count == 1 =>
                $"Thwart {PromptPresentation.Describe(composer.Targets[0], world!)}",
            _ => DecisionCopy.GenericCommit(
                selected.Verb,
                selected.Label,
                PromptPresentation.Describe(selected.AnchorId, world!)),
        };
    }

    private void RestoreFocus(string? requested, bool focusFirst) =>
        DecisionFocus.Restore(this, requested, focusFirst);

    private string? FocusKey(Control focused) => DecisionFocus.Key(this, focused);

    internal void BindAnchors(Control control, params int[] ids)
    {
        if (ids.Length == 0)
        {
            return;
        }

        bool pointerInside = false;
        control.MouseEntered += () =>
        {
            pointerInside = true;
            AnchorFocused?.Invoke(ids);
            CardHovered?.Invoke(ids[0]);
        };
        control.MouseExited += () =>
        {
            pointerInside = false;
            if (!control.HasFocus())
            {
                CardHovered?.Invoke(null);
            }
        };
        control.FocusEntered += () => AnchorFocused?.Invoke(ids);
        control.FocusExited += () =>
        {
            if (!pointerInside)
            {
                CardHovered?.Invoke(null);
            }
        };
    }

    internal static string NodeKey(string value) => new(
        value.Select(character => char.IsLetterOrDigit(character) ? character : '_').ToArray());

    internal string CostLabel(CostOption cost)
    {
        string primary = $"Pay {cost.Cost}";
        if (cost.Rule is { Count: > 0 })
        {
            primary += $" [{string.Join(", ", cost.Rule)}]";
        }
        if (cost.HasAlternative)
        {
            primary += $"  OR  {cost.OrCost}";
            if (cost.OrRule is { Count: > 0 })
            {
                primary += $" [{string.Join(", ", cost.OrRule)}]";
            }
        }
        if (cost.Target != 0)
        {
            primary += $"  ·  {PromptPresentation.Describe(cost.Target, world!)}";
        }
        return primary;
    }

    internal static Label Text(string text, string variation, bool wrap = false)
    {
        return new Label
        {
            Text = text,
            AutowrapMode = wrap
                ? TextServer.AutowrapMode.WordSmart
                : TextServer.AutowrapMode.Off,
            ThemeTypeVariation = variation,
        };
    }

    internal void StyleButton(
        Button button,
        InteractiveVisualState state,
        bool compact = false)
    {
        InteractiveStyle style = VisualSystem.For(state);
        button.ThemeTypeVariation = style.ThemeVariation;
        button.AutowrapMode = TextServer.AutowrapMode.WordSmart;
        button.CustomMinimumSize = new Vector2(
            Math.Max(
                button.CustomMinimumSize.X,
                compact
                    ? ControlMetrics.MinimumPointerTarget
                    : ControlMetrics.MinimumButtonWidth),
            ControlMetrics.MinimumHeight);
    }

    internal static string ResourceName(char resource) => resource switch
    {
        Resources.Mental => "mental",
        Resources.Energy => "energy",
        Resources.Physical => "physical",
        Resources.Wild => "wild",
        _ => $"resource {resource}",
    };
}
