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
public abstract class TargetReferenceTestBase
{
    protected static readonly SetupCatalog Setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    protected static readonly CardCatalog Cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    protected static AbilityRunner Runner(string card, string effect) => new(AbilityCatalog.Parse($$"""
            { "cards": [ { "card": "{{card}}", "abilities": [ {
              "trigger": { "event": "WhenActionTriggered", "timing": "Action", "subject": "game" },
              "effect": {{effect}}
            } ] } ] }
            """));
    protected static void AssertRevealedIdentityAssociation(string source, string identity, string unrelated, string hero)
    {
        string title = Cards.Title(identity);
        var runner = new AbilityRunner(AbilityCatalog.Parse($$"""
            { "cards": [ { "card": "{{source}}", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed", "subject": "this" },
              "effect": { "giveStatus": { "card": { "titled": "{{title}}" }, "status": "tough" } }
            } ] } ] }
            """));
        var world = WorldSetup.DealWithoutCardAbilities(Cards, Blueprints.From(Dealer.DealOrder(Setup, "rhino", [hero]), Cards), [Setup.Hero(hero).Name], 12345);
        world.Seats[0].IdentityCard.TurnTo(identity);
        var abilitySource = world.CreateCard(source, world.AreaOf(DeckType.RevealingArea, PlayArea.Of(0)));
        var sameTitledAlly = InPlay(world, unrelated, DeckType.AlliesArea);
        runner.WhenRevealed(world, abilitySource, 0);
        Assert.True(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Tough));
        Assert.False(Statuses.Has(world, sameTitledAlly, Statuses.Tough));
    }

    protected static Card InPlay(World world, string card, DeckType area) => world.CreateCard(card, world.AreaOf(area, PlayArea.Of(0), cardOwner: 0));
    protected static Resolution ResolveAction(Game game, Card source)
    {
        var action = Assert.Single(game.Pending!.Affordances, option => option.Verb == Game.ActionVerb && option.AnchorId == source.ObjectId);
        return game.Resolve(Decision.Take(action.Id));
    }

    protected static (Game Game, World World) Playing(Action<World> prepare, AbilityRunner runner, string hero = "spider_man")
    {
        var world = WorldSetup.DealWithoutCardAbilities(Cards, Blueprints.From(Dealer.DealOrder(Setup, "rhino", [hero]), Cards), [Setup.Hero(hero).Name], 12345);
        prepare(world);
        var game = Game.Begin(world, Cards, runner);
        while (game.Pending is { } pending && pending.Affordances.Any(option => option.Verb == Game.ResolveMulligans))
        {
            game.Resolve(Decision.Decline);
        }

        return (game, world);
    }
}
