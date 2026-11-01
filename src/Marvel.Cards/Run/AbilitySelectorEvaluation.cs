using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;

namespace Marvel.Cards.Run;

// One evaluation owns its observations. Nested selections share this local
// collector; separate evaluations cannot change each other's bindings or output.
internal sealed class AbilitySelectorEvaluation(
    AbilityQueryContext context, AbilitySingularAreaAdmission? admitSingularArea = null,
    AbilityProgram? program = null)
{
    private readonly List<InformationKind> information = [];

    internal AbilityQueryResult<T> Result<T>(T value) => new(value, [.. information]);

    internal Card? Find(AbilityCardSelection selector)
    {
        if (selector is AbilityCardSelection.FirstAvailable fallback) return FindFirstAvailable(fallback);
        if (selector is AbilityCardSelection.Bound bound) return AbilityCardQueries.Named(bound.Binding, context);
        if (selector is AbilityCardSelection.Titled titled) return FindTitle(titled);
        if (selector is AbilityCardSelection.Query
            {
                Kind:
            AbilityCardQuery.Villain or AbilityCardQuery.MainScheme or AbilityCardQuery.YourAsideMinion
            or AbilityCardQuery.InvocationTop or AbilityCardQuery.YourAsideSideScheme or AbilityCardQuery.ActiveVillainSideScheme or AbilityCardQuery.VillainOfAttachedScheme
            or AbilityCardQuery.VillainInThisGameArea
            } query)
            return FindQuery(query);
        if (selector is AbilityCardSelection.Last last)
        {
            if (admitSingularArea is not null && !admitSingularArea(AbilitySelectionAreaReads.For(last)))
                return null;
            return LastCard(Every(last.Cards));
        }
        if (selector is AbilityCardSelection.InAreas areas) return FindInAreas(areas);
        if (selector is AbilityCardSelection.DefeatedAllyInYourDiscardPile)
            return AbilityDefeatedAllySelection.Find(context);
        throw new RulesNotImplementedException(
            $"'{context.Source.FaceId}' uses a selector whose single-card resolution is not implemented");
    }

    private Card? FindQuery(AbilityCardSelection.Query query)
    {
        if (query.Kind == AbilityCardQuery.InvocationTop && admitSingularArea is not null
            && !admitSingularArea(AbilitySelectionAreaReads.For(query))) return null;
        return Every(query).SingleOrDefault();
    }

    private Card? FindInAreas(AbilityCardSelection.InAreas areas)
    {
        if (admitSingularArea is not null && !admitSingularArea(AbilitySelectionAreaReads.For(areas)))
            return null;
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

    private Card? FindTitle(AbilityCardSelection.Titled titled)
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

    private Card? FindFirstAvailable(AbilityCardSelection.FirstAvailable fallback)
    {
        foreach (var option in fallback.Options)
            if (Every(option).Count > 0) return Find(option);
        return null;
    }

    internal IReadOnlyList<Card> Every(AbilityCardSelection selector) => selector switch
    {
        AbilityCardSelection.FirstAvailable fallback => fallback.Options.Select(Every).FirstOrDefault(cards => cards.Count > 0) ?? [],
        AbilityCardSelection.Bound bound => AbilityCardQueries.Named(bound.Binding, context) is { } card ? [card] : [],
        AbilityCardSelection.StatusCardsOn statuses => StatusCardsOn(statuses),
        AbilityCardSelection.Query query => AbilityCardQueries.Cards(query.Kind, context, program),
        AbilityCardSelection.DefeatedAllyInYourDiscardPile => AbilityDefeatedAllySelection.Every(context),
        AbilityCardSelection.Titled titled => AbilityCardQueries.ReferencedByTitle(titled.Title, context),
        AbilityCardSelection.EnemiesWithTrait trait => [.. context.World.Areas
            .Where(area => area.Type is DeckType.VillainArea or DeckType.EngagedEnemiesArea)
            .SelectMany(area => area.Cards)
            .Where(card => Rules.State.Traits.Of(context.World, card, context.World.Facts).Contains(trait.Trait, StringComparer.Ordinal))],
        AbilityCardSelection.WithTrait or AbilityCardSelection.WithStatus => FilterEvery(selector),
        AbilityCardSelection.InObjectIdOrder ordered => [.. Every(ordered.Cards).OrderBy(card => card.ObjectId)],
        AbilityCardSelection.DefeatedWithProfile defeated => [.. context.Occurrence.Defeats
            .Where(fact => fact.ProfileId == defeated.Profile && (!defeated.RequiresFaceDown || fact.WasFaceDown))
            .Select(fact => (Fact: fact, Card: context.World.Cards[fact.Card]))
            .Where(pair => pair.Card.Incarnation == pair.Fact.Incarnation
                && !DeckTypes.IsInPlay(pair.Card.Area.Type) && pair.Card.Area.Type != DeckType.RemovedArea)
            .Select(pair => pair.Card)],
        AbilityCardSelection.FaceDown filtered => [.. Every(filtered.Cards).Where(card => !card.FaceUp)],
        AbilityCardSelection.Last last => LastCard(Every(last.Cards)) is { } lastCard ? [lastCard] : [],
        AbilityCardSelection.InPlayerArea area => PlayerAreaCards(area.Area, Player(area.Player)),
        AbilityCardSelection.WithMatchingPlayerArea filtered => MatchingPlayerArea(filtered),
        AbilityCardSelection.WithoutAnotherCopyAttached unoccupied => [.. Every(unoccupied.Cards)
            .Where(candidate => !context.World.Areas.Where(area => area.Host == candidate.ObjectId)
                .SelectMany(area => area.Cards)
                .Any(attached => attached.ObjectId != context.Source.ObjectId
                    && string.Equals(context.World.Facts.Title(attached.FaceId), context.World.Facts.Title(context.Source.FaceId), StringComparison.Ordinal)))],
        AbilityCardSelection.Discardable discardable => [.. Every(discardable.Cards).Where(card => Rules.Play.Discard.CanDiscardFromPlay(card, context.World.Facts) && CanRemove(discardable.Cards, card))],
        AbilityCardSelection.Ranked ranked => Ranked(ranked),
        AbilityCardSelection.InAreas areas => CardsIn(areas),
        _ => throw new InvalidOperationException("Unknown compiled card selection"),
    };

    private IReadOnlyList<Card> StatusCardsOn(AbilityCardSelection.StatusCardsOn selection)
    {
        var host = AbilityCardQueries.Named(selection.Host, context);
        if (host is null || !DeckTypes.IsInPlay(host.Area.Type)
            || !CardKinds.IsCharacter(EffectiveCards.Kind(host, context.World.Facts))) return [];
        // rr:status-cards: a physical status is placed "on that character".
        // Stable object-id ordering is an engine choice, never an automatic answer.
        return [.. context.World.Areas.Where(area => area.Type == DeckType.StatusArea
                && area.Host == host.ObjectId).SelectMany(area => area.Cards)
            .Where(card => selection.Statuses.IsEmpty || selection.Statuses.Contains(card.FaceId))
            .OrderBy(card => card.ObjectId)];
    }

    private IReadOnlyList<Card> FilterEvery(AbilityCardSelection selector) => selector switch
    {
        AbilityCardSelection.WithTrait trait => [.. Every(trait.Cards)
            .Where(card => Rules.State.Traits.Has(context.World, card, trait.Trait, context.World.Facts) != trait.Exclude)],
        AbilityCardSelection.WithStatus status => [.. Every(status.Cards)
            .Where(card => DeckTypes.IsInPlay(card.Area.Type)
                && Statuses.Afflicted(context.World, context.World.Facts, card, status.Status))],
        _ => throw new InvalidOperationException("Unknown compiled card filter"),
    };

    private static Card? LastCard(IReadOnlyList<Card> cards) => cards.Count == 0 ? null : cards[^1];

    private int Player(AbilityPlayer player) => player switch
    {
        AbilityPlayer.You => AbilityCardQueries.Resolver(context),
        AbilityPlayer.ChosenPlayer => AbilityCardQueries.ChosenPlayer(context).Owner,
        AbilityPlayer.Controller => AbilityCardQueries.ControllerOf(context.World, context.Source),
        AbilityPlayer.FirstPlayer => context.World.FirstPlayer,
        AbilityPlayer.Defeater => AbilityCardQueries.DefeatingPlayer(context),
        AbilityPlayer.TriggerPlayer => context.Occurrence.Player,
        AbilityPlayer.EngagedPlayer => context.Source.Area.PlayArea.Player,
        AbilityPlayer.ActivatedOrEngagedPlayer => AbilityCardQueries.ActivatedAgainst(context.World, context.Source)
            ?? context.Source.Area.PlayArea.Player,
        _ => throw new InvalidOperationException("Unknown player relation"),
    };

    private IReadOnlyList<Card> PlayerAreaCards(DeckType type, int player) =>
        [.. context.World.Areas.Where(area => area.Type == type
                && area.PlayArea == PlayArea.Of(player))
            .SelectMany(area => area.Cards)];

    private IReadOnlyList<Card> MatchingPlayerArea(AbilityCardSelection.WithMatchingPlayerArea selection) =>
        [.. Every(selection.Cards).Where(identity =>
            context.World.Seats.FirstOrDefault(seat => seat.IdentityCard == identity) is { } seat
            && PlayerAreaCards(selection.Area, seat.Index).Any(card =>
                (selection.Kind is null || EffectiveCards.Kind(card, context.World.Facts) == selection.Kind)
                && (selection.Trait is null || Rules.State.Traits.Has(
                    context.World, card, selection.Trait, context.World.Facts))))];

    private IReadOnlyList<Card> CardsIn(AbilityCardSelection.InAreas selection)
    {
        // rr:search.2: "cards being searched are not considered to leave the
        // searched area." Exposure is an output, never a write to the board.
        if (selection.Areas.Any(area => area is AbilitySearchArea.YourDeck or AbilitySearchArea.EncounterDeck))
            information.Add(InformationKind.Search);
        return [.. selection.Areas.SelectMany(area => SearchRange(area, selection.Top))
            .Where(card => selection.Set is null || string.Equals(selection.Set,
                context.World.Facts.EncounterSet(card.FaceId), StringComparison.Ordinal))
            .Where(card => !selection.AspectOnly || IsAspect(card))
            .Where(card => selection.CardClass is null || string.Equals(selection.CardClass,
                context.World.Facts.Attributes(card.FaceId).GetValueOrDefault("Class"), StringComparison.Ordinal))
            .Where(card => selection.Kind is null || EffectiveCards.Kind(card, context.World.Facts) == selection.Kind)
            .Where(card => selection.Trait is null || Rules.State.Traits.Has(context.World, card, selection.Trait, context.World.Facts))
            .Where(card => selection.Title is null || string.Equals(context.World.Facts.Title(card.FaceId), selection.Title, StringComparison.Ordinal))
            .Where(card => !selection.DifferentTitleFromRevealed || !RevealedTitle(card))];
    }

    private bool RevealedTitle(Card card) => (context.RevealedThisResolution
        ?? throw new RulesNotImplementedException(
            "differentTitleFromRevealed outside an ability invocation has no reveals to compare"))
        .Any(revealed =>
        string.Equals(context.World.Facts.Title(context.World.Cards[revealed].FaceId),
            context.World.Facts.Title(card.FaceId), StringComparison.Ordinal));

    private IEnumerable<Card> SearchRange(AbilitySearchArea area, int? top) => top is { } count
        ? AreaCards(area).Reverse().Take(count) : AreaCards(area);

    private bool IsAspect(Card card) => context.World.Facts.Attributes(card.FaceId).GetValueOrDefault("Class")
        is "Aggression" or "Justice" or "Leadership" or "Protection";

    private IReadOnlyList<Card> AreaCards(AbilitySearchArea area)
    {
        if (area == AbilitySearchArea.YourDeck) return context.World.Seats[context.Player].Deck.Cards;
        if (area == AbilitySearchArea.YourHand) return context.World.Seats[context.Player].Hand.Cards;
        if (area == AbilitySearchArea.YourPlayArea)
            return InPlay(playArea => playArea == PlayArea.Of(context.Player));
        if (area == AbilitySearchArea.PlayerPlayAreas) return InPlay(playArea => playArea.IsPlayers);
        var playArea = area == AbilitySearchArea.YourDiscardPile ? PlayArea.Of(context.Player) : PlayArea.Villains;
        return context.World.Areas.FirstOrDefault(candidate => candidate.Type == AreaType(area)
            && candidate.PlayArea == playArea && candidate.Host == -1)?.Cards ?? [];
    }

    private Card[] InPlay(Func<PlayArea, bool> playArea) =>
        [.. context.World.Areas.Where(candidate => DeckTypes.IsInPlay(candidate.Type)
            && playArea(candidate.PlayArea)).SelectMany(candidate => candidate.Cards)];

    internal static DeckType AreaType(AbilitySearchArea area) => area switch
    {
        AbilitySearchArea.EncounterDeck => DeckType.EncounterDeck,
        AbilitySearchArea.EncounterDiscardPile => DeckType.EncounterDiscardPile,
        AbilitySearchArea.ScenarioSetAside => DeckType.AsideDeck,
        AbilitySearchArea.YourDeck => DeckType.PlayerDeck,
        AbilitySearchArea.YourDiscardPile => DeckType.DiscardPile,
        AbilitySearchArea.YourHand => DeckType.HandsArea,
        _ => throw new InvalidOperationException("Unknown compiled search area"),
    };

    private IReadOnlyList<Card> Ranked(AbilityCardSelection.Ranked selection)
    {
        var among = Every(selection.Cards).ToList();
        if (among.Count == 0) return [];
        long Rank(Card card) => selection.By switch
        {
            AbilityCardRank.Threat => card.Tokens.GetValueOrDefault("k_threat"),
            AbilityCardRank.Cost => context.World.Facts.PrintedValue(card.FaceId, "Cost", context.World.Players),
            AbilityCardRank.Attack => StateFields.Modified(context.World, card, "attack", context.World.Facts, context.World.Players),
            AbilityCardRank.PrintedHealth => EffectiveCards.BaseValue(card, context.World.Facts, "HP", context.World.Players),
            AbilityCardRank.RemainingHealth => Math.Max(0, DamagePlacement.Health(context.World, context.World.Facts, card) - context.Quantities(card).Damage),
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
        AbilityCardSelection.Query { Kind: AbilityCardQuery.InvocationTop } => Every(selector).Contains(target),
        AbilityCardSelection.InAreas areas => CardsIn(areas).Any(card => card.ObjectId == target.ObjectId),
        AbilityCardSelection.WithTrait trait => ExplicitlySelectsOutOfPlayCard(trait.Cards, target),
        AbilityCardSelection.WithStatus => false,
        AbilityCardSelection.InObjectIdOrder ordered => ExplicitlySelectsOutOfPlayCard(ordered.Cards, target),
        AbilityCardSelection.DefeatedWithProfile defeated => Every(defeated).Contains(target),
        AbilityCardSelection.DefeatedAllyInYourDiscardPile => Every(selector).Contains(target),
        AbilityCardSelection.FaceDown filtered => ExplicitlySelectsOutOfPlayCard(filtered.Cards, target),
        AbilityCardSelection.FirstAvailable fallback => fallback.Options.Any(option => ExplicitlySelectsOutOfPlayCard(option, target)),
        AbilityCardSelection.Last last => ExplicitlySelectsOutOfPlayCard(last.Cards, target),
        AbilityCardSelection.InPlayerArea area => !DeckTypes.IsInPlay(area.Area)
            && Every(area).Any(card => card == target),
        AbilityCardSelection.WithoutAnotherCopyAttached unoccupied => ExplicitlySelectsOutOfPlayCard(unoccupied.Cards, target),
        AbilityCardSelection.Discardable discardable => ExplicitlySelectsOutOfPlayCard(discardable.Cards, target),
        AbilityCardSelection.Ranked ranked => ExplicitlySelectsOutOfPlayCard(ranked.Cards, target),
        _ => false,
    };
}
