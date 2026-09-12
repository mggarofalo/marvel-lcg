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

internal static class AbilityRepeatedEffectAnalysis
{
    private static readonly HashSet<string> DamageChangeOperations =
    [
        "dealDamage", "indirectDamage", "moveDamage", "replaceThreatWithDamage",
    ];
    private static readonly HashSet<string> BindingOperations =
    [
        "chooseCard", "thwartSchemes", "thwartDifferentSchemes", "legalPractice",
    ];

    internal static bool RequiresChosenPlayer(AbilityCardSelection selection) => selection switch
    {
        AbilityCardSelection.Query query => query.Kind is AbilityCardQuery.EnemiesEngagedWithChosenPlayer
            or AbilityCardQuery.TopmostTechInChosenDiscard,
        AbilityCardSelection.WithTrait filtered => RequiresChosenPlayer(filtered.Cards),
        AbilityCardSelection.WithoutAnotherCopyAttached filtered => RequiresChosenPlayer(filtered.Cards),
        AbilityCardSelection.Discardable filtered => RequiresChosenPlayer(filtered.Cards),
        AbilityCardSelection.Ranked ranked => RequiresChosenPlayer(ranked.Cards),
        _ => false,
    };

    internal static bool RepeatedEffectCanChange(
        AbilityCondition test, AbilityEffect effect, AbilityAdmissionScope cast)
    {
        int original = cast.Player;
        try
        {
            var assumed = RepeatedChange.None;
            int priorFrames = Math.Max(0, cast.World.PlayerOrder.Count() - 1);
            for (int frame = 0; frame < priorFrames; frame++)
            {
                var observed = RepeatedChange.None;
                foreach (int player in cast.World.PlayerOrder)
                {
                    cast.RestorePlayer(player);
                    observed |= RepeatedChanges(
                        effect, cast, assumed, binding: false, priorFrames,
                        effect);
                }
                assumed |= observed;
            }
            return RepeatedTestCanChange(test, assumed);
        }
        finally
        {
            cast.RestorePlayer(original);
        }
    }

    internal static RepeatedChange RepeatedChanges(
        AbilityEffect node, AbilityAdmissionScope cast, RepeatedChange assumed, bool binding,
        int priorFrames, AbilityEffect repeatedEffect)
    {
        string operation = node.OperationName();
        if (operation == "changeForm")
            return RepeatedChange.Form | RepeatedChange.CardsInPlay;
        if (DamageChangeOperations.Contains(operation))
        {
            return RepeatedChange.CardsInPlay
                | (DamageCanChangePlayerOrder(
                        node, cast, binding, repeatedEffect, assumed,
                        priorFrames)
                    ? RepeatedChange.PlayerOrder
                    : RepeatedChange.None);
        }
        if (operation is "dealAttackDamage" or "moveAttackDamage")
            return RepeatedChange.CardsInPlay;
        if (operation == "enemyAttacks")
            return RepeatedChange.CardsInPlay | RepeatedChange.PlayerOrder;
        if (StableForCardsInPlay(
            node, cast, priorFrames, repeatedEffect, assumed, binding))
            return RepeatedChange.None;
        return operation switch
        {
            "seq" or "then" or "otherwise" => OrderedRepeatedChanges(
                node, cast, assumed, binding, priorFrames, repeatedEffect),
            "and" => SimultaneousRepeatedChanges(
                node, cast, assumed, binding, priorFrames, repeatedEffect),
            "if" => ConditionalRepeatedChanges(
                node, cast, assumed, binding, priorFrames, repeatedEffect),
            _ => ChildRepeatedChanges(
                node, cast, assumed, binding || BindingOperations.Contains(operation),
                priorFrames, repeatedEffect),
        };
    }

    private static RepeatedChange OrderedRepeatedChanges(
        AbilityEffect node, AbilityAdmissionScope cast, RepeatedChange assumed,
        bool binding, int priorFrames, AbilityEffect repeatedEffect)
    {
        var changes = RepeatedChange.None;
        foreach (var child in MutationChildren(node))
        {
            changes |= RepeatedChanges(
                child, cast, assumed | changes, binding, priorFrames, repeatedEffect);
        }
        return changes;
    }

    private static RepeatedChange SimultaneousRepeatedChanges(
        AbilityEffect node, AbilityAdmissionScope cast, RepeatedChange assumed,
        bool binding, int priorFrames, AbilityEffect repeatedEffect)
    {
        var ordered = MutationChildren(node).ToList();
        var changes = RepeatedChange.None;
        for (int pass = 0; pass < ordered.Count; pass++)
        {
            var before = changes;
            foreach (var child in ordered)
                changes |= RepeatedChanges(
                    child, cast, assumed | changes, binding, priorFrames, repeatedEffect);
            if (changes == before) break;
        }
        return changes;
    }

    private static RepeatedChange ConditionalRepeatedChanges(
        AbilityEffect node, AbilityAdmissionScope cast, RepeatedChange assumed,
        bool binding, int priorFrames, AbilityEffect repeatedEffect)
    {
        var test = ConditionalOf(node, cast).Test;
        var branches = RepeatedTestCanChange(test, assumed)
                || binding && BindingCanChange(test)
            ? ConditionalBranches((AbilityEffect.Conditional)node)
                .Where(value => value is not null)
            : ActiveRepeatedBranch(node, test, cast);
        return branches.Aggregate(
            RepeatedChange.None,
            (changes, branch) => changes | RepeatedChanges(
                branch, cast, assumed, binding, priorFrames, repeatedEffect));
    }

    private static IEnumerable<AbilityEffect> ActiveRepeatedBranch(
        AbilityEffect node, AbilityCondition test, AbilityAdmissionScope cast) =>
        ConditionalBranch(node, Test(test, cast) ? "then" : "else") is { } active
            ? [active]
            : [];

    private static RepeatedChange ChildRepeatedChanges(
        AbilityEffect node, AbilityAdmissionScope cast, RepeatedChange assumed,
        bool binding, int priorFrames, AbilityEffect repeatedEffect)
    {
        var children = MutationChildren(node).ToList();
        if (children.Count == 0)
            return RepeatedChange.CardsInPlay;
        return children.Aggregate(
            RepeatedChange.None,
            (changes, child) => changes
                | RepeatedChanges(
                    child, cast, assumed, binding, priorFrames,
                    repeatedEffect));
    }

    internal static bool RepeatedTestCanChange(
        AbilityCondition test, RepeatedChange changes) => test switch
        {
            AbilityCondition.All all => all.Operands.Any(child =>
                RepeatedTestCanChange(child, changes)),
            AbilityCondition.Any any => any.Operands.Any(child =>
                RepeatedTestCanChange(child, changes)),
            AbilityCondition.Negated negated => RepeatedTestCanChange(negated.Operand, changes),
            AbilityCondition.InForm form => changes.HasFlag(RepeatedChange.Form)
                || changes.HasFlag(RepeatedChange.PlayerOrder)
                    && form.Player != AbilityPlayer.You,
            AbilityCondition.TitleInPlay => changes.HasFlag(RepeatedChange.CardsInPlay),
            AbilityCondition.Flag { Kind: AbilityConditionFact.FinalStep }
                or AbilityCondition.PaidWithResource or AbilityCondition.CausedThreat => false,
            _ => true,
        };

    [Flags]
    internal enum RepeatedChange
    {
        None = 0,
        Form = 1,
        CardsInPlay = 2,
        PlayerOrder = 4,
    }

    internal static bool StableForCardsInPlay(
        AbilityEffect node, AbilityAdmissionScope cast, int priorFrames,
        AbilityEffect repeatedEffect, RepeatedChange assumed,
        bool binding) =>
        node.OperationName() is "draw" or "drawToHandSize" or "drawToPrintedHandSize"
            or "exhaust" or "ready" or "heal" or "generate" or "giveStatus"
            or "gainSurge" or "preventDamage" or "preventThreat"
            or "cancelWhenRevealed" or "cancelOccurrence" or "grantUntil"
            or "grantCharactersControlledBy" or "reduceNextCardCost"
        || node.OperationName() == "removeThreat"
            && Every(ThreatSelectionOf(node, cast), cast) is { Count: > 0 } schemes
            && schemes.All(scheme => scheme.Area.Type == DeckType.MainSchemesArea
                || !CanExhaust(
                    // An earlier ordered mutation can switch a branch before
                    // this leaf is reached in the same repeated frame.
                    TotalThreatRemoved(
                        scheme, repeatedEffect, cast, assumed, binding),
                    priorFrames,
                    scheme.Tokens.GetValueOrDefault("k_threat")));

    internal static bool DamageCanChangePlayerOrder(
        AbilityEffect node, AbilityAdmissionScope cast, bool binding,
        AbilityEffect repeatedEffect, RepeatedChange assumed,
        int priorFrames)
    {
        AbilityCardSelection targets = node.OperationName() switch
        {
            "dealDamage" or "indirectDamage" => DamageSelectionOf(node, cast),
            "moveDamage" => EffectOf<AbilityEffect.MoveDamage>(node, cast).To,
            "replaceThreatWithDamage" => EffectOf<AbilityEffect.CardAction>(node, cast).Selection,
            _ => throw new InvalidOperationException(
                $"'{node.OperationName()}' is not a direct damage node"),
        };
        int damagingFrames = RebindsToEachPlayer(targets)
            ? 1
            : priorFrames;
        var cards = Every(targets, cast);
        return cards.Any(card => cast.World.PlayerOrder.Any(player =>
                cast.World.Seats[player].IdentityCard == card)
            && TotalRepeatedDamageTo(
                card, repeatedEffect, cast, assumed, binding,
                damagingFrames)
                >= DamagePlacement.Health(cast.World, cast.World.Facts, card) - card.Damage)
            || cards.Count == 0 && binding && BindingCanChange(targets);
    }

    internal static long TotalRepeatedDamageTo(
        Card target, AbilityEffect repeatedEffect, AbilityAdmissionScope cast,
        RepeatedChange assumed, bool binding, int frames)
        => PeakRepeatedDamageOn(
            target, repeatedEffect, cast, assumed, binding, frames)
            - target.Damage;

    internal static bool RebindsToEachPlayer(AbilityCardSelection targets) => targets switch
    {
        AbilityCardSelection.Bound bound => bound.Binding is AbilityCardBinding.You
            or AbilityCardBinding.YourHero or AbilityCardBinding.YourAlterEgo,
        AbilityCardSelection.Query query => query.Kind == AbilityCardQuery.CharactersYouControl,
        AbilityCardSelection.WithTrait trait => RebindsToEachPlayer(trait.Cards),
        AbilityCardSelection.Ranked ranked => RebindsToEachPlayer(ranked.Cards),
        AbilityCardSelection.WithoutAnotherCopyAttached other => RebindsToEachPlayer(other.Cards),
        _ => false,
    };

    internal static long TotalThreatRemoved(
        Card scheme, AbilityEffect node, AbilityAdmissionScope cast,
        RepeatedChange assumed = RepeatedChange.None, bool binding = false)
    {
        long own = node.OperationName() == "removeThreat"
            && Every(ThreatSelectionOf(node, cast), cast).Any(candidate =>
                candidate.ObjectId == scheme.ObjectId)
                ? Amount(EffectOf<AbilityEffect.RemoveThreat>(node, cast).Amount, cast)
                : 0;
        return MutationTotal(
            node, cast, assumed, binding, own,
            child => TotalThreatRemoved(
                scheme, child, cast, assumed, binding));
    }

    internal readonly record struct DamageTransfer(
        int From, int To, long Amount,
        bool GrantsHealth = false, bool DealsDamage = false,
        bool Discards = false, bool EntersPlay = false,
        bool RemovesThreat = false,
        bool PlacesThreat = false, string? GrantsTrait = null,
        string? GrantsField = null,
        AbilityCardSelection? FromVillain = null, AbilityCardSelection? ToVillain = null,
        int ChangesForm = -1, string? Form = null,
        string? GrantsStatus = null);

    internal readonly record struct TraceCard(
        Card Card, AbilityCardSelection? VillainSelector);

    internal sealed record DamageTraceState(
        Dictionary<int, long> Damage, long PeakTargetPressure,
        HashSet<int> Players, Dictionary<int, int> Tough,
        HashSet<(int Card, string Status)> StatusChanges,
        Dictionary<(int Card, string Status), int> StatusCounts,
        Dictionary<int, long> Health, HashSet<int> Discarded,
        Dictionary<int, long> Threat, Dictionary<int, HashSet<string>> Traits,
        Dictionary<(int Card, string Field), long> Modifiers,
        Dictionary<int, int> Engagement,
        ulong FormsMayChange,
        int FirstPlayer,
        int CurrentVillain,
        int VillainStagesDrawn, bool Finished);

    internal static long PeakRepeatedDamageOn(
        Card target, AbilityEffect repeatedEffect, AbilityAdmissionScope cast,
        RepeatedChange assumed, bool binding, int frames)
    {
        int original = cast.Player;
        try
        {
            int villain = cast.World.TheCardIn(DeckType.VillainArea)?.ObjectId ?? -1;
            IReadOnlyList<DamageTraceState> states =
                [new(new Dictionary<int, long>(), target.Damage, [], new(), new(),
                    new(), new(), TraceUnavailableMinions(cast), new(),
                    new(), new(), new(), 0, cast.World.FirstPlayer,
                    villain, 0, false)];
            for (int frame = 0; frame < frames; frame++)
            {
                var next = new List<DamageTraceState>();
                foreach (var state in states)
                {
                    foreach (int player in cast.World.PlayerOrder.Where(player =>
                        !state.Players.Contains(player)))
                    {
                        cast.RestorePlayer(player);
                        var traces = DamageTraces(
                            repeatedEffect, cast, assumed, binding);
                        foreach (var trace in traces)
                        {
                            var advanced = ApplyDamageTrace(
                                state, trace, target, cast);
                            next.Add(advanced with
                            {
                                Players = [.. state.Players, player],
                            });
                        }
                    }
                }
                states = next;
            }
            return states.Select(state => state.PeakTargetPressure)
                .DefaultIfEmpty(target.Damage)
                .Max();
        }
        finally
        {
            cast.RestorePlayer(original);
        }
    }

    internal static HashSet<int> TraceUnavailableMinions(AbilityAdmissionScope cast) =>
        cast.World.Areas
            .SelectMany(area => area.Cards)
            .Where(card => card.Area.Type != DeckType.EngagedEnemiesArea
                && FacedownDrones.Kind(card, cast.World.Facts) == CardKind.Minion)
            .Select(card => card.ObjectId)
            .ToHashSet();

}
