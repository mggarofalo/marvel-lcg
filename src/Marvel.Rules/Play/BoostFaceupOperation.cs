using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>One faceup boost card, before its star ability resolves.</summary>
public sealed record BoostFaceupOperation(int Enemy, bool Attacking, BoostIconCancellation Icons) : AgendaOperation
{
    /// <summary>Engine vocabulary for a single physical boost faceup occurrence.</summary>
    public const string Faceup = "WhenBoostCardTurnedFaceup";
    /// <inheritdoc/>
    public override string What => Faceup;
    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Attack;
    internal Occurrence Occurrence(int id, World world, int subject, int player)
    {
        var enemy = world.Cards[Enemy];
        var occurrence = new Occurrence(id, [Faceup], subject, player, Enemy, subject,
            OccurrenceCard.Capture(enemy, world.Facts), OccurrenceCard.Capture(world.Cards[subject], world.Facts))
        { BoostIcons = Icons };
        Icons.Occurrence = occurrence;
        return occurrence;
    }
}
