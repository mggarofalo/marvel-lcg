using Godot;
using Marvel.Decisions;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Renders a selected cost's resource-allocation controls.</summary>
internal sealed class DecisionPaymentResourceAssignmentRenderer
{
    private readonly DecisionPanel panel;
    private readonly DecisionComposer composer;
    private readonly WorldDescriptor world;
    private readonly bool submitting;
    private readonly int generation;

    internal DecisionPaymentResourceAssignmentRenderer(
        DecisionPanel panel,
        DecisionComposer composer,
        WorldDescriptor world,
        bool submitting,
        int generation)
    {
        this.panel = panel;
        this.composer = composer;
        this.world = world;
        this.submitting = submitting;
        this.generation = generation;
    }

    internal void Add(CostOption cost)
    {
        if (composer.UsesAutomaticResourceAllocation)
        {
            AddAutomaticAssignments(cost);
            return;
        }

        foreach (ResourceSource source in cost.Generators.Where(generator =>
                     composer.Resources.Contains(generator.Effect)))
        {
            for (int icon = 0; icon < source.Generates.Length; icon++)
            {
                AddAllocation(source, icon, source.Generates[icon], cost.ResourceCosts.Count);
            }
        }
    }

    private void AddAllocation(ResourceSource source, int iconIndex, char printed, int componentCount)
    {
        List<AllocationChoice> choices = Choices(printed, componentCount);
        var row = new HBoxContainer();
        var label = DecisionPanel.Text(
            $"Icon {iconIndex + 1} · {DecisionPanel.ResourceName(printed)}",
            GodotThemeVariations.Body);
        label.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        row.AddChild(label);
        var allocation = new OptionButton
        {
            Name = $"Allocation{source.Effect}_{iconIndex}",
            CustomMinimumSize = new Vector2(
                Math.Max(170, panel.ControlMetrics.MinimumButtonWidth),
                panel.ControlMetrics.MinimumHeight),
            Disabled = submitting,
        };
        foreach (AllocationChoice choice in choices)
        {
            allocation.AddItem(choice.Label);
        }
        allocation.Select(CurrentChoiceIndex(source.Effect, iconIndex, choices));
        allocation.ItemSelected += selected =>
        {
            if (!panel.IsCurrentDraft(composer, generation)) return;
            AllocationChoice choice = choices[(int)selected];
            composer.AssignResource(source.Effect, iconIndex, choice.Cost, choice.PaidAs);
            panel.Rebuild();
        };
        panel.BindAnchors(allocation, source.Effect);
        row.AddChild(allocation);
        panel.AddContent(row);
    }

    private int CurrentChoiceIndex(int source, int icon, List<AllocationChoice> choices)
    {
        ResourceIconAssignment current = composer.Assignments.FirstOrDefault(assignment =>
            assignment.Source == source && assignment.Icon == icon);
        return composer.Assignments.Any(assignment => assignment.Source == source && assignment.Icon == icon)
            ? Math.Max(0, choices.FindIndex(choice =>
                choice.Cost == current.Cost && choice.PaidAs == current.PaidAs))
            : 0;
    }

    private static List<AllocationChoice> Choices(char printed, int componentCount)
    {
        var choices = new List<AllocationChoice>
        {
            new(Cost: null, PaidAs: null, "Unused / excess"),
        };
        for (int component = 0; component < componentCount; component++)
        {
            if (printed == Resources.Wild)
            {
                choices.AddRange(Resources.Types.Select(declared => new AllocationChoice(
                    component,
                    declared,
                    $"Cost {component + 1} as {DecisionPanel.ResourceName(declared)}")));
            }
            else
            {
                choices.Add(new AllocationChoice(
                    component,
                    printed,
                    $"Cost {component + 1} as {DecisionPanel.ResourceName(printed)}"));
            }
        }
        return choices;
    }

    private void AddAutomaticAssignments(CostOption cost)
    {
        foreach (ResourceSource source in cost.Generators.Where(generator =>
                     composer.Resources.Contains(generator.Effect)))
        {
            int assigned = composer.Assignments.Count(assignment => assignment.Source == source.Effect);
            panel.AddContent(DecisionPanel.Text(
                $"{PromptPresentation.Describe(source.Effect, world)}"
                + $"  ·  PRINTED {string.Join(" + ", source.Generates.Select(DecisionPanel.ResourceName))}"
                + $"  ·  {assigned} APPLIED"
                + (source.Generates.Length > assigned
                    ? $"  ·  {source.Generates.Length - assigned} EXCESS" : string.Empty),
                GodotThemeVariations.StatusText, wrap: true));
        }
    }

    private readonly record struct AllocationChoice(int? Cost, char? PaidAs, string Label);
}
