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
        // Every core face is now authored. Norman Osborn is printed in the
        // Green Goblin pack and deliberately remains outside this slice, so he
        // keeps the fail-closed contract observable without making a core game
        // stop on missing data.
        var card = world.CreateCard("02001a", world.AreaOf(DeckType.RevealingArea));
        var thrown = Assert.Throws<RulesNotImplementedException>(() => AuthoredCards.Runner().WhenRevealed(world, card, 0));
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
        // may carry both — an interrupt to a reveal is a different ability at a
        // different tier — and matching on the condition alone would run it
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
        new Marvel.Cards.Run.AbilityRunner(book).WhenRevealed(world, card, 0);
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
        // player's card belongs to that player — neither is something the card
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
        // The runtime book is the Core Set product boundary. The complete
        // generated card catalog remains available for printed facts, but no
        // later product may acquire executable text accidentally.
        using var cards = JsonDocument.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var core = cards.RootElement.GetProperty("cards").EnumerateArray().Where(card => string.Equals(card.GetProperty("pack").GetString(), "core", StringComparison.Ordinal)).Select(card => card.GetProperty("card_id").GetString()!).ToList();
        Assert.Equal(core.Order(StringComparer.Ordinal), AuthoredCards.Book.Authored.Order(StringComparer.Ordinal));
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
            Assert.Empty(runner.WhenRevealed(world, card, 0));
        }
    }
}
