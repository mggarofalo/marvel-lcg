using System.Net;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content;
using Marvel.Decisions;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;
using Marvel.Tests;
using Marvel.Transport;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class DefeatContextTransportTests : TransportTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RepeatedSourceOffersKeepExactCausalRecipientsAcrossTransports(bool socket)
    {
        // Synthetic trigger fixture; the wire and draft contracts are repository choices.
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(cards, 1);
        Seat seat = world.CreateSeat("P0");
        seat.IdentityCard = world.CreateCard("01001a", seat.Hero);
        world.Abilities = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01101","abilities":[{
              "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt","subject":"game"},
              "effect":{"placeAccelerationToken":1}}]}]}
            """));
        world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        Area area = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        Card source = world.CreateCard("01101", area);
        Card first = world.CreateCard("01102", area);
        Card second = world.CreateCard("01102", area);
        SimultaneousDirectDamage.Resolve(world, cards, new[] { first, second }.Select(card =>
            new DamageRequest(seat.IdentityCard, card, 10, "synthetic-context", "Deal_Damage", 0)).ToArray(), []);
        Prompt prompt = Assert.IsType<Prompt>(Sequence.Work(world, cards, world.Abilities, []));
        var visible = WorldProjection.For(world, prompt, [], new RestrictedVisibilityPolicy(0).Authorize(null, 1));
        EngineRequest request = EngineRequest.SyncGame("context", "table", "capability");
        var expected = new EngineResponse(EngineProtocol.Version, request.RequestId, request.GameId,
            null, visible.Prompt, [], World: visible.World);
        var endpoint = new EchoEndpoint(request, expected);
        EngineResponse received = socket
            ? await ExchangeOverSocket(new SocketEngineServer(endpoint, IPAddress.Loopback, 0), request)
            : await new InProcessTransport(endpoint).ExchangeAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(40, received.Version);
        Assert.Equal(EngineJson.Write(expected), EngineJson.Write(received));
        Assert.Equal(new[] { first.ObjectId, second.ObjectId }, received.Prompt!.Affordances
            .Select(offer => Assert.Single(offer.ContextCardIds)));
        var rows = PromptPresentation.From(received.Prompt, received.World!).Affordances;
        Assert.Equal(2, rows.Select(row => row.ContextDescription).Distinct().Count());
        var draft = new DecisionComposer(received.Prompt);
        draft.SelectAffordance(received.Prompt.Affordances[1].Id);
        Assert.Empty(draft.Targets);
        Assert.True(draft.TryBuild(out Decision? decision, out _));
        Assert.Equal(source.ObjectId, draft.Selected!.AnchorId);
        Prompt changedCopy = received.Prompt with
        {
            Affordances = received.Prompt.Affordances.Select(offer => offer with
            { DisplayLabel = "Readable caption", CommitLabel = "Readable commitment", ContextCardIds = [] }).ToArray(),
        };
        Assert.Equal(DecisionSelector.From(received.Prompt, decision!), DecisionSelector.From(changedCopy, decision!));
        Assert.Equal(AffordanceRecord.From(draft.Selected), AffordanceRecord.From(changedCopy.Affordances[1]));
        Sequence.Answer(world, cards, world.Abilities, prompt, decision!, []);
        Prompt remaining = Assert.IsType<Prompt>(Sequence.Work(world, cards, world.Abilities, []));
        Assert.Equal([first.ObjectId], Assert.Single(remaining.Affordances).ContextCardIds);
    }
}
