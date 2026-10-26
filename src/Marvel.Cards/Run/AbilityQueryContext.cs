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
    internal System.Collections.Immutable.ImmutableArray<(int Card, int Incarnation)> DamageDefeats { get; init; } = [];
    internal ImmutableHashSet<int> ExhaustedForCost { get; init; } = [];

    internal Defeated? TriggeringDefeat { get; init; }
    internal Attacked? TriggeringAttack { get; init; }
    internal Defeated? Defeat => TriggeringDefeat ?? Occurrence.Defeat;

    internal CardInstanceState SourceState { get; init; } = Source.InstanceState;

    internal AbilityQueryProjection? Projection { get; init; }

    internal CardInstanceState Quantities(Card card) => Projection?.Cards.GetValueOrDefault(card.ObjectId) ?? (SourceBindingIsCurrent(card)
        && !DeckTypes.IsInPlay(card.Area.Type)
        ? SourceState : card.InstanceState);
    internal Card? Chosen => ChosenBinding?.Resolve(World, Source, "chosen");
    internal Card? PlayerSelection => PlayerSelectionBinding?.Resolve(World, Source, "player selection");

    internal bool WasSelectedInCurrentArea(Card card)
    {
        var binding = PlayerSelectionBinding is { } player && player.Card.ObjectId == card.ObjectId
            ? player : ChosenBinding is { } chosen && chosen.Card.ObjectId == card.ObjectId ? chosen : null;
        return binding is not null && binding.Area == card.Area.Id && binding.Incarnation == card.Incarnation;
    }

    internal bool ChosenBindingIsCurrent(Card card) => ReferenceEquals(Chosen, card);

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
