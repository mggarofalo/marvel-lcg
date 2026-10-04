using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Ends the unfinished activation of an enemy that left play.</summary>
internal static class ActivationDeparture
{
    internal static bool EndDepartedActivation(World world, PhaseStep step)
    {
        if (step.What is Steps.EndAttack or Steps.EndSchemeEarly
            || world.Activation is not { } activation
            || step.ActivationId != activation.Id
            || DeckTypes.IsInPlay(world.Cards[activation.Enemy].Area.Type)
            || world.Agenda.Stage == Stage.Responses)
        {
            return false;
        }
        if (world.Windows.Current is not null) world.Windows.Close();
        Occurrence? attackOwner = AttackOwner(world, activation.Id);
        world.Agenda.EndActivationEarly(activation.Id, preserveCurrentOccurrence: false);
        world.Agenda.Now(new PhaseStep(
            activation.Attacking ? Steps.EndAttack : Steps.EndSchemeEarly,
            step.Round, activation.Attacking ? 6 : 3,
            Index: activation.Player, Subject: activation.Enemy,
            Seat: activation.Player, ActivationId: activation.Id,
            ProcedureOwnerOccurrence: attackOwner));
        return true;
    }

    private static Occurrence? AttackOwner(World world, int activationId) =>
        world.Agenda.Outstanding
            .Where(pending => pending.What == Steps.EndAttack
                && pending.ActivationId == activationId)
            .Select(pending => pending.ProcedureOwnerOccurrence).FirstOrDefault();

}
