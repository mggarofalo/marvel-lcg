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

public sealed partial class ActionAbilityTests
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
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "choose": { "options": [ { "draw": { "player": "you", "count": 1 } }, { "heal": { "card": "you", "amount": 1 } } ] } }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        Affordance action = Assert.Single(
            game.PromptFor(0)!.Affordances,
            option => option.AnchorId == source!.ObjectId);

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
        var (game, world) = Playing(
            board =>
            {
                horn = board.CreateCard(
                    AuthoredCards.IvoryHorn, board.AreaOf(DeckType.RevealingArea));

                // Through the reveal: `rr:attach-to` makes "Attach to Rhino" a
                // rule about the card entering play rather than a "When
                // Revealed" ability, so the route in is what attaches it.
                board.Abilities = AuthoredCards.Runner();
                Reveal.Resolve(board, Cards, horn, 0, []);
                Physical(board, 3);
            },
            hero: true);

        var action = Assert.Single(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);

        // The cost is on the wire, with what could pay it -- a resource cost is
        // a choice of *which* cards, so the client has to be told.
        var price = Assert.Single(action.CostOptions);
        Assert.Equal("3", price.Cost);
        Assert.True(price.Generators.Count(source => source.Generates == "R") >= 3);

        int[] paying = [.. world.Seats[0].Hand.Cards
            .Where(card => card.FaceId == Physicals)
            .Take(3)
            .Select(card => card.ObjectId)];

        game.Resolve(Decision.Take(action.Id, [], paying));

        Assert.Equal(DeckType.EncounterDiscardPile, horn!.Area.Type);
        Assert.All(
            paying, id => Assert.Equal(DeckType.DiscardPile, world.Cards[id].Area.Type));
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
        var (game, _) = Playing(
            board =>
            {
                var horn = board.CreateCard(
                    AuthoredCards.IvoryHorn, board.AreaOf(DeckType.RevealingArea));
                AuthoredCards.Runner().WhenRevealed(board, horn, 0);
                Physical(board, 2);
            },
            hero: true);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.Verb == Game.ActionVerb);
    }

    [Rule("rr:and")]
    [Rule("rr:and.1")]
    [Rule("rr:and.2")]
    [Rule("rr:first-player.3")]
    [Fact]
    public void AndEffectsResolveIndependentlyInsideOneAbility()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "and": [ { "dealDamage": { "cards": "you", "amount": 1 } }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                Statuses.Give(board, board.Seats[0].IdentityCard, Statuses.Tough);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

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
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "and": [ { "exhaust": "this" }, { "ready": "this" } ] }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [1, 0]));

        Assert.False(source!.Ready);
    }

    [Rule("rr:first-player.3")]
    [Fact]
    public void AndOrderResumesThroughTheActiveConditionBranch()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "and": [ { "exhaust": "this" }, { "ready": "this" } ] }, "else": { "and": [ { "draw": { "player": "you", "count": 1 } }, { "heal": { "card": "you", "amount": 1 } } ] } } }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [1, 0]));

        Assert.False(source!.Ready);
    }

    [Rule("rr:and.1")]
    [Fact]
    public void AndResumesAfterAReachableNestedSuspender()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "and": [ { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "exhaust": "this" }, "else": { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } } } }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [0, 1]));

        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:then")]
    [Rule("rr:then.1")]
    [Rule("rr:then.2")]
    [Rule("rr:resolve.1")]
    [Theory]
    [InlineData(2, true)]
    [InlineData(1, false)]
    public void ThenRequiresThePrecedingEffectToResolveInFull(
        int threat, bool draws)
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "removeThreat": { "scheme": { "query": "mainScheme" }, "amount": 2 } }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", threat);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!
            .Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + (draws ? 1 : 0), world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:then.2")]
    [Fact]
    public void ThenIgnoresCharactersThatAreNotValidExhaustTargets()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "exhaust": { "query": "charactersYouControl" } }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        Card? ally = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                ally = board.CreateCard(
                    "01002",
                    board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
                ally.Exhaust();
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.False(world.Seats[0].IdentityCard.Ready);
        Assert.False(ally!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:then.2")]
    [Fact]
    public void ThenIgnoresSchemesThatAreNotValidThreatTargets()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "removeThreat": { "scheme": { "query": "sideSchemes" }, "amount": 2 } }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        Card? empty = null;
        Card? threatened = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                empty = board.CreateCard("01107", board.AreaOf(DeckType.SideSchemesArea));
                threatened = board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea));
                threatened.PlaceTokens("k_threat", 2);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(0, empty!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(0, threatened!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void AThreatPlacementBeforeThenRaisesBeforeChangingTheBoard()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "placeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 } }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("none/partial/full", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:otherwise")]
    [Rule("rr:otherwise.1")]
    [Rule("rr:otherwise.1.2")]
    [Rule("rr:otherwise.2")]
    [Theory]
    [InlineData(0, true)]
    [InlineData(1, false)]
    public void OtherwiseRequiresThePrecedingEffectToResolveNotAtAll(
        int threat, bool draws)
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            // `effect` explicitly delimits the preceding effect. That preserves
            // rr:otherwise.2's semicolon/sentence boundary without recovering
            // punctuation after printed text has become an ability tree.
            """{ "otherwise": { "effect": { "removeThreat": { "scheme": { "query": "mainScheme" }, "amount": 2 } }, "otherwise": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", threat);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!
            .Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + (draws ? 1 : 0), world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:otherwise.1.2")]
    [Fact]
    public void OtherwiseResolvesWhenThreatRemovalIsProhibited()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "otherwise": { "effect": { "removeThreat": { "scheme": { "titled": "Countdown to Oblivion" }, "amount": 2 } }, "otherwise": { "draw": { "player": "you", "count": 1 } } } }""",
            includeAuthored: true);
        Card? source = null;
        Card? scheme = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                scheme = board.CreateCard("01139b", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 2);
            },
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(2, scheme!.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:otherwise")]
    [Fact]
    public void OtherwiseTreatsAMissingPrecedingTargetAsNoResolution()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "otherwise": { "effect": { "discard": { "titled": "Missing Card" } }, "otherwise": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:otherwise.1.1")]
    [Fact]
    public void OtherwiseResolvesWhenThePrecedingConditionIsFalse()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "otherwise": { "effect": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "exhaust": "this" } } }, "otherwise": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.True(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:otherwise.1.2")]
    [Fact]
    public void AnUnusedOtherwiseBranchDoesNotAskForItsChoice()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "otherwise": { "effect": { "removeThreat": { "scheme": { "query": "mainScheme" }, "amount": 2 } }, "otherwise": { "choose": { "options": [ { "exhaust": "this" }, { "draw": { "player": "you", "count": 1 } } ] } } } }""",
            limit: 1);
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.TheCardIn(DeckType.MainSchemesArea)!
                    .PlaceTokens("k_threat", 1);
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Equal(0, world.TheCardIn(DeckType.MainSchemesArea)!
            .Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.True(source!.Ready);
    }

    [Rule("rr:then.1")]
    [Fact]
    public void AChoiceInAnInactiveConditionBranchDoesNotSuspendThen()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "exhaust": "this" }, "else": { "choose": { "options": [ { "draw": { "player": "you", "count": 1 } }, { "heal": { "card": "you", "amount": 1 } } ] } } } }, "then": { "draw": { "player": "you", "count": 1 } } } }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ADependentEffectResumesAfterItsChoice()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "choose": { "options": [ { "exhaust": "this" }, { "draw": { "player": "you", "count": 1 } } ] } }, "then": { "discard": "this" } } }""");
        Card? source = null;

        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(0));

        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
    }

    [Fact]
    public void ADependentEachPlayerEffectResumesTheAbility()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "draw": { "player": "you", "count": 1 } }, "then": { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.False(source!.Ready);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ADependentFirstActivationPersistsItsOuterContinuation()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "WhenRevealed",
            """{ "seq": [ { "then": { "effect": { "draw": { "player": "you", "count": 1 } }, "then": { "enemyAttacks": { "enemies": { "query": "villain" }, "first": "true" } } } }, { "draw": { "player": "you", "count": 1 } } ] }""",
            eventName: Steps.CardRevealed);
        Card? source = null;

        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;

        runner.WhenRevealed(world, source!, 0);
        var attack = Assert.Single(
            world.Agenda.Outstanding, pending => pending.What == Steps.Attack);

        runner.ActivationCompleted(world, new EnemyActivation(
            attack.Subject, attack.Seat, Attacking: true, attack.ActivationId, Made: false));

        Assert.True(source!.Ready);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void AChoiceOptionCanSuspendInsideAnd()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "choose": { "options": [ { "draw": { "player": "you", "count": 1 } }, { "and": [ { "draw": { "player": "you", "count": 1 } }, { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } } ] } ] } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(1));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [0, 1]));

        Assert.False(source!.Ready);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ACostCanSwitchAnIfIntoAResumableBranch()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "and": [ { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } }, { "draw": { "player": "you", "count": 1 } } ] } } }""",
            cost: """{ "discard": "this" }""");
        Card? source = null;

        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [0, 1]));

        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        Assert.Equal(held + 2, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ACostCannotSwitchAnIfIntoAnUnknownDependentOutcome()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "then": { "effect": { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }, "then": { "draw": { "player": "you", "count": 1 } } } } } }""",
            cost: """{ "discard": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("none/partial/full resolution", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void ACostCannotSwitchADependentPredecessorToAnUnknownOutcome()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "if": { "test": { "titleInPlay": "Aunt May" }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } } } }, "then": { "draw": { "player": "you", "count": 1 } } } }""",
            cost: """{ "discard": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("none/partial/full resolution", thrown.Message, StringComparison.Ordinal);
        Assert.Equal(DeckType.SupportsArea, source!.Area.Type);
    }

    [Fact]
    public void AnEarlierStepCannotSwitchADependentPredecessorToAnUnknownOutcome()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "seq": [ { "changeForm": { "player": "you", "to": "alter-ego" } }, { "then": { "effect": { "if": { "test": { "inForm": { "player": "you", "form": "alter-ego" } }, "then": { "dealDamage": { "cards": { "query": "villain" }, "amount": 1 } }, "else": { "draw": { "player": "you", "count": 1 } } } }, "then": { "draw": { "player": "you", "count": 1 } } } } ] }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("none/partial/full resolution", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Fact]
    public void ACostCanActivateAResumableOtherwiseBranch()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "otherwise": { "effect": { "exhaust": "this" }, "otherwise": { "choose": { "options": [ { "draw": { "player": "you", "count": 1 } }, { "heal": { "card": "you", "amount": 1 } } ] } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(0));

        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void AnEarlierStepCanActivateAResumableOtherwiseBranch()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "seq": [ { "exhaust": "this" }, { "otherwise": { "effect": { "exhaust": "this" }, "otherwise": { "choose": { "options": [ { "draw": { "player": "you", "count": 1 } }, { "heal": { "card": "you", "amount": 1 } } ] } } } } ] }""");
        Card? source = null;

        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(0));

        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ANestedSequenceResumesPastAnEarlierMutationBoundary()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "seq": [ { "exhaust": "this" }, { "seq": [ { "otherwise": { "effect": { "exhaust": "this" }, "otherwise": { "choose": { "options": [ { "draw": { "player": "you", "count": 1 } }, { "heal": { "card": "you", "amount": 1 } } ] } } } } ] } ] }""");
        Card? source = null;

        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        game.Resolve(Decision.Take(0));

        Assert.False(source!.Ready);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Fact]
    public void ADirectSequencePersistsItsNestedContinuationAfterItsFirstMutation()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "WhenRevealed",
            """{ "seq": [ { "draw": { "player": "you", "count": 1 } }, { "and": [ { "draw": { "player": "you", "count": 1 } }, { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } } ] } ] }""",
            eventName: Steps.CardRevealed);
        Card? source = null;
        var (_, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner);
        int held = world.Seats[0].Hand.Cards.Count;

        runner.WhenRevealed(world, source!, 0);

        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        var waiting = Assert.Single(world.Agenda.Outstanding);
        Assert.Equal(Steps.ChooseOption, waiting.What);
        Assert.Equal(["seq:1"], waiting.AbilityPath);
    }

    [Rule("rr:and.1")]
    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void AnOrderedCardAttackResumesTheRemainingSimultaneousEffect()
    {
        // The first player chooses the order of the independent effects. The
        // attack has its own agenda procedure, so the later draw must wait for
        // that procedure and then resume from the exact `and` branch.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "and": [ { "attack": { "target": { "query": "villain" }, "effect": { "dealAttackDamage": { "cards": { "query": "villain" }, "amount": 1 } } } }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        int held = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(order.Id, [0, 1]));

        Assert.Equal(1, villain.Damage);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void LeavingTheFinalEachPlayerFrameRestoresTheOriginalResolver()
    {
        // Each frame reads "you" as that frame's player. Text after the frame
        // belongs to the player resolving the ability, not whichever player
        // the first player put last in the chosen order.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "seq": [ { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } }, { "draw": { "player": "you", "count": 1 } } ] }""");
        Card? source = null;
        var (game, world) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        int firstHeld = world.Seats[0].Hand.Cards.Count;
        int secondHeld = world.Seats[1].Hand.Cards.Count;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        game.Resolve(new Decision(
            order.Id,
            [world.Seats[0].IdentityCard.ObjectId, world.Seats[1].IdentityCard.ObjectId]));

        Assert.Equal(firstHeld + 2, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(secondHeld + 1, world.Seats[1].Hand.Cards.Count);
    }

    [Rule("rr:and.1")]
    [Fact]
    public void UnsupportedThreatPlacementOrderingRaisesBeforeTheActionCost()
    {
        // Threat placement has its own interrupt and response windows. Until
        // that agenda record carries a structural card continuation, accepting
        // an order that places it first would silently skip the later effect.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "and": [ { "placeThreat": { "scheme": { "query": "mainScheme" }, "amount": 1 } }, { "draw": { "player": "you", "count": 1 } } ] }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("threat placement continuation", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:each-player.1")]
    [Fact]
    public void NestedEachPlayerFramesRaiseBeforeTheActionCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "choose": { "options": [ { "eachPlayer": { "effect": { "draw": { "player": "you", "count": 1 } } } }, { "draw": { "player": "you", "count": 1 } } ] } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("nests one each-player", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void ACardPowerThatWouldSuspendRaisesBeforeTheActionCost()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "chooseCard": { "from": { "query": "attackableEnemies" }, "effect": { "attack": { "target": "chosen", "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void LegalPracticePowerThatWouldSuspendRaisesBeforeTheActionCost()
    {
        // A labeled thwart ability "is considered to be a thwart made by that
        // player's identity." Its whole nested power must be preflighted.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "legalPractice": { "schemes": { "query": "thwartableSchemes" }, "power": { "thwart": { "target": "chosen", "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:labeled-ability.4")]
    [Rule("rr:for-each")]
    [Fact]
    public void LegalPracticeCanBindAForEachPowerAmountAfterItIsOffered()
    {
        // powerAmount is unbound while the Legal Practice prompt is built.
        // That sentinel is not an authored negative count: the selected hand
        // cards bind it before the labelled thwart effect resolves.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "legalPractice": {
              "schemes": { "query": "thwartableSchemes" },
              "power": { "thwart": {
                "target": "chosen",
                "effect": { "forEach": {
                  "count": { "powerAmount": "cardsDiscarded" },
                  "effect": { "removeThreat": {
                    "scheme": "chosen", "amount": 1
                  } }
                } }
              } }
            } }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                var scheme = board.CreateCard(
                    "01151", board.AreaOf(DeckType.SideSchemesArea));
                scheme.PlaceTokens("k_threat", 1);
            },
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:labeled-ability.4")]
    [Fact]
    public void DifferentSchemesPowerThatWouldSuspendRaisesBeforeTheActionCost()
    {
        // A labeled thwart ability "is considered to be a thwart made by that
        // player's identity." Its whole nested power must be preflighted.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "thwartDifferentSchemes": { "schemes": { "query": "thwartableSchemes" }, "power": { "thwart": { "target": "chosen", "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void DelayedPowerThatWouldSuspendRaisesBeforeTheActionCost()
    {
        // The delayed subtree is still the action's effect. Preflight must see
        // its labeled attack before any cost is paid or activation state changes.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "afterActivation": { "effect": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.Activation = new EnemyActivation(
                    board.TheCardIn(DeckType.VillainArea)!.ObjectId,
                    Player: 0,
                    Attacking: true,
                    Id: 41);
            },
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Fact]
    public void AStableFormBranchIgnoresAnUnreachableSuspendingPower()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:form-change-form")]
    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void APredecessorMutationPreflightsEveryReachableDependentBranch()
    {
        // Changing form flips which dependent branch resolves. The unsupported
        // labeled attack must be found before the form change or cost occurs.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "then": { "effect": { "changeForm": { "player": "you", "to": "alter-ego" } }, "then": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void EachPlayerPreflightUsesEveryPlayersCurrentForm()
    {
        // "For each player" reaches both identities. A safe branch for the
        // initiating hero cannot hide an unsupported branch for an alter-ego.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "eachPlayer": { "effect": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "attack": { "target": { "query": "villain" }, "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Rule("rr:choose-game-element.3")]
    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void ChosenCardContextIsPreflightedBeforeTheActionCost()
    {
        // The choice binds "chosen" before its effect resolves. Every branch
        // that binding can open must be checked before the cost is paid.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "chooseCard": { "from": { "query": "attackableEnemies" }, "effect": { "if": { "test": { "exists": "chosen" }, "then": { "attack": { "target": "chosen", "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } }, "else": { "draw": { "player": "you", "count": 1 } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;

        var thrown = Assert.Throws<RulesNotImplementedException>(() => Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner));

        Assert.Contains("suspends inside a labelled power", thrown.Message, StringComparison.Ordinal);
        Assert.True(source!.Ready);
    }

    [Fact]
    public void ChosenCardDoesNotDestabilizeAnUnrelatedFormBranch()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "chooseCard": { "from": { "query": "attackableEnemies" }, "effect": { "if": { "test": { "inForm": { "player": "you", "form": "hero" } }, "then": { "draw": { "player": "you", "count": 1 } }, "else": { "attack": { "target": "chosen", "effect": { "enemyAttacks": { "enemies": { "query": "villain" } } } } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void ChosenPlayerBindingIsPreflightedAfterSelection()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """{ "chooseCard": { "from": { "query": "identities" }, "effect": { "if": { "test": { "inForm": { "player": "chosenPlayer", "form": "hero" } }, "then": { "draw": { "player": "chosenPlayer", "count": 1 } }, "else": { "draw": { "player": "chosenPlayer", "count": 1 } } } } } }""",
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Fact]
    public void SequenceFormReachabilityWaitsForAChosenPlayerBinding()
    {
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "if": {
                  "test": { "inForm": {
                    "player": "chosenPlayer", "form": "hero"
                  } },
                  "then": { "changeForm": {
                    "player": "chosenPlayer", "to": "alter-ego"
                  } },
                  "else": { "draw": {
                    "player": "chosenPlayer", "count": 1
                  } }
                } }
              } },
              { "if": {
                "test": { "inForm": {
                  "player": "chosenPlayer", "form": "alter-ego"
                } },
                "then": { "draw": {
                  "player": "chosenPlayer", "count": 1
                } },
                "else": { "draw": {
                  "player": "chosenPlayer", "count": 1
                } }
              } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:target.2.2")]
    [Fact]
    public void LaterChosenPlayerTargetsAreCheckedAgainstTheOfferedCandidates()
    {
        // “The act of choosing a game element … makes that game element a
        // target.” Both offered identities have an engaged enemy, so the
        // continuation has a valid target whichever identity is selected.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "identities" },
                "effect": { "dealDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              } },
              { "dealDamage": {
                "cards": { "query": "enemiesEngagedWithChosenPlayer" },
                "amount": 1
              } }
            ] }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "01167",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:target.2.2")]
    [Rule("rr:attack-player-ability-type")]
    [Fact]
    public void AChosenEnemyCanTargetALaterAttackWrapper()
    {
        // The earlier choice establishes the target used by the later labelled
        // attack. Every offered enemy is currently attackable and damageable.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "chooseCard": {
                "from": { "query": "attackableEnemies" },
                "effect": { "seq": [] }
              } },
              { "attack": {
                "target": "chosen",
                "effect": { "dealAttackDamage": {
                  "cards": "chosen", "amount": 1
                } }
              } }
            ] }
            """);
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

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
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? minion = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                minion = board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Contains(
            game.Pending!.Affordances, option => option.Id == minion!.ObjectId);
        Assert.DoesNotContain(
            game.Pending.Affordances, option => option.Id == villain.ObjectId);
    }

    [Rule("rr:choose-option.2")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void ANoOpOptionCanPreserveTheAbsenceOfABinding()
    {
        // The empty option is legal as a decline branch, but it leaves
        // `chosen` unanswered. A later attack requiring that target therefore
        // makes the whole costed action unsafe to initiate.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
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
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:target.2.2")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void ASelectorCanDependOnEveryEarlierBindingCandidate()
    {
        // The first target determines which engaged area the second selector
        // reads. Every offered identity has a legal minion, so preflight must
        // evaluate the selector once under each possible earlier binding.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
                board.CreateCard(
                    "01167",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:target.2")]
    [Rule("rr:initiating-abilities.step.5")]
    [Fact]
    public void AnEarlierPromptFiltersBindingsWithNoLegalContinuation()
    {
        // At least one identity has a valid target for the later choice, so
        // the action can initiate. The identity without one is not a legal
        // target of the unresolved ability and must not be offered.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Contains(
            game.Pending!.Affordances,
            option => option.Id == world.Seats[0].IdentityCard.ObjectId);
        Assert.DoesNotContain(
            game.Pending.Affordances,
            option => option.Id == world.Seats[1].IdentityCard.ObjectId);
    }

    [Rule("rr:choose-option.1")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void AnUnavailableOptionDoesNotCreateAnEmptyBindingOutcome()
    {
        // The minion option has no valid target and is unavailable. Only the
        // villain option can run, and it supplies the target for the attack
        // that follows, so the unavailable branch contributes no empty path.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.Contains(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
    }

    [Rule("rr:choose-option.1")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void AnOptionPromptFiltersBranchesThatCannotSatisfyTheContinuation()
    {
        // Both nested selectors are locally legal, but the enemy branch leaves
        // no player for the later draw. The outer option itself is therefore
        // unavailable; the costed prompt must expose only the identity branch.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Contains(game.Pending!.Affordances, option => option.Id == 0);
        Assert.DoesNotContain(game.Pending.Affordances, option => option.Id == 1);
    }

    [Rule("rr:choose-option.1")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void SuffixValidationReplacesThePreOptionCandidateState()
    {
        // The first prompt binds an identity. The enemy option would replace
        // it with the villain, which cannot supply the chosen player required
        // by the final engaged-enemy selector. Recursive suffix validation must
        // use that post-option villain, not the stale identity candidate list.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            hero: true,
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

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
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        Card? tech = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                tech = board.CreateCard(
                    "01007",
                    board.AreaOf(
                        DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0));
                board.CreateCard(
                    "01167",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
            },
            hero: true,
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.False(source!.Ready);
        Assert.Contains(
            game.Pending!.Affordances,
            option => option.Id == world.Seats[0].IdentityCard.ObjectId);
        Assert.Contains(
            game.Pending.Affordances,
            option => option.Id == world.Seats[1].IdentityCard.ObjectId);

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
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, _) = Playing(
            board => source = InPlay(board, AuthoredCards.AuntMay),
            hero: true,
            abilities: runner);

        Assert.DoesNotContain(
            game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
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
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
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
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01101",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));

        Assert.Contains(
            game.Pending!.Affordances,
            option => option.Id == world.Seats[0].IdentityCard.ObjectId);
        Assert.DoesNotContain(
            game.Pending.Affordances,
            option => option.Id == world.Seats[1].IdentityCard.ObjectId);
    }

    [Rule("rr:each-player.1")]
    [Rule("rr:target.2.2")]
    [Fact]
    public void ANonFinalEachPlayerPromptIgnoresTheOuterContinuation()
    {
        // Only the final frame's binding reaches the outer continuation. The
        // first player's own mandatory choice therefore remains available even
        // though that player's binding could not satisfy the later selector.
        var runner = Runner(
            AuthoredCards.AuntMay,
            "Action",
            """
            { "seq": [
              { "eachPlayer": { "effect": { "chooseCard": {
                "from": "you", "effect": { "seq": [] }
              } } } },
              { "chooseCard": {
                "from": { "query": "enemiesEngagedWithChosenPlayer" },
                "effect": { "seq": [] }
              } }
            ] }
            """,
            cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var (game, world) = Playing(
            board =>
            {
                source = InPlay(board, AuthoredCards.AuntMay);
                board.CreateCard(
                    "01167",
                    board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
            },
            heroes: ["spider_man", "captain_marvel"],
            abilities: runner);
        var action = Assert.Single(
            game.Pending!.Affordances,
            option => option.AnchorId == source!.ObjectId);

        game.Resolve(Decision.Take(action.Id));
        var order = Assert.Single(game.Pending!.Affordances);
        Assert.Equal(Question.Order, game.Pending.Asking);
        game.Resolve(new Decision(order.Id,
        [
            world.Seats[0].IdentityCard.ObjectId,
            world.Seats[1].IdentityCard.ObjectId,
        ]));

        Assert.Contains(
            game.Pending!.Affordances,
            option => option.Id == world.Seats[0].IdentityCard.ObjectId);
    }

}
