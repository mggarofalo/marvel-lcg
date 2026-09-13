using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Release;

internal sealed record ReleaseAcceptedServer(
    [property: JsonPropertyName("image")] string Image,
    [property: JsonPropertyName("digest")] string Digest,
    [property: JsonPropertyName("sigstore_bundle")] string SigstoreBundle,
    [property: JsonPropertyName("sigstore_bundle_sha256")] string SigstoreBundleSha256,
    [property: JsonPropertyName("provenance")] string Provenance,
    [property: JsonPropertyName("provenance_sha256")] string ProvenanceSha256);
