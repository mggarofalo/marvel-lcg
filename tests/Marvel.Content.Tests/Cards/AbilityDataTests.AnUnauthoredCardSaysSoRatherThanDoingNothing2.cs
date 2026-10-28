using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Cards;
public sealed class AbilityDataAnUnauthoredCardSaysSoRatherThanDoingNothingTests : AbilityDataTestBase
{
    [Fact]
    public void AnUnauthoredCardSaysSoRatherThanDoingNothing()
    {
        // The property that makes an incomplete card pool safe. A revealed card
        // nobody has read must not resolve to silence, because a silent encounter
        // card produces a board that is plausible and wrong.
        var world = new World(Printed, players: 1);
        world.CreateSeat("p0");
        // Time-Travel Tactics has printed reveal text and remains unauthored.
        var card = world.CreateCard("11028", world.AreaOf(DeckType.RevealingArea));
        var thrown = Assert.Throws<RulesNotImplementedException>(() => AuthoredCards.Runner().ExecuteReveal(world, card, 0));
        Assert.Contains("no ability data", thrown.Message, StringComparison.Ordinal);
    }

    [Theory]
    // Everything unknown is refused rather than ignored. A lenient reader
    // accepts a card and does three quarters of what it says, and nothing
    // downstream can tell.
    [InlineData("""{"cards":[{"card":"01099","wibble":1}]}""", "wibble")]
    [InlineData("""{"cards":[{"card":"01099"},{"card":"01099"}]}""", "twice")]
    [InlineData("""{"nope":[]}""", "no 'cards' array")]
    [InlineData("""{"cards":[{"card":"01099","abilities":[{"effect":{"seq":[]}}]}]}""", "no 'trigger'")]
    [InlineData("""{"cards":[{"card":"01099","abilities":[{"trigger":{"event":"WhenAttackInitiated","timing":"Interrupt","actor":"nobody"},"effect":{"seq":[]}}]}]}""", "nobody")]
    [InlineData("""{"cards":[{"card":"01099","abilities":[{"trigger":{"event":"WhenAttackInitiated","timing":"Shouting","actor":"this"},"effect":{"seq":[]}}]}]}""", "Shouting")]
    [InlineData("""{"cards":[{"card":"01099","abilities":[{"trigger":{"event":"WhenAttackInitiated","timing":"Interrupt","actor":"this"}}]}]}""", "no 'effect'")]
    [InlineData("""{"cards":[{"card":"01099","abilities":[{"trigger":{"event":"WhenAttackInitiated","timing":"Interrupt","actor":"this"},"anyPlayer":"yes","effect":{"seq":[]}}]}]}""", "non-boolean")]
    [InlineData("""{"cards":[{"card":"01099","controlledBy":"lastPlayer"}]}""", "other than 'firstPlayer'")]
    [InlineData("""{"cards":[{"card":"01099","startingCounters":{"type":"web","count":3,"uses":true,"extra":1}}]}""", "extra")]
    [InlineData("""{"cards":[{"card":"01099","startingCounters":{"type":"web","count":0,"uses":true}}]}""", "positive integer")]
    [InlineData("""{"cards":[{"card":"01099","startingCounters":{"type":"web","count":3}}]}""", "boolean 'uses'")]
    [InlineData("""{"cards":[{"card":"01099"}]}""", "neither abilities nor placement")]
    public void TheReaderRefusesWhatItDoesNotUnderstand(string json, string says)
    {
        var thrown = Assert.Throws<AbilityException>(() => AbilityCatalog.Parse(json));
        Assert.Contains(says, thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ANodeWithTwoKindsIsRefused()
    {
        // The one-key rule is what makes a node's kind unambiguous. Two keys
        // would make the second one's fate arbitrary, and a card that quietly
        // lost half an effect is exactly the failure data has and code does not.
        var thrown = Assert.Throws<AbilityException>(() => AbilityCatalog.Parse("""
            {"cards":[{"card":"01099","abilities":[{
              "trigger":{"event":"WhenAttackInitiated","timing":"Interrupt","actor":"this"},
              "effect":{"discard":"this","draw":1}}]}]}
            """));
        Assert.Contains("is not a node", thrown.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnEffectNodeNothingImplementsThrowsNamingTheNode()
    {
        // Unknown syntax is an authored-data error, rejected before a board
        // exists. Valid instructions that reach unsupported game situations
        // are a separate runtime failure.
        var book = AbilityCatalog.Parse("""
            {"cards":[{"card":"01105","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
              "effect":{"summonCthulhu":1}}]}]}
            """);
        var thrown = Assert.Throws<AbilityException>(() => new Marvel.Cards.Run.AbilityRunner(book));
        Assert.Contains("summonCthulhu", thrown.Message, StringComparison.Ordinal);
        Assert.Contains("'01105' ability 0", thrown.Message, StringComparison.Ordinal);
    }

    [Rule("rr:ability.step.3")]
    [Fact]
    public void RevealingACardRunsItsWhenRevealedAndNotItsInterrupts()
    {
        // "When Revealed" *is* the occurrence, not a window around it. A card
        // may carry both â€” an interrupt to a reveal is a different ability at a
        // different tier â€” and matching on the condition alone would run it
        // here as well as in the window that is meant to offer it.
        var world = new World(Printed, players: 1);
        world.CreateSeat("p0");
        var hero = world.CreateCard("01001a", world.Seats[0].Hero);
        world.Seats[0].IdentityCard = hero;
        var card = world.CreateCard("01105", world.AreaOf(DeckType.RevealingArea));
        var book = AbilityCatalog.Parse("""
            {"cards":[{"card":"01105","abilities":[
              {"trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
               "effect":{"giveStatus":{"card":"yourHero","status":"tough"}}},
              {"trigger":{"event":"WhenCardRevealed","timing":"Interrupt","subject":"this"},
               "effect":{"giveStatus":{"card":"yourHero","status":"stunned"}}}]}]}
            """);
        new Marvel.Cards.Run.AbilityRunner(book).ExecuteReveal(world, card, 0);
        Assert.True(Statuses.Has(world, hero, "tough"));
        Assert.False(Statuses.Has(world, hero, "stunned"));
    }

    [Rule("rr:ability.8")]
    [Rule("rr:interrupt.1")]
    [Fact]
    public void WhoControlsAnAbilityIsWhoOwnsTheCard()
    {
        // "Players can only trigger interrupt abilities on cards they control
        // or on encounter cards", and any player may use the latter. So an
        // ability on a scenario-owned card has no controller, and one on a
        // player's card belongs to that player â€” neither is something the card
        // data says, and neither is the seat that happens to be first.
        var world = new World(Printed, players: 2);
        world.CreateSeat("p0");
        world.CreateSeat("p1");
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var identity = world.CreateCard("01001a", world.Seats[1].Hero);
        var book = AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01094","abilities":[{"trigger":{"event":"WhenAttackInitiated",
                "timing":"Interrupt","actor":"this"},"effect":{"seq":[]}}]},
              {"card":"01001a","abilities":[{"trigger":{"event":"WhenAttackInitiated",
                "timing":"Interrupt","actor":"this"},"effect":{"seq":[]}}]}]}
            """);
        var runner = new Marvel.Cards.Run.AbilityRunner(book);
        var onEncounter = Occurrence.ForAttack(1, [Steps.AttackInitiated], world, Printed, villain.ObjectId, identity.ObjectId, player: 0);
        var onPlayerCard = Occurrence.ForAttack(2, [Steps.AttackInitiated], world, Printed, identity.ObjectId, villain.ObjectId);
        Assert.Equal(World.Scenario, Assert.Single(runner.Waiting(world, onEncounter, WindowKind.Interrupt)).Player);
        Assert.Equal(1, Assert.Single(runner.Waiting(world, onPlayerCard, WindowKind.Interrupt)).Player);
    }

    [Rule("rr:friendly")]
    [Fact]
    public void TriggersMatchNamedRolesWithoutSourceSpecificEvents()
    {
        var world = new World(Printed, players: 1);
        world.CreateSeat("p0");
        var hero = world.CreateCard("01001a", world.Seats[0].Hero);
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var villain = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var runner = new Marvel.Cards.Run.AbilityRunner(AbilityCatalog.Parse("""
            { "cards": [
              { "card": "01001a", "abilities": [ {
                "trigger": { "event": "WhenAttackInitiated", "timing": "Interrupt",
                             "actor": "friendly", "target": "friendly" },
                "effect": { "seq": [] }
              } ] },
              { "card": "01094", "abilities": [ {
                "trigger": { "event": "WhenAttackInitiated", "timing": "Interrupt",
                             "actor": "enemy", "target": "enemy" },
                "effect": { "seq": [] }
              } ] }
            ] }
            """));
        var friendly = Occurrence.ForAttack(1, [Steps.AttackInitiated], world, Printed, hero.ObjectId, ally.ObjectId);
        var enemy = Occurrence.ForAttack(2, [Steps.AttackInitiated], world, Printed, villain.ObjectId, minion.ObjectId);
        Assert.Equal(hero.ObjectId, Assert.Single(runner.Waiting(world, friendly, WindowKind.Interrupt)).Card);
        Assert.Equal(villain.ObjectId, Assert.Single(runner.Waiting(world, enemy, WindowKind.Interrupt)).Card);
    }

    [Fact]
    public void EveryTraitACardNamesIsATraitSomePrintedCardCarries()
    {
        // The same failure as a misspelled trigger, one field along. A card
        // asking for `Criminal` when the engine stores `CRIMINAL` parses,
        // validates, and quietly matches nothing -- and "the query found no
        // enemies" is a board a real game reaches, so nothing downstream can
        // tell the typo from the empty table.
        //
        // The dataset names traits as the engine spells them, which is the rule
        // `AbilityTrigger.Event` states for conditions: a translation table
        // between the printed word and the stored one is a second vocabulary,
        // and a second vocabulary drifts.
        var real = new HashSet<string>(AuthoredCards.EveryPrintedTrait(), StringComparer.Ordinal);
        foreach (var ability in AuthoredCards.Book.Abilities)
        {
            foreach (string named in Traits(ability.Effect))
            {
                Assert.True(real.Contains(named), $"'{ability.Card}' names the trait '{named}', which no printed card " + "carries. Traits are stored upper-case with spaces underscored -- " + "`MASTERS_OF_EVIL`, not `Masters of Evil`.");
            }
        }
    }

    [Fact]
    public void TheAuthoredCardsAreTheOnesTheTestsName()
    {
        // Core completeness is independent of expansion authoring. Runtime expansion
        // admission additionally requires its complete card and scenario definitions.
        using var cards = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var core = cards.RootElement.GetProperty("cards").EnumerateArray().Where(card => string.Equals(card.GetProperty("pack").GetString(), "core", StringComparison.Ordinal)).Select(card => card.GetProperty("card_id").GetString()!).ToList();
        Assert.Equal(core.Order(StringComparer.Ordinal), AuthoredCards.Book.Authored.Where(core.Contains).Order(StringComparer.Ordinal));
        var permitted = cards.RootElement.GetProperty("cards").EnumerateArray()
            .Where(card => card.GetProperty("pack").GetString() is "core" or "trors" or "twc")
            .Select(card => card.GetProperty("card_id").GetString()!).ToHashSet(StringComparer.Ordinal);
        permitted.UnionWith(["03001a", "03001b", "03002", "03003", "03004", "03005", "03006", "03007", "03008", "03009", "03010",
            "03011", "03012", "03013", "03014", "03015", "03016", "03017", "03018", "03019", "03020", "03021", "03022", "03023", "03024", "03025", "03026", "03027", "03028", "03029", "03030", "03031", "03032", "03033", "03034"]);


        permitted.UnionWith(["02001a", "02001b", "02002a", "02002b", "02003a", "02003b", "02004a", "02004b", "02005a", "02005b", "02006a", "02006b"]);
        permitted.UnionWith(["02007", "02008", "02009", "02010", "02011", "02012", "02013"]);
        permitted.UnionWith(["02033", "02034", "02035", "02036"]);
        permitted.UnionWith(["02037", "02038", "02039", "02040", "02048"]);
        permitted.UnionWith(["02041", "02042", "02043", "02044", "02045", "02046", "02047", "02049"]);
        permitted.UnionWith(["02014", "02015", "02016", "02017a", "02017b", "02018a", "02018b"]);
        permitted.UnionWith(["02019", "02020", "02021", "02022", "02023", "02024", "02025"]);
        permitted.UnionWith(["02026", "02027", "02028", "02029", "02030", "02031", "02032"]);
        permitted.UnionWith(["05001a", "05001b", "05003", "05004", "05005", "05008", "05009", "05010", "05011", "05006", "05016", "05019", "05020", "05021", "05022", "05024", "05002", "05023", "05032", "05033", "05007", "05015", "05031", "05025", "05027", "05012", "05013", "05026", "05030", "05017", "05028", "05029", "05018", "05014"]);
        permitted.UnionWith(["06012", "06020", "06005", "06011", "06015", "06003", "06027", "06001a", "06004", "06001b", "06002", "06007", "06008", "06009", "06010", "06013", "06016", "06022", "06023", "06024", "06021", "06025", "06033", "06034"]);
        permitted.UnionWith(["06017", "06018", "06019", "06030", "06031"]);
        permitted.UnionWith(["06026", "06029", "06032"]);
        permitted.UnionWith(["06006", "06014"]);
        permitted.Add("06028");
        permitted.UnionWith(["08014", "08015", "08016", "08019", "08020", "08021", "08022", "08027", "08028"]);
        permitted.UnionWith(["08001b", "08002", "08011"]);
        permitted.UnionWith(["08001a", "08005", "08007", "08009", "08010"]);
        permitted.UnionWith(["08023", "08025", "08026", "08029"]);
        permitted.UnionWith(["08003", "08004", "08012", "08013", "08018", "08024", "08033"]);
        permitted.UnionWith(["08006", "08008", "08017", "08032"]);

        permitted.UnionWith(["08030", "08031"]);
        permitted.UnionWith(["09016", "09021", "09037", "09038", "09009", "09011", "09026", "09017", "09018", "09020", "09022", "09023", "09024", "09025"]);
        permitted.UnionWith(["09001a", "09001b", "09002", "09005", "09036"]);
        permitted.UnionWith(["09008", "09012", "09033", "09035", "09006", "09010", "09032", "09034"]);
        permitted.UnionWith(["09013", "09019", "09039", "09003", "09004", "09007", "09014", "09015", "09027", "09029", "09028", "09031", "09030"]);
        permitted.UnionWith(["10001a", "10001b", "10004", "10005"]);
        permitted.UnionWith(["10002", "10003", "10006", "10007", "10008", "10009", "10010"]);
        permitted.UnionWith(["10011", "10012", "10013", "10014", "10016", "10017", "10018", "10019"]);
        permitted.UnionWith(["10020", "10021", "10022", "10023", "10024"]);
        permitted.UnionWith(["10030", "10031", "10032"]);
        permitted.UnionWith(["10025", "10026", "10027", "10028"]);
        permitted.UnionWith(["10015", "10029"]);
        permitted.UnionWith(["12001a", "12001b", "12001c", "12002", "12004", "12007", "12010", "12005", "12008", "12012", "12013", "12015", "12017", "12018", "12019", "12021", "12022", "12023", "12033"]);
        permitted.UnionWith(["90001", "90002", "90003", "90004", "90005"]);
        Assert.Equal(permitted.Order(StringComparer.Ordinal), AuthoredCards.Book.Authored.Order(StringComparer.Ordinal));
    }

    [Fact]
    public void ACardReadAndFoundEmptyIsNotACardNobodyRead()
    {
        // The distinction the dataset exists to be able to make. Several of
        // the Rhino scenario's cards carry no ability at all -- a keyword the
        // engine already reads, a printed icon, or a rule restated on the card
        // -- and each is a row saying so.
        //
        // Revealing one resolves to silence, which is correct. Revealing a card
        // nobody has read throws, which is also correct. A dataset that could
        // not tell them apart would have to pick one, and either choice is
        // wrong for half the pool.
        var world = new World(Printed, players: 1);
        world.CreateSeat("p0");
        var runner = AuthoredCards.Runner();
        foreach (string faceId in AuthoredCards.ReadAndSilent)
        {
            var card = world.CreateCard(faceId, world.AreaOf(DeckType.RevealingArea));
            Assert.Empty(runner.ExecuteReveal(world, card, 0).Events);
        }
    }
}
