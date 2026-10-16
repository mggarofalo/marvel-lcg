using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Protocol;

/// <summary>One authored encounter set selectable as a modular set.</summary>
public sealed record ModularSetupChoice(string Key, string Name);
