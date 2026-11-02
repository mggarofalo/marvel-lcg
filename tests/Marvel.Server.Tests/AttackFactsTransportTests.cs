using System.Net;
using System.Text;
using Marvel.Protocol;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.Transport;
using Xunit;

namespace Marvel.Server.Tests;

// Protocol 46 carries the attack a decision is taken within as facts; both transports keep them whole.
public sealed class AttackFactsTransportTests : TransportTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task AttackFactsSurviveEitherTransport(bool socket)
    {
        var request = EngineRequest.SyncGame("attack", "game", "capability");
        var expected = new EngineResponse(EngineProtocol.Version, request.RequestId, request.GameId, null, Defense(), []);
        var endpoint = new EchoEndpoint(request, expected);
        var actual = socket
            ? await ExchangeOverSocket(new SocketEngineServer(endpoint, IPAddress.Loopback, 0), request)
            : await new InProcessTransport(endpoint).ExchangeAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(new PromptAttack(12, 3, 2, Calculated: false, BoostUnresolved: true), actual.Prompt!.Attack);
    }

    [Fact]
    public void AttackFactsAreWrittenInSnakeCaseAndAnUnknownMemberIsRejected()
    {
        var response = new EngineResponse(EngineProtocol.Version, "attack", "game", null, Defense(), []);
        string text = Encoding.UTF8.GetString(EngineJson.Write(response));
        const string written = "\"attack\":{\"attacker\":12,\"target\":3,\"value\":2,\"calculated\":false,\"boost_unresolved\":true}";
        Assert.Contains(written, text, StringComparison.Ordinal);
        Assert.Equal(EngineJson.Write(response), EngineJson.Write(EngineJson.ReadResponse(Encoding.UTF8.GetBytes(text))));
        string forged = text.Replace("\"boost_unresolved\":true", "\"boost_unresolved\":true,\"sentence\":\"ATK 2\"",
            StringComparison.Ordinal);
        Assert.NotEqual(text, forged);
        Assert.Throws<System.Text.Json.JsonException>(() => EngineJson.ReadResponse(Encoding.UTF8.GetBytes(forged)));
    }

    private static Prompt Defense() =>
        new(0, Question.Defender, TimingPriority.Untimed, "attack_initiated", "Defend", true,
            [new Affordance(4, "Defend", 3, 0, "Defend")])
        {
            Attack = new PromptAttack(12, 3, 2, Calculated: false, BoostUnresolved: true),
        };
}
