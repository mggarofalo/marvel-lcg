using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

internal sealed record PlayerActionAgendaOperation(
    string Name, AgendaOperationData Payload)
    : AgendaOperation(Name, AgendaProcedureKind.PlayerAction, Payload);
