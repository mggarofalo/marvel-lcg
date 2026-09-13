using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Server;

/// <summary>A rejected request, represented identically by both transports.</summary>
public sealed record EngineError(string Code, string Message);
