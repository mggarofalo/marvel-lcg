namespace Marvel.Rules.State;

/// <summary>Why threat is about to be placed on a scheme.</summary>
public enum ThreatCause
{
    /// <summary>Step one of the villain phase.</summary>
    VillainPhase,

    /// <summary>Step three of an enemy's scheme activation.</summary>
    EnemyScheme,

    /// <summary>The incite keyword.</summary>
    Incite,

    /// <summary>A card ability.</summary>
    CardAbility,
}
