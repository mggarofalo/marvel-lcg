using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Server;

/// <summary>The complete product-selection surface exposed by this host.</summary>
public sealed record SetupChoices(
    IReadOnlyList<HeroSetupChoice> Heroes,
    IReadOnlyList<ScenarioSetupChoice> Scenarios,
    IReadOnlyList<ModularSetupChoice> ModularSets,
    RuntimeIdentity Runtime);
