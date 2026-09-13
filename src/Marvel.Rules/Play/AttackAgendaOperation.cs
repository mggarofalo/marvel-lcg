using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

internal sealed record AttackAgendaOperation(
    string Name, AgendaOperationData Payload)
    : AgendaOperation(Name, AgendaProcedureKind.Attack, Payload);
