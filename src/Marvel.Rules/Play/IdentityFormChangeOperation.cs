using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>An ability-driven identity change with its flip inside the timing window.</summary>
/// <remarks>The operation payload is an engine choice; rr:interrupt.3 puts the flip after interrupts.</remarks>
public sealed record IdentityFormChangeOperation(
    int Identity, int Incarnation, string Destination, string Trigger, EffectCompletion Completion) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Steps.FormChanged;

    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Lifecycle;

    internal Prompt? Apply(World world, PhaseStep step, List<GameEvent> events)
    {
        var seat = world.Seats[step.Seat];
        var identity = seat.IdentityCard;
        // rr:ability.1: "Card abilities only interact with cards that are in play".
        // rr:leaves-play.1 makes a returning card a new copy. The engine binds
        // pending work to the physical identity and incarnation selected at scheduling.
        // rr:cannot.1: "the ability with 'cannot' takes precedence".
        if (seat.Eliminated || identity.ObjectId != Identity
            || identity.Incarnation != Incarnation || !DeckTypes.IsInPlay(identity.Area.Type)
            || !FormChangePermissions.CanChange(world, identity)
            || !Forms.Destinations(seat, world.Facts).Contains(Destination, StringComparer.Ordinal))
        {
            Completion.Complete(0, 0);
            return null;
        }
        Forms.Change(seat, world.Facts, Destination);
        Completion.Complete(0, 1);
        events.Add(new CardsFlipped([Identity], true)
        { Trigger = Trigger, Verb = "Change_Form" });
        return null;
    }
}
