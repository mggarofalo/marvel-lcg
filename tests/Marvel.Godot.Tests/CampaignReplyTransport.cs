using Marvel.Protocol;
namespace Marvel.Godot.Tests;

internal sealed class CampaignReplyTransport(Func<EngineRequest, EngineResponse> reply) : IEngineTransport
{
    public ValueTask<EngineResponse> ExchangeAsync(EngineRequest request, CancellationToken cancellationToken = default) =>
        ValueTask.FromResult(reply(request));
}
