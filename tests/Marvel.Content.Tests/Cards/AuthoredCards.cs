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

    /// <summary>The printed id of "Hard to Keep Down".</summary>
    public const string HardToKeepDown = "01104";

    /// <summary>The printed id of "False Alarm".</summary>
    public const string FalseAlarm = "01112";

    /// <summary>The printed id of "I'm Tough".</summary>
    public const string ImTough = "01105";

    /// <summary>The printed id of "Shocker".</summary>
    public const string Shocker = "01103";

    /// <summary>The printed id of "Breakin' &amp; Takin'".</summary>
    public const string BreakinAndTakin = "01107";

    /// <summary>The printed id of "Bomb Scare".</summary>
    public const string BombScare = "01109";

    /// <summary>The printed id of "Hydra Bomber".</summary>
    public const string HydraBomber = "01110";

    /// <summary>The printed ids of the Rhino cards that are read and do nothing.</summary>
    /// <remarks>
    /// A row in the dataset with no abilities means somebody read the card and
    /// found nothing to write — a keyword the engine already reads, a printed
    /// icon, or a restatement of a rule. That is a different fact from the card
    /// being absent, and only one of the two is safe to resolve to silence.
    /// </remarks>
    public static readonly string[] ReadAndSilent =
        ["01094", "01097b", "01101", "01102", "01108", "01167"];

    /// <summary>The printed id of "Caught Off Guard".</summary>
    public const string CaughtOffGuard = "01188";

    /// <summary>The printed id of Rhino's second stage.</summary>
    public const string RhinoTwo = "01095";

    /// <summary>The printed id of "Shadow of the Past".</summary>
    public const string ShadowOfThePast = "01190";

    /// <summary>The printed id of "Exhaustion".</summary>
    public const string Exhaustion = "01191";

    /// <summary>The printed id of "Masterplan".</summary>
    public const string Masterplan = "01192";

    /// <summary>The printed id of "Under Fire".</summary>
    public const string UnderFire = "01193";

    /// <summary>The printed id of "Stampede".</summary>
    public const string Stampede = "01106";

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
