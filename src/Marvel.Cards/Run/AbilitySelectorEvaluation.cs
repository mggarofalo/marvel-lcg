using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

internal sealed record AbilityQueryResult<T>(T Value, ImmutableArray<InformationKind> Information);

// One evaluation owns its observations. Nested selections share this local
// collector; separate evaluations cannot change each other's bindings or output.
internal sealed class AbilitySelectorEvaluation(AbilityQueryContext context)
{
    private readonly List<InformationKind> information = [];

    internal AbilityQueryResult<T> Result<T>(T value) => new(value, [.. information]);

    internal Card? Find(AbilityCardSelection selector)
    {
        if (selector is AbilityCardSelection.Bound bound) return AbilityCardQueries.Named(bound.Binding, context);
        if (selector is AbilityCardSelection.Titled titled)
        {
            var found = AbilityCardQueries.ReferencedByTitle(titled.Title, context);
            return found.Count switch
            {
                0 => null,
                1 => found[0],
                _ => throw new RulesNotImplementedException(
                    $"'{context.Source.FaceId}' refers to {found.Count} cards titled '{titled.Title}' where one card is required"),
            };
        }
        if (selector is AbilityCardSelection.Query { Kind:
            AbilityCardQuery.Villain or AbilityCardQuery.MainScheme or AbilityCardQuery.YourAsideMinion
            or AbilityCardQuery.YourAsideSideScheme or AbilityCardQuery.TopmostTechInChosenDiscard } query)
            return AbilityCardQueries.Cards(query.Kind, context).SingleOrDefault();
        if (selector is AbilityCardSelection.InAreas areas)
        {
            var found = CardsIn(areas);
            return found.Count switch
            {
                0 => null,
                1 => found[0],
                _ => throw new RulesNotImplementedException(
                    $"'{context.Source.FaceId}' searched and found {found.Count} matching cards; "
                    + "rr:search.1 gives the player that choice and asking is not implemented"),
            };
        }
        throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' uses a selector whose single-card resolution is not implemented");
    }

    internal IReadOnlyList<Card> Every(AbilityCardSelection selector) => selector switch
    {
        AbilityCardSelection.Bound bound => AbilityCardQueries.Named(bound.Binding, context) is { } card ? [card] : [],
        AbilityCardSelection.Query query => AbilityCardQueries.Cards(query.Kind, context),
        AbilityCardSelection.Titled titled => AbilityCardQueries.ReferencedByTitle(titled.Title, context),
        AbilityCardSelection.EnemiesWithTrait trait => [.. context.World.Areas
            .Where(area => area.Type is DeckType.VillainArea or DeckType.EngagedEnemiesArea)
            .SelectMany(area => area.Cards)
            .Where(card => Rules.State.Traits.Of(context.World, card, context.World.Facts).Contains(trait.Trait, StringComparer.Ordinal))],
        AbilityCardSelection.WithTrait trait => [.. Every(trait.Cards)
            .Where(card => Rules.State.Traits.Has(context.World, card, trait.Trait, context.World.Facts))],
        AbilityCardSelection.WithoutAnotherCopyAttached unoccupied => [.. Every(unoccupied.Cards)
            .Where(candidate => !context.World.Areas.Where(area => area.Host == candidate.ObjectId)
                .SelectMany(area => area.Cards)
                .Any(attached => attached.ObjectId != context.Source.ObjectId
                    && string.Equals(context.World.Facts.Title(attached.FaceId), context.World.Facts.Title(context.Source.FaceId), StringComparison.Ordinal)))],
        AbilityCardSelection.Discardable discardable => [.. Every(discardable.Cards).Where(card => CanRemove(discardable.Cards, card))],
        AbilityCardSelection.Ranked ranked => Ranked(ranked),
        AbilityCardSelection.InAreas areas => CardsIn(areas),
        _ => throw new InvalidOperationException("Unknown compiled card selection"),
    };

    private IReadOnlyList<Card> CardsIn(AbilityCardSelection.InAreas selection)
    {
        // rr:search.2: "cards being searched are not considered to leave the
        // searched area." Exposure is an output, never a write to the board.
        if (selection.Areas.Any(area => area is AbilitySearchArea.YourDeck or AbilitySearchArea.EncounterDeck))
            information.Add(InformationKind.Search);
        return [.. selection.Areas.SelectMany(AreaCards)
            .Where(card => selection.Kind is null || context.World.Facts.Kind(card.FaceId) == selection.Kind)
            .Where(card => selection.Trait is null || Rules.State.Traits.Has(context.World, card, selection.Trait, context.World.Facts))
            .Where(card => selection.Title is null || string.Equals(context.World.Facts.Title(card.FaceId), selection.Title, StringComparison.Ordinal))];
    }

    private IReadOnlyList<Card> AreaCards(AbilitySearchArea area) => area == AbilitySearchArea.YourDeck
        ? context.World.Seats[context.Player].Deck.Cards
        : context.World.Areas.FirstOrDefault(candidate => candidate.Type == AreaType(area)
            && candidate.PlayArea == PlayArea.Villains && candidate.Host == -1)?.Cards ?? [];

    internal static DeckType AreaType(AbilitySearchArea area) => area switch
    {
        AbilitySearchArea.EncounterDeck => DeckType.EncounterDeck,
        AbilitySearchArea.EncounterDiscardPile => DeckType.EncounterDiscardPile,
        AbilitySearchArea.ScenarioSetAside => DeckType.AsideDeck,
        AbilitySearchArea.YourDeck => DeckType.PlayerDeck,
        _ => throw new InvalidOperationException("Unknown compiled search area"),
    };

    private IReadOnlyList<Card> Ranked(AbilityCardSelection.Ranked selection)
    {
        // rr:permanent.4.1: the effect "instead targets the non-permanent card
        // that fits its criteria." Filter before comparing and keep all ties.
        var among = Every(selection.Cards).Where(card => CanRemove(selection.Cards, card)).ToList();
        if (among.Count == 0) return [];
        long Rank(Card card) => selection.By switch
        {
            AbilityCardRank.Cost => context.World.Facts.PrintedValue(card.FaceId, "Cost", context.World.Players),
            AbilityCardRank.Attack => StateFields.Modified(context.World, card, "attack", context.World.Facts, context.World.Players),
            AbilityCardRank.PrintedHealth => FacedownDrones.BaseValue(card, context.World.Facts, "HP", context.World.Players),
            _ => throw new InvalidOperationException("Unknown compiled card rank"),
        };
        long extreme = selection.Maximum ? among.Max(Rank) : among.Min(Rank);
        return [.. among.Where(card => Rank(card) == extreme)];
    }

    internal bool CanRemove(AbilityCardSelection selector, Card target)
    {
        var binding = (selector as AbilityCardSelection.Bound)?.Binding;
        bool reachable = RemovalAreaIsReachable(binding, target) || ExplicitlySelectsOutOfPlayCard(selector, target);
        bool current = binding switch
        {
            AbilityCardBinding.This => context.SourceBindingIsCurrent(target),
            AbilityCardBinding.Chosen => context.ChosenBindingIsCurrent(target),
            _ => true,
        };
        return reachable && current && Rules.Play.Discard.EffectCanRemove(context.World, context.World.Facts, context.Source, target);
    }

    // rr:in-play-and-out-of-play.4 permits out-of-play targets only when text
    // "specifically refers to an out-of-play area". Current choices and the
    // revealed-card interrupt retain the area they explicitly selected.
    private bool RemovalAreaIsReachable(AbilityCardBinding? binding, Card target) =>
        DeckTypes.IsInPlay(target.Area.Type)
        || target.Area.Type is DeckType.BoostingArea or DeckType.ProcessingArea or DeckType.RevealingArea
        || target.Area.Type == DeckType.DealtEncounterCardsDeck && binding == AbilityCardBinding.TriggerSubject
            && context.Occurrence.Conditions.Contains(Steps.CardRevealed, StringComparer.Ordinal)
        || binding == AbilityCardBinding.Chosen && context.WasSelectedInCurrentArea(target);

    private bool ExplicitlySelectsOutOfPlayCard(AbilityCardSelection selector, Card target) => selector switch
    {
        AbilityCardSelection.InAreas areas => CardsIn(areas).Any(card => card.ObjectId == target.ObjectId),
        AbilityCardSelection.WithTrait trait => ExplicitlySelectsOutOfPlayCard(trait.Cards, target),
        AbilityCardSelection.WithoutAnotherCopyAttached unoccupied => ExplicitlySelectsOutOfPlayCard(unoccupied.Cards, target),
        AbilityCardSelection.Discardable discardable => ExplicitlySelectsOutOfPlayCard(discardable.Cards, target),
        AbilityCardSelection.Ranked ranked => ExplicitlySelectsOutOfPlayCard(ranked.Cards, target),
        _ => false,
    };
}
