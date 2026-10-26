namespace Marvel.Rules.Timing;

/// <summary>Engine-owned ability identity, separate from a card's authored ordinal.</summary>
public enum NativeAbilityKind
{
    /// <summary>An ability supplied by the card interpreter.</summary>
    Authored,
    /// <summary>The forced response defined by the retaliate keyword.</summary>
    Retaliate,
    /// <summary>The printed Risky Business villain activation replacement.</summary>
    GreenGoblinActivationReplacement,
    /// <summary>The printed Norman Osborn damage replacement.</summary>
    GreenGoblinDamageReplacement,
}
