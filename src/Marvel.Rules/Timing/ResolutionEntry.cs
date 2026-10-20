using System.Text.Json.Serialization;

namespace Marvel.Rules.Timing;

/// <summary>Resolution state for a card or one exact ability on it.</summary>
/// <param name="Card">The source card's object id.</param>
/// <param name="Ability">The ability type, or null when this entry is for the card.</param>
/// <param name="Ordinal">The same-type ability ordinal, or -1 for a card entry.</param>
/// <param name="Status">Its current rule-defined status.</param>
/// <param name="Applied">Whether at least one child effect has applied so far.</param>
public sealed record ResolutionEntry(
    int Card, AbilityType? Ability, int Ordinal, ResolutionStatus Status,
    bool Applied = false)
{
    /// <summary>Engine-owned identity; authored card abilities retain their ordinary ordinal.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public NativeAbilityKind Native { get; init; }

    /// <summary>The particular defeat answered by this resolution.</summary>
    public State.Defeated? TriggeringDefeat { get; init; }

    /// <summary>The exact attacked enemy answered within a shared attack window.</summary>
    public State.Attacked? TriggeringAttack { get; init; }
}
