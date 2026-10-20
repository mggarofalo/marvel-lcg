using Marvel.Rules.Campaign;
using Marvel.Rules.Events;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Runs rulebook campaign setup after scenario setup and before opening hands.</summary>
internal static class CampaignSetupProcedure
{
    internal static void Enqueue(World world, List<GameEvent> events)
    {
        if (world.Campaign is not { } campaign) return;
        CampaignGameSetup setup = campaign.Setup;
        ShuffleWeapons(world, setup, events);
        PlaceThreat(world, setup, events);
        if (world.IsOver || !setup.ExpertCampaign || setup.Stage == CampaignStage.Crossbones) return;
        // rr:appendix-ii-setup.step.13: resolve campaign Setup instructions.
        // pack:mc10:absorbing-man-campaign-instructions#setup: restore recorded HP,
        // then each player may add one random obligation to heal to full.
        foreach (int player in world.PlayerOrder)
        {
            var seat = setup.Seats.Single(value => value.Seat == player);
            // ruling:1516351f87fc4382: restore recorded base HP plus the awarded
            // Condition's printed bonus; unrelated maximum-HP effects are not carried HP.
            if (seat.RecordedHitPoints is { } hp) CampaignRecoveryProcedure.SetHitPoints(
                world, player, (long)hp + seat.ConditionHitPointBonus, events);
            world.Agenda.Add(new PhaseStep(new CampaignRecoveryOperation(), 0, 13, player,
                Subject: world.Seats[player].IdentityCard.ObjectId, Seat: player, Plan: true));
        }
        foreach (var seat in setup.Seats.OrderBy(value => value.Seat).Where(value => value.DealEncounterCard))
            world.Agenda.Add(new PhaseStep(new CampaignEncounterDealOperation(), 0, 13, seat.Seat,
                Seat: seat.Seat, Plan: true));
    }

    private static void ShuffleWeapons(World world, CampaignGameSetup setup, List<GameEvent> events)
    {
        if (setup.ExperimentalWeapons.Length == 0) return;
        Area deck = world.AreaOf(DeckType.EncounterDeck);
        foreach (string face in setup.ExperimentalWeapons)
        {
            Card card = world.Cards.Single(value => value.FaceId == face && value.Area.Type == DeckType.AsideDeck);
            CampaignRecoveryProcedure.Move(card, deck, "CampaignSetup", events);
        }
        world.Shuffle(deck);
        events.Add(new AreaReordered(Places.Reference(deck), deck.Cards.Select(card => card.ObjectId).ToArray())
            { Trigger = "CampaignSetup", Verb = "Shuffle" });
    }

    private static void PlaceThreat(World world, CampaignGameSetup setup, List<GameEvent> events)
    {
        if (setup.AdditionalThreat == 0) return;
        Card scheme = world.TheCardIn(DeckType.MainSchemesArea)
            ?? throw new RulesNotImplementedException("campaign threat requires a main scheme");
        Threat.Place(world, world.Facts, world.ThreatAbilities, scheme, setup.AdditionalThreat,
            "CampaignSetup", events);
    }
}
