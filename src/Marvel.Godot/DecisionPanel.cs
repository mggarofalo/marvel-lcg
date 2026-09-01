using Godot;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Renders one current prompt and composes its typed decision.</summary>
public sealed partial class DecisionPanel : VBoxContainer
{
    private static readonly Color Ink = new("e8e4d8");
    private static readonly Color Muted = new("91a4a8");
    private static readonly Color Amber = new("e6a646");
    private static readonly Color Red = new("df6257");
    private DecisionComposer? composer;
    private bool submitting;
    private WorldDescriptor? world;

    /// <summary>Raised with one answer built from the current prompt.</summary>
    public event Action<EngineDecision>? Submitted;

    /// <summary>Raised when an affordance or target points at a board object.</summary>
    public event Action<int?>? AnchorFocused;

    /// <summary>Discards the old draft and renders the response's current prompt.</summary>
    public void Render(Prompt? prompt, WorldDescriptor currentWorld)
    {
        world = currentWorld ?? throw new ArgumentNullException(nameof(currentWorld));
        composer = prompt is null ? null : new DecisionComposer(prompt);
        submitting = false;
        Rebuild();
    }

    /// <summary>Prevents a second mutation while one response is outstanding.</summary>
    public void SetSubmitting(bool value)
    {
        submitting = value;
        Rebuild();
    }

    private void Rebuild()
    {
        foreach (Node child in GetChildren())
        {
            RemoveChild(child);
            child.QueueFree();
        }

        AddThemeConstantOverride("separation", 9);
        if (composer is null || world is null)
        {
            AddChild(Text("GAME COMPLETE", 11, Amber));
            AddChild(Text("No further decision is waiting.", 16, Ink, wrap: true));
            return;
        }

        PromptPresentation prompt = PromptPresentation.From(composer.Prompt, world);
        AddChild(Text("CURRENT DECISION", 10, Amber));
        AddChild(Text(prompt.Heading, 20, Ink, wrap: true));
        AddChild(Text(prompt.Context, 9, Muted, wrap: true));
        AddChild(Text(prompt.Requirement, 9,
            composer.Prompt.Cancellable ? Muted : Red));
        AddChild(new HSeparator());

        foreach (AffordancePresentation view in prompt.Affordances)
        {
            Affordance option = composer.Prompt.Affordances.Single(candidate =>
                candidate.Id == view.Id);
            var choose = new Button
            {
                Text = $"{view.Verb}  ·  {view.Label}\n{view.Anchor}",
                Alignment = HorizontalAlignment.Left,
                Disabled = submitting || !option.IsLegal,
                ToggleMode = true,
                ButtonPressed = composer.Selected?.Id == option.Id,
                TooltipText = option.Illegal ?? $"Anchor {option.AnchorId}, player {option.AnchorPlayer}",
            };
            choose.Pressed += () =>
            {
                composer.SelectAffordance(option.Id);
                AnchorFocused?.Invoke(option.AnchorId);
                Rebuild();
            };
            choose.MouseEntered += () => AnchorFocused?.Invoke(option.AnchorId);
            choose.FocusEntered += () => AnchorFocused?.Invoke(option.AnchorId);
            AddChild(choose);
            if (option.Illegal is not null)
            {
                AddChild(Text(option.Illegal, 10, Red, wrap: true));
            }
        }

        if (composer.Selected is { } selected)
        {
            AddChild(new HSeparator());
            AddChild(Text("SELECTION", 10, Amber));
            AddTargets(selected);
            AddCosts(selected);
            AddSubmit();
        }

        if (composer.Prompt.Cancellable)
        {
            var pass = new Button
            {
                Text = "Pass / decline",
                Disabled = submitting,
            };
            pass.Pressed += () =>
            {
                if (composer.TryDecline(out EngineDecision? decision, out _))
                {
                    Submitted?.Invoke(decision!);
                }
            };
            AddChild(pass);
        }
    }

    private void AddTargets(Affordance selected)
    {
        TargetRequest? request = selected.Targets;
        if (request is null)
        {
            AddChild(Text("No target selection", 11, Muted));
            return;
        }

        string badge = request.IsSearch ? "SEARCH RESULTS" : "TARGETS";
        AddChild(Text($"{badge}  ·  {request.Min}–{request.Max}", 9, Muted));
        if (!string.IsNullOrWhiteSpace(request.Rule))
        {
            AddChild(Text(request.Rule, 9, Muted));
        }

        if (request.MustIncludeTraits is { Count: > 0 })
        {
            AddChild(Text(
                $"MUST INCLUDE  ·  {string.Join(", ", request.MustIncludeTraits)}",
                9, Muted, wrap: true));
        }

        if (request.IsGrouped)
        {
            for (int index = 0; index < request.Groups!.Count; index++)
            {
                IReadOnlyList<int> group = request.Groups[index];
                var choose = new Button
                {
                    Text = $"Group {index + 1}  ·  "
                        + string.Join(" → ", group.Select(id =>
                            PromptPresentation.Describe(id, world!))),
                    Alignment = HorizontalAlignment.Left,
                    ToggleMode = true,
                    ButtonPressed = composer!.Targets.SequenceEqual(group),
                    Disabled = submitting,
                };
                choose.Pressed += () =>
                {
                    composer.SelectTargets(group);
                    Rebuild();
                };
                AddChild(choose);
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
            AddChild(Text(
                "ORDER  ·  " + string.Join(" → ", composer.Targets.Select(id =>
                    PromptPresentation.Describe(id, world!))),
                9, Amber, wrap: true));
        }
    }

    private void AddOrdinaryTarget(int target)
    {
        var choose = new Button
        {
            Text = PromptPresentation.Describe(target, world!),
            Alignment = HorizontalAlignment.Left,
            ToggleMode = true,
            ButtonPressed = composer!.Targets.Contains(target),
            Disabled = submitting,
        };
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
            AnchorFocused?.Invoke(target);
            Rebuild();
        };
        choose.MouseEntered += () => AnchorFocused?.Invoke(target);
        choose.FocusEntered += () => AnchorFocused?.Invoke(target);
        AddChild(choose);
    }

    private void AddRepeatedTarget(TargetRequest request, int target)
    {
        int count = composer!.Targets.Count(chosen => chosen == target);
        int limit = request.MaximumOccurrences?.GetValueOrDefault(target) ?? request.Max;
        var row = new HBoxContainer();
        row.AddThemeConstantOverride("separation", 5);
        var remove = new Button { Text = "−", Disabled = submitting || count == 0 };
        remove.Pressed += () =>
        {
            composer.RemoveTarget(target);
            Rebuild();
        };
        row.AddChild(remove);
        var label = Text(
            $"{count}  ·  {PromptPresentation.Describe(target, world!)}  ·  max {limit}",
            11, Ink, wrap: true);
        label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
        row.AddChild(label);
        var add = new Button
        {
            Text = "+",
            Disabled = submitting || count >= limit || composer.Targets.Count >= request.Max,
        };
        add.Pressed += () =>
        {
            composer.AddTarget(target);
            AnchorFocused?.Invoke(target);
            Rebuild();
        };
        row.AddChild(add);
        AddChild(row);
    }

    private void AddCosts(Affordance selected)
    {
        if (selected.CostOptions.Count == 0)
        {
            AddChild(Text("FREE", 9, Muted));
            return;
        }

        AddChild(Text("COST", 9, Muted));
        for (int index = 0; index < selected.CostOptions.Count; index++)
        {
            int costIndex = index;
            CostOption cost = selected.CostOptions[index];
            bool targetMatches = composer!.CostApplies(cost);
            var choose = new Button
            {
                Text = CostLabel(cost),
                Alignment = HorizontalAlignment.Left,
                ToggleMode = true,
                ButtonPressed = composer!.SelectedCost == index,
                Disabled = submitting || !targetMatches,
            };
            choose.Pressed += () =>
            {
                composer.SelectCost(costIndex);
                foreach (VariableRequest variable in cost.VariableRequests)
                {
                    composer.Define(variable.Name, variable.Min);
                }
                Rebuild();
            };
            AddChild(choose);
        }

        if (composer!.SelectedCost < 0)
        {
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
            var name = Text($"{variable.Name}  ·  {variable.Min}–{variable.Max}", 11, Ink);
            name.SizeFlagsHorizontal = SizeFlags.ExpandFill;
            row.AddChild(name);
            var value = new SpinBox
            {
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
            AddChild(row);
        }

        foreach (ResourceSource source in selectedCost.Generators)
        {
            var choose = new Button
            {
                Text = $"{PromptPresentation.Describe(source.Effect, world!)}"
                    + $"  ·  {source.Generates}",
                Alignment = HorizontalAlignment.Left,
                ToggleMode = true,
                ButtonPressed = composer.Resources.Contains(source.Effect),
                Disabled = submitting,
            };
            choose.Pressed += () =>
            {
                composer.ToggleResource(source.Effect);
                Rebuild();
            };
            AddChild(choose);
        }

        AddResourceAssignments(selectedCost);

        if (selectedCost.ResourceCosts.Count > 1)
        {
            AddChild(Text(
                "COMPONENTS  ·  " + string.Join(" + ",
                    selectedCost.ResourceCosts.Select((component, index) =>
                        $"{index + 1}:{component.Cost}")),
                9, Muted, wrap: true));
        }
    }

    private void AddResourceAssignments(CostOption cost)
    {
        foreach (ResourceSource source in cost.Generators.Where(generator =>
                     composer!.Resources.Contains(generator.Effect)))
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
                    10, Ink);
                label.SizeFlagsHorizontal = SizeFlags.ExpandFill;
                row.AddChild(label);
                var allocation = new OptionButton
                {
                    CustomMinimumSize = new Vector2(170, 0),
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
                row.AddChild(allocation);
                AddChild(row);
            }
        }
    }

    private void AddSubmit()
    {
        bool valid = composer!.TryBuild(out _, out string? error);
        if (!valid && error is not null)
        {
            AddChild(Text(error, 10, Muted, wrap: true));
        }

        var submit = new Button
        {
            Text = submitting ? "Waiting for engine…" : "Submit decision",
            Disabled = submitting || !valid,
        };
        submit.Pressed += () =>
        {
            if (composer.TryBuild(out EngineDecision? decision, out _))
            {
                Submitted?.Invoke(decision!);
            }
        };
        AddChild(submit);
    }

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

    private static Label Text(string text, int size, Color color, bool wrap = false)
    {
        var label = new Label
        {
            Text = text,
            AutowrapMode = wrap
                ? TextServer.AutowrapMode.WordSmart
                : TextServer.AutowrapMode.Off,
        };
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        return label;
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
