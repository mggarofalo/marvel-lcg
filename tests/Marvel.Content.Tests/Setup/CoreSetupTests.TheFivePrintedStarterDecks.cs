using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;
public sealed class CoreSetupTheFivePrintedStarterDecksTests : CoreSetupTestBase
{
    [Fact]
    public void TheFivePrintedStarterDecksAreTheDatasetRows()
    {
        foreach (var(name, printed)in StarterDecks)
        {
            var actual = Setup.Hero(name);
            Same(printed.Source, "identity", printed.Identity, actual.Hero);
            Same(printed.Source, "hero cards", printed.HeroCards, actual.HeroDeck);
            Same(printed.Source, "player cards", printed.PlayerCards, actual.PlayerDeck);
            Same(printed.Source, "obligation", printed.Obligation, actual.Obligations);
            Same(printed.Source, "nemesis set", printed.Nemesis, actual.NemesisSet);
            Assert.True(actual.HeroDeck.Count + actual.PlayerDeck.Count == 40, $"{printed.Source}: {name} has a 40-card starter deck");
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
        foreach (var(name, printed)in StarterDecks)
        {
            var world = Deal("rhino", name);
            var game = Game.Begin(world, Cards, AuthoredCards.Runner());
            var seat = world.Seats[0];
            Assert.True(AtMulligan(game), $"{printed.Source}; rr:appendix-ii-setup.step.15: {name} reaches the mulligan");
            Assert.True(seat.IdentityCard.Area.Type == DeckType.HeroArea && Cards.Kind(seat.IdentityCard.FaceId) == CardKind.AlterEgo, $"{printed.Source}; rr:appendix-ii-setup.step.1: {name} starts alter-ego side up");
            Assert.Equal(Cards.PrintedValue(seat.IdentityCard.FaceId, "HP", 1), DamagePlacement.Health(world, Cards, seat.IdentityCard));
            Assert.Equal(0, seat.IdentityCard.Damage);
            SameMultiset(printed.Source, "40 cards split between the player deck and opening hand", [..printed.HeroCards, ..printed.PlayerCards], seat.Deck.Cards.Concat(seat.Hand.Cards).Select(card => card.FaceId));
            SameMultiset(printed.Source, "associated obligations shuffled into the encounter deck", printed.Obligation, world.Cards.Where(card => card.Area.Type == DeckType.EncounterDeck).Select(card => card.FaceId).Where(printed.Obligation.Contains));
            Same(printed.Source, "nemesis set in the player's aside pile", printed.Nemesis, seat.Nemesis.Cards.Select(card => card.FaceId));
        }
    }

    [Rule("rr:linked-card-title.3")]
    [Fact]
    public void IronManAgainstKlawDoesNotSetAsideTheTricksterTakeoverWhirlwindAlly()
    {
        // Linked cards are limited to "the product from which the linked card
        // came." Core Whirlwind cannot bring an ally from Trickster Takeover.
        var order = Dealer.DealOrder(Setup, "klaw", ["iron_man"]);
        var blueprints = Blueprints.From(order, Cards);
        Assert.DoesNotContain(blueprints, card => card.Spec == "55065");
        Assert.Contains(blueprints, card => card.Spec == "01130");
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
        foreach (var(name, printed)in Scenarios)
        {
            var actual = Setup.Campaign(name);
            Assert.True(actual.Expert == printed.Expert, $"{printed.Source}; rr:modes-of-play.2: {name} has the printed mode");
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
        foreach (var(name, printed)in Scenarios)
        {
            var order = Dealer.DealOrder(Setup, name, ["spider_man"]);
            var world = WorldSetup.DealWithoutCardAbilities(Cards, Blueprints.From(order, Cards), [Setup.Hero("spider_man").Name], Seed, expert: printed.Expert);
            Same(printed.Source, "villain cards on the table and in the villain deck", printed.Villains, world.Cards.Where(card => card.Area.Type is DeckType.VillainArea or DeckType.VillainDeck).OrderBy(card => card.ObjectId).Select(card => card.FaceId));
            var villain = world.TheCardIn(DeckType.VillainArea)!;
            Assert.Equal(Cards.PrintedValue(villain.FaceId, "HP", world.Players), DamagePlacement.Health(world, Cards, villain));
            Assert.Equal(0, villain.Damage);
            Same(printed.Source, "main scheme cards on the table and in the main scheme deck", printed.Schemes.Select((spec, index) =>
            {
                string[] faces = spec.Split(',');
                return index == 0 ? faces[^1] : faces[0];
            }), world.Cards.Where(card => card.Area.Type is DeckType.MainSchemesArea or DeckType.MainSchemesDeck).OrderBy(card => card.ObjectId).Select(card => card.FaceId));
            var setAside = order.Select((creation, id) => (Creation: creation, Id: id)).Where(dealt => dealt.Creation.Source == CreationSource.ScenarioSetAside).ToList();
            Same(printed.Source, "scenario set-aside creation", printed.SetAside, setAside.Select(dealt => dealt.Creation.Spec));
            Assert.True(setAside.All(dealt => world.Cards[dealt.Id].Area.Type == DeckType.AsideDeck), $"{printed.Source}: every scenario set-aside card starts in the scenario aside pile");
            var fixedCards = printed.FixedSets.SelectMany(Setup.EncounterSet).ToList();
            SameMultiset(printed.Source, "standard and expert encounter cards", fixedCards, world.AreaOf(DeckType.EncounterDeck).Cards.Select(card => card.FaceId).Where(fixedCards.Contains));
            var encounterSpecs = order.Where(creation => creation.Source is CreationSource.Obligation or CreationSource.Encounter or CreationSource.EncounterSet).Select(creation => creation.Spec);
            SameMultiset(printed.Source, "encounter deck", encounterSpecs, world.AreaOf(DeckType.EncounterDeck).Cards.Select(card => card.FaceId));
        }
    }

    [Rule("rr:appendix-ii-setup.step.15")]
    [Theory]
    [MemberData(nameof(CoreScenarioModes))]
    public void EverySupportedCoreScenarioModeReachesTheFirstDecision(string campaign, string source)
    {
        var order = Dealer.DealOrder(Setup, campaign, ["spider_man"]);
        var runner = AuthoredCards.Runner();
        var world = WorldSetup.Deal(Cards, Blueprints.From(order, Cards), [Setup.Hero("spider_man").Name], Seed, runner, expert: Setup.Campaign(campaign).Expert);
        var game = Game.Begin(world, Cards, runner);
        Assert.True(AtMulligan(game), $"{source}; rr:appendix-ii-setup.step.15: {campaign} reaches the mulligan");
        if (campaign.StartsWith("klaw", StringComparison.Ordinal))
        {
            Card defenseNetwork = Assert.Single(world.Cards, card => card.FaceId == "01125");
            Assert.Equal(DeckType.SideSchemesArea, defenseNetwork.Area.Type);
            Assert.Equal(3, defenseNetwork.Tokens["k_threat"]);
        }
        else if (campaign.StartsWith("ultron", StringComparison.Ordinal))
        {
            Assert.Equal(DeckType.EnvironmentArea, Assert.Single(world.Cards, card => card.FaceId == "01140").Area.Type);
            Assert.Contains(world.Cards, FacedownDrones.Is);
            var pinnedTop = world.AreaOf(DeckType.EncounterDeck).Cards.TakeLast(5).Reverse().Select(card => card.FaceId).ToList();
            Assert.Equal(campaign == "ultron" ? ["01150", "01147", "01141", "01189", "01144a"] : ["01142", "01188", "01189", "01153", "01152"], pinnedTop);
        }
    }

    [Fact]
    public void TheFiveCoreModularSetsAreThePrintedLists()
    {
        foreach (var(name, printed)in ModularSets)
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
        foreach (var(name, printed)in ModularSets)
        {
            var order = Dealer.DealOrder(Setup, "rhino", ["spider_man"], [name], Cards);
            var chosen = order.Where(creation => creation.Source == CreationSource.EncounterSet).Select(creation => creation.Spec).ToList();
            var expected = Setup.EncounterSet("standard").Concat(printed.Cards);
            Same(printed.Source, "Standard plus the chosen modular set", expected, chosen);
            var world = WorldSetup.Deal(Cards, Blueprints.From(order, Cards), [Setup.Hero("spider_man").Name], Seed, AuthoredCards.Runner());
            var game = Game.Begin(world, Cards, AuthoredCards.Runner());
            Assert.True(AtMulligan(game), $"{printed.Source}; rr:appendix-ii-setup.step.15: {name} reaches the mulligan");
            SameMultiset(printed.Source, $"{name} in the encounter deck", printed.Cards, world.AreaOf(DeckType.EncounterDeck).Cards.Select(card => card.FaceId).Where(printed.Cards.Contains));
        }
    }

    [Rule("rr:modular-encounter-set")]
    [Fact]
    public void AnExplicitEmptyModularChoiceDoesNotInventTheRecommendation()
    {
        const string source = "Learn to Play, page 23, Customization Rules " + "(pack:mvc01:customization-rules-2)";
        var order = Dealer.DealOrder(Setup, "rhino", ["spider_man"], []);
        var chosen = order.Where(creation => creation.Source == CreationSource.EncounterSet).Select(creation => creation.Spec);
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
        var refused = Assert.Throws<ArgumentException>(() => Dealer.DealOrder(Setup, "rhino", ["spider_man"], [set], Cards));
        Assert.Contains("not a modular set", refused.Message, StringComparison.Ordinal);
    }
}
