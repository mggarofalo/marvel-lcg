namespace Marvel.Cards.Dsl;

/// <summary>The damage quantity an authored interrupt observes.</summary>
public enum DamageTriggerCondition
{
    /// <summary>Damage dealt, unchanged by recipient prevention (rr:prevent.1.1).</summary>
    Dealt,

    /// <summary>Damage the recipient would take after pending prevention.</summary>
    Taken,
}
