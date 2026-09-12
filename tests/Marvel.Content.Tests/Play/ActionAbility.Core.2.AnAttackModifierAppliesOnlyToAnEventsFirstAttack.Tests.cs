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
public sealed class ActionAbilityCoreAnAttackModifierAppliesOnlyToAnEventsFirstAttackTests
{
    [Rule("rr:attack-player-ability-type.3")]
    [Rule("rr:attack-player-ability-type.3.1")]
    [Rule("rr:event.5.1")]
    [Fact]
    public void AnAttackModifierAppliesOnlyToAnEventsFirstAttack()
    {
        // Each listed damage instance is a separate attack. A modifier to one
        // attack therefore increases only the first: it deals three and the
        // second deals one.
        var runner = Runner("01005", "Action", """
            { "seq": [
              { "dealAttackDamage": { "cards": { "query": "villain" }, "amount": 1 } },
              { "dealAttackDamage": { "cards": { "query": "villain" }, "amount": 1 } }
            ] }
            """);
        Card? played = null;
        var payments = new List<Card>();
        var(_, world) = Playing(board =>
        {
            played = board.CreateCard("01005", board.Seats[0].Hand);
            for (int index = 0; index < 3; index++)
            {
                payments.Add(board.CreateCard("01087", board.Seats[0].Hand));
            }
        }, hero: true, abilities: runner);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "attackDamage", Amount: 2, Card: played!.ObjectId, Affects: played.ObjectId, Lasts: new Duration(Uses: 1)));
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == played.ObjectId);
        runner.Act(world, action, [..payments.Select(card => card.ObjectId)], []);
        Assert.Equal(4, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:attack-player-ability-type.2.1")]
    [Rule("rr:attack-player-ability-type.3.1")]
    [Rule("rr:event.5.1")]
    [Fact]
    public void AnAttackModifierIsConsumedAtTheAttackWrapperBoundary()
    {
        // Each wrapper is one attack even when its effect uses generic damage.
        // The modifier remains through the first wrapper and is gone before
        // the second attack begins.
        var runner = Runner("01005", "Action", """
            { "seq": [
              { "attack": {
                "target": { "query": "villain" },
                "effect": { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              } },
              { "attack": {
                "target": { "query": "villain" },
                "effect": { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              } }
            ] }
            """);
        Card? played = null;
        var payments = new List<Card>();
        var(game, world) = Playing(board =>
        {
            played = board.CreateCard("01005", board.Seats[0].Hand);
            for (int index = 0; index < 3; index++)
            {
                payments.Add(board.CreateCard("01087", board.Seats[0].Hand));
            }
        }, hero: true, abilities: runner);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "attackDamage", Amount: 2, Card: played!.ObjectId, Affects: played.ObjectId, Lasts: new Duration(Uses: 1)));
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == played.ObjectId);
        game.Resolve(Decision.Take(action.Id, [], [..payments.Select(card => card.ObjectId)]));
        Assert.Equal(4, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:attack-player-ability-type.2.1")]
    [Rule("rr:attack-player-ability-type.2.2")]
    [Rule("rr:event.5.1")]
    [Fact]
    public void EveryDamageInstanceInsideTheFirstAttackIsModified()
    {
        // A labelled attack remains one attack across multiple damage
        // instances, and an increase applies to each instance that does not
        // say "additional". Both one-damage nodes therefore become three.
        var runner = Runner("01005", "Action", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } },
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              ] }
            } }
            """);
        Card? played = null;
        var payments = new List<Card>();
        var(game, world) = Playing(board =>
        {
            played = board.CreateCard("01005", board.Seats[0].Hand);
            for (int index = 0; index < 3; index++)
            {
                payments.Add(board.CreateCard("01087", board.Seats[0].Hand));
            }
        }, hero: true, abilities: runner);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "attackDamage", Amount: 2, Card: played!.ObjectId, Affects: played.ObjectId, Lasts: new Duration(Uses: 1)));
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == played.ObjectId);
        game.Resolve(Decision.Take(action.Id, [], [..payments.Select(card => card.ObjectId)]));
        Assert.Equal(6, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:player-elimination.5")]
    [Rule("rr:player-elimination.step.5")]
    [Rule("rr:upgrade.1")]
    [Fact]
    public void APlayerEliminatedByAnActionCostFinishesItAndTheirTurn()
    {
        // An upgrade is "active so long as it is in play". Focused Rage's Hero
        // Action deals one damage to its player as a cost.
        // At one remaining hit point that eliminates She-Hulk, but the ability
        // still completes. She no longer participates, so the engine asks the
        // next player instead of constructing another prompt for seat zero.
        Card? rage = null;
        var(game, world) = Playing(board =>
        {
            board.Seats[0].IdentityCard.TurnTo("01019a");
            board.Seats[0].IdentityCard.TakeDamage(14);
            rage = board.CreateCard("01027", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        }, heroes: ["she_hulk", "spider_man"]);
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == rage!.ObjectId);
        var resolved = game.Resolve(Decision.Take(action.Id));
        Assert.True(world.Seats[0].Eliminated);
        Assert.False(world.IsOver);
        Assert.Equal(1, game.Active);
        Assert.Equal(1, game.Pending!.Player);
        Assert.Equal(Question.TurnOption, game.Pending.Asking);
        Assert.All(resolved.Events, happened => Assert.Equal(Steps.TurnAction, happened.Trigger));
        game.Resolve(Decision.Decline);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
        Assert.Equal(1, game.Pending!.Player);
    }

    [Rule("rr:player-turn.5")]
    [Rule("rr:event")]
    [Rule("rr:initiating-abilities.step.1")]
    [Rule("rr:initiating-abilities.step.7")]
    [Fact]
    public void AnEventActionResumesItsOccurrenceAfterChoosingATarget()
    {
        Card? kick = null;
        Card? genius = null;
        Card? energy = null;
        Card? minion = null;
        var(game, world) = Playing(board =>
        {
            foreach (var card in board.Seats[0].Hand.Cards.ToList())
            {
                World.MoveToTop(card, board.Seats[0].Deck);
            }

            kick = board.CreateCard(AuthoredCards.SwingingWebKick, board.Seats[0].Hand);
            genius = board.CreateCard("01089", board.Seats[0].Hand);
            energy = board.CreateCard("01088", board.Seats[0].Hand);
            minion = board.CreateCard("01101", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, hero: true);
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == kick!.ObjectId);
        var paying = new[]
        {
            genius!.ObjectId,
            energy!.ObjectId
        };
        var suspended = game.Resolve(Decision.Take(action.Id, [], paying));
        Assert.Equal(Question.Element, suspended.Prompt!.Asking);
        Assert.Equal(DeckType.RevealingArea, kick!.Area.Type);
        Assert.Equal(PlayArea.Of(0), kick.Area.PlayArea);
        Assert.True(kick.FaceUp);
        Assert.False(DeckTypes.IsInPlay(kick.Area.Type));
        var finished = game.Resolve(Decision.Take(minion!.ObjectId));
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(DeckType.DiscardPile, kick.Area.Type);
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(Question.TurnOption, finished.Prompt!.Asking);
        var damage = Assert.Single(finished.Events.OfType<FieldSet>(), happened => happened.Card == minion.ObjectId && happened.Field == "health");
        Assert.Equal(Steps.TurnAction, damage.Trigger);
        Assert.Equal(paying, suspended.Events.OfType<CardsMoved>().SelectMany(moved => moved.Cards).Select(card => card.Card).Where(paying.Contains));
    }

    [Rule("rr:ability.2")]
    [Rule("rr:in-play-and-out-of-play.4")]
    [Rule("rr:in-play-and-out-of-play.7")]
    [Rule("rr:in-play-and-out-of-play.8")]
    [Rule("rr:ownership-and-control.4")]
    [Fact]
    public void ActionsComeFromInPlayCardsAndEventsInHand()
    {
        // Hero, alter-ego, ally, upgrade, and support abilities "may only be
        // used if the card is in play"; cards in a player's hand, deck, and
        // discard pile are out of play. Events implicitly resolve from an
        // out-of-play area, so the event in hand is offered while Focused Rage
        // is silent from each of those three areas. The live copy is the
        // control that proves the card's action itself is reachable.
        Card? discardedRage = null;
        Card? heldRage = null;
        Card? deckRage = null;
        Card? liveRage = null;
        Card? kick = null;
        var(game, _) = Playing(board =>
        {
            discardedRage = board.CreateCard("01027", board.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
            heldRage = board.CreateCard("01027", board.Seats[0].Hand);
            deckRage = board.CreateCard("01027", board.Seats[0].Deck);
            liveRage = board.CreateCard("01027", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
            kick = board.CreateCard(AuthoredCards.SwingingWebKick, board.Seats[0].Hand);
            board.Seats[0].IdentityCard.TakeDamage(1);
        }, hero: true);
        int[] outOfPlay = [discardedRage!.ObjectId, heldRage!.ObjectId, deckRage!.ObjectId];
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && outOfPlay.Contains(option.AnchorId));
        Assert.Contains(game.Pending.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == liveRage!.ObjectId);
        Assert.Contains(game.Pending.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == kick!.ObjectId);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void ARejectedActionCommandDoesNotRemainOnTheAgenda()
    {
        Card? kick = null;
        Card? genius = null;
        Card? energy = null;
        var(game, world) = Playing(board =>
        {
            foreach (var card in board.Seats[0].Hand.Cards.ToList())
            {
                World.MoveToTop(card, board.Seats[0].Deck);
            }

            kick = board.CreateCard(AuthoredCards.SwingingWebKick, board.Seats[0].Hand);
            genius = board.CreateCard("01089", board.Seats[0].Hand);
            energy = board.CreateCard("01088", board.Seats[0].Hand);
        }, hero: true);
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == kick!.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => game.Resolve(Decision.Take(action.Id)));
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(DeckType.HandsArea, kick!.Area.Type);
        Assert.Equal(DeckType.HandsArea, genius!.Area.Type);
        Assert.Equal(DeckType.HandsArea, energy!.Area.Type);
        var retry = game.Resolve(Decision.Take(action.Id, [], [genius.ObjectId, energy.ObjectId]));
        Assert.Equal(Question.Element, retry.Prompt!.Asking);
    }
}
