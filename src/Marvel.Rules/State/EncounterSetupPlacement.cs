namespace Marvel.Rules.State;

/// <summary>Places the pinned four-villain setup without changing physical creation order.</summary>
internal static class EncounterSetupPlacement
{
    internal static void Place(World world, EncounterSetupPlan plan)
    {
        var groups = new List<EncounterDeckGroup>();
        foreach (var recipe in plan.Groups)
        {
            var stages = recipe.Stages.Select(id => world.Cards[id]).ToArray();
            var scheme = world.Cards[recipe.Scheme];
            World.MoveToTop(stages[0], world.AreaOf(DeckType.VillainArea));
            World.MoveToTop(scheme, world.AreaOf(DeckType.SideSchemesArea));
            scheme.PlaceTokens("k_threat", world.Facts.PrintedValue(scheme.FaceId, "StartingThreat", world.Players));
            var deck = groups.Count == 0 ? world.AreaOf(DeckType.EncounterDeck)
                : world.CreateArea(DeckType.EncounterDeck);
            var discard = world.CreateArea(DeckType.EncounterDiscardPile);
            var members = recipe.Encounters.Select(id => world.Cards[id]).ToArray();
            foreach (var card in members)
                World.MoveToTop(card, deck);
            groups.Add(new EncounterDeckGroup(stages[0], scheme, deck, discard, members, stages));
        }
        world.BindEncounterTopology(new EncounterTopology(world, groups, world.Cards[plan.ActiveVillain]));
    }
}
