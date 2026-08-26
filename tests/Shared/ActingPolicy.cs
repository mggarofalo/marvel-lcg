using Marvel.Rules.Play;
using Marvel.Rules.Prompts;

namespace Marvel.Tests;

/// <summary>
/// A seeded policy that plays rather than passes.
/// </summary>
/// <remarks>
/// <para>
/// The suite's other policy declines everything it can, which is what makes it
/// good at walking the encounter deck: a hero who never acts meets more of it
/// than one who wins. The cost is that half the engine never runs — no card is
/// ever played, no attack is ever made, no cost is ever paid, and the villain
/// deck never advances. MARVEL-230.
/// </para>
/// <para>
/// This is the other half. It takes a random legal option, chooses random legal
/// targets, and pays with the generators the affordance itself offered. It is
/// not a good player and is not meant to be — <c>rr:cost.4</c> permits
/// generating beyond a cost, so paying with everything offered is always a
/// legal payment even when it is a wasteful one, and a policy that reasoned
/// about which card to spend would be a bot rather than a fuzzer.
/// </para>
/// <para>
/// <b>What it found.</b> The end-of-phase prompt offered an answer the engine
/// then refused (MARVEL-245), and two affordances in one turn prompt shared an
/// id, so taking one silently resolved the other (MARVEL-244). Neither is
/// reachable by a policy that declines.
/// </para>
/// </remarks>
/// <param name="seed">The policy's own stream, which is not the game's.</param>
/// <param name="declineOneIn">
/// How often to pass when passing is legal. A player who never passes takes
/// every option the moment it appears, which is its own kind of narrow.
/// </param>
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

        // The invariant that MARVEL-244 broke, checked on every prompt of every
        // game rather than in a test of its own: `Game.Resolve` looks the answer
        // up by id with `First`, so two options sharing one in a single prompt
        // do not fail -- they resolve the wrong option.
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

        // The mulligan is the one thing this cannot take: `Game.Mulligan`
        // discards what the answer names and draws back up, and answering it
        // with a random half of the opening hand is a different experiment from
        // this one. Declining is a legal mulligan -- "any number" includes none.
        return string.Equals(taken.Verb, Game.ResolveMulligans, StringComparison.Ordinal)
            ? Decision.Decline
            : Taking(taken);
    }

    private Decision Taking(Affordance taken) =>
        Decision.Take(taken.Id, Targets(taken), Paying(taken));

    /// <summary>A legal selection from what an affordance asks for.</summary>
    private IReadOnlyList<int> Targets(Affordance taken)
    {
        if (taken.Targets is not { } wanted)
        {
            return [];
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

    /// <summary>
    /// Every generator the affordance offered — <c>rr:cost.4</c>.
    /// </summary>
    /// <remarks>
    /// "A player may generate more resources than are required." So spending
    /// the whole offer is a payment whenever any payment exists, which
    /// <c>CardPlay.Price</c> already relies on from the other side: it asks
    /// whether the whole pool pays, "because if every generator together cannot
    /// pay then no choice among them can".
    /// </remarks>
    private static IReadOnlyList<int> Paying(Affordance taken) =>
    [
        .. taken.CostOptions
            .SelectMany(cost => cost.Generators)
            .Select(source => source.Effect)
            .Distinct(),
    ];
}

/// <summary>Two options in one prompt could not be told apart.</summary>
/// <remarks>
/// Its own type because it is not a rules gap: the rules are fine and the
/// engine's answer-lookup is not. Throwing it out of the policy is what turns
/// every game played into a check of the invariant.
/// </remarks>
public sealed class AmbiguousPromptException(Prompt asked, int id)
    : Exception(
        $"'{asked.Label.Trim()}' offers two options with id {id} — "
        + string.Join(
            ", ",
            asked.Affordances.Select(option => $"{option.Id}:{option.Verb}@{option.AnchorId}")))
{
}
