using System.Collections.Immutable;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// An immutable capture of which incarnation was selected, not just its object id.
internal sealed record AbilityCardReference(Card Card, int Area, int Incarnation)
{
    internal Card? Resolve(World world, Card source, string name)
    {
        if (Incarnation < 0 || Area < 0)
            throw new RulesNotImplementedException($"'{source.FaceId}' continuation has no {name} provenance");
        if (Card.Incarnation != Incarnation) return null;
        if (DeckTypes.IsInPlay(Card.Area.Type)
            || !CardKinds.IsVillain(EffectiveCards.Kind(Card, world.Facts))
            || !world.Areas.Any(area => area.Id == Area && area.Type == DeckType.VillainArea))
            return Card;

        // rr:villain-defeat.3.1: same-title stages are "the same character".
        // Incarnation provenance above still prevents following a reentered copy.
        var stages = world.Areas.Where(area => area.Id == Area && area.Type == DeckType.VillainArea)
            .SelectMany(area => area.Cards)
            .Where(candidate => CardKinds.IsVillain(EffectiveCards.Kind(candidate, world.Facts))
                && string.Equals(world.Facts.Title(Card.FaceId), world.Facts.Title(candidate.FaceId),
                    StringComparison.Ordinal)).ToArray();
        return stages.Length switch
        {
            0 => null,
            1 => stages[0],
            _ => throw new RulesNotImplementedException("a chosen villain has multiple same-title replacement stages"),
        };
    }
}
