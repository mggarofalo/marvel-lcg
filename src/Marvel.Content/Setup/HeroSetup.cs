namespace Marvel.Content.Setup;

/// <summary>
/// One hero's starter deck, exactly as <c>datasets/setup/setup.json</c> records it.
/// </summary>
/// <param name="Name">The printed name, e.g. <c>Spider-Man</c>.</param>
/// <param name="Hero">
/// The identity. One entry, and that entry is one card with two faces:
/// <c>01001a,01001b</c>. See <see cref="Dealer.MoveBToFront"/> for why the order
/// the engine deals it in is not the order it is printed in.
/// </param>
/// <param name="HeroDeck">The identity's signature cards.</param>
/// <param name="Obligations">Set aside into the player's nemesis pile.</param>
/// <param name="NemesisSet">The rest of the nemesis set.</param>
/// <param name="PlayerDeck">The aspect cards.</param>
public sealed record HeroSetup(
    string Name,
    IReadOnlyList<string> Hero,
    IReadOnlyList<string> HeroDeck,
    IReadOnlyList<string> Obligations,
    IReadOnlyList<string> NemesisSet,
    IReadOnlyList<string> PlayerDeck);
