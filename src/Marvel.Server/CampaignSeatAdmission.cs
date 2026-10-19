using Marvel.Rules.Campaign;

namespace Marvel.Server;

/// <summary>Validates campaign-owned physical inventories and recovery facts before dealing.</summary>
internal static class CampaignSeatAdmission
{
    internal static void Validate(CampaignGameSetup setup, CampaignDefinition definition)
    {
        int previous = -1;
        foreach (var seat in setup.Seats)
        {
            int rosterSeat = seat.CampaignSeat ?? seat.Seat;
            if (rosterSeat <= previous || rosterSeat > 3)
                throw new ArgumentException("campaign roster seats must be distinct, ordered and in range", nameof(setup));
            previous = rosterSeat;
            ValidateCondition(seat, definition);
            ValidateObligations(seat, definition);
            ValidateRecovery(setup, seat);
        }
    }

    private static void ValidateRecovery(CampaignGameSetup setup, CampaignSeatSetup seat)
    {
        bool carriesHealth = setup.ExpertCampaign && setup.Stage != CampaignStage.Crossbones;
        if (carriesHealth && (seat.RecordedHitPoints is null || seat.RequiresRecovery != (seat.RecordedHitPoints == 0)))
            throw new ArgumentException("recorded health and required recovery disagree", nameof(setup));
        if (!carriesHealth && (seat.RecordedHitPoints is not null || seat.RequiresRecovery))
            throw new ArgumentException("this attempt does not carry expert health", nameof(setup));
    }

    private static void ValidateCondition(CampaignSeatSetup seat, CampaignDefinition definition)
    {
        var conditions = seat.Cards.Where(card => card.Role == CampaignCardRole.Condition).ToArray();
        if (conditions.Length > 1)
            throw new ArgumentException("a campaign player cannot own multiple Conditions", nameof(seat));
        int bonus = conditions.Length == 0 ? 0 : definition.Conditions.Single(condition =>
            condition.Basic.Key == conditions[0].Key).HitPointBonus;
        if (seat.ConditionHitPointBonus != bonus)
            throw new ArgumentException("campaign health bonus differs from the owned Condition", nameof(seat));
    }

    private static void ValidateObligations(CampaignSeatSetup seat, CampaignDefinition definition)
    {
        var obligations = seat.Cards.Where(card => card.Role == CampaignCardRole.Obligation)
            .Concat(seat.AvailableObligations).Select(card => card.Face).ToArray();
        if (obligations.Length != definition.ObligationFaces.Length
            || obligations.Distinct(StringComparer.Ordinal).Count() != obligations.Length)
            throw new ArgumentException("each seat requires one physical copy of every campaign obligation", nameof(seat));
    }
}
