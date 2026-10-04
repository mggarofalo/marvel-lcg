using Marvel.Client;
using Marvel.Decisions;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class CoreAttackCompletionClientTests : RestrictedMultiplayerJourneyTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CompletedCoreDefenseIsAcceptedWithoutLosingItsResult(bool socket)
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        RunningServer? server = null;
        try
        {
            IEngineTransport transport = new InProcessTransport(host);
            if (socket)
            {
                server = await RunningServer.StartAsync(host, port: 0);
                transport = new SocketTransport(server.Endpoint.Address.ToString(), server.Endpoint.Port);
            }
            var client = new LocalGameClient(transport);
            ClientEntryResult opened = await client.OpenSessionAsync("attack-completion",
                new GameSpecification("rhino", ["spider_man"], [], Seed: 1),
                TestContext.Current.CancellationToken);
            Assert.True(opened.Succeeded, opened.Error?.Message);
            ClientSession session = Assert.IsType<ClientSession>(opened.Session);
            EngineResponse current = Assert.IsType<EngineResponse>(opened.Response);
            Affordance mulligan = Assert.Single(current.Prompt!.Affordances);
            Assert.Equal(Game.ResolveMulligans, mulligan.Verb);
            current = await Resolve(client, session, new EngineDecision(mulligan.Id, []));

            Affordance change = Assert.Single(current.Prompt!.Affordances,
                option => option.Verb == Game.ChangeForm);
            current = await Resolve(client, session, new EngineDecision(change.Id, []));
            current = await Resolve(client, session, EngineDecision.Decline);
            Affordance discards = Assert.Single(current.Prompt!.Affordances);
            Assert.Equal(Game.EndPhaseVerb, discards.Verb);
            current = await Resolve(client, session, new EngineDecision(discards.Id,
                discards.Targets!.Legal.Take(discards.Targets.Min).ToArray()));

            Assert.Equal(Question.Opportunity, current.Prompt!.Asking);
            current = await Resolve(client, session, EngineDecision.Decline);
            Assert.Equal(Question.Defender, current.Prompt!.Asking);
            Affordance defender = Assert.Single(current.Prompt.Affordances);
            long beforeDefense = current.Revision;
            current = await Resolve(client, session, new EngineDecision(defender.Id, []));

            Assert.Equal(beforeDefense + 1, current.Revision);
            AttackCompleted completed = Assert.Single(current.Events.OfType<AttackCompleted>());
            Assert.Equal(defender.AnchorId, completed.Target);
            Assert.Equal(defender.AnchorId, completed.Defender);
            Assert.Equal("Rhino", completed.Subjects![completed.Enemy]);
            Assert.Equal("Spider-Man", completed.Subjects[completed.Defender]);
            EventBatchPresentation presented = EventCuePlanner.Plan(current.Events,
                current.World!, Outcome.Unfinished);
            Assert.Contains(presented.Highlights, cue => cue.Motion == EventMotionKind.Attack
                && cue.Summary.Contains("Spider-Man defended", StringComparison.Ordinal));
        }
        finally
        {
            if (server is not null) await server.DisposeAsync();
        }
    }

    private static async Task<EngineResponse> Resolve(
        LocalGameClient client, ClientSession session, EngineDecision decision)
    {
        ClientResolutionResult result = await client.ResolveAsync(session, decision,
            TestContext.Current.CancellationToken);
        Assert.Null(result.Error);
        Assert.Equal(ClientMutationDisposition.Accepted, result.MutationDisposition);
        Assert.True(result.Succeeded);
        return Assert.IsType<EngineResponse>(result.Response);
    }
}
