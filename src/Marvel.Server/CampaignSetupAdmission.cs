using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Rules.Campaign;
using Marvel.Rules.State;
using Marvel.Protocol;

namespace Marvel.Server;

/// <summary>Rejects malformed campaign input before any physical game state is allocated.</summary>
internal static class CampaignSetupAdmission
{
    internal static void Validate(GameSpecification specification, CampaignDefinition? definition,
        CardCatalog cards, IReadOnlySet<string> authored)
    {
        if (specification.Campaign is not { } campaign) return;
        if (definition is null || campaign.Product != definition.Product)
            throw new ArgumentException("the complete requested campaign is not admitted", nameof(specification));
        ValidateStage(specification, campaign, definition);
        if (specification.PlayerDecks is null || campaign.Seats.Length != specification.Heroes.Count
            || !campaign.Seats.Select(seat => seat.Seat).SequenceEqual(Enumerable.Range(0, specification.Heroes.Count)))
            throw new ArgumentException("campaign requires ordered seats and complete explicit decks", nameof(specification));
        ValidateInventory(campaign, definition, cards, authored);
        CampaignSeatAdmission.Validate(campaign, definition);
    }

    private static void ValidateStage(GameSpecification specification, CampaignGameSetup campaign, CampaignDefinition definition)
    {
        int stage = (int)campaign.Stage;
        if (stage < 0 || stage >= definition.Scenarios.Length || campaign.Attempt < 0)
            throw new ArgumentException("invalid campaign stage or attempt", nameof(specification));
        var scenario = definition.Scenarios[stage];
        if (specification.Scenario != scenario.Standard && specification.Scenario != scenario.Expert)
            throw new ArgumentException("campaign stage does not match its scenario", nameof(specification));
    }

    private static void ValidateInventory(CampaignGameSetup campaign, CampaignDefinition definition,
        CardCatalog cards, IReadOnlySet<string> authored)
    {
        var inventory = campaign.Seats.SelectMany(seat => seat.Cards.Concat(seat.AvailableObligations)).ToArray();
        if (inventory.Select(card => card.Key).Distinct(StringComparer.Ordinal).Count() != inventory.Length)
            throw new ArgumentException("campaign physical cards must have distinct keys", nameof(campaign));
        SetupCatalogAdmission.ValidateFaces(inventory.Select(card => card.Face), cards, authored);
        if (inventory.Any(card => !Belongs(card, definition)))
            throw new ArgumentException("campaign inventory contains a card outside its printed role", nameof(campaign));
        ValidateScenarioCarryOver(campaign, definition);
        CampaignDeckExclusions.Validate(campaign.BannedAllyFaces, cards);
        foreach (var seat in campaign.Seats)
        {
            if (seat.RecordedHitPoints is < 0 || seat.RequiresRecovery && seat.AvailableObligations.IsEmpty)
                throw new ArgumentException("campaign seat cannot start without a legal recovery", nameof(campaign));
            if (seat.AvailableObligations.Any(card => card.Role != CampaignCardRole.Obligation
                || !definition.ObligationFaces.Contains(card.Face)))
                throw new ArgumentException("invalid recovery obligation inventory", nameof(campaign));
        }
    }

    private static void ValidateScenarioCarryOver(CampaignGameSetup campaign, CampaignDefinition definition)
    {
        if (campaign.AdditionalThreat < 0 || campaign.ExperimentalWeapons.Any(face => !definition.ExperimentalWeaponFaces.Contains(face))
            || campaign.ExperimentalWeapons.Distinct(StringComparer.Ordinal).Count() != campaign.ExperimentalWeapons.Length)
            throw new ArgumentException("invalid campaign scenario carry-over", nameof(campaign));
    }

    private static bool Belongs(CampaignCard card, CampaignDefinition definition) => card.Role switch
    {
        CampaignCardRole.Tech => definition.Tech.Contains(card),
        CampaignCardRole.Condition => definition.Conditions.Any(condition => condition.Basic.Key == card.Key
            && (condition.Basic.Face == card.Face || condition.ImprovedFace == card.Face)),
        CampaignCardRole.RescuedAlly => definition.CaptiveAllyFaces.Contains(card.Face)
            && card.Key == "captive:" + card.Face,
        CampaignCardRole.Obligation => definition.ObligationFaces.Contains(card.Face),
        _ => false,
    };
}
