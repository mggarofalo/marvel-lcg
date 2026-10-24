namespace Marvel.Rules.State;

/// <summary>Pinned Wrecking Crew component order resolved by the content dealer before allocation.</summary>
public sealed record EncounterSetupPlan(IReadOnlyList<EncounterGroupSetup> Groups, int ActiveVillain)
{
    /// <summary>Checks physical component ownership before allocation or publication of a recipe.</summary>
    public void Validate(IReadOnlyList<CardBlueprint> cards, ICardFacts facts) =>
        EncounterSetupValidation.Validate(this, cards, facts);
}
