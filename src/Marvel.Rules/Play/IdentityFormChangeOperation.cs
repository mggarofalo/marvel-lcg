using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>An ability-driven identity change with its flip inside the timing window.</summary>
/// <remarks>The operation payload is an engine choice; rr:interrupt.3 puts the flip after interrupts.</remarks>
public sealed record IdentityFormChangeOperation(string Destination, string Trigger, EffectCompletion Completion) : AgendaOperation
{
    /// <inheritdoc/>
    public override string What => Steps.FormChanged;

    /// <inheritdoc/>
    private protected override AgendaProcedureKind ProcedureOwner => AgendaProcedureKind.Lifecycle;

    internal Prompt? Apply(World world, PhaseStep step, List<GameEvent> events)
    {
        var seat = world.Seats[step.Seat];
        FormChangePermissions.Require(world, seat.IdentityCard);
        Forms.Change(seat, world.Facts, Destination);
        Completion.Complete(0, 1);
        events.Add(new CardsFlipped([seat.IdentityCard.ObjectId], true)
        { Trigger = Trigger, Verb = "Change_Form" });
        return null;
    }
}
