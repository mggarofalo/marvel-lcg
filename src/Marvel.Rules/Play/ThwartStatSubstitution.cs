using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

/// <summary>Applies an optional statistic replacement to one initiated thwart.</summary>
public static class ThwartStatSubstitution
{
    /// <summary>Whether the exact pending power has an acting-character THW read to replace.</summary>
    public static bool CanUseAttack(World world, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(occurrence);
        return world.Agenda.items.Any(item => ReferenceEquals(item.Occurrence, occurrence)
            && item.Step.Operation is CharacterThwartOperation
                { Thwart: { UsesThwartStat: true, UsesAttack: false } });
    }

    /// <summary>Replace THW only after initiation and its costs have succeeded.</summary>
    public static void UseAttack(World world, Occurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(world);
        ArgumentNullException.ThrowIfNull(occurrence);
        if (!CanUseAttack(world, occurrence))
            throw new RulesNotImplementedException("this thwart does not use a replaceable THW statistic");
        var agenda = world.Agenda;
        int at = agenda.items.FindIndex(item => ReferenceEquals(item.Occurrence, occurrence)
            && item.Step.Operation is CharacterThwartOperation);
        if (at < 0) throw new RulesNotImplementedException("ATK substitution requires an initiated thwart");
        var (step, stage, origin) = agenda.items[at];
        var operation = step.Require<CharacterThwartOperation>();
        var thwart = operation.Thwart;
        // Printed 04139 interrupts a thwart; ruling 3307f61b7b6d0690 says
        // it does not grant permission to initiate with a dash THW value.
        agenda.items[at] = (step with
        {
            Operation = operation with { Thwart = thwart with { UsesAttack = true } },
        }, stage, origin);
        MarkConsequential(agenda, at + 1, thwart);
    }

    private static void MarkConsequential(Agenda agenda, int start, CharacterThwart thwart)
    {
        // Consequential damage follows this power and its responses. The first
        // matching queued step belongs to this power, not a later use of the ally.
        for (int index = start; index < agenda.items.Count; index++)
        {
            var (step, stage, occurrence) = agenda.items[index];
            if (step.Subject != thwart.Thwarter
                || step.Operation is not ConsequentialDamageOperation
                { Kind: ConsequentialDamageKind.AllyThwartConsequentialDamage } damage
                || damage.Character != thwart.Scheme) continue;
            agenda.items[index] = (step with
            {
                Operation = damage with { UsesAttack = true },
            }, stage, occurrence);
            return;
        }
    }

    /// <summary>Read the acting character's statistic within the current thwart.</summary>
    public static long Modified(World world, Card character, string field)
    {
        if (field == "thwart" && world.Agenda.Current?.Operation is CharacterThwartOperation
            { Thwart: { UsesAttack: true } thwart } && thwart.Thwarter == character.ObjectId)
            field = "attack";
        return StateFields.Modified(world, character, field, world.Facts, world.Players);
    }
}
