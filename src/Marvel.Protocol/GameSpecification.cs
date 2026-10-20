using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Protocol;

/// <summary>Everything needed to deal one deterministic game.</summary>
/// <param name="Scenario">The scenario key in <c>datasets/setup/setup.json</c>.</param>
/// <param name="Heroes">Hero keys, in seat order.</param>
/// <param name="ModularSets">
/// Chosen modular-set keys, or null for the scenario's recommended sets. An
/// empty list deliberately means no modular set.
/// </param>
/// <param name="Seed">The seed for the game's one MT19937 stream.</param>
/// <param name="PlayerDecks">Explicit full decks in seat order, or null for published starters.</param>
/// <param name="Campaign">Authoritative campaign carry-over; absent for standalone games.</param>
public sealed record GameSpecification(
    string Scenario,
    IReadOnlyList<string> Heroes,
    IReadOnlyList<string>? ModularSets,
    uint Seed,
    IReadOnlyList<Marvel.Rules.Setup.PlayerDeckRecipe>? PlayerDecks = null,
    Marvel.Rules.Campaign.CampaignGameSetup? Campaign = null);
