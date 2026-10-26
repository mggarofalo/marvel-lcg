using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Session;
using Marvel.Tests;

namespace Marvel.Server.Tests;

// Prepared scene through the durable factory seam; the action is synthetic, prevention is printed.
internal sealed class OverkillPreventionReplayReviewFactory(bool barrier) : IDurableGameFactory
{
    public SessionCompatibility Compatibility { get; } = DatasetGameFactory.Load(RepositoryPaths.Root).Compatibility;
    internal Game Game { get; private set; } = null!;
    internal int Action { get; private set; }
    internal int Prevention { get; private set; }
    internal int Ally { get; private set; }
    internal int Villain { get; private set; }

    public OpenedGame Create(GameSpecification specification)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var authored = JsonNode.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json")))!;
        var book = JsonNode.Parse("""
            {"cards":[{"card":"01092","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "cost":{"exhaust":"this"},
              "effect":{"enemyAttacks":{"enemies":{"query":"villain"},"dealBoostCards":"false"}}
            }]}]}
            """)!;
        foreach (string face in new[] { "01003", "05017" })
            book["cards"]!.AsArray().Add(authored["cards"]!.AsArray()
                .Single(row => row!["card"]!.GetValue<string>() == face)!.DeepClone());
        var runner = new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString()));
        var world = WorldSetup.DealWithoutCardAbilities(cards,
            Blueprints.From(Dealer.DealOrder(setup, "rhino", ["spider_man"]), cards),
            [setup.Hero("spider_man").Name], specification.Seed);
        world.Seats[0].IdentityCard.TurnTo("01001a");
        var prevention = world.CreateCard(barrier ? "05017" : "01003", barrier
            ? world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0) : world.Seats[0].Hand);
        Prevention = prevention.ObjectId;
        if (barrier) prevention.PlaceTokens("c_reflection", 1);
        var ally = world.CreateCard("01020", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(2);
        Ally = ally.ObjectId;
        Villain = world.TheCardIn(DeckType.VillainArea)!.ObjectId;
        world.Effects.Register(new(EffectSource.LastingEffect, Keywords.Overkill, 1, Affects: Villain));
        Action = world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0)).ObjectId;
        Game = Marvel.Rules.Play.Game.Begin(world, cards, runner);
        for (int count = 0; count < 20; count++)
        {
            var prompt = Game.Pending ?? throw new InvalidOperationException("Missing setup prompt");
            if (prompt.PublicKind == PublicDecisionKind.PlayerAction) return new(Game, []);
            Game.Resolve(prompt.Cancellable ? Decision.Decline : Decision.Take(prompt.Affordances[0].Id));
        }
        throw new InvalidOperationException("Prepared scene did not reach its action");
    }
}
