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
                new Slot(component, resource, costs[component].Printed)));
            slots.AddRange(Enumerable.Repeat(
                new Slot(component, Required: null, costs[component].Printed),
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
        return required.Length > amount || required.Any(resource => !Resources.Types.Contains(resource))
            ? null : ((int)amount, required);
    }

    internal static bool Amount(
        string written,
        IReadOnlyDictionary<string, long>? values,
        out long amount) =>
        long.TryParse(
            written,
            System.Globalization.NumberStyles.None,
            System.Globalization.CultureInfo.InvariantCulture,
            out amount)
        || values is not null && values.TryGetValue(written, out amount);

    internal static bool Accepts(char printed, char? required, bool printedCost) =>
        required is null
        || printed == required
        || !printedCost && printed == Resources.Wild;

    internal readonly record struct Slot(int Component, char? Required, bool Printed);
}
