using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using static Marvel.Cards.Run.AbilityCardQueries;

namespace Marvel.Cards.Run;

internal sealed class CardQueryEvaluator(
    AbilityQueryContext cast, AbilityProgram? program)
{
    private static readonly HashSet<AbilityCardQuery> ScenarioQueries =
    [
        AbilityCardQuery.AttachedToThis, AbilityCardQuery.SideSchemes,
        AbilityCardQuery.Schemes, AbilityCardQuery.PowerTargets,
        AbilityCardQuery.YourAsidePile,
        AbilityCardQuery.Villain, AbilityCardQuery.MainScheme,
        AbilityCardQuery.YourAsideMinion, AbilityCardQuery.YourAsideSideScheme,
    ];
    private static readonly HashSet<AbilityCardQuery> EnemyQueries =
    [
        AbilityCardQuery.MinionsEngagedWithYou, AbilityCardQuery.Minions,
        AbilityCardQuery.Enemies, AbilityCardQuery.AttackableEnemies,
        AbilityCardQuery.AttackableMinions, AbilityCardQuery.ThwartableSchemes,
        AbilityCardQuery.EnemiesEngagedWithChosenPlayer,
    ];
    private static readonly HashSet<AbilityCardQuery> ControlledQueries =
    [
        AbilityCardQuery.UpgradesAndSupportsYouControl,
        AbilityCardQuery.IdentitySpecificInYourHand,
        AbilityCardQuery.SupportsYouControl,
        AbilityCardQuery.CharactersYouControl,
        AbilityCardQuery.UpgradesYouControl,
        AbilityCardQuery.AlliesYouControl,
    ];
    private static readonly HashSet<AbilityCardQuery> IdentityQueries =
    [
        AbilityCardQuery.IdentitiesWithinPerPlayerLimit,
        AbilityCardQuery.HeroesAndAllies, AbilityCardQuery.HeroAndAllyTargets, AbilityCardQuery.Allies,
        AbilityCardQuery.Heroes, AbilityCardQuery.Identities,
        AbilityCardQuery.Characters,
    ];

    internal IReadOnlyList<Card> Evaluate(AbilityCardQuery query)
    {
        if (ScenarioQueries.Contains(query)) return EvaluateScenario(query);
        if (EnemyQueries.Contains(query)) return EvaluateEnemies(query);
        if (ControlledQueries.Contains(query)) return EvaluateControlled(query);
        if (IdentityQueries.Contains(query)) return EvaluateIdentities(query);
        throw new InvalidOperationException("Unknown compiled card query");
    }

    private IReadOnlyList<Card> EvaluateScenario(AbilityCardQuery query) => query switch
    {
        // rr:attachment: an attachment is in an area hosted by its card.
        AbilityCardQuery.AttachedToThis =>
            [.. cast.World.Areas.Where(area => area.Host == cast.Source.ObjectId)
                .SelectMany(area => area.Cards)],
        // rr:player-side-scheme.1 puts both kinds of side scheme together.
        AbilityCardQuery.SideSchemes =>
            [.. InArea(cast.World, DeckType.SideSchemesArea, PlayArea.Villains)],
        AbilityCardQuery.Schemes =>
            [.. InArea(cast.World, DeckType.MainSchemesArea, PlayArea.Villains),
             .. InArea(cast.World, DeckType.SideSchemesArea, PlayArea.Villains)],
        AbilityCardQuery.PowerTargets => cast.PowerTargets,
        AbilityCardQuery.YourAsidePile => [.. cast.World.Seats[cast.Player].Nemesis.Cards],
        AbilityCardQuery.Villain => SingleIn(DeckType.VillainArea),
        AbilityCardQuery.MainScheme => SingleIn(DeckType.MainSchemesArea),
        AbilityCardQuery.YourAsideMinion => AsideOfKind(CardKind.Minion),
        AbilityCardQuery.YourAsideSideScheme => AsideOfKind(CardKind.EncounterSideScheme),
        _ => throw new InvalidOperationException("Unknown scenario card query"),
    };

    private IReadOnlyList<Card> EvaluateEnemies(AbilityCardQuery query) => query switch
    {
        // rr:engage.1: engagement is the player's engaged-enemy area.
        AbilityCardQuery.MinionsEngagedWithYou =>
            [.. InArea(cast.World, DeckType.EngagedEnemiesArea, PlayArea.Of(cast.Player))],
        // rr:minion.3: every in-play minion is engaged with a player.
        AbilityCardQuery.Minions =>
            [.. cast.World.Areas.Where(area => area.Type == DeckType.EngagedEnemiesArea)
                .SelectMany(area => area.Cards)
                .Where(card => EffectiveCards.Kind(card, cast.World.Facts)
                    == CardKind.Minion)],
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
        IEnumerable<Card> candidates = BasicPowers.Attackable(
            cast.World, cast.World.Facts, Resolver(cast));
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
        AbilityCardQuery.UpgradesAndSupportsYouControl =>
            [.. ControlledAreas(DeckType.UpgradesArea, DeckType.SupportsArea)],
        // rr:identity-specific-card.3: the identity icon is extracted as Hero class.
        AbilityCardQuery.IdentitySpecificInYourHand =>
            [.. cast.World.Seats[cast.Player].Hand.Cards.Where(IsIdentitySpecific)],
        AbilityCardQuery.SupportsYouControl =>
            [.. InArea(cast.World, DeckType.SupportsArea, PlayArea.Of(cast.Player))],
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
        cast.World.Facts.Attributes(card.FaceId)
            .GetValueOrDefault("Class", string.Empty).Split(';')
            .Contains("Hero", StringComparer.Ordinal);

    private IReadOnlyList<Card> EvaluateIdentities(AbilityCardQuery query) => query switch
    {
        AbilityCardQuery.IdentitiesWithinPerPlayerLimit => IdentitiesWithinLimit(),
        // rr:indirect-damage.2 and rr:you-your.3 include alter-egos with HP.
        AbilityCardQuery.HeroesAndAllies =>
            [.. Identities(),
             .. cast.World.Areas.Where(area => area.Type == DeckType.AlliesArea)
                .SelectMany(area => area.Cards)],
        // rr:form-change-form.5 excludes alter-egos when printed text names heroes.
        AbilityCardQuery.HeroAndAllyTargets =>
            [.. EvaluateIdentities(AbilityCardQuery.Heroes),
             .. EvaluateIdentities(AbilityCardQuery.Allies)],
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
        _ => throw new InvalidOperationException("Unknown identity card query"),
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
