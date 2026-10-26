using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

/// <summary>Retains one uncertain discard-and-engage payment through entry and its nested windows.</summary>
internal sealed class AbilityMinionEntryCost
{
    private readonly EffectCompletion? entry;

    private AbilityMinionEntryCost(EffectCompletion? entry) => this.entry = entry;

    internal bool IsComplete => entry is null || entry.IsComplete;
    internal bool Paid => entry is { IsComplete: true, Applied: > 0 };

    internal static AbilityMinionEntryCost Begin(World world, int player, string trigger, List<GameEvent> events)
    {
        // rr:encounter-deck.2: stop at the match or the empty deck, without
        // continuing through the replacement deck. The returned physical card
        // remains the match even when the immediate reset shuffled it.
        var found = EncounterDeck.DiscardUntil(world, world.Facts, CardKind.Minion, trigger, events);
        if (found is null) return new(null);
        var completion = new EffectCompletion(1, 1);
        EncounterEntry.Schedule(world, EncounterEntryRequest.For(found, player,
            new CardEntryPlacement(Verb: "Put_Into_Play")), completion);
        return new(completion);
    }
}
