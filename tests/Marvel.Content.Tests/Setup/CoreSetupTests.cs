using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

/// <summary>The complete core box, held against its printed setup lists.</summary>
/// <remarks>
/// The Learn to Play guide supplies the five starter decks, three scenarios,
/// and five modular sets. The Rules Reference supplies the setup procedure and
/// the Standard and Expert changes. Every comparison carries the printed
/// source in its failure message so a failure says which authority to reopen.
/// </remarks>
public sealed class CoreSetupTests
{
    private const uint Seed = 261;

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    public void TheFivePrintedStarterDecksAreTheDatasetRows()
    {
        foreach (var (name, printed) in StarterDecks)
        {
            var actual = Setup.Hero(name);

            Same(printed.Source, "identity", printed.Identity, actual.Hero);
            Same(printed.Source, "hero cards", printed.HeroCards, actual.HeroDeck);
            Same(printed.Source, "player cards", printed.PlayerCards, actual.PlayerDeck);
            Same(printed.Source, "obligation", printed.Obligation, actual.Obligations);
            Same(printed.Source, "nemesis set", printed.Nemesis, actual.NemesisSet);
            Assert.True(
                actual.HeroDeck.Count + actual.PlayerDeck.Count == 40,
                $"{printed.Source}: {name} has a 40-card starter deck");
        }
    }

    [Rule("rr:appendix-ii-setup.step.1")]
    [Rule("rr:appendix-ii-setup.step.2")]
    [Rule("rr:appendix-ii-setup.step.4")]
    [Rule("rr:appendix-ii-setup.step.5")]
    [Rule("rr:appendix-ii-setup.step.6")]
    [Rule("rr:appendix-ii-setup.step.14")]
    [Rule("rr:obligation.2")]
    [Fact]
    public void EveryCoreStarterDeckReachesTheMulliganInThePrintedPlaces()
    {
        foreach (var (name, printed) in StarterDecks)
        {
            var world = Deal("rhino", name);
            var game = Game.Begin(world, Cards, AuthoredCards.Runner());
            var seat = world.Seats[0];

            Assert.True(
                AtMulligan(game),
                $"{printed.Source}; rr:appendix-ii-setup.step.15: {name} reaches the mulligan");
            Assert.True(
                seat.IdentityCard.Area.Type == DeckType.HeroArea
                    && Cards.Kind(seat.IdentityCard.FaceId) == CardKind.AlterEgo,
                $"{printed.Source}; rr:appendix-ii-setup.step.1: {name} starts alter-ego side up");
            Assert.Equal(
                Cards.PrintedValue(seat.IdentityCard.FaceId, "HP", 1),
                Damage.Health(world, Cards, seat.IdentityCard));
            Assert.Equal(0, seat.IdentityCard.Damage);

            SameMultiset(
                printed.Source,
                "40 cards split between the player deck and opening hand",
                [.. printed.HeroCards, .. printed.PlayerCards],
                seat.Deck.Cards.Concat(seat.Hand.Cards).Select(card => card.FaceId));
            SameMultiset(
                printed.Source,
                "associated obligations shuffled into the encounter deck",
                printed.Obligation,
                world.Cards
                    .Where(card => card.Area.Type == DeckType.EncounterDeck)
                    .Select(card => card.FaceId)
                    .Where(printed.Obligation.Contains));
            Same(
                printed.Source,
                "nemesis set in the player's aside pile",
                printed.Nemesis,
                seat.Nemesis.Cards.Select(card => card.FaceId));
        }
    }

    [Rule("rr:modes-of-play.2")]
    [Rule("rr:modes-of-play.1")]
    [Rule("rr:classifications.4")]
    [Rule("rr:classifications.5")]
    [Rule("rr:classifications.7")]
    [Rule("rr:classifications.8")]
    [Rule("rr:expert-set.2")]
    [Rule("rr:scenario-specific-card.1")]
    [Rule("rr:scenario-specific-card.2")]
    [Rule("rr:scenario-specific-card.3")]
    [Rule("rr:standard-set.2")]
    [Fact]
    public void TheThreeCoreScenariosAndBothModesAreThePrintedLists()
    {
        // Standard mode follows "the content and setup instructions for the
        // chosen scenario"; expert mode substitutes its listed stages and set.
        foreach (var (name, printed) in Scenarios)
        {
            var actual = Setup.Campaign(name);

            Assert.True(
                actual.Expert == printed.Expert,
                $"{printed.Source}; rr:modes-of-play.2: {name} has the printed mode");
            Same(printed.Source, "villain deck", printed.Villains, actual.Villain);
            Same(printed.Source, "main scheme deck", printed.Schemes, actual.Schemes);
            Same(printed.Source, "scenario encounter cards", printed.Encounters, actual.Encounters);
            Same(printed.Source, "fixed encounter sets", printed.FixedSets, actual.EncounterSets);
            Same(printed.Source, "recommended modular set", printed.ModularSets, actual.ModularSets);
            Same(printed.Source, "scenario cards set aside", printed.SetAside, actual.SetAside);
        }
    }

    [Rule("rr:appendix-ii-setup.step.8")]
    [Rule("rr:appendix-ii-setup.step.9")]
    [Rule("rr:appendix-ii-setup.step.10")]
    [Rule("rr:expert-set")]
    [Rule("rr:standard-set")]
    [Fact]
    public void EveryCoreScenarioModeDealsEveryDeclaredCardToItsPreAbilityPlace()
    {
        // "The standard set is an encounter set that is added to most
        // scenarios"; in expert mode, "the expert set [...] is added to
        // scenarios." The declared encounter specs below include those whole
        // fixed sets and the dealt deck must contain every one of their cards.
        foreach (var (name, printed) in Scenarios)
        {
            var order = Dealer.DealOrder(Setup, name, ["spider_man"]);
            var world = WorldSetup.Deal(
                Cards,
                Blueprints.From(order, Cards),
                [Setup.Hero("spider_man").Name],
                Seed,
                expert: printed.Expert);
            Same(
                printed.Source,
                "villain cards on the table and in the villain deck",
                printed.Villains,
                world.Cards
                    .Where(card => card.Area.Type is DeckType.VillainArea or DeckType.VillainDeck)
                    .OrderBy(card => card.ObjectId)
                    .Select(card => card.FaceId));
            var villain = world.TheCardIn(DeckType.VillainArea)!;
            Assert.Equal(
                Cards.PrintedValue(villain.FaceId, "HP", world.Players),
                Damage.Health(world, Cards, villain));
            Assert.Equal(0, villain.Damage);
            Same(
                printed.Source,
                "main scheme cards on the table and in the main scheme deck",
                printed.Schemes.Select((spec, index) =>
                {
                    string[] faces = spec.Split(',');
                    return index == 0 ? faces[^1] : faces[0];
                }),
                world.Cards
                    .Where(card => card.Area.Type is DeckType.MainSchemesArea
                        or DeckType.MainSchemesDeck)
                    .OrderBy(card => card.ObjectId)
                    .Select(card => card.FaceId));

            var setAside = order
                .Select((creation, id) => (Creation: creation, Id: id))
                .Where(dealt => dealt.Creation.Source == CreationSource.ScenarioSetAside)
                .ToList();
            Same(
                printed.Source,
                "scenario set-aside creation",
                printed.SetAside,
                setAside.Select(dealt => dealt.Creation.Spec));
            Assert.True(
                setAside.All(dealt => world.Cards[dealt.Id].Area.Type == DeckType.AsideDeck),
                $"{printed.Source}: every scenario set-aside card starts in the scenario aside pile");

            var fixedCards = printed.FixedSets
                .SelectMany(Setup.EncounterSet)
                .ToList();
            SameMultiset(
                printed.Source,
                "standard and expert encounter cards",
                fixedCards,
                world.AreaOf(DeckType.EncounterDeck).Cards
                    .Select(card => card.FaceId)
                    .Where(fixedCards.Contains));

            var encounterSpecs = order
                .Where(creation => creation.Source is CreationSource.Obligation
                    or CreationSource.Encounter or CreationSource.EncounterSet)
                .Select(creation => creation.Spec);
            SameMultiset(
                printed.Source,
                "encounter deck",
                encounterSpecs,
                world.AreaOf(DeckType.EncounterDeck).Cards.Select(card => card.FaceId));
        }
    }

    [Rule("rr:appendix-ii-setup.step.15")]
    [Theory]
    [InlineData("rhino", "Learn to Play, page 23, Rhino (pack:mvc01:rhino)")]
    [InlineData(
        "rhino_expert",
        "Learn to Play, page 23, Rhino; Rules Reference 1.8, Modes of Play.2")]
    public void EverySupportedCoreScenarioModeReachesTheFirstDecision(
        string campaign, string source)
    {
        // Klaw and Ultron are held structurally above. Their printed setup
        // abilities are core cards and become supported with MARVEL-68; a
        // silent interpreter would make them appear to reach the mulligan on
        // boards missing Defense Network or Ultron Drones.
        var order = Dealer.DealOrder(Setup, campaign, ["spider_man"]);
        var runner = AuthoredCards.Runner();
        var world = WorldSetup.Deal(
            Cards,
            Blueprints.From(order, Cards),
            [Setup.Hero("spider_man").Name],
            Seed,
            runner,
            expert: Setup.Campaign(campaign).Expert);
        var game = Game.Begin(world, Cards, runner);

        Assert.True(
            AtMulligan(game),
            $"{source}; rr:appendix-ii-setup.step.15: {campaign} reaches the mulligan");
    }

    [Fact]
    public void TheFiveCoreModularSetsAreThePrintedLists()
    {
        foreach (var (name, printed) in ModularSets)
        {
            Same(printed.Source, "modular encounter cards", printed.Cards, Setup.EncounterSet(name));
        }
    }

    [Rule("rr:modular-encounter-set")]
    [Rule("rr:modular-encounter-set.1")]
    [Rule("rr:modular-encounter-set.2")]
    [Rule("rr:modular-encounter-set.3")]
    [Rule("rr:encounter-set.2")]
    [Rule("rr:appendix-ii-setup.step.10")]
    [Fact]
    public void EveryCoreModularSetCanReplaceRhinosRecommendation()
    {
        // "If a modular encounter set is added to a scenario, it is done so as
        // an entire set." Every printed card in each selected set must appear.
        foreach (var (name, printed) in ModularSets)
        {
            var order = Dealer.DealOrder(Setup, "rhino", ["spider_man"], [name], Cards);
            var chosen = order
                .Where(creation => creation.Source == CreationSource.EncounterSet)
                .Select(creation => creation.Spec)
                .ToList();
            var expected = Setup.EncounterSet("standard").Concat(printed.Cards);

            Same(printed.Source, "Standard plus the chosen modular set", expected, chosen);

            var world = WorldSetup.Deal(
                Cards,
                Blueprints.From(order, Cards),
                [Setup.Hero("spider_man").Name],
                Seed,
                AuthoredCards.Runner());
            var game = Game.Begin(world, Cards, AuthoredCards.Runner());

            Assert.True(
                AtMulligan(game),
                $"{printed.Source}; rr:appendix-ii-setup.step.15: {name} reaches the mulligan");
            SameMultiset(
                printed.Source,
                $"{name} in the encounter deck",
                printed.Cards,
                world.AreaOf(DeckType.EncounterDeck).Cards
                    .Select(card => card.FaceId)
                    .Where(printed.Cards.Contains));
        }
    }

    [Rule("rr:modular-encounter-set")]
    [Fact]
    public void AnExplicitEmptyModularChoiceDoesNotInventTheRecommendation()
    {
        const string source = "Learn to Play, page 23, Customization Rules "
            + "(pack:mvc01:customization-rules-2)";
        var order = Dealer.DealOrder(Setup, "rhino", ["spider_man"], []);
        var chosen = order
            .Where(creation => creation.Source == CreationSource.EncounterSet)
            .Select(creation => creation.Spec);

        Same(source, "only the fixed Standard set", Setup.EncounterSet("standard"), chosen);
    }

    [Rule("rr:standard-set.1")]
    [Rule("rr:expert-set.1")]
    [Theory]
    [InlineData("standard")]
    [InlineData("expert")]
    public void AFixedDifficultySetCannotBeSelectedAsAModularSet(string set)
    {
        // Both rules say the named set “is not a modular encounter set and
        // cannot be selected” when a scenario asks for a modular set.
        var refused = Assert.Throws<ArgumentException>(() => Dealer.DealOrder(
            Setup, "rhino", ["spider_man"], [set], Cards));

        Assert.Contains("not a modular set", refused.Message, StringComparison.Ordinal);
    }

    private static World Deal(string campaign, string hero)
    {
        var order = Dealer.DealOrder(Setup, campaign, [hero]);
        return WorldSetup.Deal(
            Cards,
            Blueprints.From(order, Cards),
            [Setup.Hero(hero).Name],
            Seed,
            AuthoredCards.Runner());
    }

    private static bool AtMulligan(Game game) =>
        game.Phase == GamePhase.Mulligan
        && game.Pending?.Affordances.Count == 1
        && game.Pending.Affordances[0].Verb == Game.ResolveMulligans;

    private static void Same(
        string source, string claim, IEnumerable<string> expected, IEnumerable<string> actual)
    {
        string[] expectedItems = [.. expected];
        string[] actualItems = [.. actual];
        Assert.True(
            expectedItems.SequenceEqual(actualItems, StringComparer.Ordinal),
            $"{source}: {claim}\nexpected: {string.Join(", ", expectedItems)}"
            + $"\nactual: {string.Join(", ", actualItems)}");
    }

    private static void SameMultiset(
        string source, string claim, IEnumerable<string> expected, IEnumerable<string> actual) =>
        Same(
            source,
            claim,
            expected.OrderBy(item => item, StringComparer.Ordinal),
            actual.OrderBy(item => item, StringComparer.Ordinal));

    private sealed record HeroAuthority(
        string Source,
        string[] Identity,
        string[] HeroCards,
        string[] PlayerCards,
        string[] Obligation,
        string[] Nemesis);

    private sealed record ScenarioAuthority(
        string Source,
        bool Expert,
        string[] Villains,
        string[] Schemes,
        string[] Encounters,
        string[] FixedSets,
        string[] ModularSets,
        string[] SetAside);

    private sealed record ModularAuthority(string Source, string[] Cards);

    private static readonly IReadOnlyDictionary<string, HeroAuthority> StarterDecks =
        new Dictionary<string, HeroAuthority>(StringComparer.Ordinal)
        {
            ["spider_man"] = new(
                "Learn to Play, page 21, Spider-Man / Justice (pack:mvc01:leadership)",
                ["01001a,01001b"],
                ["01002", "01003", "01003", "01004", "01004", "01005", "01005", "01005", "01006", "01007", "01007", "01008", "01008", "01009", "01009"],
                ["01058", "01059", "01060", "01060", "01061", "01061", "01062", "01062", "01063", "01063", "01064", "01064", "01065", "01065", "01083", "01084", "01085", "01086", "01087", "01088", "01089", "01090", "01091", "01092", "01093"],
                ["01165"],
                ["01166", "01167", "01168", "01168", "01169"]),
            ["captain_marvel"] = new(
                "Learn to Play, page 21, Captain Marvel / Leadership (pack:mvc01:leadership)",
                ["01010a,01010b"],
                ["01011", "01012", "01012", "01012", "01013", "01013", "01013", "01014", "01014", "01015", "01016", "01017", "01017", "01018", "01018"],
                ["01066", "01067", "01068", "01069", "01069", "01070", "01070", "01071", "01071", "01072", "01072", "01073", "01074", "01074", "01083", "01084", "01085", "01086", "01087", "01088", "01089", "01090", "01091", "01092", "01093"],
                ["01175"],
                ["01176", "01177", "01178", "01178", "01179"]),
            ["she_hulk"] = new(
                "Learn to Play, page 21, She-Hulk / Aggression (pack:mvc01:she-hulk-aggression)",
                ["01019a,01019b"],
                ["01020", "01021", "01022", "01022", "01023", "01023", "01024", "01024", "01024", "01025", "01026", "01027", "01027", "01028", "01028"],
                ["01050", "01051", "01052", "01052", "01053", "01053", "01054", "01054", "01055", "01055", "01056", "01056", "01057", "01057", "01083", "01084", "01085", "01086", "01087", "01088", "01089", "01090", "01091", "01092", "01093"],
                ["01160"],
                ["01161", "01162", "01163", "01164", "01164"]),
            ["iron_man"] = new(
                "Learn to Play, page 20, Iron Man / Aggression (pack:mvc01:iron-man-aggression)",
                ["01029a,01029b"],
                ["01030", "01031", "01031", "01031", "01032", "01032", "01033", "01034", "01035", "01036", "01037", "01038", "01038", "01039", "01039"],
                ["01050", "01051", "01052", "01052", "01053", "01053", "01054", "01054", "01055", "01055", "01056", "01056", "01057", "01057", "01083", "01084", "01085", "01086", "01087", "01088", "01089", "01090", "01091", "01092", "01093"],
                ["01170"],
                ["01171", "01172", "01173", "01173", "01174"]),
            ["black_panther"] = new(
                "Learn to Play, page 20, Black Panther / Protection (pack:mvc01:protection)",
                ["01040a,01040b"],
                ["01041", "01042", "01043a", "01043b", "01043c", "01043d", "01043d", "01044", "01044", "01044", "01045", "01046", "01047", "01048", "01049"],
                ["01075", "01076", "01077", "01077", "01078", "01078", "01079", "01079", "01080", "01080", "01081", "01081", "01082", "01082", "01083", "01084", "01085", "01086", "01087", "01088", "01089", "01090", "01091", "01092", "01093"],
                ["01155"],
                ["01156", "01157", "01158", "01159", "01159"]),
        };

    private static readonly IReadOnlyDictionary<string, ScenarioAuthority> Scenarios =
        new Dictionary<string, ScenarioAuthority>(StringComparer.Ordinal)
        {
            ["rhino"] = new(
                "Learn to Play, page 23, Rhino (pack:mvc01:rhino)", false,
                ["01094", "01095"], ["01097a,01097b"],
                ["01098", "01099", "01099", "01100", "01101", "01101", "01102", "01103", "01104", "01104", "01105", "01105", "01106", "01106", "01106", "01107", "01108"],
                ["standard"], ["bomb_scare"], []),
            ["rhino_expert"] = new(
                "Learn to Play, page 23, Rhino; Rules Reference 1.8, Modes of Play.2", true,
                ["01095", "01096"], ["01097a,01097b"],
                ["01098", "01099", "01099", "01100", "01101", "01101", "01102", "01103", "01104", "01104", "01105", "01105", "01106", "01106", "01106", "01107", "01108"],
                ["standard", "expert"], ["bomb_scare"], []),
            ["klaw"] = new(
                "Learn to Play, page 23, Klaw (pack:mvc01:klaw)", false,
                ["01113", "01114"], ["01116a,01116b", "01117a,01117b"],
                ["01118", "01119", "01120", "01120", "01120", "01121", "01121", "01122", "01122", "01123", "01123", "01124", "01124", "01126", "01127"],
                ["standard"], ["masters_of_evil"], ["01125"]),
            ["klaw_expert"] = new(
                "Learn to Play, page 23, Klaw; Rules Reference 1.8, Modes of Play.2", true,
                ["01114", "01115"], ["01116a,01116b", "01117a,01117b"],
                ["01118", "01119", "01120", "01120", "01120", "01121", "01121", "01122", "01122", "01123", "01123", "01124", "01124", "01126", "01127"],
                ["standard", "expert"], ["masters_of_evil"], ["01125"]),
            ["ultron"] = new(
                "Learn to Play, page 23, Ultron (pack:mvc01:ultron)", false,
                ["01134", "01135"], ["01137a,01137b", "01138a,01138b", "01139a,01139b"],
                ["01141", "01142", "01142", "01143", "01143", "01143", "01144a", "01144b", "01144c", "01145", "01145", "01146", "01146", "01147", "01147", "01148", "01149", "01150"],
                ["standard"], ["under_attack"], ["01140"]),
            ["ultron_expert"] = new(
                "Learn to Play, page 23, Ultron; Rules Reference 1.8, Modes of Play.2", true,
                ["01135", "01136"], ["01137a,01137b", "01138a,01138b", "01139a,01139b"],
                ["01141", "01142", "01142", "01143", "01143", "01143", "01144a", "01144b", "01144c", "01145", "01145", "01146", "01146", "01147", "01147", "01148", "01149", "01150"],
                ["standard", "expert"], ["under_attack"], ["01140"]),
        };

    private static readonly IReadOnlyDictionary<string, ModularAuthority> ModularSets =
        new Dictionary<string, ModularAuthority>(StringComparer.Ordinal)
        {
            ["bomb_scare"] = new(
                "Learn to Play, page 23, Rhino (pack:mvc01:rhino)",
                ["01109", "01110", "01110", "01111", "01112", "01112"]),
            ["masters_of_evil"] = new(
                "Learn to Play, page 23, Klaw (pack:mvc01:klaw)",
                ["01128", "01129", "01130", "01131", "01132", "01133", "01133"]),
            ["under_attack"] = new(
                "Learn to Play, page 23, Ultron (pack:mvc01:ultron)",
                ["01151", "01152", "01153", "01154", "01154"]),
            ["legions_of_hydra"] = new(
                "Learn to Play, page 23, Customization Rules (pack:mvc01:customization-rules-2)",
                ["01180", "01180", "01181", "01182", "01182", "01182"]),
            ["the_doomsday_chair"] = new(
                "Learn to Play, page 23, Customization Rules (pack:mvc01:customization-rules-2)",
                ["01183", "01183", "01184", "01185", "01185", "01185"]),
        };
}
