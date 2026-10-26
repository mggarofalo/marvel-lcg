using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>One faceup boost card, before its star ability resolves.</summary>
public sealed record BoostFaceupOperation(int Enemy, bool Attacking, BoostIconCancellation Icons) : AgendaOperation
{
    /// <summary>Engine vocabulary for a single physical boost faceup occurrence.</summary>
    public const string Faceup = "WhenBoostCardTurnedFaceup";
    /// <summary>Engine vocabulary for that same occurrence during its own attack activation.</summary>
    public const string AttackFaceup = "WhenAttackBoostCardTurnedFaceup";
    /// <inheritdoc/>
    public override string What => Faceup;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Attack;
    /// <summary>The independently cancellable star ability.</summary>
    public BoostAbilityCancellation? Ability { get; init; }
    internal Occurrence Occurrence(int id, World world, int subject, int player)
    {
        var enemy = world.Cards[Enemy];
        // Engine choice: one occurrence carries both descriptions. The qualifier
        // belongs to this boost operation, even while another attack is suspended.
        var occurrence = new Occurrence(id, Attacking ? [Faceup, AttackFaceup] : [Faceup], subject, player, Enemy, subject,
            OccurrenceCard.Capture(enemy, world.Facts), OccurrenceCard.Capture(world.Cards[subject], world.Facts))
        { BoostIcons = Icons, BoostAbility = Ability };
        Icons.Occurrence = occurrence;
        if (Ability is not null) Ability.Occurrence = occurrence;
        return occurrence;
    }
}
