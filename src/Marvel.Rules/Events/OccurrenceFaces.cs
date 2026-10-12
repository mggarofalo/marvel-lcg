using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Rules.Events;

/// <summary>The event contract chooses to retain exact exposed printed identities, never hidden underlying faces.</summary>
public static class OccurrenceFaces
{
    /// <summary>Captures a face only when the occurrence exposes an ordinary printed card.</summary>
    public static IReadOnlyDictionary<int, string>? For(Card card) =>
        card.FaceUp && !EffectiveCards.HasProfile(card)
            ? new Dictionary<int, string> { [card.ObjectId] = card.FaceId } : null;
}
