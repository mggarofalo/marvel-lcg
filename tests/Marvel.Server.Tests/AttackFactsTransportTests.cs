using System.Net;
using System.Text;
using Marvel.Protocol;
using Marvel.Rules.Prompts;
using Marvel.Rules.Timing;
using Marvel.Transport;
using Xunit;

namespace Marvel.Server.Tests;

// Protocol 46 carries the attack a decision is taken within, and a choice's printed effect, as facts; both
// transports keep them whole.
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
        Assert.Equal(new PromptAttack(12, 3, 2, Calculated: false, BoostUnresolved: true, Prevented: true) { BoostIcons = 2, BoostFacedown = 1 },
            actual.Prompt!.Attack);
    }

    [Fact]
    public void AttackFactsAreWrittenInSnakeCaseAndAnUnknownMemberIsRejected()
    {
        var response = new EngineResponse(EngineProtocol.Version, "attack", "game", null, Defense(), []);
        string text = Encoding.UTF8.GetString(EngineJson.Write(response));
        const string written = "\"attack\":{\"attacker\":12,\"target\":3,\"value\":2,\"calculated\":false,\"boost_unresolved\":true,\"prevented\":true,\"boost_icons\":2,\"boost_facedown\":1}";
        Assert.Contains(written, text, StringComparison.Ordinal);
        Assert.Equal(EngineJson.Write(response), EngineJson.Write(EngineJson.ReadResponse(Encoding.UTF8.GetBytes(text))));
        string forged = text.Replace("\"prevented\":true", "\"prevented\":true,\"sentence\":\"ATK 2\"",
            StringComparison.Ordinal);
        Assert.NotEqual(text, forged);
        Assert.Throws<System.Text.Json.JsonException>(() => EngineJson.ReadResponse(Encoding.UTF8.GetBytes(forged)));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task EffectFactsSurviveEitherTransport(bool socket)
    {
        var request = EngineRequest.SyncGame("effect", "game", "capability");
        var expected = new EngineResponse(EngineProtocol.Version, request.RequestId, request.GameId, null, Choice(), []);
        var endpoint = new EchoEndpoint(request, expected);
        var actual = socket
            ? await ExchangeOverSocket(new SocketEngineServer(endpoint, IPAddress.Loopback, 0), request)
            : await new InProcessTransport(endpoint).ExchangeAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(new PromptEffect(PromptEffectKind.Damage, 8), actual.Prompt!.Effect);
        Assert.Equal(new PromptEffect(PromptEffectKind.Stun, 0), actual.Prompt.Affordances[0].WindowChoices[0].Effect);
    }

    [Fact]
    public void EffectFactsAreWrittenInSnakeCaseAndAnUnknownMemberIsRejected()
    {
        var response = new EngineResponse(EngineProtocol.Version, "effect", "game", null, Choice(), []);
        string text = Encoding.UTF8.GetString(EngineJson.Write(response));
        Assert.Contains("\"effect\":{\"kind\":0,\"amount\":8}", text, StringComparison.Ordinal);
        Assert.Contains("\"effect\":{\"kind\":3,\"amount\":0}", text, StringComparison.Ordinal);
        Assert.Equal(EngineJson.Write(response), EngineJson.Write(EngineJson.ReadResponse(Encoding.UTF8.GetBytes(text))));
        string forged = text.Replace("\"amount\":8}", "\"amount\":8,\"sentence\":\"Deal 8\"}", StringComparison.Ordinal);
        Assert.NotEqual(text, forged);
        Assert.Throws<System.Text.Json.JsonException>(() => EngineJson.ReadResponse(Encoding.UTF8.GetBytes(forged)));
    }

    private static Prompt Choice() =>
        new(0, Question.Element, TimingPriority.Untimed, "card_revealed", "Choose", false,
            [new Affordance(4, "Response", 3, 0, "Mockingbird")
            {
                WindowChoices = [new WindowChoice(7, "Stun Rhino", null) { RecipientIds = [7], Effect = new PromptEffect(PromptEffectKind.Stun, 0) }],
            }])
        {
            Effect = new PromptEffect(PromptEffectKind.Damage, 8),
        };

    private static Prompt Defense() =>
        new(0, Question.Defender, TimingPriority.Untimed, "attack_initiated", "Defend", true,
            [new Affordance(4, "Defend", 3, 0, "Defend")])
        {
            Attack = new PromptAttack(12, 3, 2, Calculated: false, BoostUnresolved: true, Prevented: true) { BoostIcons = 2, BoostFacedown = 1 },
        };
}
