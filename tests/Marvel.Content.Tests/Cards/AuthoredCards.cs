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
    /// <summary>
    /// Every trait any printed card carries, in the engine's spelling.
    /// </summary>
    /// <remarks>
    /// Read out of the card dataset rather than listed, so a set that gains a
    /// trait does not need this touched. The engine's spelling is the one
    /// <c>ICardFacts.Traits</c> answers in — upper-case, spaces underscored.
    /// </remarks>
    public static IEnumerable<string> EveryPrintedTrait()
    {
        using var cards = System.Text.Json.JsonDocument.Parse(
            File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

        foreach (var card in cards.RootElement.GetProperty("cards").EnumerateArray())
        {
            if (!card.TryGetProperty("traits", out var traits)
                || traits.ValueKind != System.Text.Json.JsonValueKind.Array)
            {
                continue;
            }

            foreach (var trait in traits.EnumerateArray())
            {
                if (trait.GetString() is { Length: > 0 } text)
                {
                    yield return text.Replace(" ", "_", StringComparison.Ordinal)
                        .Replace("!", string.Empty, StringComparison.Ordinal);
                }
            }
        }
    }

    /// <summary>The printed id of Spider-Man's hero side.</summary>
    public const string SpiderMan = "01001a";

    /// <summary>The printed id of Peter Parker, his alter-ego side.</summary>
    public const string PeterParker = "01001b";

    public const string BlackCat = "01002";
    public const string Backflip = "01003";
    public const string EnhancedSpiderSense = "01004";
    public const string SwingingWebKick = "01005";
    public const string SpiderTracer = "01007";
    public const string WebShooter = "01008";
    public const string WebbedUp = "01009";

    /// <summary>The printed id of "Armored Rhino Suit".</summary>
    public const string ArmoredSuit = "01098";

    /// <summary>The printed id of "Charge".</summary>
    public const string Charge = "01099";

    /// <summary>The printed id of "Hard to Keep Down".</summary>
    public const string HardToKeepDown = "01104";

    /// <summary>The printed id of "False Alarm".</summary>
    public const string FalseAlarm = "01112";

    /// <summary>The printed id of "I'm Tough".</summary>
    public const string ImTough = "01105";

    /// <summary>The printed id of "Enhanced Ivory Horn".</summary>
    public const string IvoryHorn = "01100";

    /// <summary>The printed id of "Shocker".</summary>
    public const string Shocker = "01103";

    /// <summary>The printed id of "Breakin' &amp; Takin'".</summary>
    public const string BreakinAndTakin = "01107";

    /// <summary>The printed id of "Explosion".</summary>
    public const string Explosion = "01111";

    /// <summary>The printed id of "Bomb Scare".</summary>
    public const string BombScare = "01109";

    /// <summary>The printed id of "Biomechanical Upgrades".</summary>
    public const string BiomechanicalUpgrades = "01185";

    /// <summary>The printed id of "The Doomsday Chair".</summary>
    public const string DoomsdayChair = "01183";

    /// <summary>The printed id of "M.O.D.O.K.".</summary>
    public const string Modok = "01184";

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
        ["01094", "01097a", "01097b", "01101", "01102", "01108", "01167"];

    /// <summary>The printed id of "Caught Off Guard".</summary>
    public const string CaughtOffGuard = "01188";

    /// <summary>The printed id of "Highway Robbery".</summary>
    public const string HighwayRobbery = "01166";

    /// <summary>The printed id of Rhino's second stage.</summary>
    public const string RhinoTwo = "01095";

    /// <summary>The printed id of Rhino's third stage, which only expert mode uses.</summary>
    public const string RhinoThree = "01096";

    /// <summary>The printed id of "Aunt May".</summary>
    public const string AuntMay = "01006";

    /// <summary>The printed id of "Shadow of the Past".</summary>
    public const string ShadowOfThePast = "01190";

    /// <summary>The printed id of "Exhaustion".</summary>
    public const string Exhaustion = "01191";

    /// <summary>The printed id of "Boomerang", of the Sinister Syndicate.</summary>
    public const string Boomerang = "24044";

    /// <summary>The printed id of "Beetle", of the Sinister Syndicate.</summary>
    public const string Beetle = "24043";

    /// <summary>The printed id of "White Rabbit", of the Sinister Syndicate.</summary>
    public const string WhiteRabbit = "24047";

    /// <summary>The printed id of "Sinister Onslaught", of the Sinister Syndicate.</summary>
    public const string SinisterOnslaught = "24048";

    /// <summary>The printed id of "Crime Pays", of the Sinister Syndicate.</summary>
    public const string CrimePays = "24042";

    /// <summary>
    /// The printed id of the Sinister Syndicate's "Shocker", which is not the
    /// Rhino set's <see cref="Shocker"/>.
    /// </summary>
    public const string SyndicateShocker = "24045";

    /// <summary>The printed id of "Speed Demon", of the Sinister Syndicate.</summary>
    public const string SpeedDemon = "24046";

    /// <summary>The printed ids of Unus's three villain stages, in order.</summary>
    /// <remarks>
    /// One card each, and each prints the same constant ability. A stage is a
    /// card — <c>rr:villain-villain-deck</c> — so three rows rather than one.
    /// </remarks>
    public static readonly string[] Unus = ["45059", "45060", "45061"];

    /// <summary>The printed ids of the Unus scenario's main scheme, both sides.</summary>
    public static readonly string[] HuntingGeneTraitors = ["45062a", "45062b"];

    /// <summary>The printed id of "Gene Pool", the side scheme Unus reads.</summary>
    public const string GenePool = "45071";

    /// <summary>
    /// The Unus scenario's encounter cards that are read, in card order.
    /// </summary>
    /// <remarks>
    /// Grown one batch at a time as the whole-game test meets them. The ones
    /// still missing are what it stops on.
    /// </remarks>
    public static readonly string[] UnusEncounters =
        ["45063", "45064", "45065", "45068", "45069", "45070", "45072", "45073", "45074"];

    /// <summary>The printed id of "Hunted", the scenario's obligation.</summary>
    public const string Hunted = "45072";

    /// <summary>The printed id of "Prelate Sidearm", the attachment on Unus.</summary>
    public const string PrelateSidearm = "45063";

    /// <summary>The printed id of "Prelate Armor", the attachment on Unus.</summary>
    public const string PrelateArmor = "45064";

    /// <summary>The printed id of "Infinite Hunter", the scenario's other minion.</summary>
    public const string InfiniteHunter = "45065";

    /// <summary>The printed id of "You Dare Oppose Me?".</summary>
    public const string YouDareOpposeMe = "90005";

    /// <summary>The printed id of "Infinite Soldier", the scenario's minion.</summary>
    public const string InfiniteSoldier = "45069";

    /// <summary>
    /// The printed ids of the three Superpower attachments, in card order.
    /// </summary>
    /// <remarks>
    /// Flight, Super Strength and Telepathy — one modular set each, and the
    /// three whose whole text is a constant ability granting the attached
    /// villain a trait and a keyword.
    /// </remarks>
    public static readonly string[] Superpowers = ["40151", "40155", "40159"];

    /// <summary>The printed id of "Masterplan".</summary>
    public const string Masterplan = "01192";

    /// <summary>The printed id of "Under Fire".</summary>
    public const string UnderFire = "01193";

    /// <summary>The printed id of "Eviction Notice".</summary>
    public const string EvictionNotice = "01165";

    /// <summary>The printed id of "The Vulture's Plans".</summary>
    public const string VulturesPlans = "01169";

    /// <summary>The printed id of "Sweeping Swoop".</summary>
    public const string SweepingSwoop = "01168";

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
