using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

internal sealed record ActivationAgendaOperation(
    string Name, AgendaOperationData Payload)
    : AgendaOperation(Name, AgendaProcedureKind.Activation, Payload);
