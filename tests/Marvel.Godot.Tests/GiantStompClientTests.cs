using Marvel.Client;
using Marvel.Protocol;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.Tests;
using Marvel.Transport;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

/// <summary>A real Ant-Man game plays Giant Stomp through the client on both transports.</summary>
public sealed class GiantStompClientTests : RestrictedMultiplayerJourneyTestBase
{
    // Seed 4 deals Scott Lang an opening hand holding Giant Stomp and five other
    // cards, so the first turn can change to the Giant form and pay its cost of 3.
    private const uint Seed = 4;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:ability.4")]
    public async Task UntargetedGiantStompAttackIsAcceptedThroughItsEnemyChoice(bool socket)
    {
        // Giant Stomp's attack begins before its enemy exists: the ability resolves
        // "one sentence at a time", and "Deal 8 damage to an enemy" is chosen after
        // "Deal 1 damage to each minion". Its boundary carries the -1 target.
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
            ClientEntryResult opened = await client.OpenSessionAsync("giant-stomp",
                new GameSpecification("rhino", ["ant_man"], [], Seed), TestContext.Current.CancellationToken);
            Assert.True(opened.Succeeded, opened.Error?.Message);
            ClientSession session = Assert.IsType<ClientSession>(opened.Session);
            EngineResponse current = Assert.IsType<EngineResponse>(opened.Response);
            current = await Resolve(client, session, Decision.Take(Assert.Single(current.Prompt!.Affordances,
                offer => offer.Verb == Game.ResolveMulligans).Id));
            current = await Resolve(client, session, Decision.Take(Assert.Single(current.Prompt!.Affordances,
                offer => offer.Verb == Game.ChangeForm && offer.Label == "12001c").Id));
            current = await Resolve(client, session, Decision.Decline);

            var hand = current.World!.Areas.Single(area => area.Zone == "HandsArea").Cards;
            int stomp = Assert.Single(hand, card => card.Face?.Id == "12003").Id!.Value;
            Affordance play = Assert.Single(current.Prompt!.Affordances, offer => offer.AnchorId == stomp);
            int[] paying = hand.Where(card => card.Id != stomp).Take(3).Select(card => card.Id!.Value).ToArray();
            current = await Resolve(client, session, new Decision(play.Id, [], paying,
                Allocations: ResourcePayment.Allocate(Assert.Single(play.Costs!), paying)));

            AttackBoundary started = Assert.Single(current.Events.OfType<AttackBoundary>(), entry => entry.Started);
            Assert.Equal(-1, started.Target);
            Assert.Equal("Ant-Man · Attack begins.", EventPresenter.Present(started, current.World!).Summary);
            Assert.Equal(Question.Element, current.Prompt!.Asking);
            var villain = current.World!.Areas.Single(area => area.Zone == "VillainArea").Cards.Single();
            current = await Resolve(client, session, Decision.Take(Assert.Single(current.Prompt.Affordances,
                offer => offer.Id == villain.Id).Id));

            Assert.Contains(current.Events.OfType<AttackBoundary>(), entry => !entry.Started && entry.Target == -1);
            Assert.Equal(8, current.World!.Areas.Single(area => area.Zone == "VillainArea").Cards.Single()
                .State!.Damage);

            // The same real stream, with the boundary's target pushed past the
            // engine's -1 "no target" spelling, is not a complete event list.
            Assert.False(ClientResponseValidation.HasCompleteEvents(
                [started with { Target = -2 }]));
        }
        finally
        {
            if (server is not null) await server.DisposeAsync();
        }
    }

    private static async Task<EngineResponse> Resolve(
        LocalGameClient client, ClientSession session, Decision decision)
    {
        ClientResolutionResult result = await client.ResolveAsync(session, decision,
            TestContext.Current.CancellationToken);
        Assert.Null(result.Error);
        Assert.Equal(ClientMutationDisposition.Accepted, result.MutationDisposition);
        Assert.True(result.Succeeded);
        return Assert.IsType<EngineResponse>(result.Response);
    }
}
