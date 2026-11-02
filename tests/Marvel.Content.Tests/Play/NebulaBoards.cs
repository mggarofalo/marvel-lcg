using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic Nebula boards over the production ability book.</summary>
internal static class NebulaBoards
{
    internal const string NebulaOne = "16088";
    internal const string NebulaTwo = "16089";
    internal const string NebulaThree = "16090";
    internal const string ArtOfEvasion = "16091a,16091b";
    internal const string WarpDrive = "16092a,16092b";
    internal const string Ship = "16093";
    internal const string Cutthroat = "16094";
    internal const string Evasive = "16095";
    internal const string Unyielding = "16096";
    internal const string WeaponMastery = "16097";
    internal const string WideStance = "16098";
    internal const string LethalIntent = "16099";
    internal const string BarrelRoll = "16100";
    internal const string CombatReady = "16101";
    internal const string Ruthless = "16102";

    /// <summary>
    /// Spider-Man in every seat (seat 0 on <paramref name="face"/>) against <paramref name="villain"/>
    /// and <paramref name="scheme"/> on its B side, as <see cref="BadoonBoards.Board"/> deals it.
    /// </summary>
    internal static World Board(int players = 1, string face = "01001a", string villain = NebulaOne,
        string scheme = ArtOfEvasion) =>
        BadoonBoards.Board(players, face, villain, scheme);

    /// <summary>Nebula's Ship in play with <paramref name="evasion"/> evasion counters.</summary>
    internal static Card PutShip(World world, int evasion = 0)
    {
        var ship = world.CreateCard(Ship, world.AreaOf(DeckType.EnvironmentArea));
        if (evasion > 0) ship.PlaceTokens("c_evasion", evasion);
        return ship;
    }

    /// <summary>The evasion counters on <paramref name="ship"/>.</summary>
    internal static long Evasion(Card ship) => ship.Tokens.GetValueOrDefault("c_evasion");

    /// <summary>A Technique attachment <paramref name="face"/> attached to Nebula.</summary>
    internal static Card Technique(World world, string face) =>
        ShipCommandBoards.Attach(world, GrootBoards.Villain(world), face);

    /// <summary>The threat on the main scheme.</summary>
    internal static long Threat(World world) => GrootBoards.MainScheme(world).Tokens.GetValueOrDefault("k_threat");

    /// <summary>The facedown encounter cards dealt to <paramref name="player"/>.</summary>
    internal static int Dealt(World world, int player = 0) =>
        world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(player)).Cards.Count;

    /// <summary>
    /// Drains pending work as <see cref="BadoonBoards.Drive"/> does, taking the first offered
    /// affordance whose label or description contains <paramref name="option"/>.
    /// </summary>
    internal static List<Prompt> Choose(World world, string option)
    {
        var asked = new List<Prompt>();
        BadoonBoards.Drive(world, prompt =>
        {
            asked.Add(prompt);
            return prompt.Affordances.FirstOrDefault(a => a.Label.Contains(option, StringComparison.Ordinal)
                || (a.Description ?? "").Contains(option, StringComparison.Ordinal)) is { } chosen
                ? Decision.Take(chosen.Id) : null;
        });
        return asked;
    }
}
