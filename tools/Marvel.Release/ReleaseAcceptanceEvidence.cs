using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Release;

internal sealed record ReleaseAcceptanceEvidence(
    [property: JsonPropertyName("server_journey")] string ServerJourney,
    [property: JsonPropertyName("server_journey_sha256")] string ServerJourneySha256,
    [property: JsonPropertyName("incident_manifest")] string IncidentManifest,
    [property: JsonPropertyName("incident_manifest_sha256")] string IncidentManifestSha256);
