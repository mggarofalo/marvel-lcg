namespace Marvel.Rules.Prompts;

/// <summary>The enemy attack a decision is taken within, as facts a client can show without the engine's sentences.</summary>
/// <param name="Attacker">The attacking enemy's object id.</param>
/// <param name="Target">The attacked character's object id.</param>
/// <param name="Value">The enemy's current ATK, or the attack's damage once it has been calculated.</param>
/// <param name="Calculated">Whether <paramref name="Value"/> is the calculated damage rather than ATK.</param>
/// <param name="BoostUnresolved">Whether boost icons may still add to the attack (rr:boost-boost-icon).</param>
/// <param name="Prevented">Whether an ability prevents all damage from this attack, so <paramref name="Value"/> will not be dealt.</param>
public sealed record PromptAttack(int Attacker, int Target, long Value, bool Calculated, bool BoostUnresolved, bool Prevented)
{
    /// <summary>
    /// The uncancelled boost icons on the boost card just turned faceup, while the interrupt to that reveal is
    /// open (rr:boost-boost-icon); null otherwise. Boost abilities and later effects can still change them.
    /// </summary>
    public long? BoostIcons { get; init; }

    /// <summary>
    /// The boost cards still facedown on the attacking enemy, present exactly when <see cref="BoostIcons"/> is; they
    /// resolve after the faceup card and can add to the attack (rr:attack-enemy-activation.step.3.e: "If the enemy has any boost cards remaining, repeat these steps with the next boost card.").
    /// </summary>
    public long? BoostFacedown { get; init; }
}
