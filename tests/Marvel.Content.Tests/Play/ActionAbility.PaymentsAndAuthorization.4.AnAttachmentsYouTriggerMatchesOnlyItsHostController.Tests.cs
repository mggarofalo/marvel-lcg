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
public sealed class ActionAbilityPaymentsAndAuthorizationAnAttachmentsYouTriggerMatchesOnlyItsHostControllerTests
{
    [Rule("rr:ability.8.1")]
    [Fact]
    public void AnAttachmentsYouTriggerMatchesOnlyItsHostController()
    {
        var runner = Runner(AuthoredCards.Charge, "Response", """{ "draw": { "player": "you", "count": 1 } }""", eventName: "WhenAttacked", player: "you");
        Card? attachment = null;
        var(_, world) = Playing(board => attachment = board.CreateCard(AuthoredCards.Charge, board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), host: board.Seats[0].IdentityCard.ObjectId)), heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var ours = runner.Waiting(world, new Occurrence(1, ["WhenAttacked"], Player: 0), WindowKind.Response);
        var theirs = runner.Waiting(world, new Occurrence(2, ["WhenAttacked"], Player: 1), WindowKind.Response);
        Assert.Equal(0, Assert.Single(ours).Player);
        Assert.Empty(theirs);
    }

    [Rule("rr:ability.8.1")]
    [Rule("rr:ability.8.2")]
    [Fact]
    public void RestrictedResourceAbilitiesBelongOnlyToTheirPermittedPlayer()
    {
        var obligationRunner = Runner(AuthoredCards.EvictionNotice, "Resource", """{ "generate": "Y" }""");
        Card? obligation = null;
        var(_, obligationWorld) = Playing(board => obligation = board.CreateCard(AuthoredCards.EvictionNotice, board.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0))), heroes: ["spider_man", "captain_marvel"], abilities: obligationRunner);
        Assert.Contains(obligationRunner.ResourceAbilities(obligationWorld, 0), source => source.Effect == obligation!.ObjectId);
        Assert.DoesNotContain(obligationRunner.ResourceAbilities(obligationWorld, 1), source => source.Effect == obligation!.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => obligationRunner.UseResource(obligationWorld, 1, obligation!.ObjectId, []));
        // Compound bindings are still printed “you/your”: this query means
        // allies controlled by the resolving player and restricts a
        // player-hosted attachment just as the bare word “you” does.
        var attachmentRunner = new Marvel.Cards.Run.AbilityRunner(Marvel.Cards.Dsl.AbilityCatalog.Parse($$"""
                { "cards": [ { "card": "{{AuthoredCards.Charge}}", "abilities": [ {
                    "trigger": { "event": "WhenActionTriggered", "timing": "Resource", "subject": "game" },
                    "when": { "exists": { "query": "alliesYouControl" } },
                    "effect": { "generate": "B" }
                } ] } ] }
                """));
        Card? attachment = null;
        var(_, attachmentWorld) = Playing(board =>
        {
            board.CreateCard("01002", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
            attachment = board.CreateCard(AuthoredCards.Charge, board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), host: board.Seats[0].IdentityCard.ObjectId));
        }, heroes: ["spider_man", "captain_marvel"], abilities: attachmentRunner);
        Assert.Contains(attachmentRunner.ResourceAbilities(attachmentWorld, 0), source => source.Effect == attachment!.ObjectId);
        Assert.DoesNotContain(attachmentRunner.ResourceAbilities(attachmentWorld, 1), source => source.Effect == attachment!.ObjectId);
        // Player-relative semantics can also be the node name rather than a
        // word value. Test it in a real response occurrence whose target
        // makes `isYourIdentity` true for the host controller and false for
        // the other player.
        var kindRunner = new Marvel.Cards.Run.AbilityRunner(Marvel.Cards.Dsl.AbilityCatalog.Parse($$"""
                { "cards": [ { "card": "{{AuthoredCards.Charge}}", "abilities": [ {
                    "trigger": { "event": "WhenDamageWouldBeDealt", "timing": "Response", "subject": "game" },
                    "when": { "isYourIdentity": "trigger.target" },
                    "effect": { "draw": { "player": "you", "count": 1 } }
                } ] } ] }
                """));
        Card? kindAttachment = null;
        var(_, kindWorld) = Playing(board => kindAttachment = board.CreateCard(AuthoredCards.Charge, board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), host: board.Seats[0].IdentityCard.ObjectId)), heroes: ["spider_man", "captain_marvel"], abilities: kindRunner);
        var ours = kindRunner.Waiting(kindWorld, new Occurrence(1, ["WhenDamageWouldBeDealt"], Player: 0, Target: kindWorld.Seats[0].IdentityCard.ObjectId), WindowKind.Response);
        var theirs = kindRunner.Waiting(kindWorld, new Occurrence(2, ["WhenDamageWouldBeDealt"], Player: 1, Target: kindWorld.Seats[1].IdentityCard.ObjectId), WindowKind.Response);
        Assert.Equal(kindAttachment!.ObjectId, Assert.Single(ours).Card);
        Assert.Empty(theirs);
    }

    [Rule("rr:interrupt.1.1")]
    [Rule("rr:response.1.1")]
    [Theory]
    [InlineData("Interrupt")]
    [InlineData("Response")]
    public void AnotherPlayersObligationIsExcludedFromAbilityWindows(string timing)
    {
        var runner = Runner(AuthoredCards.EvictionNotice, timing, """{ "draw": { "player": "you", "count": 1 } }""", eventName: "WhenAttacked");
        var(_, world) = Playing(board => board.CreateCard(AuthoredCards.EvictionNotice, board.AreaOf(DeckType.ObligationsArea, PlayArea.Of(0))), heroes: ["spider_man", "captain_marvel"], abilities: runner);
        world.FirstPlayer = 1;
        var occurrence = new Occurrence(1, ["WhenAttacked"], Player: 1);
        var prompt = Offering.Work(world, runner, occurrence, timing == "Interrupt" ? WindowKind.Interrupt : WindowKind.Response, []);
        Assert.NotNull(prompt);
        Assert.Equal(0, prompt.Player);
    }

    [Rule("rr:action.2")]
    [Rule("rr:action.2.1")]
    [Rule("rr:forced.2")]
    [Fact]
    public void ALegalForcedActionMustResolveBeforeThePlayerPhaseEnds()
    {
        var runner = Runner(AuthoredCards.AuntMay, "ForcedAction", """{ "draw": { "player": "you", "count": 1 } }""", cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        // It may be used at any ordinary action opportunity.
        Assert.Contains(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == source!.ObjectId);
        // At the boundary it is no longer optional: the phase stays in the
        // player turn and the only answer is to resolve the Forced Action.
        game.Resolve(Decision.Decline);
        Assert.Equal(GamePhase.PlayerTurn, game.Phase);
        Assert.True(game.IsForcedResolutionPrompt);
        Assert.False(game.Pending!.Cancellable);
        var forced = Assert.Single(game.Pending.Affordances);
        int held = world.Seats[0].Hand.Cards.Count;
        game.Resolve(Decision.Take(forced.Id));
        Assert.False(source!.Ready);
        Assert.False(game.IsForcedResolutionPrompt);
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        // Its exhaust cost is now unpayable, so resolution continues directly
        // to the ordinary end phase. It must not reopen a normal turn.
        Assert.Equal(GamePhase.EndPhase, game.Phase);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.Verb is Game.ChangeForm or Game.ActionVerb);
    }

    [Rule("rr:action.2")]
    [Rule("rr:ability.8.2")]
    [Fact]
    public void APlayersForcedActionAsksThatPlayerToChooseItsPayment()
    {
        var runner = Runner(AuthoredCards.EvictionNotice, "ForcedAction", """{ "discard": "this" }""", cost: """{ "discardFromHand": 1 }""");
        Card? source = null;
        var(game, world) = Playing(board => source = board.CreateCard(AuthoredCards.EvictionNotice, board.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1))), heroes: ["spider_man", "captain_marvel"], abilities: runner);
        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Decline);
        Assert.Equal(1, game.Pending!.Player);
        var forced = Assert.Single(game.Pending.Affordances);
        Assert.NotNull(forced.Targets);
        var targets = forced.Targets;
        Assert.All(targets.Legal, id => Assert.Contains(world.Cards[id], world.Seats[1].Hand.Cards));
        int p0Held = world.Seats[0].Hand.Cards.Count;
        var paid = world.Cards[targets.Legal[0]];
        game.Resolve(Decision.Take(forced.Id, [paid.ObjectId], []));
        Assert.Equal(p0Held, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, paid.Area.Type);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
    }

    [Rule("rr:action.2")]
    [Fact]
    public void ACostlessForcedActionNeedOnlyResolveOnceBeforeThePhaseEnds()
    {
        var runner = Runner(AuthoredCards.AuntMay, "ForcedAction", """{ "draw": { "player": "you", "count": 1 } }""");
        Card? source = null;
        var(game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), abilities: runner);
        game.Resolve(Decision.Decline);
        var forced = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        int held = world.Seats[0].Hand.Cards.Count;
        game.Resolve(Decision.Take(forced.Id));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
    }

    [Rule("rr:action.2")]
    [Rule("rr:player-elimination.step.5")]
    [Fact]
    public void EliminatingTheFirstPlayerDuringTheGateMovesTheEndPhaseToTheSurvivor()
    {
        var runner = Runner(AuthoredCards.AuntMay, "ForcedAction", """{ "dealDamage": { "cards": "you", "amount": 99 } }""", cost: """{ "exhaust": "this" }""");
        Card? source = null;
        var(game, world) = Playing(board => source = InPlay(board, AuthoredCards.AuntMay), heroes: ["spider_man", "captain_marvel"], abilities: runner);
        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Decline);
        var forced = Assert.Single(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        game.Resolve(Decision.Take(forced.Id));
        Assert.True(world.Seats[0].Eliminated);
        Assert.Equal(1, world.FirstPlayer);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
        Assert.Equal(1, game.Pending!.Player);
    }

    [Rule("rr:action.2")]
    [Fact]
    public void ForcedActionsOnTwoFacesOfOneIdentityAreDistinctAbilities()
    {
        var runner = new Marvel.Cards.Run.AbilityRunner(Marvel.Cards.Dsl.AbilityCatalog.Parse($$"""
                { "cards": [
                  { "card": "{{AuthoredCards.SpiderMan}}", "abilities": [ {
                    "trigger": { "event": "WhenActionTriggered", "timing": "ForcedAction", "subject": "game" },
                    "limitPerRound": 1,
                    "effect": { "changeForm": { "player": "you", "to": "alter-ego" } }
                  } ] },
                  { "card": "01001b", "abilities": [ {
                    "trigger": { "event": "WhenActionTriggered", "timing": "ForcedAction", "subject": "game" },
                    "limitPerRound": 1,
                    "effect": { "draw": { "player": "you", "count": 1 } }
                  } ] }
                ] }
                """));
        var(game, world) = Playing(_ =>
        {
        }, hero: true, abilities: runner);
        game.Resolve(Decision.Decline);
        var heroAction = Assert.Single(game.Pending!.Affordances);
        game.Resolve(Decision.Take(heroAction.Id));
        Assert.Equal("01001b", world.Seats[0].IdentityCard.FaceId);
        var alterEgoAction = Assert.Single(game.Pending!.Affordances);
        int held = world.Seats[0].Hand.Cards.Count;
        game.Resolve(Decision.Take(alterEgoAction.Id));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
    }
}
