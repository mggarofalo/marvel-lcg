using System.Net;
using Marvel.Client;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class DeckUrlImportTests
{
    [Fact]
    public async Task UrlAcquisitionUsesCanonicalPublicEndpointAndTheJsonParser()
    {
        var handler = new ResponseHandler(HttpStatusCode.OK,
            """{"name":"Imported","hero_code":"01040a","slots":{"01043d":2,"01043a":1,"unknown":1}}""");
        using var importer = new MarvelCdbDeckImporter(handler);
        var draft = await importer.ImportAsync("https://marvelcdb.com/decklist/view/1/starter?ignored=metadata",
            TestContext.Current.CancellationToken);
        Assert.Equal("https://marvelcdb.com/api/public/decklist/1.json", handler.Requested!.AbsoluteUri);
        Assert.Equal(["01043a", "01043d", "01043d", "unknown"], draft.Cards);
        Assert.Equal("Imported", draft.Name);
    }

    [Fact]
    public async Task HttpFailureAndOversizedBodiesDoNotProduceAReplacementDraft()
    {
        using var redirect = new MarvelCdbDeckImporter(new ResponseHandler(HttpStatusCode.Redirect, ""));
        await Assert.ThrowsAsync<HttpRequestException>(() => redirect.ImportAsync("https://marvelcdb.com/decklist/view/1", TestContext.Current.CancellationToken));
        using var oversized = new MarvelCdbDeckImporter(new ResponseHandler(HttpStatusCode.OK, new string('x', MarvelCdbDeckJson.MaximumBytes + 1)));
        await Assert.ThrowsAsync<InvalidDataException>(() => oversized.ImportAsync("https://marvelcdb.com/decklist/view/1", TestContext.Current.CancellationToken));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        using var cancelled = new MarvelCdbDeckImporter(new ResponseHandler(HttpStatusCode.OK, "{}"));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => cancelled.ImportAsync("https://marvelcdb.com/decklist/view/1", cancellation.Token));
    }

    private sealed class ResponseHandler(HttpStatusCode status, string json) : HttpMessageHandler
    {
        internal Uri? Requested { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Requested = request.RequestUri;
            Assert.Equal(HttpMethod.Get, request.Method);
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(json) });
        }
    }
}
