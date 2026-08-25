namespace Marvel.Content.Setup;

/// <summary>
/// One scenario, exactly as <c>datasets/setup/setup.json</c> records it.
/// </summary>
/// <param name="Name">The printed name, e.g. <c>Rhino</c>.</param>
/// <param name="Villain">Every villain stage, in printed order.</param>
/// <param name="Expert">Whether this is the expert variant.</param>
/// <param name="Challenges">Challenge cards this campaign adds, usually empty.</param>
/// <param name="Schemes">Main schemes. Each entry is one card: <c>01097a,01097b</c>.</param>
/// <param name="SetAside">Cards set aside at setup.</param>
/// <param name="Encounters">The scenario's own encounter cards.</param>
/// <param name="EncounterSets">Named sets that always go in, e.g. <c>standard</c>.</param>
/// <param name="ModularSets">
/// The modular sets this scenario is played with by default. Kept separate from
/// <paramref name="EncounterSets"/> deliberately — see
/// <see cref="Dealer.EncounterSetNames"/>.
/// </param>
public sealed record CampaignSetup(
    string Name,
    IReadOnlyList<string> Villain,
    bool Expert,
    IReadOnlyList<string> Challenges,
    IReadOnlyList<string> Schemes,
    IReadOnlyList<string> SetAside,
    IReadOnlyList<string> Encounters,
    IReadOnlyList<string> EncounterSets,
    IReadOnlyList<string> ModularSets);
