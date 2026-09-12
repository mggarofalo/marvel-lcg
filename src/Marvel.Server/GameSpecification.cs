using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Server;

/// <summary>Everything needed to deal one deterministic game.</summary>
/// <param name="Scenario">The scenario key in <c>datasets/setup/setup.json</c>.</param>
/// <param name="Heroes">Hero keys, in seat order.</param>
/// <param name="ModularSets">
/// Chosen modular-set keys, or null for the scenario's recommended sets. An
/// empty list deliberately means no modular set.
/// </param>
/// <param name="Seed">The seed for the game's one MT19937 stream.</param>
public sealed record GameSpecification(
    string Scenario,
    IReadOnlyList<string> Heroes,
    IReadOnlyList<string>? ModularSets,
    uint Seed);
