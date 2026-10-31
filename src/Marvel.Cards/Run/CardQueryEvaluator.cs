using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityCardQueries;

namespace Marvel.Cards.Run;

internal sealed class CardQueryEvaluator(
    AbilityQueryContext cast, AbilityProgram? program)
{
    private static readonly ImmutableArray<AbilityCardQuery> ScenarioQueries =
    [
        AbilityCardQuery.AttachmentsOnVillain, AbilityCardQuery.AttachedToThis, AbilityCardQuery.AttachmentsOnThis, AbilityCardQuery.UpgradesOnThis, AbilityCardQuery.BeneathThis, AbilityCardQuery.SideSchemes,
        AbilityCardQuery.Schemes, AbilityCardQuery.PowerTargets,
        AbilityCardQuery.YourAsidePile, AbilityCardQuery.OtherEnvironments,
        AbilityCardQuery.Villain, AbilityCardQuery.MainScheme,
        AbilityCardQuery.YourAsideMinion, AbilityCardQuery.YourAsideSideScheme,
    ];
    private static readonly ImmutableArray<AbilityCardQuery> EnemyQueries =
    [
        AbilityCardQuery.MinionsEngagedWithYou, AbilityCardQuery.Minions,
        AbilityCardQuery.Villains, AbilityCardQuery.Enemies, AbilityCardQuery.AttackableEnemies,
        AbilityCardQuery.AttackableMinions, AbilityCardQuery.ThwartableSchemes,
        AbilityCardQuery.EnemiesEngagedWithChosenPlayer,
        AbilityCardQuery.VillainAndMinionsEngagedWithYou,
    ];
    private static readonly ImmutableArray<AbilityCardQuery> ControlledQueries =
    [
        AbilityCardQuery.UpgradesOnYourHero, AbilityCardQuery.UpgradesAndSupportsYouControl, AbilityCardQuery.CardsYouControl,
        AbilityCardQuery.AlliesAndSupportsYouControl,
        AbilityCardQuery.IdentitySpecificInYourHand,
        AbilityCardQuery.SupportsYouControl,
        AbilityCardQuery.CharactersYouControl, AbilityCardQuery.CharactersYouControlExceptCost,
        AbilityCardQuery.UpgradesYouControl,
        AbilityCardQuery.AlliesYouControl,
    ];
    private static readonly ImmutableArray<AbilityCardQuery> BoardQueries =
    [
        AbilityCardQuery.IdentitiesWithinPerPlayerLimit,
        AbilityCardQuery.HeroesAndAllies, AbilityCardQuery.HeroAndAllyTargets, AbilityCardQuery.Allies,
        AbilityCardQuery.Heroes, AbilityCardQuery.Identities,
        AbilityCardQuery.Characters, AbilityCardQuery.Supports,
    ];

    internal IReadOnlyList<Card> Evaluate(AbilityCardQuery query)
    {
        if (query == AbilityCardQuery.StatusCards)
            return StatusReplacement.Candidates(cast.World);
        if (query is AbilityCardQuery.InvocationTop or AbilityCardQuery.CardsInPlay)
            return EvaluatePublicCards(query);
        if (EvaluateEncounterRelations(query) is { } related)
            return related;
        if (ScenarioQueries.Contains(query)) return EvaluateScenario(query);
        if (EnemyQueries.Contains(query)) return EvaluateEnemies(query);
        if (ControlledQueries.Contains(query)) return EvaluateControlled(query);
        if (BoardQueries.Contains(query)) return EvaluateBoard(query);
        throw new InvalidOperationException("Unknown compiled card query");
    }

    private IReadOnlyList<Card>? EvaluateEncounterRelations(AbilityCardQuery query) => query switch
    {
        AbilityCardQuery.OtherVillainSideSchemes => AbilityOtherVillainSchemeQuery.Live(cast),
        AbilityCardQuery.VillainOfAttachedScheme => AbilityAttachedSchemeVillainQuery.Live(cast),
        AbilityCardQuery.ActiveVillainSideScheme => AbilityActiveSchemeQuery.Live(cast.World),
        AbilityCardQuery.VillainInThisGameArea => AbilityGameAreaQuery.Villain(cast),
        AbilityCardQuery.ObligationsInThisGameArea => AbilityGameAreaQuery.Obligations(cast),
        _ when AbilityEncounterSchemeQueries.IsRanked(query) =>
            EncounterSchemeRelations.AtThreatExtreme(cast.World,
                query == AbilityCardQuery.VillainsWithMostSchemeThreat),
        _ => null,
    };

    private IReadOnlyList<Card> EvaluatePublicCards(AbilityCardQuery query) =>
        query == AbilityCardQuery.InvocationTop
            ? AbilityInvocationTopQuery.Live(cast.World) : AbilityInPlayCardQuery.Live(cast);

    private IReadOnlyList<Card> EvaluateScenario(AbilityCardQuery query) => query switch
    {
        // rr:attachment: an attachment is in an area hosted by its card.
        AbilityCardQuery.AttachedToThis =>
            [.. cast.World.Areas.Where(area => area.Host == cast.Source.ObjectId && !BeneathCards.IsStored(area.Type))
                .SelectMany(area => area.Cards)],
        // Engine choice: stable physical object id order for this selector.
        AbilityCardQuery.UpgradesOnThis => cast.SourceReference is { } source
            ? [.. cast.World.Areas.Where(area => area.Host == source.ObjectId && DeckTypes.IsInPlay(area.Type))
                .SelectMany(area => area.Cards).Where(card => EffectiveCards.Kind(card, cast.World.Facts) == CardKind.Upgrade)
                .OrderBy(card => card.ObjectId)]
            : [],
        AbilityCardQuery.AttachmentsOnVillain => VillainAttachments(),
        AbilityCardQuery.AttachmentsOnThis =>
            [.. cast.World.Areas.Where(area => area.Host == cast.Source.ObjectId && DeckTypes.IsInPlay(area.Type))
                .SelectMany(area => area.Cards).Where(card => EffectiveCards.Kind(card, cast.World.Facts) == CardKind.Attachment)],
        AbilityCardQuery.OtherEnvironments => [.. cast.World.Cards.Where(card =>
            DeckTypes.IsInPlay(card.Area.Type) && card.ObjectId != cast.Occurrence.Subject
            && EffectiveCards.Kind(card, cast.World.Facts) == CardKind.Environment)],
        // rr:leaves-play.1: a departed card is "a new copy of the card".
        // A continuing ability cannot follow storage beneath its source's new incarnation.
        AbilityCardQuery.BeneathThis => cast.SourceReference is { } host
            ? BeneathCards.Of(cast.World, host) : [],
        // rr:player-side-scheme.1 puts both kinds of side scheme together.
        AbilityCardQuery.SideSchemes =>
            [.. InArea(cast.World, DeckType.SideSchemesArea, PlayArea.Villains)],
        AbilityCardQuery.Schemes =>
            [.. InArea(cast.World, DeckType.MainSchemesArea, PlayArea.Villains),
             .. InArea(cast.World, DeckType.SideSchemesArea, PlayArea.Villains)],
        AbilityCardQuery.PowerTargets => cast.PowerTargets,
        AbilityCardQuery.YourAsidePile => [.. cast.World.Seats[cast.Player].Nemesis.Cards],
        // pack:mc03:new-rules.the-active-villain: a card reference to "the
        // villain" names the active villain; selecting an enemy remains plural.
        AbilityCardQuery.Villain => EncounterRouting.Villain(cast.World) is { } villain ? [villain] : [],
        AbilityCardQuery.MainScheme => SingleIn(DeckType.MainSchemesArea),
        AbilityCardQuery.YourAsideMinion => AsideOfKind(CardKind.Minion),
        AbilityCardQuery.YourAsideSideScheme => AsideOfKind(CardKind.EncounterSideScheme),
        _ => throw new InvalidOperationException("Unknown scenario card query"),
    };

    private Card[] VillainAttachments()
    {
        var villain = EncounterRouting.Villain(cast.World);
        if (villain is null) return [];
        return [.. cast.World.Areas.Where(area => area.Host == villain.ObjectId && DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .Where(card => EffectiveCards.Kind(card, cast.World.Facts) == CardKind.Attachment)];
    }

    private IReadOnlyList<Card> EvaluateEnemies(AbilityCardQuery query) => query switch
    {
        // Engine choice: villain first, followed by the engaged area's physical order.
        AbilityCardQuery.VillainAndMinionsEngagedWithYou =>
            [.. Evaluate(AbilityCardQuery.Villain),
                .. Evaluate(AbilityCardQuery.MinionsEngagedWithYou)
                    .Where(card => EffectiveCards.Kind(card, cast.World.Facts) == CardKind.Minion)],
        // rr:engage.1: engagement is the player's engaged-enemy area.
        AbilityCardQuery.MinionsEngagedWithYou =>
            [.. InArea(cast.World, DeckType.EngagedEnemiesArea, PlayArea.Of(cast.Player))],
        // rr:minion.3: every in-play minion is engaged with a player.
        AbilityCardQuery.Minions =>
            [.. cast.World.Areas.Where(area => area.Type == DeckType.EngagedEnemiesArea)
                .SelectMany(area => area.Cards)
                .Where(card => EffectiveCards.Kind(card, cast.World.Facts)
                    == CardKind.Minion)],
        AbilityCardQuery.Villains => [.. InArea(cast.World, DeckType.VillainArea, PlayArea.Villains)],
        AbilityCardQuery.Enemies =>
            [.. cast.World.Areas.Where(area => area.Type is DeckType.VillainArea
                    or DeckType.EngagedEnemiesArea)
                .SelectMany(area => area.Cards)
                .Where(card => CardKinds.IsEnemy(
                    EffectiveCards.Kind(card, cast.World.Facts)))],
        AbilityCardQuery.AttackableEnemies => Attackable(minionsOnly: false),
        AbilityCardQuery.AttackableMinions => Attackable(minionsOnly: true),
        AbilityCardQuery.ThwartableSchemes =>
            BasicPowers.Thwartable(cast.World, cast.World.Facts, Resolver(cast)),
        AbilityCardQuery.EnemiesEngagedWithChosenPlayer =>
            [.. InArea(cast.World, DeckType.EngagedEnemiesArea,
                PlayArea.Of(ChosenPlayer(cast).Owner))],
        _ => throw new InvalidOperationException("Unknown enemy card query"),
    };

    private IReadOnlyList<Card> Attackable(bool minionsOnly)
    {
        IEnumerable<Card> candidates = AbilityAttackTargets.For(
            cast.World, Resolver(cast), cast.Source);
        if (minionsOnly)
            candidates = candidates.Where(enemy =>
                EffectiveCards.Kind(enemy, cast.World.Facts) == CardKind.Minion);
        string missingProgram = minionsOnly
            ? "Attackable-minion queries require the authored ability program"
            : "Attackable-enemy queries require the authored ability program";
        return [.. candidates.Where(enemy => AbilityProgramQueries.CanTakeDamage(
            cast.World,
            program ?? throw new InvalidOperationException(missingProgram),
            enemy,
            cast.Source))];
    }

    private IReadOnlyList<Card> EvaluateControlled(AbilityCardQuery query) => query switch
    {
        // rr:play-area.1: cards in a player's play area are under their control.
        AbilityCardQuery.UpgradesOnYourHero => AbilityCostSelection.Choices(cast.World, cast.Player, query),
        AbilityCardQuery.CardsYouControl => [.. cast.World.Cards.Where(card =>
            DeckTypes.IsInPlay(card.Area.Type) && CardControl.ControllerOf(cast.World, card) == cast.Player)],
        AbilityCardQuery.UpgradesAndSupportsYouControl =>
            [.. ControlledAreas(DeckType.UpgradesArea, DeckType.SupportsArea)],
        AbilityCardQuery.AlliesAndSupportsYouControl =>
            [.. ControlledAreas(DeckType.AlliesArea, DeckType.SupportsArea)],
        // rr:identity-specific-card.3: the identity icon is extracted as Hero class.
        AbilityCardQuery.IdentitySpecificInYourHand =>
            [.. cast.World.Seats[cast.Player].Hand.Cards.Where(IsIdentitySpecific)],
        AbilityCardQuery.SupportsYouControl =>
            [.. InArea(cast.World, DeckType.SupportsArea, PlayArea.Of(cast.Player))],
        AbilityCardQuery.CharactersYouControlExceptCost => [.. EvaluateControlled(AbilityCardQuery.CharactersYouControl)
            .Where(card => !cast.ExhaustedForCost.Contains(card.ObjectId))],
        AbilityCardQuery.CharactersYouControl =>
            [cast.World.Seats[cast.Player].IdentityCard,
             .. InArea(cast.World, DeckType.AlliesArea, PlayArea.Of(cast.Player))],
        AbilityCardQuery.UpgradesYouControl =>
            [.. ControlledAreas(DeckType.UpgradesArea)],
        AbilityCardQuery.AlliesYouControl =>
            [.. InArea(cast.World, DeckType.AlliesArea, PlayArea.Of(cast.Player))],
        _ => throw new InvalidOperationException("Unknown controlled-card query"),
    };

    private IEnumerable<Card> ControlledAreas(params DeckType[] types) =>
        cast.World.Areas.Where(area => types.Contains(area.Type)
                && area.PlayArea == PlayArea.Of(cast.Player))
            .SelectMany(area => area.Cards);

    private bool IsIdentitySpecific(Card card) =>
        CardClassification.Is(cast.World.Facts, card.FaceId, CardClassification.HeroSpecific);

    private IReadOnlyList<Card> EvaluateBoard(AbilityCardQuery query) => query switch
    {
        AbilityCardQuery.Supports =>
            [.. cast.World.Cards.Where(card => DeckTypes.IsInPlay(card.Area.Type)
                && EffectiveCards.Kind(card, cast.World.Facts) == CardKind.Support)],
        AbilityCardQuery.IdentitiesWithinPerPlayerLimit => IdentitiesWithinLimit(),
        // rr:indirect-damage.2 and rr:you-your.3 include alter-egos with HP.
        AbilityCardQuery.HeroesAndAllies =>
            [.. Identities(),
             .. cast.World.Areas.Where(area => area.Type == DeckType.AlliesArea)
                .SelectMany(area => area.Cards)],
        // rr:form-change-form.5 excludes alter-egos when printed text names heroes.
        AbilityCardQuery.HeroAndAllyTargets =>
            [.. EvaluateBoard(AbilityCardQuery.Heroes),
             .. EvaluateBoard(AbilityCardQuery.Allies)],
        // rr:friendly and rr:upgrade.3.1 reach allies controlled by any player.
        AbilityCardQuery.Allies =>
            [.. cast.World.Areas.Where(area => area.Type == DeckType.AlliesArea)
                .SelectMany(area => area.Cards)],
        // rr:form-change-form.5: alter-ego identities are not heroes.
        AbilityCardQuery.Heroes =>
            [.. cast.World.PlayerOrder.Select(player => cast.World.Seats[player])
                .Where(seat => Forms.In(cast.World, seat, cast.World.Facts, Forms.Hero))
                .Select(seat => seat.IdentityCard)],
        AbilityCardQuery.Identities => [.. Identities()],
        AbilityCardQuery.Characters =>
            [.. Identities(),
             .. cast.World.Areas.Where(area => area.Type is DeckType.AlliesArea
                    or DeckType.VillainArea or DeckType.EngagedEnemiesArea)
                .SelectMany(area => area.Cards)],
        _ => throw new InvalidOperationException("Unknown board card query"),
    };

    private IEnumerable<Card> Identities() => cast.World.PlayerOrder
        .Select(player => cast.World.Seats[player].IdentityCard);

    private IReadOnlyList<Card> IdentitiesWithinLimit()
    {
        long maximum = cast.World.Facts.PrintedValue(
            cast.Source.FaceId, "MaxPerUnit", cast.World.Players);
        string title = cast.World.Facts.Title(cast.Source.FaceId);
        return [.. cast.World.PlayerOrder
            .Where(player => maximum <= 0 || CopiesInPlay(player, title) < maximum)
            .Select(player => cast.World.Seats[player].IdentityCard)];
    }

    private int CopiesInPlay(int player, string title) => cast.World.Areas
        .Where(area => area.PlayArea == PlayArea.Of(player))
        .SelectMany(area => area.Cards)
        .Count(card => DeckTypes.IsInPlay(card.Area.Type)
            && string.Equals(
                EffectiveCards.Title(card, cast.World.Facts), title, StringComparison.Ordinal));

    private IReadOnlyList<Card> SingleIn(DeckType area) =>
        cast.World.TheCardIn(area) is { } card ? [card] : [];

    private IReadOnlyList<Card> AsideOfKind(CardKind kind) =>
        Aside(cast, kind) is { } card ? [card] : [];
}
