using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using static Marvel.Cards.Run.AbilityAdmission;

namespace Marvel.Cards.Run;

/// <summary>Detects defeat windows that a projected power cannot execute speculatively.</summary>
internal static class AbilityProjectedDefeatWindows
{
    internal static bool HasWork(AbilityPowerState state, Card subject,
        AbilityAdmissionScope cast, bool would = false)
    {
        if (!would && MissingAuthoredDefeat(subject, cast)) return true;
        string condition = would ? Steps.CardWouldBeDefeated : Steps.CardDefeated;
        var occurrence = new Occurrence(
            0, [condition], Subject: subject.ObjectId, Player: subject.Owner);
        if (!would)
            occurrence.Also(new Defeated(subject.ObjectId,
                AbilityEffectAttribution.IdentityPlayer(cast.World, cast.Source, cast.Player),
                cast.Power ?? "Deal_Damage") { Incarnation = subject.Incarnation, Kind = EffectiveCards.Kind(subject, cast.World.Facts),
                ResponsiblePlayer = EffectiveCards.Kind(subject, cast.World.Facts) is CardKind.EncounterSideScheme or CardKind.PlayerSideScheme
                    ? AbilityEffectAttribution.ResponsiblePlayer(cast.World, cast.Source, cast.Player)
                    : AbilityEffectAttribution.IdentityPlayer(cast.World, cast.Source, cast.Player) });
        return AbilityWindowAdmission.WaitingCards(
                cast.Context.Program, cast.World, occurrence,
                WindowKind.Interrupt, cast.Context.ResourceAbilities)
            .Any(card => !state.Discarded.Contains(card));
    }

    internal static bool CanProjectEncounterDeal(AbilityPowerState state, Card subject, AbilityAdmissionScope cast)
    {
        if (MissingAuthoredDefeat(subject, cast)) return false;
        var candidates = DefeatCandidates(state, subject, cast);
        // Facedown encounter dealing cannot alter the character/status projection. Its pile changes remain unknown.
        return candidates.Length > 0 && candidates.All(candidate => candidate.Card == subject
            && candidate.Ability.Trigger.Timing == AbilityType.WhenDefeated
            && candidate.Ability.When is null && candidate.Ability.Cost is null
            && candidate.Ability.Effect is AbilityEffect.DealEncounterCards);
    }

    internal static bool CanDeferDefeat(AbilityPowerState state, Card subject, AbilityAdmissionScope cast)
    {
        if (MissingAuthoredDefeat(subject, cast)) return false;
        var candidates = DefeatCandidates(state, subject, cast);
        // The live defeat procedure owns both the dying card and its attached
        // forced interrupts. Their unknown board changes stay behind the same
        // continuation and damage-batch read guards. rr:when-defeated-abilities.1:
        // "The 'When Defeated' timing trigger is equivalent to the following trigger:
        // 'Forced Interrupt: When this card is defeated...'", so the dying card's own
        // forced interrupt on its defeat joins the same window.
        return candidates.Length > 0 && candidates.All(candidate =>
            candidate.Ability.When is null && candidate.Ability.Cost is null
            && (candidate.Card == subject && candidate.Ability.Trigger.Timing
                    is AbilityType.WhenDefeated or AbilityType.ForcedInterrupt
                || candidate.Card.Area.Host == subject.ObjectId
                && candidate.Ability.Trigger.Timing == AbilityType.ForcedInterrupt));
    }

    private static AbilityWindowAdmission.Candidate[] DefeatCandidates(
        AbilityPowerState state, Card subject, AbilityAdmissionScope cast)
    {
        var occurrence = new Occurrence(0, [Steps.CardDefeated], Subject: subject.ObjectId, Player: subject.Owner);
        occurrence.Also(new Defeated(subject.ObjectId,
            AbilityEffectAttribution.IdentityPlayer(cast.World, cast.Source, cast.Player), cast.Power ?? "Deal_Damage")
            { Incarnation = subject.Incarnation, Kind = EffectiveCards.Kind(subject, cast.World.Facts),
                ResponsiblePlayer = EffectiveCards.Kind(subject, cast.World.Facts) is CardKind.EncounterSideScheme or CardKind.PlayerSideScheme
                    ? AbilityEffectAttribution.ResponsiblePlayer(cast.World, cast.Source, cast.Player)
                    : AbilityEffectAttribution.IdentityPlayer(cast.World, cast.Source, cast.Player) });
        return AbilityWindowAdmission.Waiting(cast.Context.Program, cast.World, occurrence,
            WindowKind.Interrupt, cast.Context.ResourceAbilities).Where(candidate => !state.Discarded.Contains(candidate.Card.ObjectId)).ToArray();
    }

    internal static void ValidateContinuation(AbilityEffect node, AbilityPowerState state, AbilityAdmissionScope cast)
    {
        if (AbilityPowerContinuationDependencies.IsIndependent(node)) return;
        if (state.UnprojectedStageEntry is { } reason
            && !AbilityStageEntryDependencies.PreservesReads(node, state, cast))
            throw new RulesNotImplementedException(reason);
        bool live = AbilityLiveThreatRemoval.Resolves(node);
        if (!live && DeferredWindowHidesReads(node, state, cast))
            throw new RulesNotImplementedException($"A labelled-power continuation reads the board after an {DeferredWindowDescription(state)}.");
        if (!live && EncounterDealHidesReads(node, state))
            throw new RulesNotImplementedException("A labelled-power continuation has an unprojected read after a defeat-triggered encounter deal.");
    }

    /// <summary>Whether <paramref name="node"/> is a removal the projection leaves to the live board.</summary>
    /// <remarks>The board after such a removal is not projected; a later read of it is unsupported.</remarks>
    internal static bool LeavesToLiveBoard(AbilityEffect node, AbilityPowerState state, AbilityAdmissionScope cast) =>
        !AbilityPowerContinuationDependencies.IsIndependent(node) && AbilityLiveThreatRemoval.Resolves(node)
        && (DeferredWindowHidesReads(node, state, cast) || EncounterDealHidesReads(node, state));

    private static bool DeferredWindowHidesReads(AbilityEffect node, AbilityPowerState state, AbilityAdmissionScope cast) =>
        state.DefeatWindowDeferred && !AbilityDefeatReceiptProjection.PreservesReads(node, state, cast)
        && !(state.RecoveryWindowDeferred && AbilityProjectedRecoveryWindow.PreservesReads(node, state, cast));

    private static bool EncounterDealHidesReads(AbilityEffect node, AbilityPowerState state) =>
        state.EncounterCardsChanged && !AbilityEncounterDealProjection.PreservesReads(node);

    internal static string DeferredWindowDescription(AbilityPowerState state) =>
        state.RecoveryWindowDeferred ? "unprojected defeat window (step-6 interrupt)" : "unprojected defeat window";

    private static bool MissingAuthoredDefeat(Card subject, AbilityAdmissionScope cast) =>
        !PrintedText.IsBlanked(cast.World, subject)
        && cast.World.Facts.HasWhenDefeated(subject.FaceId)
        && !cast.Context.Program.On(subject.FaceId).Any(ability =>
            ability.Trigger.Timing == AbilityType.WhenDefeated);
}
