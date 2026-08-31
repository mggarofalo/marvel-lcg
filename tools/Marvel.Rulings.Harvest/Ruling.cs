using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Serialization;

namespace Marvel.Rulings.Harvest;

public sealed record Ruling(
    [property: JsonPropertyName("id")] string Id,
    [property: JsonPropertyName("question")] string Question,
    [property: JsonPropertyName("answer")] string Answer,
    [property: JsonPropertyName("source")] string Source,
    [property: JsonPropertyName("via")] string Via,
    [property: JsonPropertyName("rrg_scope")] string RulesReferenceScope,
    [property: JsonPropertyName("observed")] string? Observed,
    [property: JsonPropertyName("section")] string Section,
    [property: JsonPropertyName("cards")] IReadOnlyList<string> Cards,
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("hash")] string Hash)
{
    public static Ruling Create(
        string question,
        string answer,
        string source,
        Page page,
        string section,
        string? observed,
        IReadOnlyList<string> cards)
    {
        string identity = string.Join('\n', page.Name, Normalize(section), Normalize(question));
        string id = "ruling:" + Digest(identity)[..16];
        string content = string.Join('\n', question, answer, source, page.Via,
            page.RulesReferenceScope, observed ?? "", string.Join(',', cards));

        return new Ruling(
            id,
            question,
            answer,
            source,
            page.Via,
            page.RulesReferenceScope,
            observed,
            section,
            cards,
            cards.Count == 0 ? "rules" : "card",
            "sha256:" + Digest(content));
    }

    internal static string Normalize(string value) =>
        string.Join(' ', value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            .Normalize(NormalizationForm.FormKC)
            .ToLowerInvariant();

    private static string Digest(string value) =>
        Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value))).ToLowerInvariant();
}

public sealed record Page(
    string Name,
    string FileName,
    string Via,
    string RulesReferenceScope,
    PageShape Shape);

public enum PageShape
{
    Compendium,
    Chronological,
}
