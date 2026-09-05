using System.Collections.Immutable;

namespace Marvel.Cards.Dsl;

/// <summary>A numeric expression whose structure has been checked before evaluation.</summary>
/// <remarks>
/// These types describe reusable language operations, not cards. Authored JSON
/// remains inert data; adding a card composes the operations the engine implements.
/// </remarks>
public abstract record AbilityNumber
{
    private AbilityNumber() { }

    /// <summary>A printed integer.</summary>
    public sealed record Constant(long Value) : AbilityNumber;

    /// <summary>A printed integer multiplied by the game's original player count.</summary>
    public sealed record PerPlayer(long Value) : AbilityNumber;

    /// <summary>A result recorded earlier in the same ability resolution.</summary>
    public sealed record Result(string Name) : AbilityNumber;

    /// <summary>The sum of the ordered operands; an empty sum is zero.</summary>
    public sealed record Sum(ImmutableArray<AbilityNumber> Operands) : AbilityNumber;

    /// <summary>The product of the ordered operands; an empty product is one.</summary>
    public sealed record Product(ImmutableArray<AbilityNumber> Operands) : AbilityNumber;

    /// <summary>The least of a nonempty ordered collection of operands.</summary>
    public sealed record Minimum(ImmutableArray<AbilityNumber> Operands) : AbilityNumber;

    /// <summary>A value read from a selected card.</summary>
    public sealed record CardValue(AbilityCardSelection Card, AbilityCardNumberProperty Property) : AbilityNumber;
    /// <summary>A named counter pool on a selected card.</summary>
    public sealed record Counters(AbilityCardSelection Card, string Counter) : AbilityNumber;
    /// <summary>A modified engine field on a selected card.</summary>
    public sealed record Modified(AbilityCardSelection Card, string Field) : AbilityNumber;
    /// <summary>The number of selected cards.</summary>
    public sealed record Count(AbilityCardSelection Cards) : AbilityNumber;
    /// <summary>A conditional number with both branches validated.</summary>
    public sealed record Conditional(AbilityCondition Test, AbilityNumber Then, AbilityNumber Else) : AbilityNumber;
    /// <summary>The number of printed resource icons on cards discarded this way.</summary>
    public sealed record PrintedResourcesDiscarded(char Resource) : AbilityNumber;
    /// <summary>The number of discarded cards generating the named resource.</summary>
    public sealed record DiscardedWithResource(char Resource) : AbilityNumber;
    /// <summary>A numeric value supplied directly by the resolution.</summary>
    public sealed record ResolutionValue(AbilityResolutionNumber Kind) : AbilityNumber;
}

/// <summary>Card values that numeric expressions can read.</summary>
public enum AbilityCardNumberProperty
{
    /// <summary>Threat tokens.</summary>
    Threat,
    /// <summary>Damage tokens.</summary>
    Damage,
    /// <summary>Modified health less damage, bounded below by zero.</summary>
    RemainingHealth,
    /// <summary>The identity's printed starting health.</summary>
    StartingHealth,
}

/// <summary>Resolution-owned numeric bindings with fixed authored arguments.</summary>
public enum AbilityResolutionNumber
{
    /// <summary>The paid or selected amount for a basic power.</summary>
    PowerAmount,
    /// <summary>Printed boost icons on all cards discarded this way.</summary>
    PrintedBoostIconsDiscarded,
    /// <summary>One plus printed boost icons on the last card discarded this way.</summary>
    TopEncounterDiscardBoostPlusOne,
}
