using System.Net;
using Marvel.Tests;
using Marvel.View;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class CardFactsTransportTests : TransportTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PrintedMarksAndAuthorizedEffectiveSourcesSurviveTransport(bool socket)
    {
        var host = new EngineHost(DatasetGameFactory.Load(RepositoryPaths.Root));
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame("open", "card-facts",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 7)));
        WorldDescriptor world = opened.World!;
        CardDescriptor card = world.Areas.SelectMany(area => area.Cards).First(item => item.Face is not null);
        // Synthetic wire stress values are not additional playable card content.
        CardFaceDescriptor face = card.Face! with
        {
            PrintedValues = new Dictionary<string, CardPrintedValue>
            {
                ["ATK"] = new("3", true, false, 3),
                ["HP"] = new("12", false, true, 0),
                ["THW"] = new("—", false, false, 0),
                ["HS"] = new("X", true, false, 0),
                ["Boost"] = new("0", true, false, 0),
            },
            EffectiveValues = new Dictionary<string, CardEffectiveValue>
            {
                ["ATK"] = new(3, 4, "Printed", true,
                    [new CardValueCalculation("Add", 1,
                        new CardValueSourceDescriptor("01091", "Combat Training", null, true)
                        {
                            RulesText = "Your hero gets +1 ATK.",
                            RulesMarkup = "Your hero gets +1 ATK.",
                        }, new CardValueDuration("round", null, 1, false))]),
            },
        };
        var persistent = new CardPersistentDescriptor(
            new(card.Face.Id, card.Face.Title, card.Id, false), new("Attached", 1, 0),
            [new(1, "ATK", "Add", -1, new(null, null, null, true))],
            [new(new("Action", null, null, null, null, "Hero", null, true),
                [new("SpendResources", "ActingPlayer", null, "YY", null, true)],
                [new("RedirectAllDamage", "Source", null, null, null, null,
                    new("Source", "damage", 5)),
                 new("Discard", "Source", null, null, null, "WhenAttackEnds", null)])],
            HasUnresolvedAbilities: true);
        WorldDescriptor described = world with
        {
            Areas = world.Areas.Select(area => area with
            {
                Cards = area.Cards.Select(item => ReferenceEquals(item, card)
                    ? item with { Face = face, Persistent = persistent } : item)
                    .ToArray(),
            }).ToArray(),
        };
        EngineRequest request = EngineRequest.SyncGame("facts", "card-facts", opened.Capability!);
        var endpoint = new EchoEndpoint(request, opened with { RequestId = request.RequestId, World = described });

        EngineResponse received = socket
            ? await ExchangeOverSocket(new SocketEngineServer(endpoint, IPAddress.Loopback, 0), request)
            : await new InProcessTransport(endpoint).ExchangeAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(EngineProtocol.Version, received.Version);
        CardFaceDescriptor restored = received.World!.Areas.SelectMany(area => area.Cards)
            .Single(item => item.Id == card.Id).Face!;
        Assert.Equal(face.PrintedValues, restored.PrintedValues);
        CardEffectiveValue effective = restored.EffectiveValues["ATK"];
        Assert.Equal(3, effective.BaseValue);
        Assert.Equal(4, effective.CurrentValue);
        Assert.Equal("Printed", effective.BaseKind);
        Assert.True(effective.IsModified);
        CardValueCalculation step = Assert.Single(effective.Calculation);
        Assert.Equal("Add", step.Operation);
        Assert.Equal(1, step.Amount);
        Assert.Equal(face.EffectiveValues["ATK"].Calculation[0].Source, step.Source);
        Assert.Equal(new CardValueDuration("round", null, 1, false), step.Duration);
        CardPersistentDescriptor sourceFacts = received.World.Areas.SelectMany(area => area.Cards)
            .Single(item => item.Id == card.Id).Persistent!;
        Assert.Equal(persistent.Source, sourceFacts.Source);
        Assert.Equal(persistent.Relation, sourceFacts.Relation);
        Assert.Equal(persistent.Contributions, sourceFacts.Contributions);
        Assert.True(sourceFacts.HasUnresolvedAbilities);
        CardPersistentAbilityDescriptor ability = Assert.Single(sourceFacts.Abilities);
        Assert.Equal(persistent.Abilities[0].Trigger, ability.Trigger);
        Assert.Equal(persistent.Abilities[0].Costs, ability.Costs);
        Assert.Equal(persistent.Abilities[0].Effects, ability.Effects);
    }
}
