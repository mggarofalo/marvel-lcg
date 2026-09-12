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
public sealed class ActionAbilityPaymentsAndAuthorizationAForcedActionOnACardThatLeavesTests
{
    [Rule("rr:action.2")]
    [Rule("rr:leaves-play.1")]
    [Fact]
    public void AForcedActionOnACardThatLeavesAndReturnsBelongsToTheNewCopy()
    {
        var runner = Runner("01101", "ForcedAction", """
            { "seq": [
              { "discard": "this" },
              { "putIntoPlay": { "card": "this", "where": "engagedWithYou" } },
              { "discard": "this" }
            ] }
            """, limit: 1);
        Card? source = null;
        var(game, _) = Playing(board => source = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))), abilities: runner);
        int firstCopy = source!.Incarnation;
        game.Resolve(Decision.Decline);
        var forced = Assert.Single(game.Pending!.Affordances);
        game.Resolve(Decision.Take(forced.Id));
        Assert.True(source.Incarnation > firstCopy);
        Assert.Equal(DeckType.EngagedEnemiesArea, source.Area.Type);
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.False(game.Pending!.Cancellable);
        Assert.Contains(game.Pending.Affordances, option => option.AnchorId == source.ObjectId);
    }

    [Rule("rr:limit")]
    [Fact]
    public void LimitedAbilitiesOnOneCardHaveIndependentUses()
    {
        var runner = new Marvel.Cards.Run.AbilityRunner(Marvel.Cards.Dsl.AbilityCatalog.Parse($$"""
                { "cards": [ { "card": "{{AuthoredCards.AuntMay}}", "abilities": [
                  {
                    "trigger": { "event": "WhenActionTriggered", "timing": "ForcedAction", "subject": "game" },
                    "limitPerRound": 1,
                    "effect": { "draw": { "player": "you", "count": 1 } }
                  },
                  {
                    "trigger": { "event": "WhenActionTriggered", "timing": "ForcedAction", "subject": "game" },
                    "limitPerRound": 1,
                    "effect": { "draw": { "player": "you", "count": 1 } }
                  }
                ] } ] }
                """));
        var(game, world) = Playing(board => InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Take(game.Pending!.Affordances[0].Id));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.False(game.Pending!.Cancellable);
        game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances).Id));
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
    }

    [Rule("rr:event.1")]
    [Fact]
    public void PlayingAnEventChoosesOneOfItsTriggeredAbilities()
    {
        // Both actions belong to the same event. Their affordance ids retain
        // the printed ordinal, so choosing the second resolves only its two-card
        // draw and does not also resolve the first ability.
        var runner = new Marvel.Cards.Run.AbilityRunner(Marvel.Cards.Dsl.AbilityCatalog.Parse("""
                { "cards": [ { "card": "01003", "abilities": [
                  { "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                    "effect": { "draw": { "player": "you", "count": 1 } } },
                  { "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                    "effect": { "draw": { "player": "you", "count": 2 } } }
                ] } ] }
                """));
        Card? eventCard = null;
        var(game, world) = Playing(board =>
        {
            Hand(board, AuthoredCards.Backflip, 0);
            eventCard = board.CreateCard(AuthoredCards.Backflip, board.Seats[0].Hand);
        }, hero: true, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var choices = game.Pending!.Affordances.Where(option => option.AnchorId == eventCard!.ObjectId).ToList();
        Assert.Equal(2, choices.Count);
        game.Resolve(Decision.Take(choices[1].Id));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, eventCard!.Area.Type);
    }

    [Rule("rr:cost.6")]
    [Rule("rr:choose-game-element.2")]
    [Rule("rr:event.3")]
    [Fact]
    public void AnEventWithNoValidTargetCannotBeOfferedOrForged()
    {
        // If a player-card ability requires targets and "there are no valid
        // targets for any part of the ability, the ability cannot be
        // initiated." The same check runs again at execution, before the event
        // leaves the hand or a payment source can be spent.
        var runner = Runner(AuthoredCards.Backflip, "Action", """{ "chooseCard": { "from": { "query": "minions" }, "effect": { "discard": "chosen" } } }""");
        Card? card = null;
        var(_, world) = Playing(board =>
        {
            Hand(board, AuthoredCards.Backflip, 0);
            card = board.CreateCard(AuthoredCards.Backflip, board.Seats[0].Hand);
        }, hero: true, abilities: runner);
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == card!.ObjectId);
        var forged = new PendingAbility(card!.ObjectId, AbilityType.Action, 0);
        Assert.Throws<RulesNotImplementedException>(() => runner.Act(world, forged, [], []));
        Assert.Same(world.Seats[0].Hand, card.Area);
    }

    [Rule("rr:limit.1")]
    [Fact]
    public void ACancelledLimitedAttackStillUsesItsLimit()
    {
        var runner = Runner("01017", "Action", """{ "chooseCard": { "from": { "query": "attackableEnemies" }, "effect": { "attack": { "target": "chosen", "effect": { "dealAttackDamage": { "cards": "chosen", "amount": 1 } } } } } }""", limit: 1);
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = board.CreateCard("01017", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), board.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
            Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Stunned);
        }, hero: true, abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(villain.ObjectId));
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(0, villain.Damage);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:max-maximum")]
    [Rule("rr:max-maximum.1")]
    [Fact]
    public void APeriodMaximumIsSharedByTitleAcrossPlayersAndExpires()
    {
        // “Max 1 per round” is across all copies by title for all players,
        // unlike a Limit, which belongs to each instance of an ability.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "draw": { "player": "you", "count": 1 } }""", maximum: "Round");
        Card? first = null;
        Card? second = null;
        var(_, world) = Playing(board =>
        {
            first = InPlay(board, AuthoredCards.AuntMay);
            second = board.CreateCard(AuthoredCards.AuntMay, board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == first!.ObjectId);
        runner.Act(world, action, [], []);
        Assert.DoesNotContain(runner.Actions(world, 1), pending => pending.Card == second!.ObjectId);
        world.Effects.Expire(TimingPoints.EndOfRound);
        Assert.Contains(runner.Actions(world, 1), pending => pending.Card == second!.ObjectId);
    }

    [Rule("rr:max-maximum.1")]
    [Fact]
    public void APhaseMaximumExpiresAtTheEndOfEitherPhase()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "draw": { "player": "you", "count": 1 } }""", maximum: "Phase");
        Card? first = null;
        Card? second = null;
        var(_, world) = Playing(board =>
        {
            first = InPlay(board, AuthoredCards.AuntMay);
            second = InPlay(board, AuthoredCards.AuntMay);
        }, abilities: runner);
        runner.Act(world, Assert.Single(runner.Actions(world, 0), pending => pending.Card == first!.ObjectId), [], []);
        Assert.DoesNotContain(runner.Actions(world, 0), pending => pending.Card == second!.ObjectId);
        world.Effects.Expire(TimingPoints.EndOfPhase);
        Assert.Contains(runner.Actions(world, 0), pending => pending.Card == second!.ObjectId);
    }

    [Rule("rr:max-maximum.1")]
    [Fact]
    public void AGameMaximumSurvivesPhaseAndRoundBoundaries()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "draw": { "player": "you", "count": 1 } }""", maximum: "Game");
        Card? first = null;
        Card? second = null;
        var(_, world) = Playing(board =>
        {
            first = InPlay(board, AuthoredCards.AuntMay);
            second = InPlay(board, AuthoredCards.AuntMay);
        }, abilities: runner);
        runner.Act(world, Assert.Single(runner.Actions(world, 0), pending => pending.Card == first!.ObjectId), [], []);
        world.Effects.Expire(TimingPoints.EndOfPhase);
        world.Effects.Expire(TimingPoints.EndOfRound);
        Assert.DoesNotContain(runner.Actions(world, 0), pending => pending.Card == second!.ObjectId);
    }

    [Rule("rr:max-maximum.1.1")]
    [Fact]
    public void ACanceledUseStillCountsTowardACardMaximum()
    {
        var runner = Runner("01017", "Action", """{ "attack": { "target": { "query": "villain" }, "effect": { "dealAttackDamage": { "cards": { "query": "villain" }, "amount": 1 } } } }""", maximum: "Round");
        Card? first = null;
        Card? second = null;
        var(_, world) = Playing(board =>
        {
            first = board.CreateCard("01017", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), board.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
            second = board.CreateCard("01017", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), board.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
            Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Stunned);
        }, hero: true, abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == first!.ObjectId);
        runner.Act(world, action, [], []);
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.DoesNotContain(runner.Actions(world, 0), pending => pending.Card == second!.ObjectId);
    }
}
