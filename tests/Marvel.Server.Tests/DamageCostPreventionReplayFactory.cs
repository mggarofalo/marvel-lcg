using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;

namespace Marvel.Server.Tests;

/// <summary>Printed Focused Rage and Energy Barrier through the existing nonpublic durable factory seam.</summary>
internal sealed class DamageCostPreventionReplayFactory : IDurableGameFactory
{
    public SessionCompatibility Compatibility { get; } = DatasetGameFactory.Load(RepositoryPaths.Root).Compatibility;
    internal Game Game { get; private set; } = null!;
    internal int Rage { get; private set; }
    internal int Barrier { get; private set; }
    internal int Villain { get; private set; }
    internal int Minion { get; private set; }

    public OpenedGame Create(GameSpecification specification)
    {
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var runner = new AbilityRunner(AbilityCatalog.Parse(
            File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json"))));
        var world = WorldSetup.DealWithoutCardAbilities(cards,
            Blueprints.From(Dealer.DealOrder(setup, "rhino", ["she_hulk"]), cards),
            [setup.Hero("she_hulk").Name], specification.Seed);
        world.Seats[0].IdentityCard.TurnTo("01019a");
        var upgrades = world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0);
        Rage = world.CreateCard("01027", upgrades).ObjectId;
        var barrier = world.CreateCard("05017", upgrades);
        barrier.PlaceTokens("c_reflection", 1);
        Barrier = barrier.ObjectId;
        Villain = world.TheCardIn(DeckType.VillainArea)!.ObjectId;
        Minion = world.CreateCard("01167",
            world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0))).ObjectId;
        Game = Marvel.Rules.Play.Game.Begin(world, cards, runner);
        for (int count = 0; count < 30; count++)
        {
            var prompt = Game.Pending ?? throw new InvalidOperationException("Missing setup prompt");
            if (prompt.PublicKind == PublicDecisionKind.PlayerAction) return new(Game, []);
            Game.Resolve(prompt.Cancellable ? Decision.Decline : Decision.Take(prompt.Affordances[0].Id));
        }
        throw new InvalidOperationException("Setup did not reach the player turn");
    }
}
