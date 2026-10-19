using AngleSharp.Dom;
using AngleSharp.Html.Parser;

namespace Marvel.Client;

/// <summary>Bounded public deck search; the site supplies filtering, sorting and pagination.</summary>
public sealed class MarvelCdbDeckSearch : IDisposable
{
    private readonly HttpClient client;
    private static readonly HashSet<string> Allowed = ["hero", "aspect", "tag", "sort", "author", "name", "cards[]", "packs[]"];

    /// <summary>A supplied handler is owned by this search service. Redirects are disabled by default.</summary>
    public MarvelCdbDeckSearch(HttpMessageHandler? handler = null) => client = new(handler ?? new HttpClientHandler { AllowAutoRedirect = false })
    { Timeout = TimeSpan.FromSeconds(20) };

    /// <summary>Builds a public search URL from bounded named filters; no account cookies are used.</summary>
    public static Uri Endpoint(IEnumerable<KeyValuePair<string, string>> filters, int page)
    {
        if (page is < 1 or > 100000) throw new ArgumentOutOfRangeException(nameof(page));
        var values = filters.ToArray();
        if (values.Length > 200 || values.Any(pair => !Allowed.Contains(pair.Key) || pair.Value.Length > 256))
            throw new ArgumentException("Invalid deck search filters.", nameof(filters));
        string query = string.Join("&", values.Select(pair => Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(pair.Value)));
        return new Uri($"https://marvelcdb.com/decklists/find/{page}?{query}");
    }

    /// <summary>Fetches a single page without changing any local deck.</summary>
    public async Task<PublishedDeckPage> SearchAsync(IEnumerable<KeyValuePair<string, string>> filters, int page = 1,
        CancellationToken cancellationToken = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(20));
        using var response = await client.GetAsync(Endpoint(filters, page), HttpCompletionOption.ResponseHeadersRead, deadline.Token).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(deadline.Token).ConfigureAwait(false);
        byte[] bytes = new byte[1024 * 1024 + 1];
        int count = await stream.ReadAtLeastAsync(bytes, bytes.Length, throwOnEndOfStream: false, deadline.Token).ConfigureAwait(false);
        if (count == bytes.Length) throw new InvalidDataException("The search response exceeds 1 MiB.");
        return Parse(System.Text.Encoding.UTF8.GetString(bytes, 0, count), page);
    }

    internal static PublishedDeckPage Parse(string html, int page)
    {
        using var document = new HtmlParser().ParseDocument(html);
        if (document.QuerySelector("#decklist-search-form") is null)
            throw new InvalidDataException("MarvelCDB returned an unrecognized search page. Your draft is unchanged.");
        var decks = ReadResults(document);
        var filters = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal);
        foreach (string name in new[] { "hero", "aspect", "tag", "sort" })
            filters[name] = document.QuerySelectorAll($"select[name='{name}'] option").ToDictionary(
                option => option.GetAttribute("value") ?? "", option => option.TextContent.Trim(), StringComparer.Ordinal);
        var packs = document.QuerySelectorAll("input[name='packs[]']").ToDictionary(
            input => input.GetAttribute("value")!, input => input.ParentElement!.TextContent.Trim(), StringComparer.Ordinal);
        if (filters.Values.Any(options => options.Count == 0) || packs.Count == 0)
            throw new InvalidDataException("MarvelCDB returned incomplete search options. Try again later.");
        bool next = document.QuerySelectorAll(".pagination a").Any(link =>
            (link.GetAttribute("href") ?? "").Split('?')[0].EndsWith("/" + (page + 1).ToString(System.Globalization.CultureInfo.InvariantCulture), StringComparison.Ordinal));
        return new(decks, filters, packs, next);
    }

    private static PublishedDeck[] ReadResults(IDocument document)
    {
        return document.QuerySelectorAll(".box").Select(box =>
        {
            var link = box.QuerySelector("h4 a[href^='/decklist/view/']");
            if (link is null) return null;
            string url = new Uri(new Uri("https://marvelcdb.com"), link.GetAttribute("href")!).AbsoluteUri;
            _ = MarvelCdbDeckImporter.Endpoint(url);
            return new PublishedDeck(link.TextContent.Trim(), url, box.QuerySelector(".card-tip")?.TextContent.Trim() ?? "",
                box.QuerySelector("a.username")?.TextContent.Trim() ?? "");
        }).OfType<PublishedDeck>().ToArray();
    }

    /// <inheritdoc />
    public void Dispose() => client.Dispose();
}
