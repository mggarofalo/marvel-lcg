namespace Marvel.Rules.Prompts;

/// <summary>The enemy attack a decision is taken within, as facts a client can show without the engine's sentences.</summary>
/// <param name="Attacker">The attacking enemy's object id.</param>
/// <param name="Target">The attacked character's object id.</param>
/// <param name="Value">The enemy's current ATK, or the attack's damage once it has been calculated.</param>
/// <param name="Calculated">Whether <paramref name="Value"/> is the calculated damage rather than ATK.</param>
/// <param name="BoostUnresolved">Whether boost icons may still add to the attack (rr:boost-boost-icon).</param>
/// <param name="Prevented">Whether an ability prevents all damage from this attack, so <paramref name="Value"/> will not be dealt.</param>
public sealed record PromptAttack(int Attacker, int Target, long Value, bool Calculated, bool BoostUnresolved, bool Prevented);
