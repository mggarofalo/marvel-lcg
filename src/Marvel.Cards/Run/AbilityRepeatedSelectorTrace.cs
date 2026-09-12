using static Marvel.Cards.Run.AbilityAdmission;
using static Marvel.Cards.Run.AbilityChoiceAnalysis;
using static Marvel.Cards.Run.AbilityDelayedReachability;
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
                or AbilityCardQuery.Enemies or AbilityCardQuery.AttackableEnemies or AbilityCardQuery.Characters,
            AbilityCardSelection.Titled titled => string.Equals(
                titled.Title, cast.World.Facts.Title(current.FaceId), StringComparison.Ordinal),
            AbilityCardSelection.EnemiesWithTrait trait => TraceHasTrait(current, trait.Trait, cast, []),
            AbilityCardSelection.WithTrait trait => TraceHasTrait(current, trait.Trait, cast, [])
                && SelectorCanTrackVillain(trait.Cards, current, cast),
            AbilityCardSelection.WithoutAnotherCopyAttached other => SelectorCanTrackVillain(other.Cards, current, cast),
            AbilityCardSelection.Ranked ranked => SelectorCanTrackVillain(ranked.Cards, current, cast),
            _ => false,
        };

    internal static int? TraceSelectorIncludesCard(
        AbilityCardSelection value, int bound, int currentVillain, AbilityAdmissionScope cast,
        HashSet<int> discarded, Dictionary<int, HashSet<string>> traits,
        Dictionary<(int Card, string Field), long> modifiers,
        Dictionary<int, int> engagement)
    {
        int boardVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        int candidateId = bound == boardVillain ? currentVillain : bound;
        if (candidateId < 0 || discarded.Contains(candidateId))
        {
            return null;
        }
        var candidate = cast.World.Cards[candidateId];
        return TraceSelectorMatches(
            value, candidate, currentVillain, cast, discarded, traits, modifiers,
            engagement)
            ? candidateId
            : null;
    }

    internal static bool TraceQueryMatches(
        AbilityCardQuery query, Card candidate, int currentVillain, AbilityAdmissionScope cast,
        HashSet<int> discarded, Dictionary<int, HashSet<string>> traits,
        Dictionary<(int Card, string Field), long> modifiers,
        Dictionary<int, int> engagement)
    {
        bool villain = candidate.ObjectId == currentVillain;
        var kind = FacedownDrones.Kind(candidate, cast.World.Facts);
        return query switch
        {
            AbilityCardQuery.Villain => villain,
            AbilityCardQuery.Enemies => villain
                || kind == CardKind.Minion,
            AbilityCardQuery.Minions => kind == CardKind.Minion,
            AbilityCardQuery.Characters => IsCharacter(villain, kind),
            AbilityCardQuery.AttackableEnemies => IsAttackableEnemy(
                villain, kind, candidate, cast, discarded, modifiers, engagement),
            AbilityCardQuery.MinionsEngagedWithYou => IsEngagedMinion(
                kind, candidate, cast.Player, engagement),
            AbilityCardQuery.DronesEngagedWithYou => IsEngagedDrone(
                kind, candidate, cast, discarded, traits, engagement),
            AbilityCardQuery.EnemiesEngagedWithChosenPlayer =>
                IsEngagedWithChosenPlayer(kind, candidate, cast, engagement),
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
            : kind == CardKind.Minion && CanTakeDamageInTrace(cast, candidate, discarded);

    private static bool IsEngagedMinion(
        CardKind kind, Card candidate, int player, Dictionary<int, int> engagement) =>
        kind == CardKind.Minion && TraceEngagedWith(candidate, player, engagement);

    private static bool IsEngagedDrone(
        CardKind kind, Card candidate, AbilityAdmissionScope cast, HashSet<int> discarded,
        Dictionary<int, HashSet<string>> traits, Dictionary<int, int> engagement) =>
        kind == CardKind.Minion
        && TraceHasTrait(candidate, "DRONE", cast, discarded, traits)
        && TraceEngagedWith(candidate, Resolver(cast), engagement);

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
                && FacedownDrones.Kind(enemy, cast.World.Facts) == CardKind.Minion
                && TraceModified(
                    enemy, "guard", cast, discarded, modifiers) > 0)
            || engagement.Any(pair => pair.Value == player
                && !discarded.Contains(pair.Key)
                && TraceModified(
                    cast.World.Cards[pair.Key], "guard",
                    cast, discarded, modifiers) > 0);
        return !guarded && CanTakeDamageInTrace(cast, current, discarded);
    }

    internal static bool TraceHasTrait(
        Card current, string trait, AbilityAdmissionScope cast, HashSet<int> discarded,
        Dictionary<int, HashSet<string>>? traits = null)
    {
        if (traits?.TryGetValue(current.ObjectId, out var gained) == true
            && gained.Contains(trait))
        {
            return true;
        }
        if (FacedownDrones.InherentTraits(current, cast.World.Facts)
            .Contains(trait, StringComparer.Ordinal))
        {
            return true;
        }

        string grantedKind = Rules.State.Traits.Granted + trait;
        if (HasActiveGrantedTrait(current, grantedKind, cast, discarded))
        {
            return true;
        }
        return HasCarriedGrantedTrait(current, grantedKind, cast, discarded);
    }

    private static bool HasActiveGrantedTrait(
        Card current, string grantedKind, AbilityAdmissionScope cast,
        HashSet<int> discarded) =>
        cast.World.Effects.Active().Any(effect =>
            effect.Affects == current.ObjectId
            && ConstantSourceIsActive(effect, discarded)
            && string.Equals(effect.Kind, grantedKind, StringComparison.Ordinal));

    private static bool ConstantSourceIsActive(
        ContinuousEffect effect, HashSet<int> discarded) =>
        effect.Source != EffectSource.ConstantAbility
        || effect.Card is not int source
        || !discarded.Contains(source);

    private static bool HasCarriedGrantedTrait(
        Card current, string grantedKind, AbilityAdmissionScope cast,
        HashSet<int> discarded)
    {
        int boardVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        var carriedIds = TraceCarriedAttachments(current, cast, discarded)
            .Select(card => card.ObjectId).ToHashSet();
        return cast.World.Effects.Active().Any(effect =>
            effect.Affects == boardVillain
            && effect.Card is int source
            && carriedIds.Contains(source)
            && string.Equals(effect.Kind, grantedKind, StringComparison.Ordinal));
    }

    internal static List<Card> TraceCarriedAttachments(
        Card current, AbilityAdmissionScope cast, HashSet<int> discarded)
    {
        int boardVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        if (boardVillain < 0 || !string.Equals(
                cast.World.Facts.Title(cast.World.Cards[boardVillain].FaceId),
                cast.World.Facts.Title(current.FaceId),
                StringComparison.Ordinal))
        {
            return [];
        }
        return
        [
            .. cast.World.Areas
                .Where(area => area.Host == boardVillain
                    || area.Host == current.ObjectId)
                .SelectMany(area => area.Cards)
                .Where(card => !discarded.Contains(card.ObjectId)),
        ];
    }

    internal static long TraceModified(
        Card current, string field, AbilityAdmissionScope cast, HashSet<int> discarded,
        Dictionary<(int Card, string Field), long>? modifiers = null)
    {
        if (IsUnmodifiableDashPower(current, field, cast))
        {
            // `rr:dash-value.3`: a referenced dash is an unmodifiable zero.
            // Match StateFields.Modified's early return before applying either
            // trace-local or carried modifiers.
            return 0;
        }

        string? printed = field switch
        {
            "attack" => "ATK+",
            "scheme" => "SCH+",
            "thwart" => "THW+",
            _ => null,
        };
        long value = StateFields.Modified(
                cast.World, current, field, cast.World.Facts, cast.World.Players)
            + (modifiers?.GetValueOrDefault((current.ObjectId, field)) ?? 0);
        value -= DiscardedAttachmentModifier(current, printed, cast, discarded);
        value -= DiscardedConstantModifier(current, field, cast, discarded);

        int boardVillain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
        if (current.ObjectId == boardVillain)
        {
            return value;
        }

        var carried = TraceCarriedAttachments(current, cast, discarded);
        return value + CarriedModifier(carried, boardVillain, printed, field, cast);
    }

    private static bool IsUnmodifiableDashPower(
        Card current, string field, AbilityAdmissionScope cast) =>
        StateFieldCatalog.FilledFrom.TryGetValue(field, out string? attribute)
        && attribute is "ATK" or "THW" or "DEF" or "REC" or "SCH"
        && !StateFieldCatalog.HasUsablePrintedPower(
            cast.World.Facts, current.FaceId, attribute);

    private static long DiscardedAttachmentModifier(
        Card current, string? printed, AbilityAdmissionScope cast,
        HashSet<int> discarded) =>
        printed is null ? 0 : cast.World.Areas
            .Where(area => area.Host == current.ObjectId)
            .SelectMany(area => area.Cards)
            .Where(card => discarded.Contains(card.ObjectId))
            .Sum(card => cast.World.Facts.PrintedValue(
                card.FaceId, printed, cast.World.Players));

    private static long DiscardedConstantModifier(
        Card current, string field, AbilityAdmissionScope cast,
        HashSet<int> discarded) =>
        cast.World.Effects.Active()
            .Where(effect => string.Equals(effect.Kind, field, StringComparison.Ordinal)
                && effect.AppliesTo(cast.World, current)
                && effect.Source == EffectSource.ConstantAbility
                && effect.Card is int source
                && discarded.Contains(source))
            .Sum(effect => effect.Amount);

    private static long CarriedModifier(
        List<Card> carried, int boardVillain, string? printed, string field,
        AbilityAdmissionScope cast)
    {
        long value = printed is null ? 0 : carried.Sum(card =>
            cast.World.Facts.PrintedValue(card.FaceId, printed, cast.World.Players));
        var carriedIds = carried.Select(card => card.ObjectId).ToHashSet();
        return value + cast.World.Effects.Active()
            .Where(effect => effect.Affects == boardVillain
                && effect.Card is int source
                && carriedIds.Contains(source)
                && string.Equals(effect.Kind, field, StringComparison.Ordinal))
            .Sum(effect => effect.Amount);
    }

}
