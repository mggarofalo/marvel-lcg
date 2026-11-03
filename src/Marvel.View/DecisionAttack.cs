namespace Marvel.View;

/// <summary>The enemy attack a decision is taken within, named for the viewer.</summary>
/// <param name="Attacker">The attacking enemy's readable title.</param>
/// <param name="Target">The attacked character's readable title.</param>
/// <param name="Value">The enemy's current ATK, or the calculated damage when <paramref name="Calculated"/>.</param>
/// <param name="Calculated">Whether <paramref name="Value"/> is the attack's calculated damage.</param>
/// <param name="BoostUnresolved">Whether boost icons may still add to the attack.</param>
/// <param name="Prevented">Whether all damage from the attack is prevented.</param>
public sealed record DecisionAttack(
    string Attacker, string Target, long Value, bool Calculated, bool BoostUnresolved, bool Prevented)
{
    /// <summary>The uncancelled icons on a boost card just turned faceup, or null when none is resolving.</summary>
    public long? BoostIcons { get; init; }

    /// <summary>The boost cards still facedown on the attacker while a boost card is faceup, or null when none is resolving.</summary>
    public long? BoostFacedown { get; init; }
}
