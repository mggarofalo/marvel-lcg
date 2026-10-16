using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Tests;

/// <summary>A seeded test policy that selects engine-offered actions and payments.</summary>
/// <param name="seed">The policy's own stream, separate from gameplay randomness.</param>
/// <param name="declineOneIn">Frequency of passing an optional prompt.</param>
public sealed class ActingPolicy(int seed, int declineOneIn = 4)
{
    private readonly Random dice = new(seed);

    /// <summary>How many prompts this policy has answered.</summary>
    public int Answered { get; private set; }

    /// <summary>Answers one prompt.</summary>
    /// <param name="asked">What the game wants to know.</param>
    public Decision Answer(Prompt asked)
    {
        ArgumentNullException.ThrowIfNull(asked);
        Answered++;

        // Affordance handles must identify exactly one current offer.
        var ids = new HashSet<int>();
        foreach (var option in asked.Affordances)
        {
            if (!ids.Add(option.Id))
            {
                throw new AmbiguousPromptException(asked, option.Id);
            }
        }

        var legal = asked.Affordances.Where(option => option.IsLegal).ToList();
        if (legal.Count == 0 || (asked.Cancellable && dice.Next(declineOneIn) == 0))
        {
            return asked.Cancellable ? Decision.Decline : Taking(asked.Affordances[0]);
        }

        var taken = legal[dice.Next(legal.Count)];

        // Keep the opening hand by committing the offered mulligan with no cards.
        return string.Equals(taken.Verb, Game.ResolveMulligans, StringComparison.Ordinal)
            ? Decision.Take(taken.Id)
            : Taking(taken);
    }

    private Decision Taking(Affordance taken)
    {
        IReadOnlyList<int> targets = Targets(taken);
        if (taken.CostOptions.Count == 0) return Decision.Take(taken.Id, targets, []);
        foreach (CostOption cost in taken.CostOptions)
        {
            if (!DecisionAssessment.CostApplies(taken, cost, targets)) continue;
            var values = cost.VariableRequests.ToDictionary(
                variable => variable.Name, variable => variable.Min, StringComparer.Ordinal);
            List<ResourceSource> sources = [.. cost.Generators];
            if (SmallestPayment(sources, cost, values) is not { } paying) continue;
            return Decision.Take(taken.Id, targets, paying, values,
                ResourcePayment.Allocate(cost, paying, values)!);
        }
        throw new InvalidOperationException($"No offered price pays for {taken.Verb}: {taken.Label}");
    }

    /// <summary>A legal selection from what an affordance asks for.</summary>
    private IReadOnlyList<int> Targets(Affordance taken)
    {
        if (taken.Targets is not { } wanted)
        {
            return [];
        }

        // An indirect-damage assignment names one character per point, so the
        // same character may appear repeatedly. It is a division rather than
        // `rr:choose-game-element` targeting; cycling the legal characters is
        // the policy's deterministic, capacity-friendly assignment strategy.
        if (string.Equals(taken.Label, "indirectDamage", StringComparison.Ordinal))
        {
            return [.. Enumerable.Range(0, wanted.Min)
                .Select(index => wanted.Legal[index % wanted.Legal.Count])];
        }

        // `TargetRequest.IsGrouped` is a different reading of the same fields
        // rather than an extra constraint on them -- Explosive Arrow offers
        // whole groups and its flat range describes neither of them.
        if (wanted.IsGrouped && wanted.Groups is { Count: > 0 } groups)
        {
            return groups[dice.Next(groups.Count)];
        }

        var pool = wanted.Legal.OrderBy(_ => dice.Next()).ToList();
        int least = Math.Min(wanted.Min, pool.Count);
        int most = Math.Min(wanted.Max, pool.Count);
        return [.. pool.Take(least + dice.Next(Math.Max(1, most - least + 1)))];
    }

    private static IReadOnlyList<int>? SmallestPayment(
        List<ResourceSource> sources, CostOption cost, IReadOnlyDictionary<string, long> values)
    {
        for (int count = 0; count <= sources.Count; count++)
        {
            var chosen = new List<ResourceSource>();
            if (Choose(start: 0, count))
            {
                return [.. chosen.Select(source => source.Effect)];
            }

            bool Choose(int start, int left)
            {
                if (left == 0)
                {
                    return ResourcePayment.Allocate(cost,
                        [.. chosen.Select(source => source.Effect)], values) is not null;
                }

                for (int index = start; index <= sources.Count - left; index++)
                {
                    chosen.Add(sources[index]);
                    if (Choose(index + 1, left - 1))
                    {
                        return true;
                    }
                    chosen.RemoveAt(chosen.Count - 1);
                }
                return false;
            }
        }

        return null;
    }
}
