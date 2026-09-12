using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.View;

namespace Marvel.Client;

/// <summary>The complete game setup known to the reporting viewer.</summary>
public sealed record InteractionTranscriptSetup(
    string Availability,
    string? Scenario,
    string? Mode,
    IReadOnlyList<string>? Seats,
    string? ModularSelection,
    IReadOnlyList<string>? ModularSets)
{
    /// <summary>Builds normalized setup evidence from a successfully opened selection.</summary>
    public static InteractionTranscriptSetup FromSelection(
        SetupChoices choices,
        GameSetupSelection selection)
    {
        ArgumentNullException.ThrowIfNull(choices);
        ArgumentNullException.ThrowIfNull(selection);
        ScenarioSetupChoice scenario = choices.Scenarios.Single(
            candidate => candidate.Key == selection.ScenarioKey);
        IReadOnlyList<string> modularSets = selection.Modular switch
        {
            ModularConfiguration.Recommended => scenario.RecommendedModularSets,
            ModularConfiguration.None => [],
            ModularConfiguration.Selected => choices.ModularSets
                .Where(candidate => selection.ModularKeys.Contains(candidate.Key))
                .Select(candidate => candidate.Key)
                .ToArray(),
            _ => throw new ArgumentOutOfRangeException(nameof(selection)),
        };
        return new InteractionTranscriptSetup(
            "known",
            scenario.Key,
            scenario.Expert ? "expert" : "standard",
            selection.HeroKeys.ToArray(),
            selection.Modular.ToString().ToLowerInvariant(),
            modularSets.ToArray());
    }

    /// <summary>Marks setup facts that this viewer never received.</summary>
    public static InteractionTranscriptSetup Unavailable(string reason) =>
        new(reason, null, null, null, null, null);
}
