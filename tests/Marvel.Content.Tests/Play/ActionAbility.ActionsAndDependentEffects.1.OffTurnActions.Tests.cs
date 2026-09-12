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
public sealed class ActionAbilityActionsAndDependentEffectsOffTurnActionsTests
{
    [Rule("rr:player-turn.6")]
    [Rule("rr:initiating-abilities")]
    [Fact]
    public void OffTurnActionsAreNotInjectedIntoADependentAbilityQuestion()
    {
        // The other player's permission exists “during the active player's
        // turn,” but it is permission to initiate an Action at the turn menu.
        // Once an ability has initiated, its target and option questions are
        // the ordered sequence in rr:initiating-abilities, not fresh turn
        // menus into which another untimed Action may be inserted.
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "choose": { "options": [ { "draw": { "player": "you", "count": 1 } }, { "heal": { "card": "you", "amount": 1 } } ] } }""");
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Affordance action = Assert.Single(game.PromptFor(0)!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(Question.Option, game.Pending!.Asking);
        Assert.Same(game.Pending, game.PromptFor(0));
        Assert.Null(game.PromptFor(1));
    }

    [Rule("rr:cost")]
    [Rule("rr:player-turn.5")]
    [Fact]
    public void AnEncounterCardsActionIsAnybodysAndCostsResources()
    {
        // "Attach to Rhino. **Hero Action**: Spend [physical][physical][physical]
        // resources → discard this card." An encounter card in play, so
        // `rr:player-turn.5.b` is what lets a player trigger it -- it is
        // nobody's card and everybody's action.
        Card? horn = null;
        var(game, world) = Playing(board =>
        {
            horn = board.CreateCard(AuthoredCards.IvoryHorn, board.AreaOf(DeckType.RevealingArea));
            // Through the reveal: `rr:attach-to` makes "Attach to Rhino" a
            // rule about the card entering play rather than a "When
            // Revealed" ability, so the route in is what attaches it.
            board.Abilities = AuthoredCards.Runner();
            Reveal.Resolve(board, CardCatalogData, horn, 0, []);
            Physical(board, 3);
        }, hero: true);
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);
        // The cost is on the wire, with what could pay it -- a resource cost is
        // a choice of *which* cards, so the client has to be told.
        var price = Assert.Single(action.CostOptions);
        Assert.Equal("3", price.Cost);
        Assert.True(price.Generators.Count(source => source.Generates == "R") >= 3);
        int[] paying = [..world.Seats[0].Hand.Cards.Where(card => card.FaceId == Physicals).Take(3).Select(card => card.ObjectId)];
        game.Resolve(Decision.Take(action.Id, [], paying));
        Assert.Equal(DeckType.EncounterDiscardPile, horn!.Area.Type);
        Assert.All(paying, id => Assert.Equal(DeckType.DiscardPile, world.Cards[id].Area.Type));
    }

    [Rule("rr:initiating-abilities.step.3")]
    [Fact]
    public void AnActionIsNotOfferedToAHandThatCannotPayIt()
    {
        // The whole hand cannot make three physicals, so the action is not
        // offered at all. `rr:cost.4` permits generating beyond the cost, so
        // asking the whole hand is the right question rather than an
        // approximation: if everything together cannot pay, no choice among it
        // can.
        var(game, _) = Playing(board =>
        {
            var horn = board.CreateCard(AuthoredCards.IvoryHorn, board.AreaOf(DeckType.RevealingArea));
            AuthoredCards.Runner().WhenRevealed(board, horn, 0);
            Physical(board, 2);
        }, hero: true);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);
    }

    [Rule("rr:and")]
    [Rule("rr:and.1")]
    [Rule("rr:and.2")]
    [Rule("rr:first-player.3")]
    [Fact]
    public void AndEffectsResolveIndependentlyInsideOneAbility()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "and": [ { "dealDamage": { "cards": "you", "amount": 1 } }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Tough);
        }, hero: true, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(Question.Order, game.Pending.Asking);
        game.Resolve(new Decision(order.Id, [0, 1]));
        // Tough independently prevents the damage effect. The draw connected
        // by “and” still resolves, and no response prompt separated the two.
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Tough));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:first-player.3")]
    [Fact]
    public void FirstPlayerChoosesTheOrderOfAndEffects()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "and": [ { "exhaust": "this" }, { "ready": "this" } ] }""");
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [1, 0]));
        Assert.False(source!.Ready);
    }

    [Rule("rr:first-player.3")]
    [Fact]
    public void AndOrderResumesThroughTheActiveConditionBranch()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "and": [ { "exhaust": "this" }, { "ready": "this" } ] }, "else": { "and": [ { "draw": { "player": "you", "count": 1 } }, { "heal": { "card": "you", "amount": 1 } } ] } } }""");
        Card? source = null;
        var(game, _) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [1, 0]));
        Assert.False(source!.Ready);
    }

    [Rule("rr:and.1")]
    [Fact]
    public void AndResumesAfterAReachableNestedSuspender()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "and": [ { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "exhaust": "this" }, "else": { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } } } }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var(game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [0, 1]));
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:first-player.3")]
    [Fact]
    public void NestedStructuralFramesWaitAndResumeEveryRemainingEffectOnce()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """
            { "seq": [
              { "and": [
                { "if": {
                  "test": { "inForm": { "player": "you", "form": "hero" } },
                  "then": { "choose": { "options": [
                    { "draw": { "player": "you", "count": 1 } },
                    { "heal": { "card": "you", "amount": 1 } }
                  ] } },
                  "else": { "draw": { "player": "you", "count": 8 } }
                } },
                { "draw": { "player": "you", "count": 2 } }
              ] },
              { "draw": { "player": "you", "count": 4 } }
            ] }
            """);
        Card? source = null;
        var(game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), hero: true, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [0, 1]));
        Assert.Equal(held, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(Question.Option, game.Pending!.Asking);
        game.Resolve(Decision.Take(0));
        Assert.Equal(held + 7, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
    }

    [Rule("rr:then")]
    [Rule("rr:then.1")]
    [Rule("rr:then.2")]
    [Rule("rr:resolve.1")]
    [Theory]
    [InlineData(2, true)]
    [InlineData(1, false)]
    public void ThenRequiresThePrecedingEffectToResolveInFull(int threat, bool draws)
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "then": { "effect": { "removeThreat": { "scheme": { "query": "mainScheme" }, "amount": 2 } }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", threat);
        }, hero: true, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + (draws ? 1 : 0), world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:then.2")]
    [Fact]
    public void ThenIgnoresCharactersThatAreNotValidExhaustTargets()
    {
        var runner = Runner(AuthoredCards.AuntMay, "Action", """{ "then": { "effect": { "exhaust": { "query": "charactersYouControl" } }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        Card? ally = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, AuthoredCards.AuntMay);
            ally = board.CreateCard("01002", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            ally.Exhaust();
        }, hero: true, abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.False(world.Seats[0].IdentityCard.Ready);
        Assert.False(ally!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }
}
