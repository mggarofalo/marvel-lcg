using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Godot;

/// <summary>Display text and object references derived only from a visible prompt.</summary>
public sealed record PromptPresentation(
    string Heading,
    string Context,
    string Requirement,
    IReadOnlyList<AffordancePresentation> Affordances)
{
    /// <summary>Builds one prompt view from its response's authorized snapshot.</summary>
    public static PromptPresentation From(Prompt prompt, WorldDescriptor world)
    {
        ArgumentNullException.ThrowIfNull(prompt);
        ArgumentNullException.ThrowIfNull(world);
        return new PromptPresentation(
            prompt.Label,
            $"PLAYER {prompt.Player + 1}  ·  {Words(prompt.Asking.ToString()).ToUpperInvariant()}"
                + $"  ·  {Words(prompt.When.ToString()).ToUpperInvariant()}",
            prompt.Cancellable ? "OPTIONAL · PASS AVAILABLE" : "DECISION REQUIRED",
            prompt.Affordances.Select(option => new AffordancePresentation(
                option.Id,
                option.Label,
                Words(option.Verb),
                Describe(option.AnchorId, world),
                option.AnchorId,
                option.AnchorPlayer,
                option.Illegal,
                option.Targets is null
                    ? "No selection"
                    : Describe(option.Targets, world),
                option.CostOptions.Select(Describe).ToArray())).ToArray());
    }

    /// <summary>Names an authorized board object, or leaves an opaque fallback.</summary>
    public static string Describe(int id, WorldDescriptor world)
    {
        CardDescriptor? card = world.Areas
            .SelectMany(area => area.Cards.Concat(area.Removed))
            .FirstOrDefault(candidate => candidate.Id == id);
        if (card?.Face is { } face)
        {
            return string.IsNullOrWhiteSpace(face.Subtitle)
                ? face.Title
                : $"{face.Title} · {face.Subtitle}";
        }

        if (card is not null)
        {
            return $"Face-down {card.Back.ToString().ToLowerInvariant()} card";
        }

        AreaDescriptor? area = world.Areas.FirstOrDefault(candidate => candidate.Id == id);
        return area is null ? $"Object {id}" : Words(area.Zone);
    }

    private static string Describe(TargetRequest request, WorldDescriptor world)
    {
        if (request.IsGrouped)
        {
            return $"Choose one of {request.Groups!.Count} complete groups";
        }

        string count = request.Min == request.Max
            ? $"Choose {request.Min}"
            : $"Choose {request.Min}–{request.Max}";
        string mode = request.IsSearch ? " search results" : " targets";
        if (request.AllowRepeated)
        {
            mode += " with repetition";
        }

        return count + mode + $" from {request.Legal.Count}";
    }

    private static string Describe(CostOption cost)
    {
        string primary = $"Cost {cost.Cost}{CostRequirement(cost.Rule)}";
        string alternative = cost.HasAlternative
            ? $" or {cost.OrCost}{CostRequirement(cost.OrRule)}"
            : string.Empty;
        return primary + alternative + $" · {cost.Generators.Count} generators";
    }

    private static string CostRequirement(IReadOnlyList<string>? rule) =>
        rule is { Count: > 0 } ? $" [{string.Join(", ", rule)}]" : string.Empty;

    internal static string Words(string value)
    {
        var result = new System.Text.StringBuilder(value.Length + 8);
        for (int index = 0; index < value.Length; index++)
        {
            char current = value[index];
            if (index > 0 && (current == '_'
                || char.IsUpper(current) && char.IsLower(value[index - 1])))
            {
                result.Append(' ');
            }

            if (current != '_')
            {
                result.Append(current);
            }
        }

        return result.ToString();
    }
}

/// <summary>One visible affordance row.</summary>
public sealed record AffordancePresentation(
    int Id,
    string Label,
    string Verb,
    string Anchor,
    int AnchorId,
    int AnchorPlayer,
    string? Illegal,
    string Targets,
    IReadOnlyList<string> Costs);

/// <summary>One generated icon explicitly assigned by the local player.</summary>
public readonly record struct ResourceIconAssignment(
    int Source,
    int Icon,
    int Cost,
    char PaidAs);

/// <summary>How the current prompt represents its target selection.</summary>
public enum TargetSelectionMode
{
    None,
    Ordinary,
    Repeated,
    Grouped,
}

/// <summary>Count-only progress for the current visible target request.</summary>
public sealed record TargetSelectionProgress(
    TargetSelectionMode Mode,
    int Selected,
    int Minimum,
    int Maximum,
    bool IsSatisfied);

/// <summary>Whether the current affordance needs a cost choice.</summary>
public enum CostSelectionState
{
    Unavailable,
    NotRequired,
    Required,
    Selected,
}

/// <summary>Count-only progress for the current offered payment.</summary>
public sealed record PaymentProgress(
    CostSelectionState CostState,
    int? SelectedCost,
    int CostOptions,
    int SelectedGenerators,
    int GeneratedIcons,
    int AssignedIcons,
    int DefinedVariables,
    int RequestedVariables,
    bool IsSatisfied);

/// <summary>
/// Render-ready progress derived from the exact draft and its visible prompt.
/// No object identities are retained in this presentation record.
/// </summary>
public sealed record DecisionProgressPresentation(
    TargetSelectionProgress Targets,
    PaymentProgress Payment,
    bool IsReady,
    string? Error);

/// <summary>
/// A draft tied to exactly one prompt. It composes offered values without
/// deriving an action from printed card text.
/// </summary>
public sealed class DecisionComposer
{
    private readonly List<int> resources = [];
    private readonly List<ResourceIconAssignment> assignments = [];
    private readonly List<int> targets = [];
    private readonly Dictionary<string, long> values = new(StringComparer.Ordinal);
    private int selectedCost = -1;

    /// <summary>Creates an empty draft for the current authorized prompt.</summary>
    public DecisionComposer(Prompt prompt) =>
        Prompt = prompt ?? throw new ArgumentNullException(nameof(prompt));

    /// <summary>The exact prompt to which this draft belongs.</summary>
    public Prompt Prompt { get; }

    /// <summary>The selected current affordance, or null.</summary>
    public Affordance? Selected { get; private set; }

    /// <summary>Targets in player-selected order, including allowed repetitions.</summary>
    public IReadOnlyList<int> Targets => targets;

    /// <summary>Selected generator effects in player-selected order.</summary>
    public IReadOnlyList<int> Resources => resources;

    /// <summary>Individual generated icons assigned to cost components.</summary>
    public IReadOnlyList<ResourceIconAssignment> Assignments => assignments;

    /// <summary>Current variable definitions.</summary>
    public IReadOnlyDictionary<string, long> Values => values;

    /// <summary>The selected index in the affordance's unmodified cost list.</summary>
    public int SelectedCost => selectedCost;

    /// <summary>Whether the only legal required target was selected without asking.</summary>
    public bool UsesAutomaticTargetSelection => Selected?.Targets is { } request
        && targets.Count > 0
        && (request.IsGrouped
            ? request.Groups is { Count: 1 } && targets.SequenceEqual(request.Groups[0])
            : !request.AllowRepeated
                && request.Min == 1
                && request.Max == 1
                && request.Legal.Distinct().Count() == 1
                && targets[0] == request.Legal[0]);

    /// <summary>
    /// Whether the selected generators have one deterministic printed-resource allocation.
    /// </summary>
    /// <remarks>
    /// A single cost component gives every printed icon the same destination. A wild
    /// declaration is automatic only when the prompt says no later effect observes it;
    /// simultaneous cost components retain their explicit player choice.
    /// </remarks>
    public bool UsesAutomaticResourceAllocation
    {
        get
        {
            CostOption? cost = SelectedCostOption();
            if (cost is null || cost.ResourceCosts.Count != 1 || resources.Count == 0)
            {
                return false;
            }

            bool hasWild = false;
            bool valid = resources.All(effect =>
            {
                ResourceSource[] matches =
                    [.. cost.Generators.Where(source => source.Effect == effect)];
                if (matches.Length != 1)
                {
                    return false;
                }

                hasWild |= matches[0].Generates.Contains(
                    Marvel.Rules.Play.Resources.Wild);
                return true;
            });
            return valid && (!hasWild || !cost.DeclarationSensitive);
        }
    }

    /// <summary>
    /// Summarizes the current draft without interpreting card text or board state.
    /// </summary>
    public DecisionProgressPresentation Progress()
    {
        TargetSelectionProgress targetProgress = TargetProgress();
        PaymentProgress paymentProgress = PaymentProgress();
        bool ready = TryBuild(out _, out string? error);
        return new DecisionProgressPresentation(
            targetProgress,
            paymentProgress,
            ready,
            error);
    }

    /// <summary>Selects one offered affordance and clears the previous draft.</summary>
    public void SelectAffordance(int id)
    {
        Selected = Prompt.Affordances.FirstOrDefault(option => option.Id == id)
            ?? throw new ArgumentOutOfRangeException(nameof(id), id, "affordance is not offered");
        targets.Clear();
        resources.Clear();
        assignments.Clear();
        values.Clear();
        selectedCost = Selected.CostOptions.Count == 1 ? 0 : -1;
        SelectOnlyRequiredTarget();
    }

    private void SelectOnlyRequiredTarget()
    {
        TargetRequest? request = Selected?.Targets;
        if (request?.IsGrouped == true && request.Groups is { Count: 1 })
        {
            targets.AddRange(request.Groups[0]);
            return;
        }

        if (request is not null
            && !request.AllowRepeated
            && request.Min == 1
            && request.Max == 1
            && request.Legal.Distinct().Take(2).Count() == 1)
        {
            targets.Add(request.Legal[0]);
        }
    }

    /// <summary>Replaces the ordered selection with values offered by the prompt.</summary>
    public void SelectTargets(IEnumerable<int> selected)
    {
        ArgumentNullException.ThrowIfNull(selected);
        targets.Clear();
        targets.AddRange(selected);
        ResetPaymentIfTargetChanged();
    }

    /// <summary>Adds one target or one repeated allocation entry.</summary>
    public void AddTarget(int id)
    {
        targets.Add(id);
        ResetPaymentIfTargetChanged();
    }

    /// <summary>Removes the last occurrence of a target, preserving prior order.</summary>
    public void RemoveTarget(int id)
    {
        int index = targets.LastIndexOf(id);
        if (index >= 0)
        {
            targets.RemoveAt(index);
            ResetPaymentIfTargetChanged();
        }
    }

    /// <summary>Selects one exact offered cost option.</summary>
    public void SelectCost(int index)
    {
        if (Selected is null || index < 0 || index >= Selected.CostOptions.Count)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        selectedCost = index;
        resources.Clear();
        assignments.Clear();
        values.Clear();
    }

    /// <summary>Whether a target-scoped offered cost applies to this draft.</summary>
    public bool CostApplies(CostOption cost)
    {
        ArgumentNullException.ThrowIfNull(cost);
        return cost.Target == 0
            || cost.Target == Selected?.AnchorId
            || targets.Contains(cost.Target);
    }

    /// <summary>Toggles one generator offered by the selected cost.</summary>
    public void ToggleResource(int effect)
    {
        bool wasAutomatic = UsesAutomaticResourceAllocation;
        int index = resources.IndexOf(effect);
        if (index >= 0)
        {
            resources.RemoveAt(index);
            assignments.RemoveAll(assignment => assignment.Source == effect);
        }
        else
        {
            resources.Add(effect);
        }

        bool isAutomatic = UsesAutomaticResourceAllocation;
        if (wasAutomatic || isAutomatic)
        {
            assignments.Clear();
        }

        if (isAutomatic)
        {
            ApplyAutomaticResourceAllocation();
        }
    }

    /// <summary>Assigns or clears one icon from a selected generator.</summary>
    public void AssignResource(
        int source,
        int icon,
        int? cost,
        char? paidAs)
    {
        if ((cost is null) != (paidAs is null))
        {
            throw new ArgumentException("A resource assignment needs both a cost and a type.");
        }

        CostOption? selected = SelectedCostOption();
        ResourceSource generator = selected?.Generators.SingleOrDefault(candidate =>
            candidate.Effect == source) ?? default;
        if (selected is null
            || !resources.Contains(source)
            || generator.Generates is null
            || icon < 0 || icon >= generator.Generates.Length
            || cost is not null && (cost < 0 || cost >= selected.ResourceCosts.Count)
            || paidAs is not null
                && !Marvel.Rules.Play.Resources.Types.Contains(paidAs.Value)
            || paidAs is not null
                && generator.Generates[icon] != Marvel.Rules.Play.Resources.Wild
                && generator.Generates[icon] != paidAs)
        {
            throw new ArgumentOutOfRangeException(nameof(source), source,
                "resource assignment is not offered by the selected cost");
        }

        assignments.RemoveAll(assignment =>
            assignment.Source == source && assignment.Icon == icon);
        if (cost is not null && paidAs is not null)
        {
            assignments.Add(new ResourceIconAssignment(
                source, icon, cost.Value, paidAs.Value));
        }
    }

    /// <summary>Defines one value requested by the selected cost.</summary>
    public void Define(string name, long value)
    {
        values[name] = value;
        if (UsesAutomaticResourceAllocation)
        {
            assignments.Clear();
            ApplyAutomaticResourceAllocation();
        }
    }

    /// <summary>Builds the wire decline only when this prompt permits it.</summary>
    public bool TryDecline(out EngineDecision? decision, out string? error)
    {
        if (!Prompt.Cancellable)
        {
            decision = null;
            error = "This decision cannot be declined.";
            return false;
        }

        decision = EngineDecision.Decline;
        error = null;
        return true;
    }

    /// <summary>Builds a typed answer entirely from values on the current prompt.</summary>
    public bool TryBuild(out EngineDecision? decision, out string? error)
    {
        decision = null;
        if (Selected is null)
        {
            error = "Choose an action.";
            return false;
        }

        if (!Selected.IsLegal)
        {
            error = Selected.Illegal;
            return false;
        }

        if (Selected.Targets is null ? targets.Count > 0 : !Selected.Targets.Allows(targets))
        {
            error = "Complete the offered target selection.";
            return false;
        }

        if (Selected.CostOptions.Count == 0)
        {
            if (resources.Count > 0 || values.Count > 0)
            {
                error = "This action has no payment.";
                return false;
            }

            decision = new EngineDecision(Selected.Id, [.. targets]);
            error = null;
            return true;
        }

        if (selectedCost < 0 || selectedCost >= Selected.CostOptions.Count)
        {
            error = "Choose an offered cost.";
            return false;
        }

        CostOption cost = Selected.CostOptions[selectedCost];
        if (!CostApplies(cost))
        {
            error = "Choose the target associated with this cost.";
            return false;
        }

        VariableRequest[] requested = cost.VariableRequests.ToArray();
        if (values.Count != requested.Length
            || requested.Any(variable => !values.TryGetValue(variable.Name, out long value)
                || !variable.Allows(value)))
        {
            error = "Define every requested value inside its offered range.";
            return false;
        }

        IReadOnlyList<ResourceAllocation> allocated = CollapseAssignments();
        if (!ResourcePayment.Allows(cost, resources, values, allocated))
        {
            error = "Assign generated icons to satisfy every offered cost component.";
            return false;
        }

        decision = new EngineDecision(
            Selected.Id,
            [.. targets],
            [.. resources],
            new Dictionary<string, long>(values, StringComparer.Ordinal),
            [.. allocated]);
        error = null;
        return true;
    }

    private void ResetPaymentIfTargetChanged()
    {
        if (Selected?.CostOptions.Count > 0)
        {
            selectedCost = Selected.CostOptions.Count == 1 ? 0 : -1;
            resources.Clear();
            assignments.Clear();
            values.Clear();
        }
    }

    private IReadOnlyList<ResourceAllocation> CollapseAssignments()
    {
        var order = new List<(int Source, int Cost)>();
        var paid = new Dictionary<(int Source, int Cost), System.Text.StringBuilder>();
        foreach (ResourceIconAssignment assignment in assignments
                     .OrderBy(assignment => resources.IndexOf(assignment.Source))
                     .ThenBy(assignment => assignment.Icon))
        {
            var key = (assignment.Source, assignment.Cost);
            if (!paid.TryGetValue(key, out System.Text.StringBuilder? declared))
            {
                declared = new System.Text.StringBuilder();
                paid.Add(key, declared);
                order.Add(key);
            }

            declared.Append(assignment.PaidAs);
        }

        return [.. order.Select(key =>
            new ResourceAllocation(key.Source, key.Cost, paid[key].ToString()))];
    }

    private void ApplyAutomaticResourceAllocation()
    {
        CostOption cost = SelectedCostOption()!;
        IReadOnlyList<ResourceAllocation>? allocation =
            ResourcePayment.Allocate(cost, resources, values);
        if (allocation is null)
        {
            return;
        }

        foreach (ResourceAllocation payment in allocation)
        {
            ResourceSource source = cost.Generators.Single(candidate =>
                candidate.Effect == payment.Source);
            var used = new HashSet<int>();
            foreach (char paidAs in payment.PaidAs)
            {
                int icon = Enumerable.Range(0, source.Generates.Length)
                    .Where(index => !used.Contains(index))
                    .OrderBy(index => source.Generates[index] == paidAs ? 0 : 1)
                    .First(index => source.Generates[index] == paidAs
                        || source.Generates[index] == Marvel.Rules.Play.Resources.Wild);
                used.Add(icon);
                assignments.Add(new ResourceIconAssignment(
                    source.Effect, icon, payment.Cost, paidAs));
            }
        }
    }

    private CostOption? SelectedCostOption() =>
        Selected is not null
        && selectedCost >= 0
        && selectedCost < Selected.CostOptions.Count
            ? Selected.CostOptions[selectedCost]
            : null;

    private TargetSelectionProgress TargetProgress()
    {
        TargetRequest? request = Selected?.Targets;
        if (request is null)
        {
            return new TargetSelectionProgress(
                TargetSelectionMode.None,
                targets.Count,
                0,
                0,
                targets.Count == 0);
        }

        bool satisfied = request.Allows(targets);
        if (request.IsGrouped)
        {
            return new TargetSelectionProgress(
                TargetSelectionMode.Grouped,
                satisfied ? 1 : 0,
                1,
                1,
                satisfied);
        }

        return new TargetSelectionProgress(
            request.AllowRepeated
                ? TargetSelectionMode.Repeated
                : TargetSelectionMode.Ordinary,
            targets.Count,
            request.Min,
            request.Max,
            satisfied);
    }

    private PaymentProgress PaymentProgress()
    {
        if (Selected is null)
        {
            return new PaymentProgress(
                CostSelectionState.Unavailable,
                null,
                0,
                0,
                0,
                0,
                0,
                0,
                false);
        }

        if (Selected.CostOptions.Count == 0)
        {
            return new PaymentProgress(
                CostSelectionState.NotRequired,
                null,
                0,
                resources.Count,
                0,
                assignments.Count,
                values.Count,
                0,
                resources.Count == 0 && assignments.Count == 0 && values.Count == 0);
        }

        CostOption? cost = SelectedCostOption();
        if (cost is null)
        {
            return new PaymentProgress(
                CostSelectionState.Required,
                null,
                Selected.CostOptions.Count,
                resources.Count,
                0,
                assignments.Count,
                values.Count,
                0,
                false);
        }

        int generatedIcons = resources.Sum(effect =>
            cost.Generators
                .Where(generator => generator.Effect == effect)
                .Sum(generator => generator.Generates.Length));
        VariableRequest[] requested = cost.VariableRequests.ToArray();
        bool variablesSatisfied = values.Count == requested.Length
            && requested.All(variable => values.TryGetValue(variable.Name, out long value)
                && variable.Allows(value));
        bool paymentSatisfied = CostApplies(cost)
            && variablesSatisfied
            && ResourcePayment.Allows(cost, resources, values, CollapseAssignments());
        return new PaymentProgress(
            CostSelectionState.Selected,
            selectedCost,
            Selected.CostOptions.Count,
            resources.Count,
            generatedIcons,
            assignments.Count,
            values.Count,
            requested.Length,
            paymentSatisfied);
    }
}
