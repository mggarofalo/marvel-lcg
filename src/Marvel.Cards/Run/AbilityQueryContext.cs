using System.Collections.Immutable;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Cards.Run;

// Engine-owned evaluation input. The board is read in place, while bindings and
// ordered targets are captured for this evaluation. There is no event sink,
// agenda continuation, payment mutation or execution-mode switch here.
internal sealed record AbilityQueryContext(
    World World, Card Source, Occurrence Occurrence, int Player,
    int SourceIncarnation, AbilityCardReference? ChosenBinding,
    AbilityCardReference? PlayerSelectionBinding, Card? Altered,
    ImmutableArray<Card> PowerTargets)
{
    internal Card? Chosen => ChosenBinding?.Resolve(Source, "chosen");
    internal Card? PlayerSelection => PlayerSelectionBinding?.Resolve(Source, "player selection");

    internal bool WasSelectedInCurrentArea(Card card)
    {
        var binding = PlayerSelectionBinding is { } player && player.Card.ObjectId == card.ObjectId
            ? player : ChosenBinding is { } chosen && chosen.Card.ObjectId == card.ObjectId ? chosen : null;
        return binding is not null && binding.Area == card.Area.Id && binding.Incarnation == card.Incarnation;
    }

    internal bool ChosenBindingIsCurrent(Card card) => ChosenBinding is { } binding
        && binding.Card.ObjectId == card.ObjectId && binding.Incarnation == card.Incarnation;

    internal bool SourceBindingIsCurrent(Card card) => Source.ObjectId == card.ObjectId && SourceIncarnation == card.Incarnation;

    internal Card? SourceReference
    {
        get
        {
            if (SourceIncarnation < 0)
                throw new RulesNotImplementedException($"'{Source.FaceId}' continuation has no source-card provenance");
            return Source.Incarnation == SourceIncarnation ? Source : null;
        }
    }
}

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
