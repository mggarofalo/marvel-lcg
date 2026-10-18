using System.Text.RegularExpressions;

namespace Marvel.Client;

/// <summary>Acquires a published MarvelCDB deck before gameplay; never used during replay.</summary>
public sealed class MarvelCdbDeckImporter : IDisposable
{
    private readonly HttpClient client;

    /// <summary>Creates a bounded, non-redirecting HTTP client. A supplied handler is owned by this importer.</summary>
    public MarvelCdbDeckImporter(HttpMessageHandler? handler = null) =>
        client = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false })
        { Timeout = TimeSpan.FromSeconds(20) };

    /// <summary>Converts a published deck URL into the single public JSON endpoint.</summary>
    public static Uri Endpoint(string url)
    {
        if (url.Length > 2048 || !Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || uri.Scheme != "https" || !uri.IsDefaultPort || uri.UserInfo.Length != 0
            || uri.Host is not ("marvelcdb.com" or "www.marvelcdb.com"))
            throw new InvalidDataException("Use an HTTPS published-deck URL from marvelcdb.com.");
        var match = Regex.Match(uri.AbsolutePath,
            @"^/(?:decklist/view/([0-9]{1,18})(?:/[^/]*)?/?|api/public/decklist/([0-9]{1,18})(?:\.json)?/?)$",
            RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        if (!match.Success) throw new InvalidDataException("Use a published decklist URL. For a private deck, import its JSON export.");
        string id = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
        return new Uri($"https://marvelcdb.com/api/public/decklist/{id}.json");
    }

    /// <summary>Downloads one bounded JSON snapshot; cancellation leaves the caller's current draft untouched.</summary>
    public async Task<DeckDraft> ImportAsync(string url, CancellationToken cancellationToken = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(20));
        CancellationToken bounded = deadline.Token;
        using var response = await client.GetAsync(Endpoint(url), HttpCompletionOption.ResponseHeadersRead, bounded)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.Content.Headers.ContentLength > MarvelCdbDeckJson.MaximumBytes)
            throw new InvalidDataException("The deck response exceeds 1 MiB.");
        await using var stream = await response.Content.ReadAsStreamAsync(bounded).ConfigureAwait(false);
        using var output = new MemoryStream();
        byte[] buffer = new byte[8192];
        while (true)
        {
            int count = await stream.ReadAsync(buffer, bounded).ConfigureAwait(false);
            if (count == 0) break;
            if (output.Length + count > MarvelCdbDeckJson.MaximumBytes)
                throw new InvalidDataException("The deck response exceeds 1 MiB.");
            output.Write(buffer, 0, count);
        }
        return MarvelCdbDeckJson.Read(output.ToArray());
    }

    /// <inheritdoc />
    public void Dispose() => client.Dispose();
}
