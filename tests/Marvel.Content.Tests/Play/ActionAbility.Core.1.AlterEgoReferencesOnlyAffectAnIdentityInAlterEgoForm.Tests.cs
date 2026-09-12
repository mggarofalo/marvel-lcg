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
public sealed class ActionAbilityCoreAlterEgoReferencesOnlyAffectAnIdentityInAlterEgoFormTests
{
    [Rule("rr:form-change-form.4")]
    [Theory]
    [InlineData(true, 2)]
    [InlineData(false, 1)]
    public void AlterEgoReferencesOnlyAffectAnIdentityInAlterEgoForm(bool hero, int remainingDamage)
    {
        // "While a player is in hero form, card abilities that interact with
        // their alter-ego do not interact with their identity." The explicit
        // alter-ego selector therefore has no target in hero form and names the
        // same physical identity card after it changes to alter-ego form.
        var runner = Runner("01017", "Action", """{ "heal": { "card": "yourAlterEgo", "amount": 1 } }""");
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = board.CreateCard("01017", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), board.Seats[0].IdentityCard.ObjectId, cardOwner: 0));
            board.Seats[0].IdentityCard.TakeDamage(2);
        }, hero: hero, abilities: runner);
        var action = game.Pending!.Affordances.SingleOrDefault(option => option.AnchorId == source!.ObjectId);
        if (hero)
        {
            Assert.Null(action);
        }
        else
        {
            game.Resolve(Decision.Take(Assert.IsType<Affordance>(action).Id));
        }

        Assert.Equal(remainingDamage, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:player-turn.5")]
    [Rule("rr:support.2")]
    [Fact]
    public void AnActionIsOfferedOnTheTurnAndDoesWhatItSays()
    {
        // "Alter-Ego Action: Exhaust Aunt May → heal 4 damage from Peter
        // Parker." The whole path: offered among the turn options, taken, cost
        // paid, effect resolved, and the turn goes on. A support is "active
        // while it is in play", which is why Aunt May offers that action.
        Card? may = null;
        var(game, world) = Playing(board =>
        {
            may = InPlay(board, AuthoredCards.AuntMay);
            board.Seats[0].IdentityCard.TakeDamage(5);
        });
        var identity = world.Seats[0].IdentityCard;
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);
        Assert.Equal(may!.ObjectId, action.AnchorId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(1, identity.Damage);
        Assert.False(may!.Ready);
        // `rr:player-turn`: every option but changing form may be taken "as
        // many times as the player is able", so the turn is still going.
        Assert.NotNull(game.Pending);
        Assert.Equal(Question.TurnOption, game.Pending.Asking);
    }

    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:initiating-abilities.3")]
    [Rule("rr:damage.step.7")]
    [Rule("rr:triggering-condition.2")]
    [Fact]
    public void ASourceDefeatedByItsActionCostStillFinishesThatAction()
    {
        // "Action: Exhaust War Machine and deal 2 damage to him → deal 1
        // damage to each enemy." The cost is paid before the effect. At two
        // remaining hit points that cost defeats and discards War Machine, but
        // rr:initiating-abilities.3 says leaving play does not stop the
        // sequence, and the post-arrow effect still deals damage to every
        // enemy.
        Card? warMachine = null;
        var(game, world) = Playing(board =>
        {
            warMachine = board.CreateCard("01030", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            warMachine.TakeDamage(2);
        });
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == warMachine!.ObjectId);
        var resolved = game.Resolve(Decision.Take(action.Id));
        Assert.Equal(DeckType.DiscardPile, warMachine!.Area.Type);
        Assert.Equal(1, villain.Damage);
        Assert.All(resolved.Events, happened => Assert.Equal(Steps.TurnAction, happened.Trigger));
        int discarded = resolved.Events.Select((happened, index) => (happened, index)).First(pair => pair.happened is CardsMoved moved && moved.Cards.Any(card => card.Card == warMachine.ObjectId)).index;
        int damaged = resolved.Events.Select((happened, index) => (happened, index)).First(pair => pair.happened is FieldSet changed && changed.Card == villain.ObjectId && changed.Field == "health").index;
        Assert.True(discarded >= 0);
        Assert.True(damaged > discarded);
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }

    [Rule("rr:cost.11")]
    [Rule("rr:prevent.1.4")]
    [Fact]
    public void DealingDamageAsACostIsPaidWhenTheDamageIsPrevented()
    {
        // War Machine deals two damage to himself as its cost. "That cost is
        // considered paid even if some or all of that damage is prevented,"
        // so Tough can prevent all of it and the post-arrow damage still
        // resolves against every enemy.
        Card? warMachine = null;
        var(game, world) = Playing(board =>
        {
            warMachine = board.CreateCard("01030", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            Statuses.Give(board, warMachine, Statuses.Tough);
        }, heroes: ["iron_man"]);
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == warMachine!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.False(Statuses.Has(world, warMachine!, Statuses.Tough));
        Assert.Equal(0, warMachine!.Damage);
        Assert.Equal(1, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:cost.12")]
    [Rule("rr:prevent.1.5")]
    [Fact]
    public void TakingDamageAsACostIsUnpaidWhenDamageIsPrevented()
    {
        // A take-damage cost "is not considered paid unless all of that
        // damage was taken." Preventing even part of it therefore suppresses
        // the post-arrow draw.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "draw": { "player": "you", "count": 1 } }""", cost: """
            { "takeDamage": { "cards": "you", "amount": 2 } }
            """);
        Card? source = null;
        var(game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);
        int held = world.Seats[0].Hand.Cards.Count;
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: "preventDamage", Amount: 1, Card: source!.ObjectId, Affects: world.Seats[0].IdentityCard.ObjectId, Lasts: new Duration(Uses: 1)));
        var failure = Assert.Throws<RulesNotImplementedException>(() => runner.Act(world, action, [], []));
        Assert.Contains("only 1 was taken", failure.Message);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:cost.12")]
    [Fact]
    public void TakingAllDamagePaysTheCost()
    {
        // The cost is paid when "all of that damage was taken," so the effect
        // after the arrow resolves.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "draw": { "player": "you", "count": 1 } }""", cost: """
            { "takeDamage": { "cards": "you", "amount": 2 } }
            """);
        Card? source = null;
        var(_, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == source!.ObjectId);
        int held = world.Seats[0].Hand.Cards.Count;
        runner.Act(world, action, [], []);
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:event.5")]
    [Fact]
    public void AnEventDamageModifierAppliesToEveryDamageInstance()
    {
        // When an event "deals multiple instances of damage, each of those
        // instances is modified." Two one-damage instances with +2 each deal
        // six, not four.
        var runner = Runner("01005", "Action", """
            { "seq": [
              { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } },
              { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }
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
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "eventDamage", Amount: 2, Card: played!.ObjectId, Affects: played.ObjectId));
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == played.ObjectId);
        runner.Act(world, action, [..payments.Select(card => card.ObjectId)], []);
        Assert.Equal(6, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Rule("rr:event.5")]
    [Fact]
    public void AnEventThreatModifierAppliesToEveryRemovalInstance()
    {
        // The same clause says that when an event "removes multiple instances
        // of threat, each of those instances is modified."
        var runner = Runner("01005", "Action", """
            { "seq": [
              { "removeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 } },
              { "removeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 } }
            ] }
            """);
        Card? played = null;
        var payments = new List<Card>();
        var(_, world) = Playing(board =>
        {
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 10);
            played = board.CreateCard("01005", board.Seats[0].Hand);
            for (int index = 0; index < 3; index++)
            {
                payments.Add(board.CreateCard("01087", board.Seats[0].Hand));
            }
        }, hero: true, abilities: runner);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "eventThreatRemoval", Amount: 2, Card: played!.ObjectId, Affects: played.ObjectId));
        var action = Assert.Single(runner.Actions(world, 0), pending => pending.Card == played.ObjectId);
        runner.Act(world, action, [..payments.Select(card => card.ObjectId)], []);
        Assert.Equal(4, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
    }
}
