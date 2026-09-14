using Marvel.Rules.Prompts;

namespace Marvel.Decisions;

/// <summary>
/// Applies direct table selections to one prompt-bound decision draft.
/// </summary>
/// <remarks>
/// This presentation operation accepts only identifiers carried by the prompt.
/// It updates a draft, never submits an answer; the authoritative host still
/// validates the completed <see cref="EngineDecision"/>.
/// </remarks>
public sealed class TableDraftOperations
{
    private readonly DecisionComposer composer;

    /// <summary>Creates operations for one exact prompt-bound draft.</summary>
    public TableDraftOperations(DecisionComposer composer) =>
        this.composer = composer ?? throw new ArgumentNullException(nameof(composer));

    /// <summary>Selects one currently legal offered affordance without submitting it.</summary>
    public bool TrySelectAffordance(int id)
    {
        Affordance? affordance = composer.Prompt.Affordances
            .SingleOrDefault(candidate => candidate.Id == id);
        if (affordance is null || !affordance.IsLegal)
        {
            return false;
        }

        composer.SelectAffordance(affordance.Id);
        return true;
    }

    /// <summary>Chooses one complete offered target group in its offered order.</summary>
    public bool TrySelectGroup(IReadOnlyList<int> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);
        TargetRequest? request = composer.Selected?.Targets;
        if (!HasLegalSelection()
            || request?.Groups?.Any(group => group.SequenceEqual(targets)) != true)
        {
            return false;
        }

        composer.SelectTargets(targets);
        return true;
    }

    /// <summary>Toggles an ordinary, non-grouped target.</summary>
    public bool TryToggleTarget(int id)
    {
        TargetRequest? request = OrdinaryRequest(id);
        if (request is null || request.AllowRepeated)
        {
            return false;
        }

        return composer.Targets.Contains(id)
            ? TryRemoveTarget(id)
            : TryAddTarget(id);
    }

    /// <summary>Adds one offered target while retaining order and allocation bounds.</summary>
    public bool TryAddTarget(int id)
    {
        TargetRequest? request = OrdinaryRequest(id);
        if (request is null)
        {
            return false;
        }

        if (!CanAdd(request, id))
        {
            return false;
        }

        composer.AddTarget(id);
        return true;
    }

    /// <summary>Removes one selected target, retaining the order of all others.</summary>
    public bool TryRemoveTarget(int id)
    {
        if (OrdinaryRequest(id) is null || !composer.Targets.Contains(id))
        {
            return false;
        }

        composer.RemoveTarget(id);
        return true;
    }

    /// <summary>Selects one applicable offered cost option.</summary>
    public bool TrySelectCost(int index)
    {
        if (composer.Selected is not { IsLegal: true } selected
            || index < 0
            || index >= selected.CostOptions.Count)
        {
            return false;
        }

        CostOption cost = selected.CostOptions[index];
        if (!composer.CostApplies(cost))
        {
            return false;
        }

        composer.SelectCost(index);
        foreach (VariableRequest variable in cost.VariableRequests)
        {
            composer.Define(variable.Name, variable.Min);
        }
        return true;
    }

    /// <summary>Toggles only a generator offered by the selected applicable cost.</summary>
    public bool TryToggleGenerator(int effect)
    {
        CostOption? cost = composer.SelectedCostOption();
        if (!HasLegalSelection()
            || cost is null
            || !composer.CostApplies(cost)
            || !cost.Generators.Any(generator => generator.Effect == effect))
        {
            return false;
        }

        composer.ToggleResource(effect);
        return true;
    }

    private TargetRequest? OrdinaryRequest(int id)
    {
        TargetRequest? request = composer.Selected?.Targets;
        return HasLegalSelection()
            && request is { IsGrouped: false }
            && request.Legal.Contains(id)
            ? request
            : null;
    }

    private bool HasLegalSelection() => composer.Selected?.IsLegal == true;

    private bool CanAdd(TargetRequest request, int id) => request.AllowRepeated
        ? CanAddRepeated(request, id)
        : CanAddDistinct(request, id);

    private bool CanAddDistinct(TargetRequest request, int id) =>
        !composer.Targets.Contains(id)
        && (composer.Targets.Count < request.Max || request.Max == 1);

    private bool CanAddRepeated(TargetRequest request, int id)
    {
        int occurrences = composer.Targets.Count(target => target == id);
        int maximumOccurrences = request.MaximumOccurrences?.GetValueOrDefault(id)
            ?? request.Max;
        return occurrences < maximumOccurrences && composer.Targets.Count < request.Max;
    }
}
