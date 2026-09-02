using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Session;

/// <summary>One bounded reason that committed history became unsafe to erase.</summary>
public sealed record InformationExposure(
    [property: JsonRequired] string Reason,
    [property: JsonRequired] IReadOnlyList<int> Seats);

/// <summary>
/// Derives the audience-aware knowledge boundary from authoritative engine output.
/// </summary>
/// <remarks>
/// Save-file undo is a product operation, not a tabletop rule. These signals are
/// deliberately internal to the ledger: public events may be visibility-filtered,
/// while this classifier must record what became knowable even when no client was
/// connected to render it.
/// </remarks>
public static class InformationFrontier
{
    /// <summary>A concealed card entered its owner's hand.</summary>
    public const string Draw = "draw";

    /// <summary>Gameplay randomness was consumed or a hidden area was shuffled.</summary>
    public const string Random = "random";

    /// <summary>A concealed card or hidden face became public.</summary>
    public const string Reveal = "reveal";

    /// <summary>A seat received a prompt exposing hidden search candidates.</summary>
    public const string Search = "search";

    private static readonly HashSet<string> Reasons =
        new([Draw, Random, Reveal, Search], StringComparer.Ordinal);

    /// <summary>Classifies one resolved decision without consulting client behavior.</summary>
    public static IReadOnlyList<InformationExposure> Classify(
        int players,
        long rngBefore,
        long rngAfter,
        IReadOnlyList<InformationSignal> information,
        IReadOnlyList<GameEvent> events,
        Prompt? nextPrompt)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(players);
        ArgumentOutOfRangeException.ThrowIfNegative(rngBefore);
        ArgumentOutOfRangeException.ThrowIfLessThan(rngAfter, rngBefore);
        ArgumentNullException.ThrowIfNull(information);
        ArgumentNullException.ThrowIfNull(events);

        var audiences = new SortedDictionary<string, SortedSet<int>>(StringComparer.Ordinal);
        int[] everyone = [.. Enumerable.Range(0, players)];
        if (information.Any(signal => signal.Kind == InformationKind.Search))
        {
            Add(audiences, Search, everyone);
        }
        if (information.Any(signal => signal.Kind == InformationKind.Reveal))
        {
            Add(audiences, Reveal, everyone);
        }

        foreach (GameEvent happened in events)
        {
            switch (happened)
            {
                case AreaReordered:
                    Add(audiences, Random, everyone);
                    break;
                case CardsMoved moved when IsDraw(moved):
                    if (moved.To.Owner < 0 || moved.To.Owner >= players)
                    {
                        throw new ArgumentOutOfRangeException(
                            nameof(events), "a drawn card names an invalid hand owner");
                    }

                    // The current product is cooperative and player-controlled
                    // cards are visible to the table. Keeping an explicit set
                    // here lets a later concealed-hand policy narrow this
                    // audience without changing the frontier model.
                    Add(audiences, Draw, everyone);
                    break;
            }
        }

        if (nextPrompt is { ExposesConcealedCandidates: true })
        {
            if (nextPrompt.Player < 0 || nextPrompt.Player >= players)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(nextPrompt), "a search prompt names an invalid player");
            }

            Add(audiences, Search, everyone);
        }

        // This is intentionally conservative. It prevents an edit from changing
        // which earlier operation consumes the one gameplay RNG stream and thereby
        // rerolling even a result that remains concealed.
        if (rngAfter > rngBefore)
        {
            Add(audiences, Random, everyone);
        }

        return [.. audiences.Select(pair =>
            new InformationExposure(pair.Key, [.. pair.Value]))];
    }

    /// <summary>Combines signals from dependent decisions in one indivisible unit.</summary>
    public static IReadOnlyList<InformationExposure> Merge(
        IEnumerable<InformationExposure> first,
        IEnumerable<InformationExposure> second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        var audiences = new SortedDictionary<string, SortedSet<int>>(StringComparer.Ordinal);
        foreach (InformationExposure exposure in first.Concat(second))
        {
            Add(audiences, exposure.Reason, exposure.Seats);
        }

        return [.. audiences.Select(pair =>
            new InformationExposure(pair.Key, [.. pair.Value]))];
    }

    /// <summary>Whether a persisted signal uses the bounded canonical form.</summary>
    public static bool IsCanonical(InformationExposure exposure, int players) =>
        exposure is not null
        && Reasons.Contains(exposure.Reason)
        && exposure.Seats is { Count: > 0 }
        && exposure.Seats.All(seat => seat >= 0 && seat < players)
        && exposure.Seats.SequenceEqual(exposure.Seats.Distinct().Order());

    private static void Add(
        SortedDictionary<string, SortedSet<int>> audiences,
        string reason,
        IEnumerable<int> seats)
    {
        if (!Reasons.Contains(reason))
        {
            throw new ArgumentException($"unknown information exposure '{reason}'", nameof(reason));
        }

        if (!audiences.TryGetValue(reason, out SortedSet<int>? audience))
        {
            audience = [];
            audiences.Add(reason, audience);
        }

        foreach (int seat in seats)
        {
            audience.Add(seat);
        }
    }

    private static bool IsDraw(CardsMoved moved) =>
        IsHiddenPile(moved.From.Zone)
        && string.Equals(moved.To.Zone, nameof(DeckType.HandsArea), StringComparison.Ordinal);

    private static bool IsHiddenPile(string zone) => zone is
        nameof(DeckType.PlayerDeck)
        or nameof(DeckType.AdditionalDeck)
        or nameof(DeckType.EncounterDeck)
        or nameof(DeckType.DealtEncounterCardsDeck)
        or nameof(DeckType.BoostCardsDeck);
}
