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
public sealed class ActionAbilityActionsAndDependentEffectsContinuationFilteringRemovesUnsafeEarlierBindingTests
{
    [Rule("rr:choose-option.2")]
    [Rule("rr:guard")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void ContinuationFilteringRemovesUnsafeEarlierBindingCandidates()
    {
        // A player-card option may be chosen if it can resolve at least
        // partially. Choosing the no-op option preserves the first selected
        // enemy, so the Guard-protected villain is removed from the first
        // prompt while the minion path keeps the costed action legal.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "enemies" },
                "effect": { "seq": [] }
              } },
              { "choose": { "options": [
                { "chooseCard": {
                  "from": { "query": "attackableMinions" },
                  "effect": { "seq": [] }
                } },
                { "seq": [] }
              ] } },
              { "attack": {
                "target": "chosen",
                "effect": { "dealAttackDamage": {
                  "cards": "chosen", "amount": 1
                } }
              } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            minion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Contains(game.Pending!.Affordances, option => option.Id == minion!.ObjectId);
        Assert.DoesNotContain(game.Pending.Affordances, option => option.Id == villain.ObjectId);
    }

    [Rule("rr:choose-option.2")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void ANoOpOptionCanPreserveTheAbsenceOfABinding()
    {
        // The empty option is legal as a decline branch, but it leaves
        // `chosen` unanswered. A later attack requiring that target therefore
        // makes the whole costed action unsafe to initiate.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "choose": { "options": [
                { "chooseCard": {
                  "from": { "query": "attackableMinions" },
                  "effect": { "seq": [] }
                } },
                { "seq": [] }
              ] } },
              { "attack": {
                "target": "chosen",
                "effect": { "dealAttackDamage": {
                  "cards": "chosen", "amount": 1
                } }
              } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, abilities: runner);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.True(source!.Ready);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void AnEmptyEachPlayerFramePreservesAnEarlierBinding()
    {
        // “Each player” frames resolve in the order chosen by the first
        // player. A frame with no legal minion is a no-op, so it preserves the
        // enemy selected before the frame instead of erasing that target.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "attackableEnemies" },
                "effect": { "seq": [] }
              } },
              { "eachPlayer": { "effect": { "chooseCard": {
                "from": { "query": "minionsEngagedWithYou" },
                "effect": { "seq": [] }
              } } } },
              { "attack": {
                "target": "chosen",
                "effect": { "dealAttackDamage": {
                  "cards": "chosen", "amount": 1
                } }
              } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:target.2.2")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void ASelectorCanDependOnEveryEarlierBindingCandidate()
    {
        // The first target determines which engaged area the second selector
        // reads. Every offered identity has a legal minion, so preflight must
        // evaluate the selector once under each possible earlier binding.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "seq": [] }
              } },
              { "chooseCard": {
                "from": { "query": "enemiesEngagedWithChosenPlayer" },
                "effect": { "seq": [] }
              } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            board.CreateCard("01167", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:target.2")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void AnEarlierPromptFiltersBindingsWithNoLegalContinuation()
    {
        // At least one identity has a valid target for the later choice, so
        // the action can initiate. The identity without one is not a legal
        // target of the unresolved ability and must not be offered.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "seq": [] }
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
            board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Contains(game.Pending!.Affordances, option => option.Id == world.Seats[0].IdentityCard.ObjectId);
        Assert.DoesNotContain(game.Pending.Affordances, option => option.Id == world.Seats[1].IdentityCard.ObjectId);
    }

    [Rule("rr:choose-option.1")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void AnUnavailableOptionDoesNotCreateAnEmptyBindingOutcome()
    {
        // The minion option has no valid target and is unavailable. Only the
        // villain option can run, and it supplies the target for the attack
        // that follows, so the unavailable branch contributes no empty path.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "choose": { "options": [
                { "chooseCard": {
                  "from": { "query": "attackableMinions" },
                  "effect": { "seq": [] }
                } },
                { "chooseCard": {
                  "from": { "query": "attackableEnemies" },
                  "effect": { "seq": [] }
                } }
              ] } },
              { "attack": {
                "target": "chosen",
                "effect": { "dealAttackDamage": {
                  "cards": "chosen", "amount": 1
                } }
              } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        Assert.Contains(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:choose-option.1")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void AnOptionPromptFiltersBranchesThatCannotSatisfyTheContinuation()
    {
        // Both nested selectors are locally legal, but the enemy branch leaves
        // no player for the later draw. The outer option itself is therefore
        // unavailable; the costed prompt must expose only the identity branch.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "choose": { "options": [
                { "chooseCard": {
                  "from": { "query": "identities" },
                  "effect": { "seq": [] }
                } },
                { "chooseCard": {
                  "from": { "query": "attackableEnemies" },
                  "effect": { "seq": [] }
                } }
              ] } },
              { "draw": { "player": "chosenPlayer", "count": 1 } }
            ] }
            """, cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Contains(game.Pending!.Affordances, option => option.Id == 0);
        Assert.DoesNotContain(game.Pending.Affordances, option => option.Id == 1);
    }
}
