using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Decisions;

/// <summary>
/// A draft tied to exactly one prompt. It composes offered values without
/// deriving an action from printed card text.
/// </summary>
public sealed class DecisionComposer
{
    private readonly ResourceAssignmentDraft allocation = new();
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
    public IReadOnlyList<int> Resources => allocation.Resources;

    /// <summary>Individual generated icons assigned to cost components.</summary>
    public IReadOnlyList<ResourceIconAssignment> Assignments => allocation.Assignments;

    /// <summary>Current variable definitions.</summary>
    public IReadOnlyDictionary<string, long> Values => values;

    /// <summary>The selected index in the affordance's unmodified cost list.</summary>
    public int SelectedCost => selectedCost;

    /// <summary>Whether the only legal required target was selected without asking.</summary>
    public bool UsesAutomaticTargetSelection => Selected?.Targets is { } request
        && Prompt.PublicKind != PublicDecisionKind.VisibleCardSelection
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
    public bool UsesAutomaticResourceAllocation => allocation.UsesAutomaticAllocation(SelectedCostOption());

    /// <summary>
    /// Summarizes the current draft without interpreting card text or board state.
    /// </summary>
    public DecisionProgressPresentation Progress()
        => DecisionProgressProjection.From(this);

    /// <summary>Selects one offered affordance and clears the previous draft.</summary>
    public void SelectAffordance(int id)
    {
        Selected = Prompt.Affordances.FirstOrDefault(option => option.Id == id)
            ?? throw new ArgumentOutOfRangeException(nameof(id), id, "affordance is not offered");
        targets.Clear();
        allocation.Clear();
        values.Clear();
        selectedCost = Selected.CostOptions.Count == 1 ? 0 : -1;
        SelectOnlyRequiredTarget();
    }

    private void SelectOnlyRequiredTarget()
    {
        if (Prompt.PublicKind == PublicDecisionKind.VisibleCardSelection) return;
        TargetRequest? request = Selected?.Targets;
        if (HasOnlyGroupedTarget(request))
        {
            targets.AddRange(request!.Groups![0]);
            return;
        }
        if (HasOnlyOrdinaryTarget(request))
        {
            targets.Add(request!.Legal[0]);
        }
    }

    private static bool HasOnlyGroupedTarget(TargetRequest? request) =>
        request?.IsGrouped == true && request.Groups is { Count: 1 };

    private static bool HasOnlyOrdinaryTarget(TargetRequest? request) =>
        request is not null
        && !request.AllowRepeated
        && request.Min == 1
        && request.Max == 1
        && request.Legal.Distinct().Take(2).Count() == 1;

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
        TargetRequest? request = Selected?.Targets;
        if (request is { IsGrouped: false, AllowRepeated: false, Max: 1 })
        {
            targets.Clear();
        }
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
        allocation.Clear();
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
    public void ToggleResource(int effect) => allocation.Toggle(effect, SelectedCostOption(), values);

    /// <summary>Assigns or clears one icon from a selected generator.</summary>
    public void AssignResource(int source, int icon, int? cost, char? paidAs) =>
        allocation.Assign(SelectedCostOption(), source, icon, cost, paidAs);

    /// <summary>Defines one value requested by the selected cost.</summary>
    public void Define(string name, long value)
    {
        values[name] = value;
        allocation.Refresh(SelectedCostOption(), values);
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
        if (!SelectedDraftIsValid(out error))
        {
            return false;
        }
        return Selected!.CostOptions.Count == 0
            ? TryBuildFree(out decision, out error)
            : TryBuildPaid(out decision, out error);
    }

    private bool SelectedDraftIsValid(out string? error)
    {
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
        bool targetsValid = Selected.Targets is null
            ? targets.Count == 0
            : Selected.Targets.Allows(targets);
        error = targetsValid ? null : "Complete the offered target selection.";
        return targetsValid;
    }

    private bool TryBuildFree(out EngineDecision? decision, out string? error)
    {
        if (allocation.Resources.Count > 0 || values.Count > 0)
        {
            decision = null;
            error = "This action has no payment.";
            return false;
        }
        decision = new EngineDecision(Selected!.Id, [.. targets]);
        error = null;
        return true;
    }

    private bool TryBuildPaid(out EngineDecision? decision, out string? error)
    {
        decision = null;
        Affordance selected = Selected!;
        if (selectedCost < 0 || selectedCost >= selected.CostOptions.Count)
        {
            error = "Choose an offered cost.";
            return false;
        }

        CostOption cost = selected.CostOptions[selectedCost];
        if (!CostApplies(cost))
        {
            error = "Choose the target associated with this cost.";
            return false;
        }

        if (!VariablesAreValid(cost))
        {
            error = "Define every requested value inside its offered range.";
            return false;
        }

        IReadOnlyList<ResourceAllocation> allocated = CollapseAssignments();
        if (!ResourcePayment.Allows(cost, allocation.Resources, values, allocated))
        {
            error = "Assign generated icons to satisfy every offered cost component.";
            return false;
        }

        decision = new EngineDecision(
            selected.Id,
            [.. targets],
            [.. allocation.Resources],
            new Dictionary<string, long>(values, StringComparer.Ordinal),
            [.. allocated]);
        error = null;
        return true;
    }

    private bool VariablesAreValid(CostOption cost)
    {
        VariableRequest[] requested = cost.VariableRequests.ToArray();
        return values.Count == requested.Length
            && requested.All(variable => values.TryGetValue(variable.Name, out long value)
                && variable.Allows(value));
    }

    private void ResetPaymentIfTargetChanged()
    {
        if (Selected?.CostOptions.Count > 0)
        {
            selectedCost = Selected.CostOptions.Count == 1 ? 0 : -1;
            allocation.Clear();
            values.Clear();
        }
    }

    internal IReadOnlyList<ResourceAllocation> CollapseAssignments() => allocation.CollapseAssignments();

    internal CostOption? SelectedCostOption() =>
        Selected is not null
        && selectedCost >= 0
        && selectedCost < Selected.CostOptions.Count
            ? Selected.CostOptions[selectedCost]
            : null;

}
