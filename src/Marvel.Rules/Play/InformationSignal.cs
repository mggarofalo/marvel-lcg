using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Rules.Play;

/// <summary>Internal resolution metadata that never carries concealed identities.</summary>
public sealed record InformationSignal(InformationKind Kind);
