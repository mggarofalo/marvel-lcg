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
public sealed class ActionAbilityPaymentsAndAuthorizationOnlyTheHostControllerMayUseAnAttachmentAbilityThatSaysYoTests
{
    [Rule("rr:ability.8.1")]
    [Rule("rr:attachment.2")]
    [Rule("rr:attachment.2.1")]
    [Fact]
    public void OnlyTheHostControllerMayUseAnAttachmentAbilityThatSaysYou()
    {
        // "You" on an attachment refers to "the attached player card's
        // controller", and "only" that player can trigger its abilities or
        // pay its costs. The attachment itself belongs to the scenario.
        var runner = Runner(AuthoredCards.Charge, "Action", """{ "draw": { "player": "you", "count": 1 } }""");
        Card? attachment = null;
        var(_, world) = Playing(board => attachment = board.CreateCard(AuthoredCards.Charge, board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), host: board.Seats[0].IdentityCard.ObjectId)), heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(runner.Actions(world, 0), action => action.Card == attachment!.ObjectId);
        Assert.DoesNotContain(runner.Actions(world, 1), action => action.Card == attachment!.ObjectId);
    }

    [Rule("rr:ability.8.1")]
    [Theory]
    [InlineData("""{ "exhaust": "you" }""", false)]
    [InlineData("""{ "removeCounters": { "card": "this", "counter": "yourMarker", "count": 1 } }""", true)]
    public void AttachmentCostAuthorizationUsesBindingsNotCounterNames(string cost, bool anotherPlayerMayAct)
    {
        // rr:ability.8.1 restricts an attachment that "uses the word “you” or
        // “your”". A counter's engine-chosen name is not a printed
        // player binding, even when its spelling includes "your".
        var runner = Runner(AuthoredCards.Charge, "Action", """{ "discard": "this" }""", cost: cost);
        Card? attachment = null;
        var(_, world) = Playing(board =>
        {
            attachment = board.CreateCard(AuthoredCards.Charge, board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), host: board.Seats[0].IdentityCard.ObjectId));
            attachment.PlaceTokens("c_yourMarker", 1);
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(runner.Actions(world, 0), action => action.Card == attachment!.ObjectId);
        Assert.Equal(anotherPlayerMayAct, runner.Actions(world, 1).Any(action => action.Card == attachment!.ObjectId));
    }

    [Rule("rr:ability.8.1")]
    [Fact]
    public void YourInTheAttachmentInstructionRestrictsEveryAbilityOnTheCard()
    {
        // All Tied Up says “Attach to your identity card,” while its Action is
        // only “spend resources → discard this card.” The permission belongs
        // to the attachment's whole printed text, not only the selected
        // ability, so another player cannot trigger that Action.
        var runner = new Marvel.Cards.Run.AbilityRunner(Marvel.Cards.Dsl.AbilityCatalog.Parse("""
                { "cards": [ { "card": "02048", "attachTo": "you", "abilities": [ {
                    "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                    "effect": { "discard": "this" }
                } ] } ] }
                """));
        Card? attachment = null;
        var(_, world) = Playing(board => attachment = board.CreateCard("02048", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), host: board.Seats[0].IdentityCard.ObjectId)), heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(runner.Actions(world, 0), action => action.Card == attachment!.ObjectId);
        Assert.DoesNotContain(runner.Actions(world, 1), action => action.Card == attachment!.ObjectId);
    }

    [Rule("rr:ability.8.1")]
    [Theory]
    [InlineData("""{"placeCounters":{"card":"this","counter":"yourMarker","count":1}}""", true)]
    [InlineData("""{"placeCounters":{"card":"you","counter":"marker","count":1}}""", false)]
    [InlineData("""{"draw":{"player":"you","count":1}}""", false)]
    [InlineData("""{"draw":{"player":"firstPlayer","count":1}}""", true)]
    [InlineData("""{"choose":{"descriptions":["yourChoice","anotherChoice"],"options":[{"discard":"this"},{"discard":"this"}]}}""", true)]
    [InlineData("""{"choose":{"options":[{"discard":"this"},{"draw":{"player":"you","count":1}}]}}""", false)]
    [InlineData("""{"if":{"test":{"titleInPlay":"yourTitle"},"then":{"discard":"this"},"else":{"discard":"this"}}}""", true)]
    [InlineData("""{"if":{"test":{"titleInPlay":"yourTitle"},"then":{"draw":{"player":"you","count":1}},"else":{"discard":"this"}}}""", false)]
    [InlineData("""{"if":{"test":{"titleInPlay":"absent"},"then":{"putIntoPlay":{"card":"this","where":"engagedWithYou"}},"else":{"discard":"this"}}}""", false)]
    [InlineData("""{"if":{"test":{"titleInPlay":"absent"},"then":{"putIntoPlay":{"card":"this","where":"printedDestination"}},"else":{"discard":"this"}}}""", true)]
    [InlineData("""{"placeCounters":{"card":"this","counter":"marker","count":{"add":[1,{"countersOn":{"card":"this","counter":"yourMarker"}}]}}}""", true)]
    [InlineData("""{"placeCounters":{"card":"this","counter":"marker","count":{"add":[1,{"countersOn":{"card":"you","counter":"marker"}}]}}}""", false)]
    public void AttachmentEffectAuthorizationUsesBindingsNotLiteralText(string effect, bool anotherPlayerMayAct)
    {
        // rr:ability.8.1 restricts an attachment whose text "uses the word
        // “you” or “your”". Engine-chosen literal counter names and choice
        // descriptions do not represent that binding. A binding in an
        // unchosen branch is still part of the ability's text.
        var runner = Runner(AuthoredCards.Charge, "Action", effect);
        Card? attachment = null;
        var(_, world) = Playing(board => attachment = board.CreateCard(AuthoredCards.Charge, board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), host: board.Seats[0].IdentityCard.ObjectId)), heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(runner.Actions(world, 0), action => action.Card == attachment!.ObjectId);
        Assert.Equal(anotherPlayerMayAct, runner.Actions(world, 1).Any(action => action.Card == attachment!.ObjectId));
    }

    [Theory]
    [InlineData("this", "you", true)]
    [InlineData("you", "this", false)]
    public void AttachmentEffectAuthorizationUsesTheCompiledSnapshot(string originalBinding, string changedBinding, bool anotherPlayerMayAct)
    {
        var parsed = AbilityCatalog.Parse("""
            {"cards":[{"card":"01099","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
              "effect":{"discard":"this"}
            }]}]}
            """);
        var fields = new Dictionary<string, AbilityValue>(StringComparer.Ordinal)
        {
            ["card"] = new AbilityValue.Word(originalBinding),
            ["counter"] = new AbilityValue.Word("marker"),
            ["count"] = new AbilityValue.Number(1),
        };
        var ability = parsed.Abilities[0] with
        {
            Effect = new AbilityNode("placeCounters", new AbilityValue.Map(fields))
        };
        var runner = new Marvel.Cards.Run.AbilityRunner(new AbilityBook([ability], parsed.Authored));
        // Engine choice: editing caller-owned syntax after compilation cannot
        // grant or revoke permission to use the compiled ability.
        fields["card"] = new AbilityValue.Word(changedBinding);
        Card? attachment = null;
        var(_, world) = Playing(board => attachment = board.CreateCard(AuthoredCards.Charge, board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), host: board.Seats[0].IdentityCard.ObjectId)), heroes: ["spider_man", "captain_marvel"], abilities: runner);
        Assert.Contains(runner.Actions(world, 0), action => action.Card == attachment!.ObjectId);
        Assert.Equal(anotherPlayerMayAct, runner.Actions(world, 1).Any(action => action.Card == attachment!.ObjectId));
    }

    [Rule("rr:ability.8.1")]
    [Rule("rr:the-golden-rules")]
    [Fact]
    public void AnExplicitAnyPlayerPermissionOverridesTheAttachmentRestriction()
    {
        // Obedience Potion says “Attach to your identity,” then its Hero
        // Action ends “Any player can do this.” The printed exception lets the
        // other player initiate the ability and pay from their own hand.
        var runner = new Marvel.Cards.Run.AbilityRunner(Marvel.Cards.Dsl.AbilityCatalog.Parse("""
                { "cards": [ { "card": "16123", "attachTo": "you", "abilities": [ {
                    "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game", "form": "hero" },
                    "anyPlayer": true,
                    "cost": { "spend": "BB" },
                    "effect": { "discard": "this" }
                } ] } ] }
                """));
        Card? attachment = null;
        Card[] payment = [];
        var(_, world) = Playing(board =>
        {
            board.Seats[1].IdentityCard.TurnTo("01010a");
            attachment = board.CreateCard("16123", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), host: board.Seats[0].IdentityCard.ObjectId));
            Hand(board, player: 1, Mentals, count: 2);
            payment = [..board.Seats[1].Hand.Cards.Take(2)];
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var action = Assert.Single(runner.Actions(world, 1), option => option.Card == attachment!.ObjectId);
        runner.Act(world, action, [..payment.Select(card => card.ObjectId)], []);
        Assert.Equal(DeckType.EncounterDiscardPile, attachment!.Area.Type);
        Assert.All(payment, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
    }

    [Rule("rr:player-turn.5")]
    [Fact]
    public void AnyPlayerMayUseAPlayerCardThatPrintsThatPermission()
    {
        // Player-turn option 5.c is exactly Plot Convenience's last line:
        // “Any player may trigger this ability.” The permission makes another
        // player's support visible, that player initiates it, and its printed
        // exhaust cost is paid before the effect resolves for that player.
        var runner = new Marvel.Cards.Run.AbilityRunner(Marvel.Cards.Dsl.AbilityCatalog.Parse("""
                { "cards": [ { "card": "44050", "abilities": [
                  {
                    "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                    "anyPlayer": true,
                    "cost": { "exhaust": "this" },
                    "effect": { "draw": { "player": "you", "count": 1 } }
                  },
                  {
                    "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                    "effect": { "draw": { "player": "you", "count": 2 } }
                  }
                ] } ] }
                """));
        Card? support = null;
        var(_, world) = Playing(board => support = InPlay(board, "44050"), heroes: ["spider_man", "captain_marvel"], abilities: runner);
        int held = world.Seats[1].Hand.Cards.Count;
        var action = Assert.Single(runner.Actions(world, 1), option => option.Card == support!.ObjectId);
        runner.Act(world, action, [], []);
        Assert.False(support!.Ready);
        Assert.Equal(held + 1, world.Seats[1].Hand.Cards.Count);
    }

    [Rule("rr:interrupt.1")]
    [Rule("rr:response.1")]
    [Fact]
    public void AnyPlayerWindowAbilitiesAreEvaluatedForEachPlayer()
    {
        var runner = Runner("44050", "Response", """{ "draw": { "player": "you", "count": 1 } }""", cost: """{ "spend": "B" }""", eventName: Steps.DamageDealt, anyPlayer: true);
        Card? support = null;
        Card[] payment = [];
        var(_, world) = Playing(board =>
        {
            support = InPlay(board, "44050");
            Hand(board, player: 0, Physicals, count: 0);
            Hand(board, player: 1, Mentals, count: 1);
            payment = [..board.Seats[1].Hand.Cards];
        }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        int held = world.Seats[1].Hand.Cards.Count;
        var response = Assert.Single(runner.Waiting(world, new Occurrence(1, [Steps.DamageDealt], Player: 0), WindowKind.Response), option => option.Player == 1);
        Assert.Equal(1, response.Player);
        runner.Act(world, response, [payment[0].ObjectId], []);
        Assert.Equal(DeckType.DiscardPile, payment[0].Area.Type);
        Assert.Equal(held, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(support!.ObjectId, response.Card);
    }

    [Rule("rr:in-player-order.1")]
    [Rule("rr:response.1")]
    [Fact]
    public void AnyPlayerWindowAnswerResumesForThePlayerWhoAcceptedIt()
    {
        var runner = Runner("44050", "Response", """{ "draw": { "player": "you", "count": 1 } }""", eventName: Steps.DamageDealt, anyPlayer: true);
        var(_, world) = Playing(board => InPlay(board, "44050"), heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var occurrence = new Occurrence(1, [Steps.DamageDealt], Player: 0);
        var events = new List<GameEvent>();
        int firstHeld = world.Seats[0].Hand.Cards.Count;
        int secondHeld = world.Seats[1].Hand.Cards.Count;
        var first = Offering.Work(world, runner, occurrence, WindowKind.Response, events)!;
        Assert.Equal(0, first.Player);
        Sequence.Answer(world, CardCatalogData, runner, first, Decision.Decline, events);
        var second = Offering.Work(world, runner, occurrence, WindowKind.Response, events)!;
        Assert.Equal(1, second.Player);
        Sequence.Answer(world, CardCatalogData, runner, second, Decision.Take(Assert.Single(second.Affordances).Id), events);
        Assert.Equal(firstHeld, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(secondHeld + 1, world.Seats[1].Hand.Cards.Count);
    }

    [Rule("rr:ability.8")]
    [Fact]
    public void TriggerPlayerStillNarrowsAnAnyPlayerWindow()
    {
        var runner = Runner("44050", "Response", """{ "draw": { "player": "you", "count": 1 } }""", eventName: Steps.DamageDealt, player: "trigger.player", anyPlayer: true);
        var(_, world) = Playing(board => InPlay(board, "44050"), heroes: ["spider_man", "captain_marvel"], abilities: runner);
        var waiting = runner.Waiting(world, new Occurrence(1, [Steps.DamageDealt], Player: 0), WindowKind.Response);
        Assert.Equal(0, Assert.Single(waiting).Player);
    }
}
