using System.Collections.Immutable;
using Marvel.Rules.State;

namespace Marvel.Cards.Dsl;

/// <summary>A checked relation selecting cards without executing authored code.</summary>
public abstract record AbilityCardSelection
{
    private AbilityCardSelection() { }

    /// <summary>A card bound by the current resolution.</summary>
    public sealed record Bound(AbilityCardBinding Binding) : AbilityCardSelection;
    /// <summary>A named engine query.</summary>
    public sealed record Query(AbilityCardQuery Kind) : AbilityCardSelection;
    /// <summary>A printed-title reference.</summary>
    public sealed record Titled(string Title) : AbilityCardSelection;
    /// <summary>Enemies carrying a named trait.</summary>
    public sealed record EnemiesWithTrait(string Trait) : AbilityCardSelection;
    /// <summary>Selected cards carrying a named trait.</summary>
    public sealed record WithTrait(AbilityCardSelection Cards, string Trait) : AbilityCardSelection;
    /// <summary>Cards without another attached copy of the source.</summary>
    public sealed record WithoutAnotherCopyAttached(AbilityCardSelection Cards) : AbilityCardSelection;
    /// <summary>Selected cards removable by the effect.</summary>
    public sealed record Discardable(AbilityCardSelection Cards) : AbilityCardSelection;
    /// <summary>All selected cards tied at the requested rank extreme.</summary>
    public sealed record Ranked(AbilityCardSelection Cards, AbilityCardRank By, bool Maximum) : AbilityCardSelection;
    /// <summary>Cards matching printed criteria in an ordered collection of areas.</summary>
    public sealed record InAreas(ImmutableArray<AbilitySearchArea> Areas, CardKind? Kind,
        string? Trait, string? Title) : AbilityCardSelection;
}
