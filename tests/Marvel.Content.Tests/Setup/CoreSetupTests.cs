using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;
public abstract class CoreSetupTestBase
{
    protected const uint Seed = 261;
    protected static readonly SetupCatalog Setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    protected static readonly CardCatalog Cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    public static TheoryData<string, string> CoreScenarioModes => new()
    {
        {
            "rhino",
            Scenarios["rhino"].Source
        },
        {
            "rhino_expert",
            Scenarios["rhino_expert"].Source
        },
        {
            "klaw",
            Scenarios["klaw"].Source
        },
        {
            "klaw_expert",
            Scenarios["klaw_expert"].Source
        },
        {
            "ultron",
            Scenarios["ultron"].Source
        },
        {
            "ultron_expert",
            Scenarios["ultron_expert"].Source
        },
    };

    protected static World Deal(string campaign, string hero)
    {
        var order = Dealer.DealOrder(Setup, campaign, [hero]);
        return WorldSetup.Deal(Cards, Blueprints.From(order, Cards), [Setup.Hero(hero).Name], Seed, AuthoredCards.Runner());
    }

    protected static bool AtMulligan(Game game) => game.Phase == GamePhase.Mulligan && game.Pending?.Affordances.Count == 1 && game.Pending.Affordances[0].Verb == Game.ResolveMulligans;
    protected static void Same(string source, string claim, IEnumerable<string> expected, IEnumerable<string> actual)
    {
        string[] expectedItems = [..expected];
        string[] actualItems = [..actual];
        Assert.True(expectedItems.SequenceEqual(actualItems, StringComparer.Ordinal), $"{source}: {claim}\nexpected: {string.Join(", ", expectedItems)}" + $"\nactual: {string.Join(", ", actualItems)}");
    }

    protected static void SameMultiset(string source, string claim, IEnumerable<string> expected, IEnumerable<string> actual) => Same(source, claim, expected.OrderBy(item => item, StringComparer.Ordinal), actual.OrderBy(item => item, StringComparer.Ordinal));
    protected sealed record HeroAuthority(string Source, string[] Identity, string[] HeroCards, string[] PlayerCards, string[] Obligation, string[] Nemesis);
    protected sealed record ScenarioAuthority(string Source, bool Expert, string[] Villains, string[] Schemes, string[] Encounters, string[] FixedSets, string[] ModularSets, string[] SetAside);
    protected sealed record ModularAuthority(string Source, string[] Cards);
    protected static readonly IReadOnlyDictionary<string, HeroAuthority> StarterDecks = new Dictionary<string, HeroAuthority>(StringComparer.Ordinal)
    {
        ["spider_man"] = new("Learn to Play, page 21, Spider-Man / Justice (pack:mvc01:leadership)", ["01001a,01001b"], ["01002", "01003", "01003", "01004", "01004", "01005", "01005", "01005", "01006", "01007", "01007", "01008", "01008", "01009", "01009"], ["01058", "01059", "01060", "01060", "01061", "01061", "01062", "01062", "01063", "01063", "01064", "01064", "01065", "01065", "01083", "01084", "01085", "01086", "01087", "01088", "01089", "01090", "01091", "01092", "01093"], ["01165"], ["01166", "01167", "01168", "01168", "01169"]),
        ["captain_marvel"] = new("Learn to Play, page 21, Captain Marvel / Leadership (pack:mvc01:leadership)", ["01010a,01010b"], ["01011", "01012", "01012", "01012", "01013", "01013", "01013", "01014", "01014", "01015", "01016", "01017", "01017", "01018", "01018"], ["01066", "01067", "01068", "01069", "01069", "01070", "01070", "01071", "01071", "01072", "01072", "01073", "01074", "01074", "01083", "01084", "01085", "01086", "01087", "01088", "01089", "01090", "01091", "01092", "01093"], ["01175"], ["01176", "01177", "01178", "01178", "01179"]),
        ["she_hulk"] = new("Learn to Play, page 21, She-Hulk / Aggression (pack:mvc01:she-hulk-aggression)", ["01019a,01019b"], ["01020", "01021", "01022", "01022", "01023", "01023", "01024", "01024", "01024", "01025", "01026", "01027", "01027", "01028", "01028"], ["01050", "01051", "01052", "01052", "01053", "01053", "01054", "01054", "01055", "01055", "01056", "01056", "01057", "01057", "01083", "01084", "01085", "01086", "01087", "01088", "01089", "01090", "01091", "01092", "01093"], ["01160"], ["01161", "01162", "01163", "01164", "01164"]),
        ["iron_man"] = new("Learn to Play, page 20, Iron Man / Aggression (pack:mvc01:iron-man-aggression)", ["01029a,01029b"], ["01030", "01031", "01031", "01031", "01032", "01032", "01033", "01034", "01035", "01036", "01037", "01038", "01038", "01039", "01039"], ["01050", "01051", "01052", "01052", "01053", "01053", "01054", "01054", "01055", "01055", "01056", "01056", "01057", "01057", "01083", "01084", "01085", "01086", "01087", "01088", "01089", "01090", "01091", "01092", "01093"], ["01170"], ["01171", "01172", "01173", "01173", "01174"]),
        ["black_panther"] = new("Learn to Play, page 20, Black Panther / Protection (pack:mvc01:protection)", ["01040a,01040b"], ["01041", "01042", "01043a", "01043b", "01043c", "01043d", "01043d", "01044", "01044", "01044", "01045", "01046", "01047", "01048", "01049"], ["01075", "01076", "01077", "01077", "01078", "01078", "01079", "01079", "01080", "01080", "01081", "01081", "01082", "01082", "01083", "01084", "01085", "01086", "01087", "01088", "01089", "01090", "01091", "01092", "01093"], ["01155"], ["01156", "01157", "01158", "01159", "01159"]),
    };
    protected static readonly Dictionary<string, ScenarioAuthority> Scenarios = new Dictionary<string, ScenarioAuthority>(StringComparer.Ordinal)
    {
        ["rhino"] = new("Learn to Play, page 23, Rhino (pack:mvc01:rhino)", false, ["01094", "01095"], ["01097a,01097b"], ["01098", "01099", "01099", "01100", "01101", "01101", "01102", "01103", "01104", "01104", "01105", "01105", "01106", "01106", "01106", "01107", "01108"], ["standard"], ["bomb_scare"], []),
        ["rhino_expert"] = new("Learn to Play, page 23, Rhino; Rules Reference 1.8, Modes of Play.2", true, ["01095", "01096"], ["01097a,01097b"], ["01098", "01099", "01099", "01100", "01101", "01101", "01102", "01103", "01104", "01104", "01105", "01105", "01106", "01106", "01106", "01107", "01108"], ["standard", "expert"], ["bomb_scare"], []),
        ["klaw"] = new("Learn to Play, page 23, Klaw (pack:mvc01:klaw)", false, ["01113", "01114"], ["01116a,01116b", "01117a,01117b"], ["01118", "01119", "01120", "01120", "01120", "01121", "01121", "01122", "01122", "01123", "01123", "01124", "01124", "01125", "01126", "01127"], ["standard"], ["masters_of_evil"], []),
        ["klaw_expert"] = new("Learn to Play, page 23, Klaw; Rules Reference 1.8, Modes of Play.2", true, ["01114", "01115"], ["01116a,01116b", "01117a,01117b"], ["01118", "01119", "01120", "01120", "01120", "01121", "01121", "01122", "01122", "01123", "01123", "01124", "01124", "01125", "01126", "01127"], ["standard", "expert"], ["masters_of_evil"], []),
        ["ultron"] = new("Learn to Play, page 23, Ultron (pack:mvc01:ultron)", false, ["01134", "01135"], ["01137a,01137b", "01138a,01138b", "01139a,01139b"], ["01141", "01142", "01142", "01143", "01143", "01143", "01144a", "01144b", "01144c", "01145", "01145", "01146", "01146", "01147", "01147", "01148", "01149", "01150"], ["standard"], ["under_attack"], ["01140"]),
        ["ultron_expert"] = new("Learn to Play, page 23, Ultron; Rules Reference 1.8, Modes of Play.2", true, ["01135", "01136"], ["01137a,01137b", "01138a,01138b", "01139a,01139b"], ["01141", "01142", "01142", "01143", "01143", "01143", "01144a", "01144b", "01144c", "01145", "01145", "01146", "01146", "01147", "01147", "01148", "01149", "01150"], ["standard", "expert"], ["under_attack"], ["01140"]),
    };
    protected static readonly IReadOnlyDictionary<string, ModularAuthority> ModularSets = new Dictionary<string, ModularAuthority>(StringComparer.Ordinal)
    {
        ["bomb_scare"] = new("Learn to Play, page 23, Rhino (pack:mvc01:rhino)", ["01109", "01110", "01110", "01111", "01112", "01112"]),
        ["masters_of_evil"] = new("Learn to Play, page 23, Klaw (pack:mvc01:klaw)", ["01128", "01129", "01130", "01131", "01132", "01133", "01133"]),
        ["under_attack"] = new("Learn to Play, page 23, Ultron (pack:mvc01:ultron)", ["01151", "01152", "01153", "01154", "01154"]),
        ["legions_of_hydra"] = new("Learn to Play, page 23, Customization Rules (pack:mvc01:customization-rules-2)", ["01180", "01180", "01181", "01182", "01182", "01182"]),
        ["the_doomsday_chair"] = new("Learn to Play, page 23, Customization Rules (pack:mvc01:customization-rules-2)", ["01183", "01183", "01184", "01185", "01185", "01185"]),
    };
}
