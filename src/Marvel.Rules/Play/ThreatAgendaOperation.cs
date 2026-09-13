using Marvel.Rules.State;
using Marvel.Rules.Timing;

namespace Marvel.Rules.Play;

internal sealed record ThreatAgendaOperation(
    string Name, AgendaOperationData Payload)
    : AgendaOperation(Name, AgendaProcedureKind.Threat, Payload);
