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
using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using PowerReachability = Marvel.Rules.Play.RuleProjection<Marvel.Cards.Run.AbilityPowerState>;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

using static Marvel.Cards.Run.AbilityPowerStateProjection;
namespace Marvel.Cards.Run;

internal static class AbilityPowerProjection
{
    internal static EliminationLayout PlanTracePlayerElimination(
        int player, AbilityAdmissionScope cast, HashSet<int> discarded,
        Dictionary<int, int> engagement)
        => EliminationLayout.Calculate(
            new AbilityEliminationLayout(cast.World, discarded, engagement), player);

    internal static AbilityPowerState AdvancePowerVillain(
        AbilityPowerState state, Card damaged, Card first, AbilityAdmissionScope cast)
    {
        if (damaged.ObjectId != state.CurrentVillain
            || PowerDamage(state, damaged) < PowerHealth(state, damaged, cast))
        {
            return state;
        }

        var deck = cast.World.AreaOf(DeckType.VillainDeck);
        int nextIndex = deck.Cards.Count - 1 - state.VillainStagesDrawn;
        Card? next = nextIndex >= 0 ? deck.Cards[nextIndex] : null;
        bool carriesAttachments = next is not null && string.Equals(
            cast.World.Facts.Title(damaged.FaceId),
            cast.World.Facts.Title(next.FaceId),
            StringComparison.Ordinal);
        var discarded = DiscardedAfterStage(damaged, cast, state, carriesAttachments);
        EnsureStageConstantsProjectable(next, damaged, cast, state, discarded);
        var engagement = EngagementAfterStage(
            damaged, state, discarded, carriesAttachments);
        if (nextIndex < 0)
        {
            return state with
            {
                Discarded = discarded,
                Engagement = engagement,
                CurrentVillain = -1,
                Finished = true,
            };
        }

        next = deck.Cards[nextIndex];
        bool carriesTough = string.Equals(
                cast.World.Facts.Title(damaged.FaceId),
                cast.World.Facts.Title(next.FaceId),
                StringComparison.Ordinal)
            && PowerTough(state, damaged, cast);
        var advanced = state with
        {
            Discarded = discarded,
            Engagement = engagement,
            CurrentVillain = next.ObjectId,
            VillainStagesDrawn = state.VillainStagesDrawn + 1,
        };
        advanced = SetPowerDamage(advanced, next, 0, first, cast);
        bool printedTough = StateFields.Modified(
            cast.World, next, "toughness",
            cast.World.Facts, cast.World.Players) > 0;
        return SetPowerTough(
            advanced, next, carriesTough || printedTough, first, cast);
    }

    private static HashSet<int> DiscardedAfterStage(
        Card damaged, AbilityAdmissionScope cast, AbilityPowerState state,
        bool carriesAttachments)
    {
        var discarded = new HashSet<int>(state.Discarded) { damaged.ObjectId };
        if (carriesAttachments) return discarded;
        foreach (int leaving in PowerLeavingTree(damaged, cast).Skip(1))
        {
            discarded.Add(leaving);
        }
        return discarded;
    }

    private static void EnsureStageConstantsProjectable(
        Card? next, Card damaged, AbilityAdmissionScope cast,
        AbilityPowerState state, HashSet<int> discarded)
    {
        if (next is null) return;
        if (AbilityProgramQueries.On(cast.Context.Program, next).Any(ability =>
            ability.Trigger.Timing == AbilityType.Constant))
        {
            throw new RulesNotImplementedException(
                $"villain stage '{next.FaceId}' enters play before a "
                + "labelled-power continuation reads its constant abilities, "
                + "which is not implemented");
        }
        if (!HasLiveVillainRetargetingConstant(
            discarded, damaged, next, cast,
            threatChanges: state.SchemeThreat,
            damageChanges: state.CardDamage,
            modifierChanges: state.Modifiers,
            traitChanges: state.Traits,
            statusChanges:
            [
                .. state.StatusChanges,
                .. state.CardTough.Keys.Select(card => (card, Statuses.Tough)),
            ],
            engagementChanges: state.Engagement,
            formsMayChange: state.FormsMayChange,
            traceFirstPlayer: state.FirstPlayer))
        {
            return;
        }
        throw new RulesNotImplementedException(
            $"villain stage '{next.FaceId}' enters play before a "
            + "labelled-power continuation reads retargeting constant "
            + "abilities, which is not implemented");
    }

    private static Dictionary<int, int> EngagementAfterStage(
        Card damaged, AbilityPowerState state, HashSet<int> discarded,
        bool carriesAttachments)
    {
        var engagement = new Dictionary<int, int>(state.Engagement);
        if (carriesAttachments) return engagement;
        foreach (int leaving in discarded.Where(cardId =>
            cardId != damaged.ObjectId && !state.Discarded.Contains(cardId)))
        {
            engagement.Remove(leaving);
        }
        return engagement;
    }

    internal static bool ConstantCanRetargetVillain(
        AbilityEffect node, Card current, Card next, AbilityAdmissionScope cast,
        HashSet<int> discarded,
        IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange,
        int traceFirstPlayer)
    {
        if (node is AbilityEffect.Conditional conditional)
        {
            return ConditionalCanRetargetVillain(
                conditional, current, next, cast, discarded, threatChanges,
                damageChanges, modifierChanges, traitChanges, statusChanges,
                engagementChanges, formsMayChange, traceFirstPlayer);
        }
        AbilityCardSelection? target = node switch
        {
            AbilityEffect.GrantField { Until: null } grant => grant.Cards,
            AbilityEffect.GrantTrait { Until: null } grant => grant.Cards,
            _ => null,
        };
        if (target is not null && PotentialVillainSelector(target, cast)) return true;
        return StructuralChildren(node).Any(child =>
            ConstantCanRetargetVillain(
                child, current, next, cast, discarded,
                threatChanges, damageChanges, modifierChanges,
                traitChanges, statusChanges, engagementChanges,
                formsMayChange, traceFirstPlayer));
    }

    private static bool ConditionalCanRetargetVillain(
        AbilityEffect.Conditional conditional, Card current, Card next,
        AbilityAdmissionScope cast, HashSet<int> discarded,
        IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange, int traceFirstPlayer)
    {
        if (TryTraceConstantTest(
            conditional.Test, current, next, cast, discarded,
            threatChanges, damageChanges, modifierChanges, traitChanges,
            statusChanges, engagementChanges, formsMayChange, traceFirstPlayer,
            out bool tracedTest))
        {
            return BranchCanRetargetVillain(
                tracedTest ? conditional.Then : conditional.Else,
                current, next, cast, discarded, threatChanges, damageChanges,
                modifierChanges, traitChanges, statusChanges, engagementChanges,
                formsMayChange, traceFirstPlayer);
        }
        if (TestCanChangeOnVillainAdvance(
            conditional.Test, current, next, cast, discarded,
            threatChanges, damageChanges, modifierChanges, traitChanges,
            statusChanges, engagementChanges, formsMayChange, traceFirstPlayer))
        {
            return StructuralChildren(conditional).Any(child =>
                BranchCanRetargetVillain(
                    child, current, next, cast, discarded, threatChanges,
                    damageChanges, modifierChanges, traitChanges, statusChanges,
                    engagementChanges, formsMayChange, traceFirstPlayer));
        }
        return BranchCanRetargetVillain(
            Test(conditional.Test, cast) ? conditional.Then : conditional.Else,
            current, next, cast, discarded, threatChanges, damageChanges,
            modifierChanges, traitChanges, statusChanges, engagementChanges,
            formsMayChange, traceFirstPlayer);
    }

    private static bool BranchCanRetargetVillain(
        AbilityEffect? branch, Card current, Card next, AbilityAdmissionScope cast,
        HashSet<int> discarded, IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange, int traceFirstPlayer) =>
        branch is not null && ConstantCanRetargetVillain(
            branch, current, next, cast, discarded, threatChanges, damageChanges,
            modifierChanges, traitChanges, statusChanges, engagementChanges,
            formsMayChange, traceFirstPlayer);

    internal static bool HasLiveVillainRetargetingConstant(
        HashSet<int> discarded, Card current, Card next, AbilityAdmissionScope cast,
        IReadOnlyDictionary<int, long>? threatChanges = null,
        IReadOnlyDictionary<int, long>? damageChanges = null,
        IReadOnlyDictionary<(int Card, string Field), long>? modifierChanges = null,
        IReadOnlyDictionary<int, HashSet<string>>? traitChanges = null,
        HashSet<(int Card, string Status)>? statusChanges = null,
        IReadOnlyDictionary<int, int>? engagementChanges = null,
        ulong formsMayChange = 0, int traceFirstPlayer = -1) =>
        cast.World.Areas
            .Where(area => DeckTypes.IsInPlay(area.Type))
            .SelectMany(area => area.Cards)
            .ToList()
            .Where(card => !discarded.Contains(card.ObjectId))
            .Any(card =>
            {
                var placement = engagementChanges ?? new Dictionary<int, int>();
                var constantCast = TraceConstantCast(cast, card, placement);
                return AbilityProgramQueries.On(cast.Context.Program, card).Any(ability =>
                    ability.Trigger.Timing == AbilityType.Constant
                    && ConstantCanRetargetVillain(
                        ability.Effect, current, next, constantCast, discarded,
                        threatChanges ?? new Dictionary<int, long>(),
                        damageChanges ?? new Dictionary<int, long>(),
                        modifierChanges
                            ?? new Dictionary<(int Card, string Field), long>(),
                        traitChanges ?? new Dictionary<int, HashSet<string>>(),
                        statusChanges ?? [],
                        placement,
                        formsMayChange,
                        traceFirstPlayer < 0
                            ? cast.World.FirstPlayer
                            : traceFirstPlayer));
            });

    internal static AbilityAdmissionScope TraceConstantCast(
        AbilityAdmissionScope cast, Card source,
        IReadOnlyDictionary<int, int> placement)
    {
        int controller = AbilityCardQueries.ControllerOf(cast.World, source);
        if (AbilityCardQueries.IsPlayerCard(cast.World.Facts, source)
            && DeckTypes.IsInPlay(source.Area.Type)
            && placement.TryGetValue(source.ObjectId, out int projectedPlayer))
        {
            controller = projectedPlayer;
        }
        int? projected = placement.TryGetValue(source.ObjectId, out int player)
            ? player : null;
        var bindings = new AbilityQueryContext(
            cast.World, source, new Occurrence(0, []), controller,
            source.Incarnation, null, null, null, []);
        var expressions = new AbilityExpressionContext(
            bindings, System.Collections.Immutable.ImmutableDictionary<string, long>.Empty,
            [], string.Empty, -1, false, projected);
        return new AbilityAdmissionScope(
            new AbilityAdmissionContext(
                cast.Context.Program, cast.Context.ResourceAbilities,
                expressions, cast.Reachability, cast.Power),
            []);
    }


    internal static bool ConditionalModifierCanDiffer(
        Card target, string field, Card current, Card next, AbilityAdmissionScope cast,
        HashSet<int> discarded,
        IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange,
        int traceFirstPlayer,
        int dependencyDepth)
    {
        if (dependencyDepth <= 0)
        {
            throw new RulesNotImplementedException(
                $"'{cast.Source.FaceId}' reaches a cyclic conditional modifier "
                + "before a labelled-power continuation, which is not implemented");
        }
        return cast.World.Areas
                .Where(area => DeckTypes.IsInPlay(area.Type))
                .SelectMany(area => area.Cards)
                .Where(source => !discarded.Contains(source.ObjectId))
                .Any(source =>
                {
                    var constantCast = TraceConstantCast(
                        cast, source, engagementChanges);
                    return AbilityProgramQueries.On(cast.Context.Program, source).Any(ability =>
                        ability.Trigger.Timing == AbilityType.Constant
                        && ConstantFieldCanDiffer(
                            ability.Effect, source, target, field,
                            current, next, constantCast, discarded,
                            threatChanges, damageChanges, modifierChanges,
                            traitChanges, statusChanges, engagementChanges,
                            formsMayChange, traceFirstPlayer, dependencyDepth));
                });
    }

    internal static bool ConstantFieldCanDiffer(
        AbilityEffect node, Card source, Card target, string field,
        Card current, Card next, AbilityAdmissionScope cast, HashSet<int> discarded,
        IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange,
        int traceFirstPlayer,
        int dependencyDepth)
    {
        if (!ContainsFieldGrant(node, source, target, field, cast))
        {
            return false;
        }
        if (node is AbilityEffect.Conditional conditional)
        {
            return ConditionalFieldCanDiffer(
                conditional, source, target, field, current, next, cast, discarded,
                threatChanges, damageChanges, modifierChanges, traitChanges,
                statusChanges, engagementChanges, formsMayChange, traceFirstPlayer,
                dependencyDepth);
        }
        return StructuralChildren(node).Any(child => ConstantFieldCanDiffer(
            child, source, target, field, current, next, cast, discarded,
            threatChanges, damageChanges, modifierChanges,
            traitChanges, statusChanges, engagementChanges,
            formsMayChange, traceFirstPlayer, dependencyDepth));
    }

    private static bool ConditionalFieldCanDiffer(
        AbilityEffect.Conditional conditional, Card source, Card target, string field,
        Card current, Card next, AbilityAdmissionScope cast, HashSet<int> discarded,
        IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange, int traceFirstPlayer, int dependencyDepth)
    {
        if (TryTraceConstantTest(
            conditional.Test, current, next, cast, discarded,
            threatChanges, damageChanges, modifierChanges, traitChanges,
            statusChanges, engagementChanges, formsMayChange, traceFirstPlayer,
            out bool tracedTest))
        {
            return TracedConditionalFieldCanDiffer(
                conditional, tracedTest, source, target, field, current, next, cast,
                discarded, threatChanges, damageChanges, modifierChanges, traitChanges,
                statusChanges, engagementChanges, formsMayChange, traceFirstPlayer,
                dependencyDepth);
        }
        if (TestCanChangeOnVillainAdvance(
            conditional.Test, current, next, cast, discarded,
            threatChanges, damageChanges, modifierChanges, traitChanges,
            statusChanges, engagementChanges, formsMayChange, traceFirstPlayer,
            dependencyDepth - 1))
        {
            return StructuralChildren(conditional).Any(child =>
                ContainsFieldGrant(child, source, target, field, cast));
        }
        return BranchFieldCanDiffer(
            Test(conditional.Test, cast) ? conditional.Then : conditional.Else,
            source, target, field, current, next, cast, discarded, threatChanges,
            damageChanges, modifierChanges, traitChanges, statusChanges,
            engagementChanges, formsMayChange, traceFirstPlayer, dependencyDepth);
    }

    private static bool TracedConditionalFieldCanDiffer(
        AbilityEffect.Conditional conditional, bool tracedTest,
        Card source, Card target, string field, Card current, Card next,
        AbilityAdmissionScope cast, HashSet<int> discarded,
        IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange, int traceFirstPlayer, int dependencyDepth)
    {
        bool liveTest = Test(conditional.Test, cast);
        if (tracedTest != liveTest)
        {
            return BranchContainsFieldGrant(
                    liveTest ? conditional.Then : conditional.Else,
                    source, target, field, cast)
                || BranchContainsFieldGrant(
                    tracedTest ? conditional.Then : conditional.Else,
                    source, target, field, cast);
        }
        return BranchFieldCanDiffer(
            tracedTest ? conditional.Then : conditional.Else,
            source, target, field, current, next, cast, discarded, threatChanges,
            damageChanges, modifierChanges, traitChanges, statusChanges,
            engagementChanges, formsMayChange, traceFirstPlayer, dependencyDepth);
    }

    private static bool BranchContainsFieldGrant(
        AbilityEffect? branch, Card source, Card target, string field,
        AbilityAdmissionScope cast) =>
        branch is not null && ContainsFieldGrant(branch, source, target, field, cast);

    private static bool BranchFieldCanDiffer(
        AbilityEffect? branch, Card source, Card target, string field,
        Card current, Card next, AbilityAdmissionScope cast, HashSet<int> discarded,
        IReadOnlyDictionary<int, long> threatChanges,
        IReadOnlyDictionary<int, long> damageChanges,
        IReadOnlyDictionary<(int Card, string Field), long> modifierChanges,
        IReadOnlyDictionary<int, HashSet<string>> traitChanges,
        IReadOnlySet<(int Card, string Status)> statusChanges,
        IReadOnlyDictionary<int, int> engagementChanges,
        ulong formsMayChange, int traceFirstPlayer, int dependencyDepth) =>
        branch is not null && ConstantFieldCanDiffer(
            branch, source, target, field, current, next, cast, discarded,
            threatChanges, damageChanges, modifierChanges, traitChanges,
            statusChanges, engagementChanges, formsMayChange, traceFirstPlayer,
            dependencyDepth);

}
