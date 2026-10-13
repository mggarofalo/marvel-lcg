using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.View;

/// <summary>Projects printed references only from explicit, visibility-filtered occurrence grants.</summary>
internal static class EventFaceProjection
{
    internal static IReadOnlyDictionary<string, BoardCardPresentation> From(
        IReadOnlyList<GameEvent> events, ICardFacts facts)
    {
        var references = new Dictionary<string, BoardCardPresentation>(StringComparer.Ordinal);
        foreach (string id in events.SelectMany(entry => entry.SubjectFaces?.Values ?? []).Distinct())
        {
            CardFaceDescriptor face = PrintedCardFaceProjection.From(facts, id);
            CardBack back = face.Kind is CardKind.AlterEgo or CardKind.Hero or CardKind.Ally or CardKind.Event
                or CardKind.Resource or CardKind.Support or CardKind.Upgrade ? CardBack.Player : CardBack.Encounter;
            var descriptor = new CardDescriptor(null, back, true, true, -1, face);
            references[id] = BoardCardPresentationFactory.Present(descriptor, "DiscardPile") with
            { Fields = [], EffectiveValues = new Dictionary<string, CardEffectiveValue>() };
        }
        return references;
    }
}
