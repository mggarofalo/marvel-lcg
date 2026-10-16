using Marvel.Content.Setup;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class ScenarioSetupDecisionTests
{
    [Rule("rr:when-revealed-abilities.1")]
    [Rule("rr:appendix-ii-setup.step.14")]
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void ScenarioChoiceResumesSetupBeforeOpeningHands(int option)
    {
        // Synthetic scenario text exercises a mandatory setup decision.
        // The engine must finish this instruction before step 14 opening hands.
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var abilities = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01116a","abilities":[]},{"card":"01113","abilities":[]},
              {"card":"01116b","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"WhenRevealed","subject":"this"},
              "effect":{"choose":{"options":[
                {"dealDamage":{"cards":"you","amount":2}},
                {"placeThreat":{"scheme":{"query":"mainScheme"},"amount":1}}
              ]}}
            }]}]}
            """));
        var world = WorldSetup.Deal(cards,
            Blueprints.From(Dealer.DealOrder(setup, "klaw", ["iron_man"], ["bomb_scare"], cards), cards),
            ["Iron Man"], 285, abilities);
        Game game = Game.Begin(world, cards, abilities);

        Assert.Equal(GamePhase.ScenarioSetup, game.Phase);
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.NotNull(game.Pending);
        Assert.False(game.Pending.Cancellable);
        Assert.Equal(2, game.Pending.Affordances.Count);
        long damage = world.Seats[0].IdentityCard.Damage;
        Card scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        long threat = scheme.Tokens["k_threat"];

        game.Resolve(Decision.Take(game.Pending.Affordances[option].Id));

        Assert.Equal(GamePhase.Mulligan, game.Phase);
        Assert.Equal(6, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(damage + (option == 0 ? 2 : 0), world.Seats[0].IdentityCard.Damage);
        Assert.Equal(threat + (option == 1 ? 1 : 0), scheme.Tokens["k_threat"]);
        Assert.Equal(Game.ResolveMulligans, Assert.Single(game.Pending!.Affordances).Verb);
    }
    [Rule("rr:when-revealed-abilities.2")]
    [Fact]
    public void KlawPutIntoPlayDoesNotTriggerHydraBombersWhenRevealedChoice()
    {
        // "If an encounter card with a 'When Revealed' ability is put into play
        // without being revealed, the 'When Revealed' ability does not trigger."
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var blueprints = Blueprints.From(Dealer.DealOrder(setup, "klaw", ["iron_man"], ["bomb_scare"], cards), cards);
        var abilities = AuthoredCards.Runner();
        for (uint seed = 0; seed < 100; seed++)
        {
            var world = WorldSetup.Deal(cards, blueprints, ["Iron Man"], seed, abilities);
            if (!world.Cards.Any(card => card.FaceId == "01110" && DeckTypes.IsInPlay(card.Area.Type))) continue;
            Game game = Game.Begin(world, cards, abilities);
            Assert.Equal(GamePhase.Mulligan, game.Phase);
            Assert.Equal(6, world.Seats[0].Hand.Cards.Count);
            Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
            Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
            return;
        }
        Assert.Fail("The deterministic fixture seeds did not place Hydra Bomber during Klaw setup.");
    }

    [Fact]
    public void SetupAppendsToAnEventSinkWithoutReinterpretingItsExistingEntries()
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var previous = new CardsMoved(AreaRef.Scenario("EncounterDeck"), AreaRef.Player("EngagedEnemiesArea", 0),
            [new Landing(9999, 0)]);
        var events = new List<GameEvent> { previous };
        var world = WorldSetup.Deal(cards,
            Blueprints.From(Dealer.DealOrder(setup, "rhino", ["spider_man"], facts: cards), cards),
            ["Spider-Man"], 1, AuthoredCards.Runner(), events);
        Assert.Same(previous, events[0]);
        Assert.Equal(6, world.Seats[0].Hand.Cards.Count);
    }

}

