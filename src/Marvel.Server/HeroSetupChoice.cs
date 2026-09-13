using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Server;

/// <summary>One authored hero choice.</summary>
public sealed record HeroSetupChoice(string Key, string Name);
