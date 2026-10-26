using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Content.Behavior;
using Marvel.Content.Setup;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Server;
using Marvel.Tests;
using Xunit;

namespace Marvel.Godot.Tests;

internal sealed class NativeSceneGameFactory(string contract) : IGameFactory, ISetupDiscovery
{
    private readonly DatasetGameFactory discovery = DatasetGameFactory.Load(RepositoryPaths.Root);
    private readonly CardCatalog cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private readonly SetupCatalog setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    private readonly AbilityRunner abilities = new(AbilityCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("abilities", "abilities.json"))));

    public SetupChoices DiscoverSetup() => discovery.DiscoverSetup();
    private Game? prepared;
    internal Exception? Failure { get; private set; }

    internal Decision PreludeAnswer() => prepared!.Pending!.Cancellable
        ? Decision.Decline : new CoreGamePolicy(cards).Answer(prepared);

    public OpenedGame Create(GameSpecification specification)
    {
        try { return CreateScene(specification); }
        catch (Exception error) { Failure = error; throw; }
    }

    private OpenedGame CreateScene(GameSpecification specification)
    {
        if (contract == "dense-table") return DenseTable();
        bool wakanda = contract.StartsWith("wakanda", StringComparison.Ordinal);
        string hero = Hero();
        Assert.Equal(hero, Assert.Single(specification.Heroes));
        // Prepared legal Core component scenes, not claims of a complete opening transcript.
        var scene = CanonicalCoreScene.Deal(new CoreSceneRequest(
            "native-component:" + contract,
            "rhino", [hero], 855), setup, cards, abilities);
        Prepare(scene, wakanda);
        Game game = Game.Begin(scene.World, cards, abilities);
        Assert.Equal(PublicDecisionKind.OpeningHand, game.Pending!.PublicKind);
        game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances).Id));
        Assert.Equal(PublicDecisionKind.PlayerAction, game.Pending!.PublicKind);
        Assert.Equal(Outcome.Unfinished, scene.World.Result);
        Assert.Equal(0, game.Pending.Player);
        prepared = game;
        return new(game, []);
    }

    private string Hero() => contract switch
    {
        "variable-payment" => "captain_marvel",
        "card-look" => "iron_man",
        _ => contract.StartsWith("wakanda", StringComparison.Ordinal) ? "black_panther" : "spider_man",
    };

    private void Prepare(CanonicalCoreScene scene, bool wakanda)
    {
        scene.Apply(new SetSceneForm(0, contract switch
        {
            "variable-payment" => "01010a",
            "card-look" => "01029b",
            _ => wakanda ? "01040a" : "01001a",
        }));
        PrepareCards(scene, wakanda);
    }

    private void PrepareCards(CanonicalCoreScene scene, bool wakanda)
    {
        if (contract == "lockjaw-discard") LockjawNativeScene.Prepare(scene);
        else if (contract == "melee-sequential") MeleeNativeScene.Prepare(scene);
        else if (wakanda) PrepareWakanda(scene);
        else if (contract == "variable-payment") PrepareEnergyChannel(scene);
        else if (contract == "repeated-targets") PrepareExplosion(scene);
        else if (contract == "card-look") PrepareLook(scene);
        else if (contract.StartsWith("bruno-", StringComparison.Ordinal)) BrunoNativeScene.Prepare(scene);
        else if (contract == "resource-declaration") PrepareDeclaration(scene);
        else PrepareSpiderMan(scene);
    }

    private static void PrepareLook(CanonicalCoreScene scene)
    {
        scene.Apply(new SetPlayerHand(0, [new("01088"), new("01089"), new("01090"),
            new("01030"), new("01032"), new("01031")]));
        scene.Apply(new StackPlayerDeck(0, [new("01035"), new("01036"), new("01038")]));
    }

    private static void PrepareDeclaration(CanonicalCoreScene scene)
    {
        scene.Apply(new SetPlayerHand(0, [new("01060"), new("01062")]));
        scene.Apply(new SetSceneCounters(new("01097b"), "threat", 5));
    }

    private OpenedGame DenseTable()
    {
        OpenedGame opened = discovery.Create(new("rhino", ["iron_man"], null, Seed: 5));
        var policy = new CoreGamePolicy(cards);
        for (int answer = 0; answer < 48; answer++) opened.Game.Resolve(policy.Answer(opened.Game));
        Assert.NotNull(opened.Game.Pending);
        Assert.Equal(Outcome.Unfinished, opened.Game.State.Result);
        return opened;
    }

    private static void PrepareEnergyChannel(CanonicalCoreScene scene)
    {
        scene.Apply(new MoveSceneCard(new("01018"), new(SceneZone.Upgrade, 0)));
        scene.Apply(new SetPlayerHand(0, [new("01088")]));
    }

    private static void PrepareExplosion(CanonicalCoreScene scene)
    {
        // rr:indirect-damage: assign all damage before applying it, bounded by remaining hit points.
        scene.Apply(new MoveSceneCard(new("01002"), new(SceneZone.Ally, 0)));
        scene.Apply(new MoveSceneCard(new("01109"), new(SceneZone.SideScheme)));
        scene.Apply(new SetSceneCounters(new("01109"), "threat", 3));
        scene.Apply(new SetPlayerHand(0, []));
        scene.Apply(new StackEncounterDeck([new("01101"), new("01111")]));
    }

    private static void PrepareWakanda(CanonicalCoreScene scene)
    {
        scene.Apply(new MoveSceneCard(new("01046"), new(SceneZone.Upgrade, 0)));
        scene.Apply(new MoveSceneCard(new("01047"), new(SceneZone.Upgrade, 0)));
        scene.Apply(new SetPlayerHand(0, [new("01043a"), new("01044")]));
        Assert.Equal(2, scene.World.Seats[0].Hand.Cards.Count);
        Assert.Equal(DeckType.UpgradesArea, scene.Find(new("01046")).Area.Type);
        Assert.Equal(DeckType.UpgradesArea, scene.Find(new("01047")).Area.Type);
    }

    private void PrepareSpiderMan(CanonicalCoreScene scene)
    {
        if (contract is "victory" or "keyboard-target")
        {
            // rr:villain-defeat: defeating the final villain stage wins the game.
            scene.Apply(new SetSceneVillain(new("01095")));
            scene.Apply(new SetSceneDamage(new("01095"), 14));
            scene.Apply(new SetPlayerHand(0, []));
            if (contract == "keyboard-target")
                scene.Apply(new MoveSceneCard(new("01103"), new(SceneZone.EngagedMinion, 0)));
            Assert.Equal(14, scene.Find(new("01095")).Damage);
        }
        else scene.Apply(new SetPlayerHand(0, [new("01002"), new("01088")]));
    }
}
