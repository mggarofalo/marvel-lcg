using Marvel.Cards.Dsl;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;
public sealed class ActionAbilityContinuationBindingsANestedDependentChoiceTests
{
    [Rule("rr:then")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void ANestedDependentChoiceIsRejectedBeforeItsCost()
    {
        // The outer answer does not determine whether the complete predecessor
        // resolved: its nested choice and any later siblings still have to run.
        // Until that aggregate outcome is modelled, fail before paying a cost.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "then": {
              "effect": { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "chooseCard": {
                  "from": { "query": "identities" },
                  "effect": { "draw": { "player": "you", "count": 1 } }
                } }
              } },
              "then": { "draw": {
                "player": "chosenPlayer", "count": 1
              } }
            } }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), heroes: ["spider_man", "captain_marvel"], abilities: runner));
        Assert.Contains("multiple-stage player choices", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:then")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void SiblingChoicesBeforeDependentTextAreRejectedBeforeTheirCost()
    {
        // The first answer cannot classify the complete predecessor while a
        // sibling choice and a later effect remain unresolved. Fail closed
        // before exhaustion rather than recording the first leaf's outcome.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "then": {
              "effect": { "seq": [
                { "chooseCard": {
                  "from": { "query": "identities" },
                  "effect": { "draw": { "player": "you", "count": 1 } }
                } },
                { "chooseCard": {
                  "from": { "query": "identities" },
                  "effect": { "draw": { "player": "you", "count": 1 } }
                } },
                { "heal": { "card": "you", "amount": 1 } }
              ] },
              "then": { "draw": { "player": "chosenPlayer", "count": 1 } }
            } }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var refused = Assert.Throws<RulesNotImplementedException>(() => Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner));
        Assert.Contains("multiple-stage player choices", refused.Message);
        Assert.True(source!.Ready);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void AnEmptyEachPlayerOutcomeInvalidatesAChosenPlayerDraw()
    {
        // Player 0 has a hero target and player 1 does not. If player 1's frame
        // resolves last, the outer draw has no chosen player; unlike a card
        // prompt, the order decision cannot filter out that reachable outcome.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "eachPlayer": { "effect": { "chooseCard": {
                "from": "yourHero",
                "effect": { "seq": [] }
              } } } },
              { "draw": { "player": "chosenPlayer", "count": 1 } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:target.2")]
    [Fact]
    public void AMixedBindingPromptKeepsTheDrawablePlayerPath()
    {
        // The selector includes an identity and the villain. At least one path
        // supplies the player required by the later draw, so the action is
        // legal and the scenario-owned character is filtered from its prompt.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "characters" },
                "effect": { "seq": [] }
              } },
              { "draw": { "player": "chosenPlayer", "count": 1 } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Contains(game.Pending!.Affordances, option => option.Id == world.Seats[0].IdentityCard.ObjectId);
        Assert.DoesNotContain(game.Pending.Affordances, option => option.Id == villain.ObjectId);
    }

    [Rule("rr:for-each.3")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void AnEarlyForEachPromptAccountsForTheLaterIteration()
    {
        // The first iteration may choose player 0 even though only player 1 can
        // satisfy the outer selector: the second iteration asks again and its
        // answer is the binding that reaches the continuation.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "forEach": {
                "count": 2,
                "effect": { "chooseCard": {
                  "from": { "query": "identities" },
                  "effect": { "seq": [] }
                } }
              } },
              { "chooseCard": {
                "from": { "query": "enemiesEngagedWithChosenPlayer" },
                "effect": { "seq": [] }
              } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01167", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Contains(game.Pending!.Affordances, option => option.Id == world.Seats[0].IdentityCard.ObjectId);
    }

    [Rule("rr:alteration-effect")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void AnEachTimePromptKeepsTheOuterFilterWhenLaterBodiesAreSkipped()
    {
        // The first discarded card matches and asks for a character; the next
        // card is not Kree, so no later prompt will replace that binding. The
        // first prompt must therefore apply the outer chosen-player draw and
        // exclude the scenario-owned villain before the cost is exposed.
        var runner = Runner(AuthoredCards.AuntMay, "WhenRevealed", """
            { "seq": [
              { "eachTime": {
                "effect": { "discardTop": {
                  "from": "encounterDeck", "count": 2
                } },
                "when": { "cardSet": {
                  "card": "that", "set": "kree_fanatic"
                } },
                "then": { "chooseCard": {
                  "from": { "query": "characters" },
                  "effect": { "seq": [] }
                } }
              } },
              { "draw": { "player": "chosenPlayer", "count": 1 } }
            ] }
            """, eventName: Steps.CardRevealed);
        var world = WorldSetup.DealWithoutCardAbilities(CardCatalogData, Blueprints.From(Dealer.DealOrder(SetupCatalogData, "rhino", ["spider_man"]), CardCatalogData), ["Spider-Man"], 12345);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var removed = world.AreaOf(DeckType.RemovedArea);
        foreach (var card in deck.Cards.ToList())
        {
            World.MoveToTop(card, removed);
        }

        world.CreateCard(AuthoredCards.ImTough, deck);
        world.CreateCard("90001", deck);
        var source = world.CreateCard(AuthoredCards.AuntMay, world.AreaOf(DeckType.RevealingArea));
        world.Abilities = runner;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var events = runner.WhenRevealed(world, source, 0).ToList();
        var prompt = Sequence.Work(world, CardCatalogData, runner, events)!;
        Assert.Contains(prompt.Affordances, option => option.Id == world.Seats[0].IdentityCard.ObjectId);
        Assert.DoesNotContain(prompt.Affordances, option => option.Id == villain.ObjectId);
    }

    [Rule("rr:choose-game-element.3")]
    [Rule("rr:activation.8")]
    [Fact]
    public void AChosenPlayerBindingSurvivesAnActivationContinuation()
    {
        // The selection is part of the unresolved ability when an activation
        // “resolves after the current … ability.” Resuming that same ability
        // must retain which identity the player selected.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "chooseCard": {
              "from": { "query": "identities" },
              "effect": { "seq": [
                { "enemyAttacks": { "enemies": { "query": "villain" } } },
                { "draw": { "player": "chosenPlayer", "count": 1 } }
              ] }
            } }
            """);
        Card? source = null;
        var(_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        int held = world.Seats[1].Hand.Cards.Count;
        var action = Assert.Single(runner.Actions(world, 0), ability => ability.Card == source!.ObjectId);
        runner.Act(world, action, [], []);
        var choice = Assert.Single(world.Agenda.Outstanding, step => step.What == Steps.ChooseOption);
        runner.Chose(world, source!, 0, choice.Index, Decision.Take(world.Seats[1].IdentityCard.ObjectId), choice.Tier);
        var attack = Assert.Single(world.Agenda.Outstanding, pending => pending.What == Steps.Attack);
        runner.ActivationCompleted(world, new EnemyActivation(attack.Subject, attack.Seat, Attacking: true, attack.ActivationId, Made: false));
        Assert.Equal(held + 1, world.Seats[1].Hand.Cards.Count);
    }
}
