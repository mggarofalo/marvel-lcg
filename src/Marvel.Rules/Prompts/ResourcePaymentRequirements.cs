using Marvel.Rules.Play;

namespace Marvel.Rules.Prompts;

/// <summary>Describes required resource slots and their accepted icons.</summary>
internal static class ResourcePaymentRequirements
{
    internal static List<Slot>? Slots(
        IReadOnlyList<ResourceCost> costs, IReadOnlyDictionary<string, long>? values)
    {
        var slots = new List<Slot>();
        for (int component = 0; component < costs.Count; component++)
        {
            if (Resolve(costs[component], values) is not { } resolved) return null;
            (int amount, string required) = resolved;
            slots.AddRange(required.Select(resource =>
                new Slot(component, resource, costs[component].Printed, costs[component].DistinctTypes)));
            slots.AddRange(Enumerable.Repeat(
                new Slot(component, Required: null, costs[component].Printed, costs[component].DistinctTypes),
                amount - required.Length));
        }
        return slots;
    }

    internal static (int Amount, string Required)? Resolve(
        ResourceCost cost, IReadOnlyDictionary<string, long>? values)
    {
        if (!Amount(cost.Cost, values, out long amount)
            || amount < 0 || amount > int.MaxValue) return null;
        string required = string.Concat(cost.Rule ?? []);
        if (cost.RepeatedResource is { } repeated)
        {
            if (required.Length != 0 || !Resources.Types.Contains(repeated)) return null;
            required = new string(repeated, (int)amount);
        }
        return ValidRequirements(cost, amount, required) ? ((int)amount, required) : null;
    }

    private static bool ValidRequirements(ResourceCost cost, long amount, string required) =>
        required.Length <= amount && required.All(Resources.Types.Contains)
        && (!cost.DistinctTypes || amount <= Resources.Types.Length
            && required.Distinct().Count() == required.Length);

    internal static bool Amount(
        string written,
        IReadOnlyDictionary<string, long>? values,
        out long amount) =>
        long.TryParse(
            written,
            System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture,
            out amount)
        || values is not null && values.TryGetValue(written, out amount)
        || ReducedVariable(written, values, out amount);

    // Engine spelling "X-2": the amount paid for a defined variable after a cost
    // reduction. rr:non-numerical-variable.1: once X is defined, "the amount paid may
    // be modified by effects without changing the value of X". rr:modifiers.4: "a
    // card cannot have 'negative' ... cost".
    private static bool ReducedVariable(
        string written, IReadOnlyDictionary<string, long>? values, out long amount)
    {
        amount = 0;
        int minus = written.IndexOf('-', StringComparison.Ordinal);
        if (minus <= 0 || values is null
            || !values.TryGetValue(written[..minus], out long defined)
            || !long.TryParse(written[(minus + 1)..], System.Globalization.NumberStyles.None,
                System.Globalization.CultureInfo.InvariantCulture, out long reduction)) return false;
        amount = Math.Max(0, defined - reduction);
        return true;
    }

    internal static bool CanDeclare(char icon, char declared, bool printedCost) =>
        Resources.Types.Contains(declared)
        && (icon == declared || !printedCost && icon == Resources.Wild);

    internal static bool Accepts(char printed, char? required, bool printedCost) =>
        required is null
        || printed == required
        || !printedCost && printed == Resources.Wild;

    internal static IEnumerable<char> Declarations(Slot slot, char printed)
    {
        if (slot.Required is { } required) return [required];
        // rr:wild-resource.1: the player may specify energy, mental, physical,
        // or wild. Search each declaration when the cost distinguishes types.
        return slot.DistinctTypes && !slot.Printed && printed == Resources.Wild
            ? Resources.Types.OrderBy(type => type == Resources.Wild ? 0 : 1)
            : [printed];
    }

    internal readonly record struct Slot(
        int Component, char? Required, bool Printed, bool DistinctTypes, int? Source = null);
}
