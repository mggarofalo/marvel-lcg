using Marvel.Rules.Campaign;
using Marvel.Rules.Setup;
using Marvel.Rules.State;

namespace Marvel.Server;

/// <summary>Joins validated campaign overlays to the admitted scenario's physical deal.</summary>
internal static class CampaignBlueprints
{
    internal static CampaignDealPlan From(
        IReadOnlyList<CardBlueprint> ordinary, CampaignGameSetup campaign, CampaignDefinition definition)
    {
        var cards = ordinary.ToList();
        var bindings = new List<CampaignCardBinding>();
        // Engine allocation choice: ordinary ids retain their order; campaign
        // additions follow in fixed seat/inventory order, before any RNG draws.
        foreach (var seat in campaign.Seats.OrderBy(value => value.Seat))
        {
            foreach (var card in seat.Cards)
                Add(card, seat.Seat, SetupSlot.PlayerDeck);
            foreach (var obligation in seat.AvailableObligations)
                Add(obligation, seat.Seat, SetupSlot.SetAside);
        }

        foreach (string weapon in campaign.ExperimentalWeapons)
        {
            if (!cards.Any(card => card.Spec.Split(',').Contains(weapon, StringComparer.Ordinal)))
                cards.Add(new CardBlueprint(weapon, SetupSlot.SetAside, -1));
        }

        for (int id = 0; id < ordinary.Count; id++)
        {
            string face = ordinary[id].Spec;
            if (ordinary[id].Seat == -1 && definition.CaptiveAllyFaces.Contains(face))
                bindings.Add(new CampaignCardBinding(id, -1,
                    new CampaignCard("captive:" + face, face, CampaignCardRole.RescuedAlly)));
        }
        return new CampaignDealPlan(cards, bindings,
            [.. cards.Select((card, id) => (card, id))
                .Where(pair => definition.ExperimentalWeaponFaces.Contains(pair.card.Spec))
                .Select(pair => pair.id)]);

        void Add(CampaignCard card, int seat, SetupSlot slot)
        {
            bindings.Add(new CampaignCardBinding(cards.Count, seat, card));
            cards.Add(new CardBlueprint(card.Face, slot, seat));
        }
    }
}
