using System.Buffers.Binary;
using System.Globalization;
using System.Security.Cryptography;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.View;

namespace Marvel.Client;

/// <summary>Raw values selected by the setup screen.</summary>
public sealed record GameSetupSelection(
    IReadOnlyList<string> HeroKeys,
    string ScenarioKey,
    ModularConfiguration Modular,
    IReadOnlyList<string> ModularKeys,
    string Seed)
{
    /// <summary>Captured complete decks in seat order, or null for published starters.</summary>
    public IReadOnlyList<DeckDraft>? Decks { get; init; }

    /// <summary>Creates the single-hero selection used by the local-play screen.</summary>
    public GameSetupSelection(
        string heroKey,
        string scenarioKey,
        ModularConfiguration modular,
        IReadOnlyList<string> modularKeys,
        string seed)
        : this([heroKey], scenarioKey, modular, modularKeys, seed)
    {
    }
}
