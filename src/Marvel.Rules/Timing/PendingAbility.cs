namespace Marvel.Rules.Timing;

/// <summary>One ability waiting in a window.</summary>
/// <param name="Card">The object id of the card carrying it.</param>
/// <param name="Type">Its bold timing trigger.</param>
/// <param name="Player">
/// The seat that would resolve it, or <c>-1</c> for an ability on an encounter
/// card that no player has claimed. <c>rr:ability.8</c> lets any player use an
/// optional ability on an encounter card, so who resolves it is settled when it
/// is offered, not when it is collected.
/// </param>
/// <param name="Ordinal">
/// Which ability at this timing on the card is waiting, in printed/data order.
/// Most cards have one and therefore use zero.
/// </param>
public readonly record struct PendingAbility(
    int Card, AbilityType Type, int Player, int Ordinal = 0)
{
    /// <summary>Engine-owned identity; authored card abilities retain their ordinary ordinal.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public NativeAbilityKind Native { get; init; }

    /// <summary>Resolve before ordinary forced interrupts to the same condition.</summary>
    public bool Would { get; init; }

    /// <summary>The exact defeat that satisfies this trigger within its shared window.</summary>
    public State.Defeated? TriggeringDefeat { get; init; }

    /// <summary>The exact imminent defeat answered by this prompt-local offer.</summary>
    public int? TriggeringWouldDefeat { get; init; }

    /// <summary>The exact attacked enemy answered within a shared attack window.</summary>
    public State.Attacked? TriggeringAttack { get; init; }

    /// <summary>The exact committed threat placement answered in a shared window.</summary>
    [System.Text.Json.Serialization.JsonIgnore]
    public Occurrence? TriggeringThreat { get; init; }

    /// <summary>Prompt-local handle assigned when several instances share a source card.</summary>
    public int? OfferId { get; init; }

    /// <summary>Event-play generation within the containing triggering occurrence.</summary>
    [System.Text.Json.Serialization.JsonIgnore(Condition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingDefault)]
    public int EventPlay { get; init; }
}
