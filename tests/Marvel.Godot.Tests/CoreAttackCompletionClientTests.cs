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
            var changed = Assert.Single(current.Events.OfType<CardFormChanged>());
            var changedSubject = Assert.Single(EventPresenter.Present(changed, current.World!).Activity!.Subjects);
            Assert.Equal(changed.To, Assert.IsType<BoardCardPresentation>(changedSubject.Reference).FaceId);
            current = await Resolve(client, session, EngineDecision.Decline);
            Affordance discards = Assert.Single(current.Prompt!.Affordances);
            Assert.Equal(Game.EndPhaseVerb, discards.Verb);
            current = await Resolve(client, session, new EngineDecision(discards.Id,
                discards.Targets!.Legal.Take(discards.Targets.Min).ToArray()));

            Assert.Equal(Question.Opportunity, current.Prompt!.Asking);
            ActiveAttackDescriptor initiated = Assert.IsType<ActiveAttackDescriptor>(current.World!.ActiveAttack);
            Assert.Equal("Rhino", initiated.Source.Name);
            Assert.Equal("Spider-Man", initiated.Target.Name);
            current = await Resolve(client, session, EngineDecision.Decline);
            Assert.Equal(Question.Defender, current.Prompt!.Asking);
            Assert.Equal(initiated, current.World!.ActiveAttack);
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
            Assert.NotEmpty(presented.Cues);
            Assert.NotEmpty(EventActivityBeats.From(presented.Cues));
            Assert.Contains(current.Events.OfType<AttackBoundary>(), boundary => !boundary.Started);
            Assert.Contains(presented.Highlights, cue => cue.Motion == EventMotionKind.Attack
                && cue.Summary.Contains("Spider-Man defended", StringComparison.Ordinal));
            // This seed immediately reveals another attack. Each live descriptor
            // belongs to that new attack; decline its actual offered windows.
            for (int windows = 0; current.World!.ActiveAttack is not null; windows++)
            {
                Assert.True(windows < 8);
                Assert.True(current.Prompt!.Cancellable);
                current = await Resolve(client, session, EngineDecision.Decline);
            }
            Assert.Null(current.World!.ActiveAttack);
        }
        finally
        {
            if (server is not null) await server.DisposeAsync();
        }
    }

    [Fact]
    public async Task CoreSchemeResultCarriesTheExactActivationCause()
    {
        var client = new LocalGameClient(new InProcessTransport(new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root))));
        var opened = await client.OpenSessionAsync("scheme-cause",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 1), TestContext.Current.CancellationToken);
        ClientSession session = Assert.IsType<ClientSession>(opened.Session);
        EngineResponse current = Assert.IsType<EngineResponse>(opened.Response);
        current = await Resolve(client, session, new EngineDecision(Assert.Single(current.Prompt!.Affordances).Id, []));
        current = await Resolve(client, session, EngineDecision.Decline);
        var discards = Assert.Single(current.Prompt!.Affordances);
        current = await Resolve(client, session, new EngineDecision(discards.Id,
            discards.Targets!.Legal.Take(discards.Targets.Min).ToArray()));
        FieldSet threat = Assert.Single(current.Events.OfType<FieldSet>(), entry => entry.Cause?.Kind == EventCauseKind.Scheme && entry.Field == "k_threat");
        Assert.Equal("Rhino", threat.Cause!.SourceTitle);
        Assert.Equal("Peter Parker", threat.Cause.TargetTitle);
        Assert.Equal("Rhino schemes against Peter Parker", EventPresenter.Present(threat, current.World!).Intention);
        Assert.All(current.Events.Where(entry => entry.Cause is not null), entry => Assert.Equal(threat.Cause.Occurrence, entry.Cause!.Occurrence));
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
