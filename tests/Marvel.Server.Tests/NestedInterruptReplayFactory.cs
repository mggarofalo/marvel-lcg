using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Behavior;
using Marvel.Content.Setup;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

internal sealed class NestedInterruptReplayFactory : IDurableGameFactory
{
    private readonly CardCatalog cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private readonly SetupCatalog setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    private readonly AbilityRunner abilities = new(AbilityCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json"))));

    public SessionCompatibility Compatibility { get; } = DatasetGameFactory.Load(RepositoryPaths.Root).Compatibility;
    internal Game Game { get; private set; } = null!;
    internal int Interrupt { get; private set; }
    internal int Resource { get; private set; }

    public OpenedGame Create(GameSpecification specification)
    {
        // A legal Core component scene is the deterministic setup of this ledger
        // test. Opening-hand and hero setup use real engine decisions.
        var scene = CanonicalCoreScene.Deal(new CoreSceneRequest(
            "nested-interrupt-replay", "rhino", ["black_panther"], specification.Seed),
            setup, cards, abilities);
        scene.Apply(new SetSceneForm(0, "01040a"));
        scene.Apply(new SetSceneCounters(new("01097b"), "threat", 0));
        scene.Apply(new SetPlayerHand(0, [new("01078"), new("01088")]));
        scene.Apply(new StackEncounterDeck([new("01103"), new("01186"), new("01101")]));
        Interrupt = scene.Find(new("01078")).ObjectId;
        Resource = scene.Find(new("01088")).ObjectId;
        Game = Marvel.Rules.Play.Game.Begin(scene.World, cards, abilities);
        for (int answer = 0; answer < 30; answer++)
        {
            Prompt prompt = Assert.IsType<Prompt>(Game.Pending);
            if (prompt.PublicKind == PublicDecisionKind.PlayerAction) return new(Game, []);
            Game.Resolve(prompt.Cancellable ? Decision.Decline
                : Decision.Take(prompt.Affordances[0].Id));
        }
        throw new InvalidOperationException("The Core prelude did not reach the player turn.");
    }
}
