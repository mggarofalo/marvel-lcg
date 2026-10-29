using Marvel.Rules.State;

namespace Marvel.Rules.Play;

internal sealed record AllyEntryRequest(Card Ally, int Controller, string Trigger, EntryPayment? Payment = null);
