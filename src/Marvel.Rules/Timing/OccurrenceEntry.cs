using Marvel.Rules.Play;

namespace Marvel.Rules.Timing;

/// <summary>Entry provenance owned by one occurrence, reconstructed during replay.</summary>
public sealed record OccurrenceEntry(
    PlayedCardPayment? CardPlayPayment = null,
    EntryPayment? EntryPayment = null,
    PlayerAllyEntryOperation? PendingAllyEntry = null);
