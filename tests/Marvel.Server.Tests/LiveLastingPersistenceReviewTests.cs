using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class LiveLastingPersistenceReviewTests : EngineHostTestBase
{
    [Fact]
    public void OpenLiveGrantRestartsAndSourceDepartureMatchesUninterruptedHistory()
    {
        // The existing IDurableGameFactory seam installs a synthetic Action to
        // isolate live-amount reconstruction; this is not Combat Training's text.
        const string label = "live-lasting-review";
        var factory = new LiveGrantFactory(DatasetGameFactory.Load(RepositoryPaths.Root));
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, store: store);
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", label,
            new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
        string capability = RequiredCapability(opened);
        EngineResponse kept = host.Exchange(EngineRequest.ResolveGame("keep", label, capability,
            TakeOnly(opened), opened.Revision));
        Affordance action = Assert.Single(kept.Prompt!.Affordances,
            option => option.AnchorId == factory.Source.ObjectId && option.Verb == Game.ActionVerb);
        EngineResponse waiting = host.Exchange(EngineRequest.ResolveGame("act", label, capability,
            Decision.Take(action.Id), kept.Revision));
        Assert.Null(waiting.Error);
        Assert.Equal(Question.Option, waiting.Prompt!.Asking);
        Assert.Equal(3, Attack(factory.Game.State));
        StoredSession prefix = Assert.Single(store.Load());
        string saved = SessionSaveJson.Write(prefix.Save);
        string digest = factory.Game.State.Digest().Canonical();
        long rng = factory.Game.State.Random.Generator.WordsConsumed;
        var restartStore = new MemorySessionStore();
        Assert.Null(restartStore.Commit(StoredSessionJson.Read(StoredSessionJson.Write(prefix))));
        var restartFactory = new LiveGrantFactory(DatasetGameFactory.Load(RepositoryPaths.Root));
        var restarted = new EngineHost(restartFactory, store: restartStore);
        EngineResponse restored = restarted.Exchange(EngineRequest.SyncGame("sync", label, capability));
        Assert.Null(restored.Error);
        Assert.Equal(saved, SessionSaveJson.Write(Assert.Single(restartStore.Load()).Save));
        Assert.Equal(digest, restartFactory.Game.State.Digest().Canonical());
        Assert.Equal(rng, restartFactory.Game.State.Random.Generator.WordsConsumed);
        Assert.Equal(3, Attack(restartFactory.Game.State));
        Assert.Equal(EngineJson.Write(waiting with { RequestId = "same", Events = [] }),
            EngineJson.Write(restored with { RequestId = "same", Events = [] }));
        EngineResponse invalid = restarted.Exchange(EngineRequest.ResolveGame("invalid", label,
            capability, Decision.Take(int.MaxValue), restored.Revision));
        Assert.NotNull(invalid.Error);
        Assert.Equal(saved, SessionSaveJson.Write(Assert.Single(restartStore.Load()).Save));
        EngineResponse expected = host.Exchange(EngineRequest.ResolveGame("finish", label, capability,
            Decision.Take(waiting.Prompt.Affordances[0].Id), waiting.Revision));
        EngineResponse actual = restarted.Exchange(EngineRequest.ResolveGame("finish", label, capability,
            Decision.Take(restored.Prompt!.Affordances[0].Id), restored.Revision));
        Assert.Null(expected.Error);
        Assert.Null(actual.Error);
        Assert.Equal(2, Attack(restartFactory.Game.State));
        Assert.Equal(EngineJson.Write(expected), EngineJson.Write(actual));
        Assert.Equal(SessionSaveJson.Write(Assert.Single(store.Load()).Save),
            SessionSaveJson.Write(Assert.Single(restartStore.Load()).Save));
        Assert.Equal(factory.Game.State.Digest().Canonical(), restartFactory.Game.State.Digest().Canonical());
        Assert.Equal(rng, restartFactory.Game.State.Random.Generator.WordsConsumed);
    }

    private static long Attack(World world) => StateFields.Modified(world,
        world.TheCardIn(DeckType.VillainArea)!, "attack", world.Facts, world.Players);

    private sealed class LiveGrantFactory(IDurableGameFactory inner) : IDurableGameFactory
    {
        public SessionCompatibility Compatibility => inner.Compatibility;
        public Card Source { get; private set; } = null!;
        public Game Game { get; private set; } = null!;

        public OpenedGame Create(GameSpecification specification)
        {
            OpenedGame opened = inner.Create(specification);
            World world = opened.Game.State;
            Source = world.CreateCard("01057", world.AreaOf(DeckType.UpgradesArea,
                PlayArea.Of(0), cardOwner: 0));
            var book = AbilityCatalog.Parse("""
                {"cards":[{"card":"01057","abilities":[{
                  "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"game"},
                  "effect":{"seq":[
                    {"grantUntil":{"card":{"query":"villain"},"keyword":"attack",
                      "amount":{"count":{"query":"upgradesYouControl"}},"until":"EndOfPlayerPhase"}},
                    {"choose":{"options":[{"discard":"this"},
                      {"dealDamage":{"cards":"you","amount":1}}]}}
                  ]}
                }]}]}
                """);
            Game = Game.Begin(world, world.Facts, new AbilityRunner(book));
            return opened with { Game = Game };
        }
    }
}
