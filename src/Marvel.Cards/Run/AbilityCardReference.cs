using System.Collections.Immutable;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// An immutable capture of which incarnation was selected, not just its object id.
internal sealed record AbilityCardReference(Card Card, int Area, int Incarnation)
{
    internal Card? Resolve(Card source, string name)
    {
        if (Incarnation < 0 || Area < 0)
            throw new RulesNotImplementedException($"'{source.FaceId}' continuation has no {name} provenance");
        return Card.Incarnation == Incarnation ? Card : null;
    }
}
