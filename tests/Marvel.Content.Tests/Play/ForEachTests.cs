using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Count-based “for each” effects — <c>rr:for-each</c>.</summary>
public sealed class ForEachTests
{
    private const string Campaign = "rhino";
    private const string SourceFace = "01110";
    private const uint Seed = 12345;

    private static readonly SetupCatalog Setup =
        SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:for-each.1")]
    [Rule("rr:for-each.2")]
    [Fact]
    public void DamageWithoutChooseIsOneCombinedInstanceAgainstOneTarget()
    {
        // “That effect applies to a single target” and is “a single instance
        // of damage dealt.” Three iterations of one damage therefore produce
        // one three-damage instance. Tough prevents all of that instance; a
        // resolver that looped would spend Tough and then deal two damage.
        var world = Deal();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        Statuses.Give(world, villain, Statuses.Tough);
        var runner = Runner(
            """
            { "forEach": { "count": 3, "effect": {
              "dealDamage": { "cards": { "query": "villain" }, "amount": 1 }
            } } }
            """);

        Resolve(world, runner);

        Assert.Equal(0, villain.Damage);
        Assert.False(Statuses.Has(world, villain, Statuses.Tough));
    }

    [Rule("rr:for-each.1")]
    [Rule("rr:for-each.2")]
    [Fact]
    public void ThreatWithoutChooseIsOneCombinedRemoval()
    {
        // “A single instance of … threat removed”: the event stream must say
        // four became one once, not expose three one-point removals that
        // responses could observe separately.
        var world = Deal();
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        scheme.PlaceTokens("k_threat", 4);
        var runner = Runner(
            """
            { "forEach": { "count": 3, "effect": {
              "removeThreat": {
                "scheme": { "query": "mainScheme" }, "amount": 1 }
            } } }
            """);

        var events = Resolve(world, runner);

        Assert.Equal(1, scheme.Tokens.GetValueOrDefault("k_threat"));
        var removal = Assert.Single(events.OfType<FieldSet>(), change =>
            change.Card == scheme.ObjectId && change.Field == "k_threat");
        Assert.Equal(4, removal.From);
        Assert.Equal(1, removal.To);
    }

    [Rule("rr:for-each.1")]
    [Fact]
    public void UnsupportedTargetedShapeRaisesBeforeItCanRetarget()
    {
        // The one-target rule applies to the whole repeated effect, including
        // a target nested in a sequence. The executable binding that would
        // persist that target does not exist yet, so the engine refuses before
        // the first iteration rather than defeating Guard and silently
        // re-running the selector against the newly legal villain.
        var world = Deal();
        var guard = world.CreateCard(
            "01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var runner = Runner(
            """
            { "forEach": { "count": 2, "effect": { "seq": [
              { "dealDamage": {
                "cards": { "query": "attackableEnemies" }, "amount": 3
              } }
            ] } } }
            """);

        var refused = Assert.Throws<RulesNotImplementedException>(() =>
            Resolve(world, runner));

        Assert.Contains("one target cannot be persisted", refused.Message);
        Assert.Equal(0, guard.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, guard.Area.Type);
    }

    [Rule("rr:for-each.1")]
    [Fact]
    public void AddToHandWithoutChooseDoesNotRunAFreshTargetLookup()
    {
        // addToHand is target-bearing too. Until no-choice for-each carries a
        // persisted target binding, it must fail before moving even a stable
        // first target; omitting this leaf would let a dynamic selector move a
        // different card on the next iteration.
        var world = Deal();
        var runner = Runner(
            """{ "forEach": { "count": 2, "effect": { "addToHand": "this" } } }""");
        var source = world.CreateCard(SourceFace, world.AreaOf(DeckType.RevealingArea));

        var refused = Assert.Throws<RulesNotImplementedException>(() =>
            runner.WhenRevealed(world, source, 0));

        Assert.Contains("one target cannot be persisted", refused.Message);
        Assert.Equal(DeckType.RevealingArea, source.Area.Type);
    }

    [Rule("rr:for-each.3")]
    [Rule("rr:for-each.3.1")]
    [Rule("rr:for-each.3.2")]
    [Fact]
    public void ChooseReevaluatesLegalTargetsAfterEverySeparateInstance()
    {
        // Each iteration is separate and “the game state updates after each
        // instance.” Guard initially leaves only the minion attackable. The
        // first three damage defeats it; only then may the next iteration
        // offer the villain. Reusing the first prompt or resolving both before
        // defeat would make the second answer illegal.
        var world = Deal();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var guard = world.CreateCard(
            "01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var runner = Runner(
            """
            { "forEach": { "count": 2, "effect": {
              "chooseCard": {
                "from": { "query": "attackableEnemies" },
                "effect": { "dealDamage": { "cards": "chosen", "amount": 3 } }
              }
            } } }
            """);
        var source = world.CreateCard(SourceFace, world.AreaOf(DeckType.RevealingArea));

        runner.WhenRevealed(world, source, 0);
        var first = Assert.Single(world.Agenda.Outstanding);
        var firstPrompt = runner.Choosing(world, source, 0, first.Index, first.Tier)!;
        Assert.Equal([guard.ObjectId], firstPrompt.Affordances.Select(each => each.Id));

        var firstEvents = runner.Chose(
            world, source, 0, first.Index, Decision.Take(guard.ObjectId), first.Tier);
        Assert.Equal(DeckType.EncounterDiscardPile, guard.Area.Type);
        Assert.Contains(firstEvents, gameEvent => gameEvent is CardsMoved);

        var second = world.Agenda.Outstanding[^1];
        var secondPrompt = runner.Choosing(world, source, 0, second.Index, second.Tier)!;
        Assert.Equal([villain.ObjectId], secondPrompt.Affordances.Select(each => each.Id));

        runner.Chose(
            world, source, 0, second.Index, Decision.Take(villain.ObjectId), second.Tier);

        Assert.Equal(3, villain.Damage);
    }

    [Rule("rr:for-each.4")]
    [Fact]
    public void AModifierChangesEveryChosenInstance()
    {
        // “That modifier is applied to each instance.” The lasting effect is
        // the other ability's +1, read beside the printed two. Two choices
        // therefore deal three each. Applying the modifier once to the whole
        // repeated effect would deal only five.
        var world = Deal();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var source = world.CreateCard(SourceFace, world.AreaOf(DeckType.RevealingArea));
        world.Effects.Register(new ContinuousEffect(
            EffectSource.LastingEffect,
            Kind: "forEachDamage",
            Amount: 1,
            Card: source.ObjectId,
            Affects: source.ObjectId));
        var runner = Runner(
            """
            { "forEach": { "count": 2, "effect": {
              "chooseCard": {
                "from": { "query": "attackableEnemies" },
                "effect": { "dealDamage": {
                  "cards": "chosen",
                  "amount": { "add": [
                    2,
                    { "modified": { "card": "this", "field": "forEachDamage" } }
                  ] }
                } }
              }
            } } }
            """);

        runner.WhenRevealed(world, source, 0);
        var first = Assert.Single(world.Agenda.Outstanding);
        runner.Chose(
            world, source, 0, first.Index, Decision.Take(villain.ObjectId), first.Tier);
        var second = world.Agenda.Outstanding[^1];
        runner.Chose(
            world, source, 0, second.Index, Decision.Take(villain.ObjectId), second.Tier);

        Assert.Equal(6, villain.Damage);
    }

    private static AbilityRunner Runner(string effect) => new(AbilityCatalog.Parse(
        $$"""
        { "cards": [ { "card": "{{SourceFace}}", "abilities": [ {
          "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                       "subject": "this" },
          "effect": {{effect}}
        } ] } ] }
        """));

    private static IReadOnlyList<GameEvent> Resolve(World world, AbilityRunner runner)
    {
        var source = world.CreateCard(SourceFace, world.AreaOf(DeckType.RevealingArea));
        return runner.WhenRevealed(world, source, 0);
    }

    private static World Deal() => WorldSetup.Deal(
        Cards,
        Blueprints.From(Dealer.DealOrder(Setup, Campaign, ["spider_man"]), Cards),
        [Setup.Hero("spider_man").Name],
        Seed);
}
