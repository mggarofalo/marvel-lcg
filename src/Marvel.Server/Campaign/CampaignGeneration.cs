using System.Text.Json.Serialization;

namespace Marvel.Server;

/// <summary>Immutable payload identity selected by the campaign's single atomic manifest.</summary>
internal sealed record CampaignGeneration(
    [property: JsonRequired] string Generation,
    [property: JsonRequired] long Length,
    [property: JsonRequired] string Sha256,
    [property: JsonRequired] string? Previous);
