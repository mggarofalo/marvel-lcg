using Marvel.Rules.Play;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Rules.Tests.Play;

public sealed class AgendaProcedureOwnershipTests
{
    [Fact]
    public void CommittedIndirectDamageCannotCrossAnOccurrenceWithTheSameNumericId()
    {
        // Numeric occurrence ids are diagnostic values; a suspended procedure
        // owns its exact occurrence and cannot consume a nested procedure's damage.
        var agenda = new Agenda();
        var outer = new Occurrence(7, [Steps.ApplyIndirectAttackDamage], Subject: 4, Player: 0);
        var nested = new Occurrence(7, [Steps.ApplyIndirectAttackDamage], Subject: 4, Player: 0);
        agenda.Add(new PhaseStep(new IndirectDamageOperation(
            IndirectDamageKind.FinishIndirectAttackDamage, Origin: outer), 1, 3));
        agenda.Add(new PhaseStep(new IndirectDamageOperation(
            IndirectDamageKind.FinishIndirectAttackDamage, Origin: nested), 1, 3));

        agenda.RecordProcedureAmount(outer, 4, 2);
        agenda.RecordProcedureAmount(nested, 4, 1);

        var results = agenda.Outstanding.Select(step => step.Require<IndirectDamageOperation>()).ToList();
        Assert.Equal(2, Assert.Single(results, operation => ReferenceEquals(operation.Origin, outer)).Amounts![4]);
        Assert.Equal(1, Assert.Single(results, operation => ReferenceEquals(operation.Origin, nested)).Amounts![4]);
    }
}
