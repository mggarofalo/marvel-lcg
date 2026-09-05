using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

public sealed partial class AbilityRunner
{
    private static IReadOnlyList<Card> Every(AbilityCardSelection selector, Cast cast) => selector switch
    {
        AbilityCardSelection.Bound bound => Named(bound.Binding, cast) is { } card ? [card] : [],
        AbilityCardSelection.Query query => QueryCards(query.Kind, cast),
        AbilityCardSelection.Titled titled => ReferencedByTitle(titled.Title, cast),
        AbilityCardSelection.EnemiesWithTrait trait => [.. cast.World.Areas
            .Where(area => area.Type is DeckType.VillainArea or DeckType.EngagedEnemiesArea)
            .SelectMany(area => area.Cards)
            .Where(card => Rules.State.Traits.Of(cast.World, card, cast.World.Facts)
                .Contains(trait.Trait, StringComparer.Ordinal))],
        AbilityCardSelection.WithTrait trait => [.. Every(trait.Cards, cast)
            .Where(card => Rules.State.Traits.Has(cast.World, card, trait.Trait, cast.World.Facts))],
        AbilityCardSelection.WithoutAnotherCopyAttached unoccupied => [.. Every(unoccupied.Cards, cast)
            .Where(candidate => !cast.World.Areas.Where(area => area.Host == candidate.ObjectId)
                .SelectMany(area => area.Cards)
                .Any(attached => attached.ObjectId != cast.Source.ObjectId
                    && string.Equals(cast.World.Facts.Title(attached.FaceId),
                        cast.World.Facts.Title(cast.Source.FaceId), StringComparison.Ordinal)))],
        AbilityCardSelection.Discardable discardable => [.. Every(discardable.Cards, cast)
            .Where(card => CanRemoveByEffect(discardable.Cards, cast, card))],
        AbilityCardSelection.Ranked ranked => Ranked(ranked, cast),
        AbilityCardSelection.InAreas areas => CardsIn(areas, cast),
        _ => throw new InvalidOperationException("Unknown compiled card selection"),
    };

    private static Area Area(AbilitySearchArea area, Cast cast) => area switch
    {
        AbilitySearchArea.EncounterDeck => cast.World.AreaOf(DeckType.EncounterDeck),
        AbilitySearchArea.EncounterDiscardPile => cast.World.AreaOf(DeckType.EncounterDiscardPile),
        AbilitySearchArea.ScenarioSetAside => cast.World.AreaOf(DeckType.AsideDeck),
        AbilitySearchArea.YourDeck => cast.World.Seats[cast.Player].Deck,
        _ => throw new InvalidOperationException("Unknown compiled search area"),
    };

    private static IReadOnlyList<Card> CardsIn(AbilityCardSelection.InAreas selection, Cast cast)
    {
        if (cast.ObservingInformation && selection.Areas.Any(area => area is
            AbilitySearchArea.YourDeck or AbilitySearchArea.EncounterDeck))
        {
            cast.World.RecordInformation(InformationKind.Search);
        }
        return [.. selection.Areas.SelectMany(area => Area(area, cast).Cards)
            .Where(card => selection.Kind is null || cast.World.Facts.Kind(card.FaceId) == selection.Kind)
            .Where(card => selection.Trait is null
                || Rules.State.Traits.Has(cast.World, card, selection.Trait, cast.World.Facts))
            .Where(card => selection.Title is null || string.Equals(
                cast.World.Facts.Title(card.FaceId), selection.Title, StringComparison.Ordinal))];
    }

    private static IReadOnlyList<Card> Ranked(AbilityCardSelection.Ranked selection, Cast cast)
    {
        var among = Every(selection.Cards, cast)
            .Where(card => CanRemoveByEffect(selection.Cards, cast, card)).ToList();
        return RankedCandidates(among, selection.By, selection.Maximum, cast);
    }

    private static IReadOnlyList<Card> RankedCandidates(
        List<Card> among, AbilityCardRank by, bool maximum, Cast cast)
    {
        if (among.Count == 0) return [];
        long Rank(Card card) => by switch
        {
            AbilityCardRank.Cost => cast.World.Facts.PrintedValue(card.FaceId, "Cost", cast.World.Players),
            AbilityCardRank.Attack => StateFields.Modified(
                cast.World, card, "attack", cast.World.Facts, cast.World.Players),
            AbilityCardRank.PrintedHealth => FacedownDrones.BaseValue(card, cast.World.Facts, "HP", cast.World.Players),
            _ => throw new InvalidOperationException("Unknown compiled card rank"),
        };
        long extreme = maximum ? among.Max(Rank) : among.Min(Rank);
        return [.. among.Where(card => Rank(card) == extreme)];
    }

    // rr:in-play-and-out-of-play.4 permits out-of-play targets only when text
    // "specifically refers to an out-of-play area". Current choices and the
    // revealed-card interrupt retain the area they explicitly selected.
    private static bool RemovalAreaIsReachable(AbilityCardBinding? binding, Cast cast, Card target) =>
        DeckTypes.IsInPlay(target.Area.Type)
        || target.Area.Type is DeckType.BoostingArea or DeckType.ProcessingArea or DeckType.RevealingArea
        || target.Area.Type == DeckType.DealtEncounterCardsDeck
            && binding == AbilityCardBinding.TriggerSubject
            && cast.Occurrence.Conditions.Contains(Steps.CardRevealed, StringComparer.Ordinal)
        || binding == AbilityCardBinding.Chosen && cast.WasSelectedInCurrentArea(target);

    private static bool RemovalBindingIsCurrent(AbilityCardBinding? binding, Cast cast, Card target) => binding switch
    {
        AbilityCardBinding.This => cast.SourceBindingIsCurrent(target),
        AbilityCardBinding.Chosen => cast.ChosenBindingIsCurrent(target),
        _ => true,
    };

    private static bool CanRemoveByEffect(AbilityCardSelection selector, Cast cast, Card target)
    {
        var binding = (selector as AbilityCardSelection.Bound)?.Binding;
        bool reachable = RemovalAreaIsReachable(binding, cast, target)
            || ExplicitlySelectsOutOfPlayCard(selector, cast, target);
        return reachable && RemovalBindingIsCurrent(binding, cast, target)
            && Rules.Play.Discard.EffectCanRemove(cast.World, cast.World.Facts, cast.Source, target);
    }

    private static bool ExplicitlySelectsOutOfPlayCard(AbilityCardSelection selector, Cast cast, Card target) => selector switch
    {
        AbilityCardSelection.InAreas areas => CardsIn(areas, cast).Any(card => card.ObjectId == target.ObjectId),
        AbilityCardSelection.WithTrait trait => ExplicitlySelectsOutOfPlayCard(trait.Cards, cast, target),
        AbilityCardSelection.WithoutAnotherCopyAttached unoccupied => ExplicitlySelectsOutOfPlayCard(unoccupied.Cards, cast, target),
        AbilityCardSelection.Discardable discardable => ExplicitlySelectsOutOfPlayCard(discardable.Cards, cast, target),
        AbilityCardSelection.Ranked ranked => ExplicitlySelectsOutOfPlayCard(ranked.Cards, cast, target),
        _ => false,
    };

    private static bool ContainsYouOrYour(AbilityCardSelection selector) => selector switch
    {
        AbilityCardSelection.Bound bound => bound.Binding is
            AbilityCardBinding.You or AbilityCardBinding.YourHero or AbilityCardBinding.YourAlterEgo,
        AbilityCardSelection.Query query => query.Kind is
            AbilityCardQuery.YourAsideMinion or AbilityCardQuery.YourAsideSideScheme
            or AbilityCardQuery.MinionsEngagedWithYou or AbilityCardQuery.YourAsidePile
            or AbilityCardQuery.UpgradesAndSupportsYouControl or AbilityCardQuery.IdentitySpecificInYourHand
            or AbilityCardQuery.SupportsYouControl or AbilityCardQuery.CharactersYouControl
            or AbilityCardQuery.UpgradesYouControl or AbilityCardQuery.AlliesYouControl or AbilityCardQuery.DronesEngagedWithYou,
        AbilityCardSelection.WithTrait trait => ContainsYouOrYour(trait.Cards),
        AbilityCardSelection.WithoutAnotherCopyAttached unoccupied => ContainsYouOrYour(unoccupied.Cards),
        AbilityCardSelection.Discardable discardable => ContainsYouOrYour(discardable.Cards),
        AbilityCardSelection.Ranked ranked => ContainsYouOrYour(ranked.Cards),
        AbilityCardSelection.InAreas areas => areas.Areas.Contains(AbilitySearchArea.YourDeck),
        AbilityCardSelection.Titled or AbilityCardSelection.EnemiesWithTrait => false,
        _ => throw new InvalidOperationException("Unknown compiled selector in player-binding analysis"),
    };
}
