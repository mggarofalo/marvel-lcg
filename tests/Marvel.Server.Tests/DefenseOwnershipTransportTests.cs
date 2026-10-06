using System.Net;
using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Session;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class DefenseOwnershipTransportTests : TransportTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task InProcessSeatsOwnDefenseAcrossRestartAndCompetingSubmissions(bool pass) => Verify(false, pass);

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public Task SocketSeatsOwnDefenseAcrossRestartAndCompetingSubmissions(bool pass) => Verify(true, pass);

    private static async Task Verify(bool socket, bool pass)
    {
        // "the defending player becomes the new target"; initiation's "you"
        // still means the original player. The capability protocol is ours.
        var factory = DatasetGameFactory.Load(RepositoryPaths.Root);
        var store = new MemorySessionStore();
        var host = new EngineHost(factory, visibility: new RestrictedVisibilityPolicy(0), store: store);
        const string game = "defense-ownership";
        async Task<EngineResponse> Exchange(EngineRequest request)
        {
            EngineResponse response = socket
                ? await ExchangeOverSocket(new SocketEngineServer(host, IPAddress.Loopback, 0), request)
                : await new InProcessTransport(host).ExchangeAsync(request, TestContext.Current.CancellationToken);
            return EngineJson.ReadResponse(EngineJson.Write(response));
        }
        EngineResponse opened = await Exchange(EngineRequest.OpenGame("open", game,
            new GameSpecification("rhino", ["spider_man", "captain_marvel"], [], Seed: 1)));
        EngineResponse joined = await Exchange(EngineRequest.AttachGame("join", game,
            Assert.Single(opened.Invitations!).Invitation));
        string[] capabilities = [opened.Capability!, joined.Capability!];
        EngineResponse current = await ReachDefense(Exchange, capabilities, opened, game);
        Assert.Equal(1, current.World!.Table!.PromptOwner);
        EngineResponse helper = await Exchange(EngineRequest.SyncGame("helper", game, capabilities[1]));
        Prompt offered = Assert.IsType<Prompt>(helper.Prompt);
        Affordance defense = Assert.Single(offered.Affordances);
        Assert.Equal(1, defense.AnchorPlayer);
        Assert.Equal("Pass defense opportunity", offered.DeclineLabel);
        Assert.Contains("Spider-Man", offered.Description);
        Assert.Contains("Exhaust Captain Marvel", defense.Description);
        EngineResponse attacked = await Exchange(EngineRequest.SyncGame("attacked", game, capabilities[0]));
        Assert.Null(attacked.Prompt);
        Assert.Equal(1, attacked.World!.Table!.PromptOwner);
        Assert.Equal(2, attacked.World.Table.PendingSituation!.SourceCardIds.Count);
        Assert.All(Hand(attacked, 1), card => Assert.Null(card.Face));
        EngineResponse stolen = await Exchange(EngineRequest.ResolveGame("stolen", game, capabilities[0],
            new EngineDecision(defense.Id, []), helper.Revision));
        Assert.Equal("not_your_turn", stolen.Error?.Code);

        host = new EngineHost(factory, visibility: new RestrictedVisibilityPolicy(0), store: store);
        EngineResponse restored = await Exchange(EngineRequest.SyncGame("restored", game, capabilities[1]));
        Assert.Equal(EngineJson.Write(helper with { RequestId = "same", Events = [] }),
            EngineJson.Write(restored with { RequestId = "same", Events = [] }));
        EngineDecision answer = pass ? EngineDecision.Decline : new EngineDecision(defense.Id, []);
        EngineResponse accepted = await Exchange(EngineRequest.ResolveGame("answer", game,
            capabilities[1], answer, restored.Revision));
        Assert.Null(accepted.Error);
        EngineResponse repeated = await Exchange(EngineRequest.ResolveGame("repeat", game,
            capabilities[1], new EngineDecision(defense.Id, []), restored.Revision));
        Assert.Equal("stale_decision", repeated.Error?.Code);
        EngineResponse competing = await Exchange(EngineRequest.ResolveGame("compete", game,
            capabilities[0], EngineDecision.Decline, restored.Revision));
        Assert.Equal("stale_decision", competing.Error?.Code);
        if (pass)
        {
            host = new EngineHost(factory, visibility: new RestrictedVisibilityPolicy(0), store: store);
            EngineResponse self = await Exchange(EngineRequest.SyncGame("self", game, capabilities[0]));
            Assert.Equal(0, self.Prompt!.Player);
            Assert.Equal("Leave attack undefended", self.Prompt.DeclineLabel);
            Assert.All(self.Prompt.Affordances, option => Assert.Equal(0, option.AnchorPlayer));
            Assert.DoesNotContain(accepted.Events.OfType<FieldSet>(), change => change.Field == "is_exhaust");
        }
        else
        {
            Assert.Contains(accepted.Events.OfType<FieldSet>(), change =>
                change.Card == defense.AnchorId && change.Field == "is_exhaust" && change.To == 1);
            AttackCompleted attack = Assert.Single(accepted.Events.OfType<AttackCompleted>());
            Assert.Equal(defense.AnchorId, attack.Defender);
            Assert.Equal(defense.AnchorId, attack.Target);
            EngineResponse later = await Exchange(EngineRequest.SyncGame("later-activation", game, capabilities[0]));
            Assert.Equal(Question.Defender, later.Prompt!.Asking);
            Assert.Contains("Captain Marvel", later.Prompt.Description);
            Assert.All(later.Prompt.Affordances, option => Assert.Equal(0, option.AnchorPlayer));
            EngineResponse laterDamage = await Exchange(EngineRequest.ResolveGame("pass-later", game,
                capabilities[0], EngineDecision.Decline, later.Revision));
            Assert.Null(laterDamage.Error);
            AttackCompleted ownActivation = Assert.Single(laterDamage.Events.OfType<AttackCompleted>());
            Assert.Equal(defense.AnchorId, ownActivation.Target);
            Assert.Equal(-1, ownActivation.Defender);
        }
        Assert.DoesNotContain("capability", SessionSaveJson.Write(Assert.Single(store.Load()).Save),
            StringComparison.OrdinalIgnoreCase);
    }
    private static async Task<EngineResponse> ReachDefense(
        Func<EngineRequest, Task<EngineResponse>> exchange, string[] capabilities,
        EngineResponse current, string game)
    {
        int answers = 0;
        bool spiderSense = false;
        var changed = new HashSet<int>();
        while (current.World!.Table!.PendingSituation!.Kind != PublicDecisionKind.Defense)
        {
            Assert.True(answers++ < 20, "ordinary setup and first turn did not reach defense");
            int owner = current.World.Table.PromptOwner!.Value;
            current = await exchange(EngineRequest.SyncGame("owner", game, capabilities[owner]));
            Prompt prompt = Assert.IsType<Prompt>(current.Prompt);
            Affordance? change = prompt.Affordances.FirstOrDefault(option => option.Verb == Game.ChangeForm);
            EngineDecision decision;
            if (change is not null && changed.Add(owner)) decision = new EngineDecision(change.Id, []);
            else if (prompt.Asking == Question.Opportunity)
            {
                Assert.Equal(0, owner);
                Affordance sense = Assert.Single(prompt.Affordances);
                Assert.Equal("Spider-Sense", sense.Label);
                spiderSense = true;
                decision = new EngineDecision(sense.Id, []);
            }
            else if (prompt.Cancellable) decision = EngineDecision.Decline;
            else
            {
                Affordance only = Assert.Single(prompt.Affordances);
                decision = new EngineDecision(only.Id,
                    only.Targets?.Legal.Take(only.Targets.Min).ToArray() ?? []);
            }
            current = await exchange(EngineRequest.ResolveGame("advance", game,
                capabilities[owner], decision, current.Revision));
            Assert.Null(current.Error);
        }
        Assert.True(spiderSense);
        return current;
    }

}
