using Marvel.Core.Random;
using Marvel.Rules.Prompts;

namespace Marvel.Sim;

/// <summary>Chooses research-policy targets within the offered target request.</summary>
internal static class ActingPolicyTargets
{
    /// <summary>Selects targets for one offer, preferring the villain for a single target.</summary>
    /// <remarks>
    /// An allocation request names one target per point, so it may repeat a
    /// target up to its offered occurrence limit. rr:indirect-damage.1: "Indirect
    /// damage dealt to a player can be divided as that player chooses among
    /// characters under their control." The earliest distinct targets are kept
    /// whenever the request admits them, which leaves every answer of policy
    /// version 3 that the engine accepts unchanged and draws nothing from the
    /// stream. Otherwise each point goes to a target drawn from the seat's policy
    /// stream among those the request's own
    /// <see cref="TargetRequest.AllowsPartial"/> still admits.
    /// </remarks>
    internal static IReadOnlyList<int> Select(TargetRequest? wanted, int villain, EngineRandom random)
    {
        ArgumentNullException.ThrowIfNull(random);
        if (wanted is null)
        {
            return [];
        }

        if (wanted.IsGrouped && wanted.Groups is { Count: > 0 } groups)
        {
            return groups[0];
        }

        var selected = wanted.Legal.Take(wanted.Min).ToList();
        if (wanted.Min == 1 && wanted.Legal.Contains(villain))
        {
            selected[0] = villain;
        }

        return wanted.AllowRepeated && !wanted.Allows(selected)
            ? Allocate(wanted, random)
            : selected;
    }

    private static List<int> Allocate(TargetRequest wanted, EngineRandom random)
    {
        var selected = new List<int>(wanted.Min);
        while (selected.Count < wanted.Min)
        {
            List<int> open = [.. wanted.Legal.Where(id => wanted.AllowsPartial([.. selected, id]))];
            if (open.Count == 0)
            {
                throw new InvalidOperationException(
                    $"no offered target extends the allocation after {selected.Count} of {wanted.Min}");
            }

            selected.Add(random.Choice(open));
        }

        return selected;
    }
}
