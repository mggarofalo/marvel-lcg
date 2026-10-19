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
                cast.Power ?? "Deal_Damage") { Incarnation = subject.Incarnation, Kind = EffectiveCards.Kind(subject, cast.World.Facts) });
        return AbilityWindowAdmission.WaitingCards(
                cast.Context.Program, cast.World, occurrence,
                WindowKind.Interrupt, cast.Context.ResourceAbilities)
            .Any(card => !state.Discarded.Contains(card));
    }

    internal static bool CanProjectEncounterDeal(AbilityPowerState state, Card subject, AbilityAdmissionScope cast)
    {
        if (MissingAuthoredDefeat(subject, cast)) return false;
        var occurrence = new Occurrence(0, [Steps.CardDefeated], Subject: subject.ObjectId, Player: subject.Owner);
        occurrence.Also(new Defeated(subject.ObjectId,
            AbilityEffectAttribution.IdentityPlayer(cast.World, cast.Source, cast.Player), cast.Power ?? "Deal_Damage")
            { Incarnation = subject.Incarnation, Kind = EffectiveCards.Kind(subject, cast.World.Facts) });
        var candidates = AbilityWindowAdmission.Waiting(cast.Context.Program, cast.World, occurrence,
            WindowKind.Interrupt, cast.Context.ResourceAbilities).Where(candidate => !state.Discarded.Contains(candidate.Card.ObjectId)).ToArray();
        // Facedown encounter dealing cannot alter the character/status projection. Its pile changes remain unknown.
        return candidates.Length > 0 && candidates.All(candidate => candidate.Card == subject
            && candidate.Ability.Trigger.Timing == AbilityType.WhenDefeated
            && candidate.Ability.When is null && candidate.Ability.Cost is null
            && candidate.Ability.Effect is AbilityEffect.DealEncounterCards);
    }

    internal static void ValidateContinuation(AbilityEffect node, AbilityPowerState state, AbilityAdmissionScope cast)
    {
        if (!state.EncounterCardsChanged) return;
        if (!AbilityEncounterDealProjection.PreservesReads(node))
            throw new RulesNotImplementedException("A labelled-power continuation has an unprojected read after a defeat-triggered encounter deal.");
    }

    private static bool MissingAuthoredDefeat(Card subject, AbilityAdmissionScope cast) =>
        cast.World.Facts.HasWhenDefeated(subject.FaceId)
        && !AbilityProgramQueries.On(cast.Context.Program, subject).Any(ability =>
            ability.Trigger.Timing == AbilityType.WhenDefeated);
}
