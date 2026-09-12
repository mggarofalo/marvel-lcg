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
using PowerReachability = Marvel.Rules.Play.RuleProjection<Marvel.Cards.Run.AbilityPowerState>;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;

using static Marvel.Cards.Run.AbilityPowerOutcomeTrace;
using static Marvel.Cards.Run.AbilityPowerStateMutation;
using static Marvel.Cards.Run.AbilityPowerHealthTrace;
namespace Marvel.Cards.Run;

internal static class AbilityPowerOutcomeTrace
{
    internal static HashSet<AdmissionResolution> PowerOutcomes(
        AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange,
        bool bindingMayChange, AbilityPowerState reachability)
    {
        HashSet<AdmissionResolution>? known = node.OperationName() switch
        {
            "draw" => DrawOutcomes(node, cast, reachability),
            "heal" => HealOutcomes(node, cast, reachability),
            "discard" => DiscardOutcomes(node, cast, bindingMayChange, reachability),
            "removeThreat" => ThreatOutcomes(node, cast, reachability),
            "exhaust" or "ready" => ReadinessOutcomes(
                node, cast, stateMayChange, bindingMayChange, reachability),
            "changeForm" => FormOutcomes(node, cast, bindingMayChange, reachability),
            _ => null,
        };
        if (known is not null) return known;
        bool outcomeMayChange = stateMayChange
            || cast.Reachability.PaymentMayMutate
            || bindingMayChange
            || ActiveChoices(node, cast).Any();
        return outcomeMayChange
            ? [AdmissionResolution.None, AdmissionResolution.Partial, AdmissionResolution.Full]
            : [ResolutionOf(node, cast)];
    }

    private static HashSet<AdmissionResolution> DrawOutcomes(
        AbilityEffect node, AbilityAdmissionScope cast, AbilityPowerState state)
    {
        var draw = EffectOf<AbilityEffect.Draw>(node, cast);
        return [CombinedOutcomes(Seats(draw.Players, cast).Select(player =>
            ResolutionOfAmount(PowerCardsAvailable(state, player, cast), draw.Count)))];
    }

    private static HashSet<AdmissionResolution>? HealOutcomes(
        AbilityEffect node, AbilityAdmissionScope cast, AbilityPowerState state)
    {
        var heal = EffectOf<AbilityEffect.Heal>(node, cast);
        var card = PowerFind(heal.Card, cast, state);
        return card is null ? null : [ResolutionOfAmount(
            PowerDamage(state, card), Amount(heal.Amount, cast))];
    }

    private static HashSet<AdmissionResolution>? DiscardOutcomes(
        AbilityEffect node, AbilityAdmissionScope cast, bool bindingMayChange,
        AbilityPowerState state)
    {
        var target = EffectOf<AbilityEffect.CardAction>(node, cast).Selection;
        var card = PowerFind(target, cast, state);
        if (card is not null)
            return [state.Discarded.Contains(card.ObjectId)
                ? AdmissionResolution.None : AdmissionResolution.Full];
        var unchanged = Find(target, cast);
        return unchanged is not null
            && state.Discarded.Contains(unchanged.ObjectId)
            && !(bindingMayChange && BindingCanChange(target))
                ? [AdmissionResolution.None]
                : null;
    }

    private static HashSet<AdmissionResolution> ThreatOutcomes(
        AbilityEffect node, AbilityAdmissionScope cast, AbilityPowerState state)
    {
        var removal = EffectOf<AbilityEffect.RemoveThreat>(node, cast);
        long wanted = Amount(removal.Amount, cast);
        var valid = PowerEvery(removal.Schemes, cast, state)
            .Where(scheme => CanRemovePowerThreat(node, scheme, cast, state));
        return [CombinedOutcomes(valid.Select(scheme =>
            ResolutionOfAmount(PowerThreat(state, scheme), wanted)))];
    }

    private static bool CanRemovePowerThreat(
        AbilityEffect node, Card scheme, AbilityAdmissionScope cast,
        AbilityPowerState state) =>
        PowerThreat(state, scheme) > 0
        && AbilityProgramQueries.CanRemoveThreat(
            cast.World, cast.Context.Program, scheme,
            OverriddenThreatRemovalSource(node, cast))
        && (IgnoresCrisis(node, cast)
            || !(scheme.Area.Type == DeckType.MainSchemesArea
                && IsPlayerCard(cast) && PowerCrisis(state, cast)));

    private static HashSet<AdmissionResolution>? ReadinessOutcomes(
        AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange,
        bool bindingMayChange, AbilityPowerState state)
    {
        var target = EffectOf<AbilityEffect.CardAction>(node, cast).Selection;
        var current = PowerEvery(target, cast, state);
        if (!ReadinessTargetIsFixed(target, current, stateMayChange, state)
            || bindingMayChange && BindingCanChange(target)) return null;
        var possibilities = new HashSet<(bool Changed, bool Unchanged)>
        {
            (false, false),
        };
        foreach (var card in current.Where(card => !state.Discarded.Contains(card.ObjectId)))
        {
            possibilities = NextReadinessPossibilities(
                possibilities, PowerReady(card, state), node.OperationName() == "exhaust");
        }
        return [.. possibilities.Select(ReadinessResolution)];
    }

    private static bool ReadinessTargetIsFixed(
        AbilityCardSelection target, List<Card> current,
        bool stateMayChange, AbilityPowerState state) =>
        !stateMayChange
        || target is AbilityCardSelection.Bound
            { Binding: AbilityCardBinding.This or AbilityCardBinding.You }
        || current.Count > 0
            && current.All(card => state.Discarded.Contains(card.ObjectId));

    private static HashSet<(bool Changed, bool Unchanged)> NextReadinessPossibilities(
        HashSet<(bool Changed, bool Unchanged)> prior, PowerReadiness readiness,
        bool exhausting)
    {
        bool canChange = readiness.HasFlag(
            exhausting ? PowerReadiness.Ready : PowerReadiness.Exhausted);
        bool canStay = readiness.HasFlag(
            exhausting ? PowerReadiness.Exhausted : PowerReadiness.Ready);
        var next = new HashSet<(bool Changed, bool Unchanged)>();
        foreach (var possibility in prior)
        {
            if (canChange) next.Add((true, possibility.Unchanged));
            if (canStay) next.Add((possibility.Changed, true));
        }
        return next;
    }

    private static AdmissionResolution ReadinessResolution(
        (bool Changed, bool Unchanged) possibility) => possibility switch
        {
            (false, _) => AdmissionResolution.None,
            (true, false) => AdmissionResolution.Full,
            _ => AdmissionResolution.Partial,
        };

    private static HashSet<AdmissionResolution>? FormOutcomes(
        AbilityEffect node, AbilityAdmissionScope cast, bool bindingMayChange,
        AbilityPowerState state)
    {
        var change = FormChangeOf(node, cast);
        if (bindingMayChange && change.Player == AbilityPlayer.ChosenPlayer) return null;
        int seat = Seat(change.Player, cast);
        bool live = AlreadyInForm(change, cast);
        bool current = SeatMayChange(state.FormsMayChange, seat) ? !live : live;
        return [current ? AdmissionResolution.None : AdmissionResolution.Full];
    }

    internal static bool ConditionalCanSkipBranch(
        AbilityEffect node, AbilityAdmissionScope cast, bool stateMayChange,
        bool bindingMayChange, AbilityPowerState reachability)
    {
        var test = ConditionalOf(node, cast).Test;
        bool canSwitch = PowerTestCanChange(
            test, cast, stateMayChange, bindingMayChange, reachability);
        if (!canSwitch)
        {
            string active = Test(test, cast) ? "then" : "else";
            return ConditionalBranch(node, active) is null;
        }
        return ConditionalBranch(node, "then") is null || ConditionalBranch(node, "else") is null;
    }

    internal static AbilityPowerState ChangeFormState(
        AbilityEffect.ChangeForm change, AbilityAdmissionScope cast, bool bindingMayChange,
        AbilityPowerState reachability)
    {
        var player = change.Player;
        if (bindingMayChange && player == AbilityPlayer.ChosenPlayer)
        {
            return reachability with
            {
                FormsMayChange = reachability.FormsMayChange | AllPlayerSeats(cast),
            };
        }
        int seat = Seat(player, cast);
        ulong bit = PlayerSeat(seat);
        bool destinationIsCurrent = AlreadyInForm(change, cast);
        return reachability with
        {
            FormsMayChange = destinationIsCurrent
                ? reachability.FormsMayChange & ~bit
                : reachability.FormsMayChange | bit,
        };
    }

    internal const ulong FirstPlayerRebinding = 1UL << 63;

    internal static bool FirstPlayerMayRebind(ulong state) =>
        (state & FirstPlayerRebinding) != 0;

}
