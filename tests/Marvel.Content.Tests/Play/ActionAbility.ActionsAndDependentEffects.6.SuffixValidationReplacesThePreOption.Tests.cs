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
public sealed class ActionAbilityActionsAndDependentEffectsSuffixValidationReplacesThePreOptionTests
{
    [Rule("rr:choose-option.1")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void SuffixValidationReplacesThePreOptionCandidateState()
    {
        // The first prompt binds an identity. The enemy option would replace
        // it with the villain, which cannot supply the chosen player required
        // by the final engaged-enemy selector. Recursive suffix validation must
        // use that post-option villain, not the stale identity candidate list.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "seq": [] }
              } },
              { "choose": { "options": [
                { "chooseCard": {
                  "from": { "query": "attackableEnemies" },
                  "effect": { "seq": [] }
                } },
                { "seq": [] }
              ] } },
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
            board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(world.Seats[0].IdentityCard.ObjectId));
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.Id == 0);
        Assert.Contains(game.Pending.Affordances, option => option.Id == 1);
    }

    [Rule("rr:target.2.2")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void SuffixValidationUsesTheActiveBranchForEachConcreteBinding()
    {
        // Each identity has a legal target in only the branch selected by that
        // identity's form. Once suffix validation installs one candidate, the
        // binding is concrete: requiring both branches would reject every
        // identity after the source had already paid its exhaust cost.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "seq": [] }
              } },
              { "if": {
                "test": { "inForm": {
                  "player": "chosenPlayer", "form": "hero"
                } },
                "then": { "chooseCard": {
                  "from": { "query": "topmostTechInChosenDiscard" },
                  "effect": { "seq": [] }
                } },
                "else": { "chooseCard": {
                  "from": { "query": "enemiesEngagedWithChosenPlayer" },
                  "effect": { "seq": [] }
                } }
              } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? tech = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            tech = board.CreateCard("01007", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
            board.CreateCard("01167", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.False(source!.Ready);
        Assert.Contains(game.Pending!.Affordances, option => option.Id == world.Seats[0].IdentityCard.ObjectId);
        Assert.Contains(game.Pending.Affordances, option => option.Id == world.Seats[1].IdentityCard.ObjectId);
        game.Resolve(Decision.Take(world.Seats[0].IdentityCard.ObjectId));
        Assert.Contains(game.Pending!.Affordances, option => option.Id == tech!.ObjectId);
    }

    [Rule("rr:target.2.2")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void AChoiceEffectContributesItsFinalBindingToTheContinuation()
    {
        // The nested enemy choice replaces the identity chosen by the outer
        // prompt. It leaves no chosen player for the later draw, so the costed
        // action must be refused before it exhausts its source.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "chooseCard": {
                  "from": { "query": "attackableEnemies" },
                  "effect": { "seq": [] }
                } }
              } },
              { "draw": { "player": "chosenPlayer", "count": 1 } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:then")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void ADependentContinuationFiltersItsPredecessorPrompt()
    {
        // The predecessor resolves fully after either identity is selected,
        // so “then” reaches the engaged-enemy choice. Only player 0 supplies a
        // target there and only that identity may be offered before payment.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "then": {
              "effect": { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "draw": {
                  "player": "you", "count": 1
                } }
              } },
              "then": { "chooseCard": {
                "from": { "query": "enemiesEngagedWithChosenPlayer" },
                "effect": { "seq": [] }
              } }
            } }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Contains(game.Pending!.Affordances, option => option.Id == world.Seats[0].IdentityCard.ObjectId);
        Assert.DoesNotContain(game.Pending.Affordances, option => option.Id == world.Seats[1].IdentityCard.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void ANonFinalEachPlayerPromptIgnoresTheOuterContinuation()
    {
        // Only the final frame's binding reaches the outer continuation. The
        // first player's own mandatory choice therefore remains available even
        // though that player's binding could not satisfy the later selector.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "eachPlayer": { "effect": { "chooseCard": {
                "from": "you", "effect": { "seq": [] }
              } } } },
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
        var order = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(Question.Order, game.Pending.Asking);
        game.Resolve(new Decision(order.Id, [world.Seats[0].IdentityCard.ObjectId, world.Seats[1].IdentityCard.ObjectId, ]));
        Assert.Contains(game.Pending!.Affordances, option => option.Id == world.Seats[0].IdentityCard.ObjectId);
    }
}
