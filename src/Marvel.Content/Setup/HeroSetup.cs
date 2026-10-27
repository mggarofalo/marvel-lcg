namespace Marvel.Content.Setup;

/// <summary>
/// One hero's starter deck, exactly as <c>datasets/setup/setup.json</c> records it.
/// </summary>
/// <param name="Name">The printed name, e.g. <c>Spider-Man</c>.</param>
/// <param name="Hero">
/// The identity. One entry, and that entry is one card with two faces:
/// <c>01001a,01001b</c>. The engine selects its starting alter-ego face by kind.
/// </param>
/// <param name="HeroDeck">The identity's signature cards.</param>
/// <param name="Obligations">Set aside into the player's nemesis pile.</param>
/// <param name="NemesisSet">The rest of the nemesis set.</param>
/// <param name="NemesisSetIcon">Authored association with the identity’s nemesis encounter set.</param>
/// <param name="PlayerDeck">The aspect cards.</param>
public sealed record HeroSetup(
    string Name,
    IReadOnlyList<string> Hero,
    IReadOnlyList<string> HeroDeck,
    IReadOnlyList<string> Obligations,
    IReadOnlyList<string> NemesisSet,
    IReadOnlyList<string> PlayerDeck,
    string NemesisSetIcon)
{
    /// <summary>The separate five-card Invocation deck, excluded from the player deck.</summary>
    public Marvel.Rules.Setup.InvocationDeckRecipe? InvocationDeck { get; init; }

    /// <summary>Printed identity exception to ordinary aspect construction.</summary>
    public Marvel.Rules.Setup.DeckAspectRule AspectRule { get; init; }
}
