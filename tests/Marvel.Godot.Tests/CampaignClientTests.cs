using Marvel.Client;
using Marvel.Protocol;
using Xunit;
namespace Marvel.Godot.Tests;

public sealed class CampaignClientTests
{
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(false, true, false)]
    [InlineData(true, false, false)]
    public async Task DeleteRequiresAnExplicitAcknowledgementForTheRequestedCampaign(
        bool acknowledged, bool matching, bool accepted)
    {
        using var client = new LocalGameClient(new CampaignReplyTransport(request =>
            new(EngineProtocol.Version, request.RequestId, matching ? request.GameId : "other", null, null, [])
            { CampaignDeleted = acknowledged }));
        var request = new EngineRequest(EngineProtocol.Version, "delete", EngineProtocol.Campaign, "campaign")
            { Campaign = new("delete") };
        var result = await client.ExchangeCampaignAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(accepted, result.Succeeded);
        Assert.Equal(!accepted, result.Pending == request);
    }

    [Fact]
    public async Task LostCampaignResponseRetainsTheExactCommandAndEntrySecret()
    {
        using var client = new LocalGameClient(new CampaignReplyTransport(_ => throw new IOException("lost response")));
        var request = new EngineRequest(EngineProtocol.Version, "retained", EngineProtocol.Campaign, "campaign",
            Entry: EntryClaim.Create()) { Campaign = new("create") };
        var result = await client.ExchangeCampaignAsync(request, TestContext.Current.CancellationToken);
        Assert.False(result.Succeeded);
        Assert.Same(request, result.Pending);
        Assert.Equal(request.Entry!.Secret, result.Pending!.Entry!.Secret);
    }

    [Theory]
    [InlineData(EngineFailureDisposition.Rejected, false)]
    [InlineData(EngineFailureDisposition.CommitUncertain, true)]
    public async Task ServerDispositionDeterminesWhetherExactChoiceMustBeRecovered(EngineFailureDisposition disposition, bool retained)
    {
        using var client = new LocalGameClient(new CampaignReplyTransport(request =>
            new(EngineProtocol.Version, request.RequestId, request.GameId, null, null, [],
                Error: new("save", "Save requires attention", disposition))));
        var request = new EngineRequest(EngineProtocol.Version, "choice", EngineProtocol.Campaign, "campaign")
            { Campaign = new("mode") };
        var result = await client.ExchangeCampaignAsync(request, TestContext.Current.CancellationToken);
        Assert.Equal(retained, result.Pending is not null);
        Assert.False(result.Succeeded);
        Assert.NotNull(result.Error);
    }
}
