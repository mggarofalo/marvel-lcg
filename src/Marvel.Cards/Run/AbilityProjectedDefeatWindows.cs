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

    private static bool MissingAuthoredDefeat(Card subject, AbilityAdmissionScope cast) =>
        cast.World.Facts.HasWhenDefeated(subject.FaceId)
        && !AbilityProgramQueries.On(cast.Context.Program, subject).Any(ability =>
            ability.Trigger.Timing == AbilityType.WhenDefeated);
}
