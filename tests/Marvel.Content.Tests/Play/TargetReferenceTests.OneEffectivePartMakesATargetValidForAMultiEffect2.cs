using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;
public sealed class TargetReferenceOneEffectivePartMakesATargetValidForAMultiEffectTests : TargetReferenceTestBase
{
    [Rule("rr:target.3.4")]
    [Fact]
    public void OneEffectivePartMakesATargetValidForAMultiEffectAbility()
    {
        // “If an ability or game function has multiple effects on its target,
        // the target is valid if at least one of those effects can affect the
        // target.” Killmonger cannot take this upgrade's damage, but he can be
        // exhausted, so the action initiates and only the exhaust changes him.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            { "cards": [
              { "card": "01046", "abilities": [ {
                "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                "effect": { "seq": [
                  { "dealDamage": { "cards": { "titled": "Killmonger" }, "amount": 1 } },
                  { "exhaust": { "titled": "Killmonger" } }
                ] }
              } ] },
              { "card": "01157", "abilities": [ {
                "trigger": { "timing": "Constant", "subject": "this" },
                "effect": { "preventDamageFrom": { "card": "this", "sourceKind": "upgrade", "sourceTrait": "BLACK_PANTHER" } }
              } ] }
            ] }
            """));
        Card? source = null;
        Card? target = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, "01046", DeckType.UpgradesArea);
            target = board.CreateCard("01157", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, runner);
        ResolveAction(game, source!);
        Assert.Equal(0, target!.Damage);
        Assert.False(target.Ready);
    }

    [Rule("rr:target.3")]
    [Rule("rr:status-cards.1")]
    [Fact]
    public void AStatusAtItsLimitIsNotAValidTargetForTheSameStatus()
    {
        // A target is valid only if some part of the ability can affect it,
        // and a character cannot hold a second Tough card. The already-tough
        // villain therefore cannot make this status-only action initiable.
        var runner = Runner("01006", """{ "giveStatus": { "card": { "query": "villain" }, "status": "tough" } }""");
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, "01006", DeckType.SupportsArea);
            Statuses.Give(board, board.TheCardIn(DeckType.VillainArea)!, Statuses.Tough);
        }, runner);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.Equal(1, Statuses.Count(world, world.TheCardIn(DeckType.VillainArea)!, Statuses.Tough));
    }

    [Rule("rr:target.3.5")]
    [Rule("rr:ready.1")]
    [Fact]
    public void ACardThatCannotReadyIsSkippedAndDoesNotMakeTheAbilityLegal()
    {
        // A target is invalid when the ability would make it perform a game
        // function another ability prohibits. Offering and execution ask the
        // same CanReady question.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            { "cards": [
              { "card": "01006", "abilities": [ {
                "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                "effect": { "ready": { "titled": "Helicarrier" } }
              } ] },
              { "card": "01092", "abilities": [ {
                "trigger": { "timing": "Constant", "subject": "this" },
                "effect": { "preventReady": "this" }
              } ] }
            ] }
            """));
        Card? source = null;
        Card? target = null;
        var(game, _) = Playing(board =>
        {
            source = InPlay(board, "01006", DeckType.SupportsArea);
            target = InPlay(board, "01092", DeckType.SupportsArea);
            target.Exhaust();
        }, runner);
        Assert.DoesNotContain(game.Pending!.Affordances, option => option.AnchorId == source!.ObjectId);
        Assert.False(target!.Ready);
    }

    [Rule("rr:target.3.7")]
    [Rule("rr:target.4")]
    [Rule("rr:target.4.1")]
    [Fact]
    public void AGroupEffectUsesItsValidTargetAndSkipsOneThatCannotTakeDamage()
    {
        // The Black Panther upgrade cannot damage Killmonger, but the villain
        // remains a valid target. The ability initiates and resolves only
        // against that valid element.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            { "cards": [
              { "card": "01046", "abilities": [ {
                "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
                "effect": { "dealDamage": { "cards": { "query": "enemies" }, "amount": 1 } }
              } ] },
              { "card": "01157", "abilities": [ {
                "trigger": { "timing": "Constant", "subject": "this" },
                "effect": { "preventDamageFrom": { "card": "this", "sourceKind": "upgrade", "sourceTrait": "BLACK_PANTHER" } }
              } ] }
            ] }
            """));
        Card? source = null;
        Card? killmonger = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, "01046", DeckType.UpgradesArea);
            killmonger = board.CreateCard("01157", board.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        }, runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        ResolveAction(game, source!);
        Assert.Equal(1, villain.Damage);
        Assert.Equal(0, killmonger!.Damage);
    }

    [Rule("rr:target.2")]
    [Rule("rr:target.4")]
    [Rule("rr:target.4.1")]
    [Fact]
    public void ALabelledPowerSkipsAnEmptyStatusTargetGroup()
    {
        // An ability with multiple targets can initiate with at least one
        // valid target, and it does not resolve against invalid group members.
        // The villain remains a valid attack target while the empty minion
        // group contributes no status target and no simulator-only failure.
        var runner = Runner("01006", """
            { "attack": {
              "target": { "query": "villain" },
              "effect": { "seq": [
                { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } },
                { "giveStatus": {
                  "card": { "query": "minionsEngagedWithYou" },
                  "status": "stunned"
                } }
              ] }
            } }
            """);
        Card? source = null;
        var(game, world) = Playing(board => source = InPlay(board, "01006", DeckType.SupportsArea), runner);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        ResolveAction(game, source!);
        Assert.Equal(1, villain.Damage);
        Assert.Empty(world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards);
    }

    [Rule("rr:cannot.3")]
    [Rule("rr:crisis-icon.1")]
    [Rule("rr:target.3.9")]
    [Fact]
    public void ALabelledThwartHonorsItsExplicitCrisisException()
    {
        // Crisis normally says player cards cannot remove threat from the main
        // scheme. This exact effect says it ignores Crisis, so the explicit
        // exception wins and makes the declared thwart target valid.
        var runner = Runner("01006", """
            { "thwart": {
              "target": { "query": "mainScheme" },
              "effect": { "removeThreat": {
                "scheme": { "query": "mainScheme" },
                "amount": 1,
                "ignoresCrisis": "true"
              } }
            } }
            """);
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, "01006", DeckType.SupportsArea);
            var crisis = board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea));
            crisis.PlaceTokens("k_threat", 1);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 2);
        }, runner);
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        ResolveAction(game, source!);
        Assert.Equal(1, main.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:cannot.3")]
    [Rule("rr:crisis-icon.1")]
    [Fact]
    public void ALabelledThwartBindsItsTargetBeforeCheckingACrisisException()
    {
        // The power target becomes `chosen` before the nested effect resolves.
        // Offer-time legality uses that same binding, so the explicit Crisis
        // exception is visible on both sides of the decision.
        var runner = Runner("01006", """
            { "thwart": {
              "target": { "query": "mainScheme" },
              "effect": { "removeThreat": {
                "scheme": "chosen", "amount": 1,
                "ignoresCrisis": "true"
              } }
            } }
            """);
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, "01006", DeckType.SupportsArea);
            var crisis = board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea));
            crisis.PlaceTokens("k_threat", 1);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 2);
        }, runner);
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        ResolveAction(game, source!);
        Assert.Equal(1, main.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:cannot.3")]
    [Rule("rr:crisis-icon.1")]
    [Rule("rr:then")]
    [Fact]
    public void ALabelledThwartFindsACrisisExceptionInsideADependency()
    {
        // A `then` predecessor necessarily executes. Its explicit Crisis
        // exception therefore makes the declared target valid before the
        // dependent draw is considered.
        var runner = Runner("01006", """
            { "thwart": {
              "target": { "query": "mainScheme" },
              "effect": { "then": {
                "effect": { "removeThreat": {
                  "scheme": { "query": "mainScheme" },
                  "amount": 1,
                  "ignoresCrisis": "true"
                } },
                "then": { "draw": { "player": "you", "count": 1 } }
              } }
            } }
            """);
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, "01006", DeckType.SupportsArea);
            var crisis = board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea));
            crisis.PlaceTokens("k_threat", 1);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 2);
        }, runner);
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        ResolveAction(game, source!);
        Assert.Equal(1, main.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:cannot.3")]
    [Rule("rr:target.3.3")]
    [Fact]
    public void APrePaymentCrisisExceptionSurvivesACostChangingItsBranch()
    {
        // Target validity ignores the cost. Before payment the explicit
        // Crisis exception makes the main scheme a valid thwart target; after
        // discarding its source, the other branch draws instead. Scheduling
        // preserves the established target mode rather than failing after the
        // board has already mutated.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            { "cards": [ { "card": "01092", "abilities": [ {
              "trigger": {
                "event": "WhenActionTriggered", "timing": "Action",
                "subject": "game"
              },
              "cost": { "discard": "this" },
              "effect": { "thwart": {
                "target": { "query": "mainScheme" },
                "effect": { "if": {
                  "test": { "titleInPlay": "Helicarrier" },
                  "then": { "removeThreat": {
                    "scheme": { "query": "mainScheme" },
                    "amount": 1,
                    "ignoresCrisis": "true"
                  } },
                  "else": { "draw": { "player": "you", "count": 1 } }
                } }
              } }
            } ] } ] }
            """));
        Card? source = null;
        var(game, world) = Playing(board =>
        {
            source = InPlay(board, "01092", DeckType.SupportsArea);
            var crisis = board.CreateCard("01108", board.AreaOf(DeckType.SideSchemesArea));
            crisis.PlaceTokens("k_threat", 1);
            board.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 2);
        }, runner);
        var main = world.TheCardIn(DeckType.MainSchemesArea)!;
        int held = world.Seats[0].Hand.Cards.Count;
        ResolveAction(game, source!);
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        Assert.Equal(2, main.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(held + 1, world.Seats[0].Hand.Cards.Count);
    }
}
