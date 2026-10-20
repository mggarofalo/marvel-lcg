namespace Marvel.Rules.Timing;

/// <summary>The printed class of attacks to which a keyword grant applies.</summary>
public abstract record AttackKeywordRestriction
{
    /// <summary>Only a character's basic ATK power.</summary>
    public sealed record Basic : AttackKeywordRestriction;
    /// <summary>Only attacks made by the card carrying the grant.</summary>
    public sealed record Source : AttackKeywordRestriction;
    /// <summary>Only attacks whose source has the named trait.</summary>
    public sealed record SourceTrait(string Trait) : AttackKeywordRestriction;
}
