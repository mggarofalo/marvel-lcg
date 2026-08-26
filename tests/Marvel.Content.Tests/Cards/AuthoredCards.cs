using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Tests;

namespace Marvel.Content.Tests.Cards;

/// <summary>
/// The authored card abilities, loaded from the dataset once.
/// </summary>
/// <remarks>
/// <para>
/// The tests' way in to <c>datasets/abilities/abilities.json</c>. There is no
/// production class holding cards and there should not be: a card is a row in
/// that file, and the engine's only knowledge of one is
/// <see cref="AbilityRunner"/> reading it.
/// </para>
/// <para>
/// The printed ids below are here because a test naming <c>01099</c> three
/// times is a test nobody can grep. They are test data, not a registry — adding
/// a card means adding a row to the dataset, and it appears here only if a test
/// needs to point at it.
/// </para>
/// </remarks>
internal static class AuthoredCards
{
    /// <summary>The printed id of Spider-Man's hero side.</summary>
    public const string SpiderMan = "01001a";

    /// <summary>The printed id of "Charge".</summary>
    public const string Charge = "01099";

    /// <summary>The printed id of "I'm Tough".</summary>
    public const string ImTough = "01105";

    /// <summary>The printed id of "Advance".</summary>
    public const string Advance = "01186";

    /// <summary>The printed id of "Assault".</summary>
    public const string Assault = "01187";

    /// <summary>The printed id of "Gang-Up".</summary>
    public const string GangUp = "01189";

    /// <summary>The canonical dataset text. Declared first: <see cref="Book"/> reads it.</summary>
    public static string Text { get; } =
        File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json"));

    /// <summary>The dataset, parsed once.</summary>
    public static AbilityBook Book { get; } = AbilityCatalog.Parse(Text);

    /// <summary>A runner over the authored cards.</summary>
    public static AbilityRunner Runner() => new(Book);
}
