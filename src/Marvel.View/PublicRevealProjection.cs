using Marvel.Rules.Events;

namespace Marvel.View;

/// <summary>Authorizes only a flip's own engine-captured public faces, without exposing its later state.</summary>
internal static class PublicRevealProjection
{
    internal static CardsFlipped? HistoryReveal(GameEvent happened) =>
        happened is CardsFlipped { FaceUp: true, PublicFaces.Count: > 0 } flipped
            ? Keep(flipped, []) : null;

    internal static CardsFlipped? Keep(CardsFlipped flipped, HashSet<int> addressable)
    {
        var grants = flipped.FaceUp && flipped.PublicFaces is not null
            ? flipped.PublicFaces.Where(face => flipped.Cards.Contains(face.Key))
                .ToDictionary(face => face.Key, face => face.Value)
            : new Dictionary<int, string>();
        var cards = flipped.Cards.Where(card => addressable.Contains(card) || grants.ContainsKey(card)).ToArray();
        if (cards.Length == 0) return null;
        var faces = flipped.SubjectFaces?.Where(face => addressable.Contains(face.Key))
            .ToDictionary(face => face.Key, face => face.Value) ?? new Dictionary<int, string>();
        foreach (var grant in grants) faces[grant.Key] = grant.Value;
        return flipped with
        {
            Cards = cards,
            PublicFaces = grants,
            SubjectFaces = faces.Count == 0 ? null : faces,
        };
    }
}
