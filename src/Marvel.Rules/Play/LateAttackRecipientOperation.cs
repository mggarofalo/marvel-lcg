namespace Marvel.Rules.Play;

/// <summary>Announces one enemy a begun player attack reaches after its initiation.</summary>
/// <remarks>
/// rr:attack-player-ability-type.5: the attacker "is considered to have attacked
/// each of those enemies". Ruling 310af249be272877: an enemy hit by a later
/// sentence of the attack is attacked then. The step's occurrence is the same
/// attack-initiated condition <see cref="Steps.CharacterAttacks"/> creates, with
/// that enemy as its target, so its interrupts resolve before the damage
/// (rr:interrupt.3). The step name is an engine choice.
/// </remarks>
/// <param name="Attacker">The attacking character's object id.</param>
/// <param name="Recipient">The enemy now attacked.</param>
/// <param name="Player">The attacking player's seat.</param>
public sealed record LateAttackRecipientOperation(int Attacker, int Recipient, int Player) : AgendaOperation
{
    /// <summary>The step name.</summary>
    public const string Step = "LateAttackRecipient";

    /// <inheritdoc/>
    public override string What => Step;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Attack;
}
