using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Gives a revealed card the status card its revealing sentence names.</summary>
internal static class RevealSentenceStatus
{
    // "Reveal that minion, then give it a tough status card" (Planetary Invasion, 16057):
    // ruling ef63f3668700ef73 gives the status in the revealing sentence, after a
    // quickstrike attack has initiated and before that attack's steps resolve.
    // Equivalent mutant: only the revealed card reaches here, in play, and no admitted
    // minion Planetary Invasion can reveal has both quickstrike and teamwork, so
    // dropping either test or the teamwork-branch call survives.
    internal static void Give(World world, Card card, Occurrence revealOccurrence, List<GameEvent> events)
    {
        if (revealOccurrence.RevealOrigin is { StatusAfterReveal: { } status } origin
            && origin.Card == card.ObjectId && DeckTypes.IsInPlay(card.Area.Type))
            Reveal.Afflict(world, world.Facts, card, status, Steps.RevealEncounterCard, events);
    }
}
