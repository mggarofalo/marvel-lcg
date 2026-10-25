namespace Marvel.Rules.State;

/// <summary>Rejects incomplete Wrecking Crew ownership before allocating or executing a game.</summary>
internal static class EncounterSetupValidation
{
    internal static void Validate(EncounterSetupPlan plan, IReadOnlyList<CardBlueprint> cards, ICardFacts facts)
    {
        // pack:mc03:additional-setup: four villains, each with its own 15-card deck;
        // "Nemesis cards and obligations are not used when playing this scenario."
        if (plan.Groups.Count != 4 || cards.Any(card => card.Slot is SetupSlot.Obligation or SetupSlot.Nemesis))
            throw new ArgumentException("Wrecking Crew requires four groups and excludes nemesis cards and obligations");
        var owned = new HashSet<int>();
        foreach (var group in plan.Groups) ValidateGroup(group, cards, facts, owned);
        int twoStageGroups = plan.Groups.Count(group => group.Stages.Count == 2);
        if (twoStageGroups is not (0 or 4))
            throw new ArgumentException("Extreme mode requires both stages for all four villains");
        if (!plan.Groups.Any(group => group.Stages[0] == plan.ActiveVillain))
            throw new ArgumentException("initial active villain must be a group's initial stage");
        for (int id = 0; id < cards.Count; id++)
            if (cards[id].Slot is SetupSlot.Villain or SetupSlot.Encounter && !owned.Contains(id))
                throw new ArgumentException($"scenario card {id} has no encounter group");
    }

    private static void ValidateGroup(EncounterGroupSetup group, IReadOnlyList<CardBlueprint> cards,
        ICardFacts facts, HashSet<int> owned)
    {
        if (group.Stages.Count is < 1 or > 2 || group.Encounters.Count != 15)
            throw new ArgumentException("each Wrecking group requires one or two stages and fifteen encounter cards");
        foreach (int stage in group.Stages)
        {
            var card = Require(stage, cards, owned);
            if (card.Slot != SetupSlot.Villain || Kind(card, facts) != CardKind.EncounterVillain)
                throw new ArgumentException("villain stage IDs must identify villain blueprints");
        }
        var scheme = Require(group.Scheme, cards, owned);
        if (scheme.Slot is not (SetupSlot.SetAside or SetupSlot.Encounter)
            || Kind(scheme, facts) != CardKind.EncounterSideScheme)
            throw new ArgumentException("paired scheme must identify a scenario side-scheme blueprint");
        foreach (int id in group.Encounters)
        {
            var card = Require(id, cards, owned);
            if (card.Slot != SetupSlot.Encounter || Kind(card, facts) is not
                (CardKind.Minion or CardKind.Treachery or CardKind.Attachment
                    or CardKind.EncounterSideScheme or CardKind.Environment))
                throw new ArgumentException("encounter membership must identify encounter blueprints");
        }
    }

    private static CardBlueprint Require(int id, IReadOnlyList<CardBlueprint> cards, HashSet<int> owned)
    {
        if (id < 0 || id >= cards.Count || !owned.Add(id))
            throw new ArgumentException("scenario component IDs must be in range and owned by exactly one group");
        return cards[id];
    }

    private static CardKind Kind(CardBlueprint card, ICardFacts facts) =>
        facts.Kind(card.Spec.Split(',')[0]);
}
