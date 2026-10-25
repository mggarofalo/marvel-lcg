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
using System.Collections.Immutable;
using Marvel.Cards.Dsl;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityAdmissionMutation;
using static Marvel.Cards.Run.AbilityBindingReachability;
using static Marvel.Cards.Run.AbilityInitiationConstraints;
using static Marvel.Cards.Run.AbilityTargetAdmission;

namespace Marvel.Cards.Run;

internal static class AbilityChoiceStability
{
    internal static bool ChoicesHaveStableAreaContinuation(
        AbilityEffect effect, IReadOnlyList<AbilityEffect> suffix, AbilityAdmissionScope cast)
    {
        var sensitiveAreas = new HashSet<DeckType>();
        foreach (var step in suffix)
        {
            AbilityAdmissionAreaDependencies.Collect(
                step, cast.Context, sensitiveAreas);
        }
        if (sensitiveAreas.Count == 0)
        {
            return true;
        }

        return ChoicesAreStable(effect, sensitiveAreas, cast);
    }

    internal static bool ChoicesAreStable(
        AbilityEffect effect, HashSet<DeckType> sensitiveAreas, AbilityAdmissionScope cast)
    {
        if (effect.OperationName() == "seq")
        {
            return SequenceChoicesAreStable(effect, sensitiveAreas, cast);
        }

        var priorChosen = cast.CaptureChosen();
        var priorSelection = cast.CapturePlayerSelection();
        try
        {
            return ChoicesAreStableByOperation(effect, sensitiveAreas, cast);
        }
        finally
        {
            cast.RestoreChosen(priorChosen);
            cast.RestorePlayerSelection(priorSelection);
        }
    }

    private static bool SequenceChoicesAreStable(
        AbilityEffect effect, HashSet<DeckType> sensitiveAreas, AbilityAdmissionScope cast)
    {
        var steps = OrderedEffects(effect).ToList();
        for (int step = 0; step < steps.Count; step++)
        {
            var after = new HashSet<DeckType>(sensitiveAreas);
            foreach (var later in steps.Skip(step + 1))
            {
                AbilityAdmissionAreaDependencies.Collect(later, cast.Context, after);
            }
            if (!ChoicesAreStable(steps[step], after, cast))
            {
                return false;
            }
        }
        return true;
    }

    private static bool ChoicesAreStableByOperation(
        AbilityEffect effect, HashSet<DeckType> sensitiveAreas, AbilityAdmissionScope cast) =>
        effect.OperationName() switch
        {
            "choose" => ChoiceOptionsAreStable(effect, sensitiveAreas, cast),
            "chooseCard" => CardChoicesAreStable(effect, sensitiveAreas, cast),
            "forEach" when CurrentlyZeroForEach(effect, cast) => true,
            "eachPlayer" => EachPlayerChoicesAreStable(effect, sensitiveAreas, cast),
            _ => ChildChoicesAreStable(effect, sensitiveAreas, cast),
        };

    private static bool ChoiceOptionsAreStable(
        AbilityEffect effect, HashSet<DeckType> sensitiveAreas, AbilityAdmissionScope cast) =>
        ((AbilityEffect.Choose)effect).Options.Any(option =>
            OptionIsLegal(option, cast)
            && !MayChangeAnyArea(option, sensitiveAreas, cast)
            && ChoicesAreStable(option, sensitiveAreas, cast));

    private static bool CardChoicesAreStable(
        AbilityEffect effect, HashSet<DeckType> sensitiveAreas, AbilityAdmissionScope cast)
    {
        var chosenEffect = EffectBody(effect);
        return LegalCardChoices(effect, cast).Any(candidate =>
        {
            cast.ChooseSelection(candidate);
            return !MayChangeAnyArea(chosenEffect, sensitiveAreas, cast)
                && ChoicesAreStable(chosenEffect, sensitiveAreas, cast);
        });
    }

    private static bool EachPlayerChoicesAreStable(
        AbilityEffect effect, HashSet<DeckType> sensitiveAreas, AbilityAdmissionScope cast)
    {
        int priorPlayer = cast.Player;
        try
        {
            return AbilityEachPlayerSelection.Players(effect, cast.Context.Expressions).All(player =>
            {
                cast.RestorePlayer(player);
                return ChoicesAreStable(EffectBody(effect), sensitiveAreas, cast);
            });
        }
        finally
        {
            cast.RestorePlayer(priorPlayer);
        }
    }

    private static bool ChildChoicesAreStable(
        AbilityEffect effect, HashSet<DeckType> sensitiveAreas, AbilityAdmissionScope cast)
    {
        var children = effect.OperationName() switch
        {
            "if" => ReachableMutationBranches(effect, cast),
            "forEach" => [EffectBody(effect)],
            _ => ResolutionChildren(effect),
        };
        return children.All(child => ChoicesAreStable(child, sensitiveAreas, cast));
    }

}
