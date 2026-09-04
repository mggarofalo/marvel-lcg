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
    private ControlMetrics ControlMetrics => VisualSystem.Controls(interfaceScale);
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

    private void Rebuild(bool focusFirst = false)
    {
        Control? focused = GetViewport()?.GuiGetFocusOwner();
        string? focusName = focused is not null && IsAncestorOf(focused)
            ? FocusKey(focused)
            : null;
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }

        ThemeTypeVariation = GodotThemeVariations.TightStack;
        if (composer is null || world is null)
        {
            ProgressChanged?.Invoke(null);
            (string heading, string detail) = world?.Outcome switch
            {
                Outcome.Unfinished => (
                    "WAITING FOR ANOTHER PLAYER",
                    "Another player has the current decision."),
                Outcome.PlayersWin => (
                    "VICTORY",
                    "The players won. No further decision is waiting."),
                Outcome.VillainWins => (
                    "DEFEAT",
                    "The villain won. No further decision is waiting."),
                Outcome.PlayersLose => (
                    "DEFEAT",
                    "The players lost. No further decision is waiting."),
                _ => ("NO DECISION", "No decision is available."),
            };
            AddChild(Text(heading, GodotThemeVariations.Eyebrow));
            AddChild(Text(
                detail,
                GodotThemeVariations.Body,
                wrap: true));
            return;
        }

        PromptPresentation prompt = PromptPresentation.From(composer.Prompt, world);
        CreateDecisionLayout(prompt);
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
            Affordance option = composer.Prompt.Affordances.Single(candidate =>
                candidate.Id == view.Id);
            bool unavailable = submitting || !option.IsLegal;
            bool isSelected = composer.Selected?.Id == option.Id;
            bool resolving = submitting && isSelected;
            string action = DecisionCopy.Choice(view);
            var choose = new Button
            {
                Name = $"Affordance{option.Id}",
                Text = resolving
                    ? $"✓ {action}  ·  resolving"
                    : unavailable
                    ? $"— Unavailable  ·  {action}"
                    : isSelected
                        ? $"✓ {action}"
                        : action,
                Alignment = HorizontalAlignment.Left,
                Disabled = unavailable,
                ToggleMode = true,
                ButtonPressed = isSelected,
                TooltipText = option.Illegal ?? $"Anchor {option.AnchorId}, player {option.AnchorPlayer}",
            };
            StyleButton(
                choose,
                resolving
                    ? InteractiveVisualState.Selected
                    : !option.IsLegal || submitting
                    ? InteractiveVisualState.Unavailable
                    : choose.ButtonPressed
                        ? InteractiveVisualState.Selected
                        : InteractiveVisualState.Resting);
            choose.Pressed += () =>
            {
                composer.SelectAffordance(option.Id);
                AnchorFocused?.Invoke([option.AnchorId]);
                if (composer.Prompt.Asking == Question.Element
                    && composer.Prompt.Affordances.Count == 1
                    && composer.TryBuild(out EngineDecision? automatic, out _))
                {
                    Submitted?.Invoke(automatic!);
                    return;
                }
                Rebuild();
            };
            BindAnchors(choose, option.AnchorId);
            AddContent(choose);
            if (option.Illegal is not null)
            {
                AddContent(Text(
                    $"! {option.Illegal}",
                    GodotThemeVariations.DangerText,
                    wrap: true));
            }
        }

        if (composer.Selected is { } selected)
        {
            DecisionProgressPresentation progress = composer.Progress();
            AddContent(new HSeparator());
            AddContent(Text(TargetProgressText(progress.Targets), GodotThemeVariations.Eyebrow));
            AddTargets(selected, progress.Targets);
            AddCosts(selected);
            progress = composer.Progress();
            AddSubmit(progress);
        }

        if (composer.Prompt.Cancellable)
        {
            var pass = new Button
            {
                Name = "Decline",
                Text = submitting
                    ? "— UNAVAILABLE  ·  Pass / decline"
                    : "Pass / decline",
                Disabled = submitting,
            };
            StyleButton(
                pass,
                submitting
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

        ProgressChanged?.Invoke(composer.Progress());
        Callable.From(() => RestoreFocus(focusName, focusFirst)).CallDeferred();
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

    private void AddTargets(Affordance selected, TargetSelectionProgress progress)
    {
        TargetRequest? request = selected.Targets;
        if (request is null)
        {
            AddContent(Text("No target selection", GodotThemeVariations.MutedText));
            return;
        }

        if (composer!.UsesAutomaticTargetSelection)
        {
            Label automatic = Text(
                "TARGET  ·  " + string.Join(" → ", composer.Targets.Select(id =>
                    PromptPresentation.Describe(id, world!))) + "  ·  automatic",
                GodotThemeVariations.StatusText,
                wrap: true);
            automatic.Name = "AutomaticTargets";
            BindAnchors(automatic, [.. composer.Targets]);
            AddContent(automatic);
            return;
        }

        string badge = request.IsSearch ? "SEARCH RESULTS" : "TARGETS";
        AddContent(Text($"{badge}  ·  " + TargetProgressText(progress),
            GodotThemeVariations.Caption, wrap: true));
        if (!string.IsNullOrWhiteSpace(request.Rule))
        {
            AddContent(Text(request.Rule, GodotThemeVariations.Caption));
        }

        if (request.MustIncludeTraits is { Count: > 0 })
        {
            AddContent(Text(
                $"MUST INCLUDE  ·  {string.Join(", ", request.MustIncludeTraits)}",
                GodotThemeVariations.Caption, wrap: true));
        }

        if (request.IsGrouped)
        {
            for (int index = 0; index < request.Groups!.Count; index++)
            {
                IReadOnlyList<int> group = request.Groups[index];
                var choose = new Button
                {
                    Name = $"Group{index}",
                    Text = (composer!.Targets.SequenceEqual(group)
                            ? "✓ SELECTED  ·  "
                            : "◇ LEGAL  ·  ")
                        + $"Group {index + 1}  ·  "
                        + string.Join(" → ", group.Select(id =>
                            PromptPresentation.Describe(id, world!))),
                    Alignment = HorizontalAlignment.Left,
                    ToggleMode = true,
                    ButtonPressed = composer!.Targets.SequenceEqual(group),
                    Disabled = submitting,
                };
                StyleButton(
                    choose,
                    choose.ButtonPressed
                        ? InteractiveVisualState.Selected
                        : InteractiveVisualState.Legal);
                choose.Pressed += () =>
                {
                    composer.SelectTargets(group);
                    Rebuild();
                };
                BindAnchors(choose, [.. group]);
                AddContent(choose);
            }

            return;
        }

        foreach (int target in request.Legal.Distinct())
        {
            if (request.AllowRepeated)
            {
                AddRepeatedTarget(request, target);
            }
            else
            {
                AddOrdinaryTarget(target);
            }
        }

        if (composer!.Targets.Count > 0)
        {
            AddContent(Text(
                "ORDER  ·  " + string.Join(" → ", composer.Targets.Select(id =>
                    PromptPresentation.Describe(id, world!))),
                GodotThemeVariations.Eyebrow, wrap: true));
        }
    }

    private void AddOrdinaryTarget(int target)
    {
        string targetName = PromptPresentation.Describe(target, world!);
        string detail = composer!.Selected?.Targets?.Details?.GetValueOrDefault(target) is { } text
            ? $"  ·  {text}"
            : string.Empty;
        var choose = new Button
        {
            Name = $"Target{target}",
            Text = composer!.Targets.Contains(target)
                ? $"✓ {TargetAction(selected: true)}  ·  {targetName}{detail}"
                : $"◇ {TargetAction(selected: false)}  ·  {targetName}{detail}",
            Alignment = HorizontalAlignment.Left,
            ToggleMode = true,
            ButtonPressed = composer!.Targets.Contains(target),
            Disabled = submitting,
        };
        StyleButton(
            choose,
            choose.ButtonPressed
                ? InteractiveVisualState.Selected
                : InteractiveVisualState.Legal);
        choose.Pressed += () =>
        {
            if (composer.Targets.Contains(target))
            {
                composer.RemoveTarget(target);
            }
            else
            {
                composer.AddTarget(target);
            }
            AnchorFocused?.Invoke([target]);
            Rebuild();
        };
        BindAnchors(choose, target);
        AddContent(choose);
    }

    private void AddRepeatedTarget(TargetRequest request, int target)
    {
        int count = composer!.Targets.Count(chosen => chosen == target);
        int limit = request.MaximumOccurrences?.GetValueOrDefault(target) ?? request.Max;
        var row = new HBoxContainer
        {
            ThemeTypeVariation = GodotThemeVariations.CompactRow,
        };
        var remove = new Button
        {
            Name = $"Target{target}Remove",
            Text = "−",
            Disabled = submitting || count == 0,
        };
        StyleButton(
            remove,
            remove.Disabled
                ? InteractiveVisualState.Unavailable
                : InteractiveVisualState.Resting,
            compact: true);
        remove.Pressed += () =>
        {
            composer.RemoveTarget(target);
            Rebuild();
        };
        BindAnchors(remove, target);
        row.AddChild(remove);
        var label = Text(
            $"{count}  ·  {PromptPresentation.Describe(target, world!)}  ·  max {limit}",
            GodotThemeVariations.Body, wrap: true);
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(label);
        var add = new Button
        {
            Name = $"Target{target}Add",
            Text = "+",
            Disabled = submitting || count >= limit || composer.Targets.Count >= request.Max,
        };
        StyleButton(
            add,
            add.Disabled
                ? InteractiveVisualState.Unavailable
                : InteractiveVisualState.Legal,
            compact: true);
        add.Pressed += () =>
        {
            composer.AddTarget(target);
            AnchorFocused?.Invoke([target]);
            Rebuild();
        };
        BindAnchors(add, target);
        row.AddChild(add);
        AddContent(row);
    }

    private void AddCosts(Affordance selected)
    {
        if (selected.CostOptions.Count == 0)
        {
            AddContent(Text("PAYMENT  ·  FREE  ·  READY", GodotThemeVariations.StatusText));
            return;
        }

        AddContent(Text("COST", GodotThemeVariations.Caption));
        for (int index = 0; index < selected.CostOptions.Count; index++)
        {
            int costIndex = index;
            CostOption cost = selected.CostOptions[index];
            bool targetMatches = composer!.CostApplies(cost);
            bool unavailable = submitting || !targetMatches;
            bool isSelected = composer.SelectedCost == index;
            var choose = new Button
            {
                Name = $"Cost{costIndex}",
                Text = unavailable
                    ? $"— UNAVAILABLE  ·  {CostLabel(cost)}"
                    : isSelected
                    ? $"✓ SELECTED  ·  {CostLabel(cost)}"
                    : $"◇ CHOOSE  ·  {CostLabel(cost)}",
                Alignment = HorizontalAlignment.Left,
                ToggleMode = true,
                ButtonPressed = isSelected,
                Disabled = unavailable,
            };
            StyleButton(
                choose,
                choose.Disabled
                    ? InteractiveVisualState.Unavailable
                    : choose.ButtonPressed
                        ? InteractiveVisualState.Selected
                        : InteractiveVisualState.Legal);
            choose.Pressed += () =>
            {
                composer.SelectCost(costIndex);
                foreach (VariableRequest variable in cost.VariableRequests)
                {
                    composer.Define(variable.Name, variable.Min);
                }
                Rebuild();
            };
            if (cost.Target != 0)
            {
                BindAnchors(choose, cost.Target);
            }
            AddContent(choose);
        }

        if (composer!.SelectedCost < 0)
        {
            PaymentProgress pending = composer.Progress().Payment;
            AddContent(Text(
                $"PAYMENT  ·  CHOOSE 1 OF {pending.CostOptions} COSTS",
                GodotThemeVariations.Caption));
            return;
        }

        CostOption selectedCost = selected.CostOptions[composer.SelectedCost];
        foreach (VariableRequest variable in selectedCost.VariableRequests)
        {
            if (!composer.Values.ContainsKey(variable.Name))
            {
                composer.Define(variable.Name, variable.Min);
            }

            var row = new HBoxContainer();
            var name = Text(
                $"{variable.Name}  ·  {variable.Min}–{variable.Max}",
                GodotThemeVariations.Body);
            name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(name);
            var value = new SpinBox
            {
                Name = $"Variable{NodeKey(variable.Name)}",
                CustomMinimumSize = new Vector2(
                    ControlMetrics.MinimumButtonWidth,
                    ControlMetrics.MinimumHeight),
                MinValue = variable.Min,
                MaxValue = variable.Max,
                Step = 1,
                Value = composer.Values[variable.Name],
                AllowGreater = false,
                AllowLesser = false,
                Editable = !submitting,
            };
            value.ValueChanged += chosen =>
            {
                composer.Define(variable.Name, checked((long)chosen));
                Rebuild();
            };
            row.AddChild(value);
            AddContent(row);
        }

        foreach (ResourceSource source in selectedCost.Generators)
        {
            var choose = new Button
            {
                Name = $"Resource{source.Effect}",
                Text = (composer.Resources.Contains(source.Effect)
                        ? "✓ SELECTED  ·  "
                        : "◇ RESOURCE  ·  ")
                    + $"{PromptPresentation.Describe(source.Effect, world!)}"
                    + $"  ·  {source.Generates}",
                Alignment = HorizontalAlignment.Left,
                ToggleMode = true,
                ButtonPressed = composer.Resources.Contains(source.Effect),
                Disabled = submitting,
            };
            StyleButton(
                choose,
                choose.ButtonPressed
                    ? InteractiveVisualState.Selected
                    : InteractiveVisualState.Legal);
            choose.Pressed += () =>
            {
                composer.ToggleResource(source.Effect);
                Rebuild();
            };
            BindAnchors(choose, source.Effect);
            AddContent(choose);
        }

        AddResourceAssignments(selectedCost);

        if (selectedCost.ResourceCosts.Count > 1)
        {
            AddContent(Text(
                "COMPONENTS  ·  " + string.Join(" + ",
                    selectedCost.ResourceCosts.Select((component, index) =>
                        $"{index + 1}:{component.Cost}")),
                GodotThemeVariations.Caption, wrap: true));
        }

        PaymentProgress progress = composer.Progress().Payment;
        AddContent(Text(
            $"PAYMENT  ·  {progress.SelectedGenerators} GENERATORS"
            + $"  ·  {progress.AssignedIcons}/{progress.GeneratedIcons} ICONS"
            + (progress.ExcessIcons > 0
                ? $"  ·  {progress.ExcessIcons} EXCESS"
                : string.Empty)
            + (progress.RequestedVariables > 0
                ? $"  ·  {progress.DefinedVariables}/{progress.RequestedVariables} VALUES"
                : string.Empty)
            + (progress.IsSatisfied ? "  ·  READY" : "  ·  INCOMPLETE"),
            progress.ExcessIcons > 0
                ? GodotThemeVariations.DangerText
                : progress.IsSatisfied
                ? GodotThemeVariations.StatusText
                : GodotThemeVariations.Caption,
            wrap: true));
    }

    private void AddResourceAssignments(CostOption cost)
    {
        if (composer!.UsesAutomaticResourceAllocation)
        {
            foreach (ResourceSource source in cost.Generators.Where(generator =>
                         composer.Resources.Contains(generator.Effect)))
            {
                int assigned = composer.Assignments.Count(assignment =>
                    assignment.Source == source.Effect);
                AddContent(Text(
                    $"{PromptPresentation.Describe(source.Effect, world!)}"
                    + $"  ·  PRINTED {string.Join(" + ", source.Generates.Select(ResourceName))}"
                    + $"  ·  {assigned} APPLIED"
                    + (source.Generates.Length > assigned
                        ? $"  ·  {source.Generates.Length - assigned} EXCESS"
                        : string.Empty),
                    GodotThemeVariations.StatusText,
                    wrap: true));
            }

            return;
        }

        foreach (ResourceSource source in cost.Generators.Where(generator =>
                     composer.Resources.Contains(generator.Effect)))
        {
            for (int icon = 0; icon < source.Generates.Length; icon++)
            {
                int iconIndex = icon;
                char printed = source.Generates[icon];
                var choices = new List<AllocationChoice>
                {
                    new(Cost: null, PaidAs: null, "Unused / excess"),
                };
                for (int component = 0; component < cost.ResourceCosts.Count; component++)
                {
                    if (printed == Resources.Wild)
                    {
                        choices.AddRange(Resources.Types.Select(declared =>
                            new AllocationChoice(
                                component,
                                declared,
                                $"Cost {component + 1} as {ResourceName(declared)}")));
                    }
                    else
                    {
                        choices.Add(new AllocationChoice(
                            component,
                            printed,
                            $"Cost {component + 1} as {ResourceName(printed)}"));
                    }
                }

                var row = new HBoxContainer();
                var label = Text(
                    $"Icon {icon + 1} · {ResourceName(printed)}",
                    GodotThemeVariations.Body);
                label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                row.AddChild(label);
                var allocation = new OptionButton
                {
                    Name = $"Allocation{source.Effect}_{iconIndex}",
                    CustomMinimumSize = new Vector2(
                        Math.Max(170, ControlMetrics.MinimumButtonWidth),
                        ControlMetrics.MinimumHeight),
                    Disabled = submitting,
                };
                foreach (AllocationChoice choice in choices)
                {
                    allocation.AddItem(choice.Label);
                }

                ResourceIconAssignment current = composer!.Assignments.FirstOrDefault(
                    assignment => assignment.Source == source.Effect
                        && assignment.Icon == iconIndex);
                int currentIndex = composer.Assignments.Any(assignment =>
                        assignment.Source == source.Effect && assignment.Icon == iconIndex)
                    ? choices.FindIndex(choice =>
                        choice.Cost == current.Cost && choice.PaidAs == current.PaidAs)
                    : 0;
                allocation.Select(Math.Max(0, currentIndex));
                allocation.ItemSelected += selected =>
                {
                    AllocationChoice choice = choices[(int)selected];
                    composer.AssignResource(
                        source.Effect, iconIndex, choice.Cost, choice.PaidAs);
                    Rebuild();
                };
                BindAnchors(allocation, source.Effect);
                row.AddChild(allocation);
                AddContent(row);
            }
        }
    }

    private void AddSubmit(DecisionProgressPresentation progress)
    {
        if (!progress.IsReady && progress.Error is not null)
        {
            Label validation = Text(
                $"! {progress.Error}",
                GodotThemeVariations.DangerText,
                wrap: true);
            validation.Name = "ValidationError";
            AddCommit(validation);
        }

        if (progress.Payment.ExcessIcons > 0)
        {
            AddCommit(Text(
                $"! {DecisionCopy.OverpaymentWarning(progress.Payment)}",
                GodotThemeVariations.DangerText,
                wrap: true));
        }

        string action = DecisionCopy.WithPaymentConsequence(
            SubmitAction(), progress.Payment);
        var submit = new Button
        {
            Name = "Submit",
            Text = submitting
                ? "— UNAVAILABLE  ·  Waiting for engine…"
                : progress.IsReady
                    ? action
                    : $"— Unavailable  ·  {action}",
            Disabled = submitting || !progress.IsReady,
        };
        StyleButton(
            submit,
            submit.Disabled
                ? InteractiveVisualState.Unavailable
                : InteractiveVisualState.Danger);
        submit.Pressed += () =>
        {
            if (composer!.TryBuild(out EngineDecision? decision, out _))
            {
                Submitted?.Invoke(decision!);
            }
        };
        AddCommit(submit);
    }

    private void AddContent(Control control) =>
        (content ?? throw new InvalidOperationException("decision content is unavailable"))
            .AddChild(control);

    private void AddCommit(Control control) =>
        (commit ?? throw new InvalidOperationException("decision commit bar is unavailable"))
            .AddChild(control);

    private string TargetProgressText(TargetSelectionProgress progress) =>
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

    private string TargetAction(bool selected) => composer!.Selected?.Verb switch
    {
        "Resolve Mulligans" => "DISCARD AND REDRAW",
        "End Phase" => "DISCARD",
        "Attack" => "ATTACK",
        "Thwart" => "THWART",
        _ => selected ? "CHOSEN" : "TARGET",
    };

    private string SubmitAction()
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

    private void RestoreFocus(string? requested, bool focusFirst)
    {
        Control? candidate = EnabledControl(requested);
        if (candidate is null && requested is not null)
        {
            string? paired = requested.EndsWith("Add", StringComparison.Ordinal)
                ? requested[..^3] + "Remove"
                : requested.EndsWith("Remove", StringComparison.Ordinal)
                    ? requested[..^6] + "Add"
                    : null;
            candidate = EnabledButton(paired) ?? EnabledButton("Submit");
        }

        if (candidate is null && (focusFirst || requested is not null))
        {
            candidate = FindChildren("*", "BaseButton", recursive: true, owned: false)
                .OfType<BaseButton>()
                .FirstOrDefault(button => !button.Disabled);
        }

        if (candidate is not null)
        {
            candidate.GrabFocus();
            Callable.From(() =>
            {
                EnsureFocusedControlVisible(candidate);
                // Nested scroll containers settle from the decision rail out
                // to the page. A second layout pass lets the outer rail use
                // the position produced by the inner one.
                Callable.From(() => EnsureFocusedControlVisible(candidate)).CallDeferred();
            }).CallDeferred();
        }
    }

    private static void EnsureFocusedControlVisible(Control control)
    {
        Node? ancestor = control.GetParent();
        while (ancestor is not null)
        {
            if (ancestor is ScrollContainer scroll)
            {
                if (scroll.Name == "Margin")
                {
                    if (scroll.VerticalScrollMode == ScrollContainer.ScrollMode.Disabled)
                    {
                        // The table and decision rail own their scrolling in play.
                        // A focused decision must not move the fixed page header.
                        scroll.ScrollVertical = 0;
                    }
                    else
                    {
                        EnsurePromptContextVisible(scroll, control);
                    }
                }
                else
                {
                    scroll.EnsureControlVisible(control);
                    if (scroll.Name == "DecisionBodyScroll")
                    {
                        // Focus rings expand outside the button geometry. Keep the
                        // wrapped action label anchored at its readable left edge.
                        scroll.ScrollHorizontal = 0;
                    }
                }
            }

            ancestor = ancestor.GetParent();
        }
    }

    private static void EnsurePromptContextVisible(
        ScrollContainer page,
        Control control)
    {
        Control? header = PromptHeader(control);
        if (header is null)
        {
            page.EnsureControlVisible(control);
            return;
        }

        Rect2 viewport = page.GetGlobalRect();
        Rect2 headerRect = header.GetGlobalRect();
        Rect2 controlRect = control.GetGlobalRect();
        float top = Math.Min(headerRect.Position.Y, controlRect.Position.Y);
        float bottom = Math.Max(headerRect.End.Y, controlRect.End.Y);
        if (bottom - top > viewport.Size.Y)
        {
            page.EnsureControlVisible(control);
            return;
        }

        page.ScrollVertical += Mathf.RoundToInt(top - viewport.Position.Y);
    }

    private static Control? PromptHeader(Control control)
    {
        Node? ancestor = control.GetParent();
        while (ancestor is not null)
        {
            if (ancestor.Name == "Stack")
            {
                return ancestor.GetNodeOrNull<Control>("PromptHeader");
            }

            ancestor = ancestor.GetParent();
        }

        return null;
    }

    private string? FocusKey(Control focused)
    {
        Node? current = focused;
        while (current is not null && current != this)
        {
            string name = current.Name.ToString();
            if (IsStableFocusName(name))
            {
                return name;
            }

            current = current.GetParent();
        }

        return null;
    }

    private static bool IsStableFocusName(string name) =>
        name is "Submit" or "Decline"
        || name.StartsWith("Affordance", StringComparison.Ordinal)
        || name.StartsWith("Group", StringComparison.Ordinal)
        || name.StartsWith("Target", StringComparison.Ordinal)
        || name.StartsWith("Cost", StringComparison.Ordinal)
        || name.StartsWith("Variable", StringComparison.Ordinal)
        || name.StartsWith("Resource", StringComparison.Ordinal)
        || name.StartsWith("Allocation", StringComparison.Ordinal);

    private Control? EnabledControl(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return FindChild(name, recursive: true, owned: false) switch
        {
            BaseButton { Disabled: false } button => button,
            SpinBox { Editable: true } spin => spin.GetLineEdit(),
            LineEdit { Editable: true } line => line,
            _ => null,
        };
    }

    private BaseButton? EnabledButton(string? name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return FindChild(name, recursive: true, owned: false) is BaseButton
            {
                Disabled: false,
            } button
            ? button
            : null;
    }

    private void BindAnchors(Control control, params int[] ids)
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

    private static string NodeKey(string value) => new(
        value.Select(character => char.IsLetterOrDigit(character) ? character : '_').ToArray());

    private string CostLabel(CostOption cost)
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

    private static Label Text(string text, string variation, bool wrap = false)
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

    private void StyleButton(
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

    private static string ResourceName(char resource) => resource switch
    {
        Resources.Mental => "mental",
        Resources.Energy => "energy",
        Resources.Physical => "physical",
        Resources.Wild => "wild",
        _ => $"resource {resource}",
    };

    private readonly record struct AllocationChoice(
        int? Cost,
        char? PaidAs,
        string Label);
}
