using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class MainSchemeAdvancementAbilityTests
{
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));

    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:main-scheme-main-scheme-deck.2.2")]
    [Fact]
    public void CardEffectAdvancesWithoutCompletingTheOldScheme()
    {
        // "If the main scheme advances other than through having threat on it
        // equal to or greater than its target threat value, that main scheme is
        // not considered completed." The card effect moves stages but never
        // writes the completion marker used by threat-driven advancement.
        var (world, source, next) = Board();
        var old = world.TheCardIn(DeckType.MainSchemesArea)!;
        old.PlaceTokens("k_threat", 1);
        var runner = Runner();
        world.Abilities = runner;

        var action = Assert.Single(
            runner.Actions(world, 0), ability => ability.Card == source.ObjectId);
        var events = runner.Act(world, action, [], []);
        var resolved = new Resolution(world, Prompt: null, events);

        Assert.Equal(DeckType.RemovedArea, old.Area.Type);
        Assert.Equal(0, old.Tokens.GetValueOrDefault("is_completed"));
        Assert.Equal(DeckType.MainSchemesArea, next.Area.Type);
        Assert.Same(next, world.TheCardIn(DeckType.MainSchemesArea));
        Assert.Contains(events, gameEvent => gameEvent is Marvel.Rules.Events.CardsMoved);
        Assert.DoesNotContain(
            events.OfType<Marvel.Rules.Events.FieldSet>(),
            change => change.Card == old.ObjectId && change.Field == "is_completed");
        Assert.DoesNotContain(
            resolved.Information,
            signal => signal.Kind == InformationKind.Reveal);
    }

    [Rule("rr:main-scheme-main-scheme-deck.2.2")]
    [Fact]
    public void MissingNextStageRaisesBeforeMovingTheCurrentScheme()
    {
        var (world, source, next) = Board();
        var old = world.TheCardIn(DeckType.MainSchemesArea)!;
        var runner = Runner();
        world.Abilities = runner;
        var offered = Assert.Single(
            runner.Actions(world, 0), ability => ability.Card == source.ObjectId);

        // The board can change after an affordance was made. Runtime must
        // recheck the transition and refuse before moving the current stage.
        World.MoveToTop(next, world.AreaOf(DeckType.RemovedArea));

        Assert.Throws<RulesNotImplementedException>(
            () => runner.Act(world, offered, [], []));
        Assert.Same(old, world.TheCardIn(DeckType.MainSchemesArea));
        Assert.Equal(DeckType.MainSchemesArea, old.Area.Type);
        Assert.Equal(0, old.Tokens.GetValueOrDefault("is_completed"));
    }

    [Rule("rr:main-scheme-main-scheme-deck.2.2")]
    [Fact]
    public void NextStageUsesTheInitiatingRunnersRevealedAbility()
    {
        var (world, source, _) = Board();
        var runner = RunnerWithNextStageAbility();
        world.Abilities = new UnrelatedWorldAbilities();
        int handBefore = world.Seats[0].Hand.Cards.Count;
        var action = Assert.Single(
            runner.Actions(world, 0), ability => ability.Card == source.ObjectId);

        runner.Act(world, action, [], []);

        Assert.Equal(handBefore + 1, world.Seats[0].Hand.Cards.Count);
    }

    private static (World World, Card Source, Card Next) Board()
    {
        var world = WorldSetup.DealWithoutCardAbilities(
            Cards,
            Blueprints.From(Dealer.DealOrder(Setup, "rhino", ["spider_man"]), Cards),
            [Setup.Hero("spider_man").Name],
            12345);
        var source = world.CreateCard(
            AuthoredCards.AuntMay,
            world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var next = world.CreateCard(
            "01116a,01116b", world.AreaOf(DeckType.MainSchemesDeck));
        return (world, source, next);
    }

    private static AbilityRunner Runner() => new(AbilityCatalog.Parse(
        $$"""
        { "cards": [
          { "card": "{{AuthoredCards.AuntMay}}", "abilities": [ {
            "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
            "effect": { "advanceMainScheme": "next" }
          } ] },
          { "card": "01116a", "abilities": [] },
          { "card": "01116b", "abilities": [] }
        ] }
        """));

    private static AbilityRunner RunnerWithNextStageAbility() => new(AbilityCatalog.Parse(
        $$"""
        { "cards": [
          { "card": "{{AuthoredCards.AuntMay}}", "abilities": [ {
            "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
            "effect": { "advanceMainScheme": "next" }
          } ] },
          { "card": "01116a", "abilities": [ {
            "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed" },
            "effect": { "draw": { "player": "you", "count": 1 } }
          } ] },
          { "card": "01116b", "abilities": [] }
        ] }
        """));

    private sealed class UnrelatedWorldAbilities : NoCardAbilities
    {
        public override IReadOnlyList<Marvel.Rules.Events.GameEvent> WhenRevealed(
            World world, Card card, int player) =>
            throw new InvalidOperationException("the unrelated World ability aggregate was used");
    }
}
