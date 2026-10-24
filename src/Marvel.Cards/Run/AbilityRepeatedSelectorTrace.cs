using static Marvel.Cards.Run.AbilityRepeatedCharacteristicTrace;
using static Marvel.Cards.Run.AbilitySelectorTrace;
using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityChoiceAnalysis;
using static Marvel.Cards.Run.AbilityPowerProjection;
using static Marvel.Cards.Run.AbilityPowerTrace;
using static Marvel.Cards.Run.AbilityInitiationPrimitives;
using static Marvel.Cards.Run.AbilityProjection;
using static Marvel.Cards.Run.AbilityRepeatedEffectAnalysis;
using static Marvel.Cards.Run.AbilityResolutionAdmission;
using static Marvel.Cards.Run.AbilityInitiation;
using static Marvel.Cards.Run.AbilityEffectStructure;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

using static Marvel.Cards.Run.AbilityRepeatedDamageTrace;
using static Marvel.Cards.Run.AbilityRepeatedDamageAnalysis;
using static Marvel.Cards.Run.AbilityRepeatedSelectorTrace;
using static Marvel.Cards.Run.AbilityRepeatedStatusTrace;
namespace Marvel.Cards.Run;

internal static class AbilityRepeatedSelectorTrace
{
    internal static List<TraceCard> TraceCards(
        AbilityCardSelection value, AbilityAdmissionScope cast)
    {
        bool dynamic = SelectorMembershipCanChange(value)
            || PotentialVillainSelector(value, cast);
        var selected = dynamic
            ? TraceCandidateCards(value, cast)
            : Every(value, cast).ToList();
        if (dynamic && cast.World.EncounterTopology is { } topology)
        {
            // A dynamic trace follows one living character per group, not one
            // candidate for every physical stage waiting in its villain deck.
            // Include every group because the active counter can move before
            // this selection is evaluated in a subsequent trace frame.
            selected = selected.Concat(topology.Groups.Select(group => group.CurrentVillain).OfType<Card>())
                .Select(card =>
            {
                var group = topology.Groups.SingleOrDefault(group => group.Stages.Contains(card));
                return group is null ? card : group.CurrentVillain;
            }).OfType<Card>().Distinct().ToList();
        }
        var current = cast.World.TheCardIn(DeckType.VillainArea);
        var traced = selected.Select(card => new TraceCard(
            card, dynamic ? value : VillainSelector(value, card, cast))).ToList();
        if (current is not null
            && selected.All(card => card.ObjectId != current.ObjectId)
            && dynamic)
        {
            traced.Insert(0, new TraceCard(current, value));
        }
        return traced;
    }

    internal static TraceCard? TraceCardNamed(
        AbilityCardSelection value, AbilityAdmissionScope cast)
    {
        if (Find(value, cast) is { } found)
        {
            return new TraceCard(found, VillainSelector(value, found, cast));
        }
        var current = cast.World.TheCardIn(DeckType.VillainArea);
        return current is not null && PotentialVillainSelector(value, cast)
            ? new TraceCard(current, value)
            : null;
    }

    internal static AbilityCardSelection? VillainSelector(
        AbilityCardSelection value, Card resolved, AbilityAdmissionScope cast)
    {
        var current = cast.World.TheCardIn(DeckType.VillainArea);
        if (current is null || resolved.ObjectId != current.ObjectId
            || value is AbilityCardSelection.Bound)
        {
            return null;
        }
        return SelectorCanTrackVillain(value, current, cast) ? value : null;
    }

    internal static bool SelectorCanTrackVillain(
        AbilityCardSelection selector, Card current, AbilityAdmissionScope cast) => selector switch
        {
            AbilityCardSelection.Query query => query.Kind is AbilityCardQuery.Villain
                or AbilityCardQuery.Villains or AbilityCardQuery.Enemies or AbilityCardQuery.AttackableEnemies or AbilityCardQuery.Characters
                || AbilityEncounterSchemeQueries.IsRanked(query.Kind),
            AbilityCardSelection.Titled titled => string.Equals(
                titled.Title, cast.World.Facts.Title(current.FaceId), StringComparison.Ordinal),
            AbilityCardSelection.EnemiesWithTrait trait => TraceHasTrait(current, trait.Trait, cast, []),
            AbilityCardSelection.WithTrait trait => TraceHasTrait(current, trait.Trait, cast, []) != trait.Exclude
                && SelectorCanTrackVillain(trait.Cards, current, cast),
            AbilityCardSelection.FirstAvailable fallback => fallback.Options.Any(option => SelectorCanTrackVillain(option, current, cast)),
            AbilityCardSelection.Last last => SelectorCanTrackVillain(last.Cards, current, cast),
            AbilityCardSelection.InObjectIdOrder ordered => SelectorCanTrackVillain(ordered.Cards, current, cast),
            AbilityCardSelection.FaceDown facedown => !current.FaceUp && SelectorCanTrackVillain(facedown.Cards, current, cast),
            AbilityCardSelection.WithoutAnotherCopyAttached other => SelectorCanTrackVillain(other.Cards, current, cast),
            AbilityCardSelection.Ranked ranked => SelectorCanTrackVillain(ranked.Cards, current, cast),
            _ => false,
        };

    internal static int? TraceSelectorIncludesCard(
        AbilityCardSelection value, int bound, AbilitySelectorTraceContext trace)
    {
        var world = trace.Cast.World;
        int boardVillain = Rules.Play.EncounterRouting.Villain(world)?.ObjectId ?? -1;
        int candidateId = trace.EncounterStages is { } stages
            ? AbilityPowerEncounterProjection.Current(world.Cards[bound], trace.Cast, stages)?.ObjectId ?? -1
            : bound == boardVillain ? trace.CurrentVillain : bound;
        if (candidateId < 0 || trace.Discarded.Contains(candidateId)) return null;
        return TraceSelectorMatches(value, world.Cards[candidateId], trace) ? candidateId : null;
    }

    internal static bool TraceQueryMatches(
        AbilityCardQuery query, Card candidate, AbilitySelectorTraceContext trace)
    {
        if (query == AbilityCardQuery.ActiveVillainSideScheme)
            return AbilityActiveSchemeQuery.InTrace(trace).Contains(candidate);
        if (AbilityEncounterSchemeQueries.IsRanked(query))
            return AbilityEncounterSchemeQueries.InTrace(query, trace).Contains(candidate);
        var cast = trace.Cast;
        var discarded = trace.Discarded;
        var modifiers = trace.Modifiers;
        var engagement = trace.Engagement;
        bool villain = trace.EncounterStages is { } stages
            ? stages.Values.Contains(candidate.ObjectId) : candidate.ObjectId == trace.CurrentVillain;
        var kind = EffectiveCards.Kind(candidate, cast.World.Facts);
        return query switch
        {
            AbilityCardQuery.Villain => candidate.ObjectId == trace.CurrentVillain,
            AbilityCardQuery.Villains => villain,
            AbilityCardQuery.Enemies => villain
                || kind == CardKind.Minion,
            AbilityCardQuery.Minions => kind == CardKind.Minion,
            AbilityCardQuery.Characters => IsCharacter(villain, kind),
            AbilityCardQuery.AttackableEnemies => IsAttackableEnemy(
                villain, kind, candidate, cast, discarded, modifiers, engagement),
            AbilityCardQuery.MinionsEngagedWithYou => IsEngagedMinion(
                kind, candidate, cast.Player, engagement),
            AbilityCardQuery.EnemiesEngagedWithChosenPlayer =>
                IsEngagedWithChosenPlayer(kind, candidate, cast, engagement),
            AbilityCardQuery.CardsYouControl => DeckTypes.IsInPlay(candidate.Area.Type)
                && CardControl.IsPlayerCard(cast.World.Facts, candidate)
                && TracePlayAreaPlayer(candidate, engagement) == cast.Player,
            AbilityCardQuery.UpgradesYouControl => IsControlledInArea(
                candidate, DeckType.UpgradesArea, cast.Player, engagement),
            AbilityCardQuery.SupportsYouControl => IsControlledInArea(
                candidate, DeckType.SupportsArea, cast.Player, engagement),
            AbilityCardQuery.UpgradesAndSupportsYouControl => IsControlledSupportOrUpgrade(
                candidate, cast.Player, engagement),
            _ => QueryCards(query, cast).Any(card =>
                card.ObjectId == candidate.ObjectId),
        };
    }

    private static bool IsCharacter(bool villain, CardKind kind) =>
        villain || kind is CardKind.Minion or CardKind.Hero or CardKind.AlterEgo or CardKind.Ally;

    private static bool IsAttackableEnemy(
        bool villain, CardKind kind, Card candidate, AbilityAdmissionScope cast,
        HashSet<int> discarded, Dictionary<(int Card, string Field), long> modifiers,
        Dictionary<int, int> engagement) =>
        villain
            ? VillainIsAttackableInTrace(cast, candidate, discarded, modifiers, engagement)
            : kind == CardKind.Minion && AbilityDamageProtectionTrace.CanTakeDamageInTrace(cast, candidate, discarded);

    private static bool IsEngagedMinion(
        CardKind kind, Card candidate, int player, Dictionary<int, int> engagement) =>
        kind == CardKind.Minion && TraceEngagedWith(candidate, player, engagement);

    private static bool IsEngagedWithChosenPlayer(
        CardKind kind, Card candidate, AbilityAdmissionScope cast,
        Dictionary<int, int> engagement) =>
        kind == CardKind.Minion
        && cast.Chosen is { Owner: >= 0 } chosen
        && TraceEngagedWith(candidate, chosen.Owner, engagement);

    private static bool IsControlledInArea(
        Card candidate, DeckType area, int player, Dictionary<int, int> engagement) =>
        candidate.Area.Type == area && TracePlayAreaPlayer(candidate, engagement) == player;

    private static bool IsControlledSupportOrUpgrade(
        Card candidate, int player, Dictionary<int, int> engagement) =>
        candidate.Area.Type is DeckType.UpgradesArea or DeckType.SupportsArea
        && TracePlayAreaPlayer(candidate, engagement) == player;

    internal static bool TraceEngagedWith(
        Card card, int player, Dictionary<int, int> engagement) =>
        engagement.TryGetValue(card.ObjectId, out int traced)
            ? traced == player
            : card.Area.Type == DeckType.EngagedEnemiesArea
                && card.Area.PlayArea == PlayArea.Of(player);

    internal static int TracePlayAreaPlayer(
        Card card, Dictionary<int, int> placement) =>
        placement.TryGetValue(card.ObjectId, out int traced)
            ? traced
            : card.Area.PlayArea.Player;

    internal static bool VillainIsAttackableInTrace(
        AbilityAdmissionScope cast, Card current, HashSet<int> discarded,
        Dictionary<(int Card, string Field), long> modifiers,
        Dictionary<int, int> engagement)
    {
        int player = Resolver(cast);
        bool guarded = cast.World
            .AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(player))
            .Cards.Any(enemy => !discarded.Contains(enemy.ObjectId)
                && !engagement.ContainsKey(enemy.ObjectId)
                && EffectiveCards.Kind(enemy, cast.World.Facts) == CardKind.Minion
                && TraceModified(
                    enemy, "guard", cast, discarded, modifiers) > 0)
            || engagement.Any(pair => pair.Value == player
                && !discarded.Contains(pair.Key)
                && TraceModified(
                    cast.World.Cards[pair.Key], "guard",
                    cast, discarded, modifiers) > 0);
        return !guarded && AbilityDamageProtectionTrace.CanTakeDamageInTrace(cast, current, discarded);
    }


}
