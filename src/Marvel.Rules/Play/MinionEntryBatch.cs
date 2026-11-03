using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Minions put into play by one instruction whose after-entry abilities wait for the last of them.</summary>
/// <remarks>
/// Ruling 82bcc63411002a00 (Drang III): each minion's Interrupt abilities resolve as it
/// engages; "after they've all engaged, resolve all Quickstrike and Teamwork abilities on
/// those minions, then any Response abilities." An entry that names this batch keeps its
/// interrupt window, records its engagement here, and leaves its keyword activations and
/// response window to <see cref="MinionEntryBatchProcedure"/>. The batch object is an
/// engine choice; it is rebuilt by replaying the accepted decisions.
/// </remarks>
public sealed class MinionEntryBatch
{
    private readonly List<MinionEntryBatchEntry> entries = [];

    /// <summary>The minions that engaged, in the order they entered play.</summary>
    public IReadOnlyList<MinionEntryBatchEntry> Entries => entries;

    internal void Record(Card card, int player, Occurrence occurrence) =>
        entries.Add(new(card.ObjectId, card.Incarnation, player, occurrence));
}
