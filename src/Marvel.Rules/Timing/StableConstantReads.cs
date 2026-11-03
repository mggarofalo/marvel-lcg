using Marvel.Rules.State;

namespace Marvel.Rules.Timing;

/// <summary>
/// Reuses one settled constant-effect list for the length of a read that
/// changes nothing.
/// </summary>
/// <remarks>
/// <para>
/// <c>rr:modifiers</c>: "The game constantly checks and (if necessary) updates
/// the count of any variable quantity that is being modified." A count must be
/// current whenever it is read; the rule does not ask for the same board to be
/// settled again when nothing has changed. A stable read -- the state digest,
/// a turn prompt, the abilities waiting in a window -- changes nothing, so
/// every settlement inside it derives the same list. The engine chooses to
/// settle once per stable read and answer later settlements from that list.
/// </para>
/// <para>
/// A stable read may still preflight a departure or project an entry, which
/// moves cards and hides effects for the length of a nested check. The list is
/// therefore remembered together with the board it was derived from -- every
/// card's area and the effect bookkeeping a settlement reads -- and reused only
/// while that board is unchanged. Outside a stable read nothing is remembered.
/// </para>
/// </remarks>
internal sealed class StableConstantReads
{
    private int depth;
    private SettledBoard? settled;

    /// <summary>Opens a read that changes nothing; reads may nest.</summary>
    internal Scope Begin()
    {
        depth++;
        return new Scope(this);
    }

    /// <summary>The list settled earlier in this read on the same board, if any.</summary>
    internal IReadOnlyList<ContinuousEffect>? Reuse(ContinuousEffects effects) =>
        settled is { } board && board.Describes(effects) ? board.Constants : null;

    /// <summary>Remembers a fresh settlement for the rest of this read.</summary>
    internal void Remember(ContinuousEffects effects, IReadOnlyList<ContinuousEffect> constants)
    {
        if (depth > 0)
        {
            settled = SettledBoard.Capture(effects, constants);
        }
    }

    private void End()
    {
        if (--depth == 0)
        {
            settled = null;
        }
    }

    /// <summary>Closes a stable read.</summary>
    internal readonly struct Scope(StableConstantReads reads) : IDisposable
    {
        public void Dispose() => reads.End();
    }

    private sealed class SettledBoard
    {
        private Area[] areas = [];
        private ContinuousEffectEntry[] entries = [];
        private ContinuousEffectEntry[] suppressed = [];
        private ContinuousEffect[] suppressedConstants = [];
        private int[] departing = [];

        internal IReadOnlyList<ContinuousEffect> Constants { get; private init; } = [];

        internal static SettledBoard Capture(
            ContinuousEffects effects, IReadOnlyList<ContinuousEffect> constants) => new()
        {
            Constants = [.. constants],
            areas = [.. effects.world.Cards.Select(card => card.Area)],
            entries = [.. effects.entries],
            suppressed = [.. effects.suppressed],
            suppressedConstants = [.. effects.suppressedConstants],
            departing = [.. effects.departing],
        };

        internal bool Describes(ContinuousEffects effects) =>
            SameSequence(effects.world.Cards, areas, (card, area) => ReferenceEquals(card.Area, area))
            && SameSequence(effects.entries, entries, ReferenceEquals)
            // A cascade preflight can replace one hidden batch with another of the
            // same size; only a nested cascade reaches that, so no test drives it.
            && SameSequence(effects.suppressedConstants, suppressedConstants, (live, kept) => live == kept)
            && SameSet(effects.suppressed, suppressed)
            && SameSet(effects.departing, departing);

        private static bool SameSequence<TLive, TKept>(
            IReadOnlyList<TLive> live, TKept[] kept, Func<TLive, TKept, bool> same)
        {
            if (live.Count != kept.Length)
            {
                return false;
            }

            for (int index = 0; index < kept.Length; index++)
            {
                if (!same(live[index], kept[index]))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool SameSet<T>(HashSet<T> live, T[] kept) =>
            live.Count == kept.Length && Array.TrueForAll(kept, live.Contains);
    }
}
