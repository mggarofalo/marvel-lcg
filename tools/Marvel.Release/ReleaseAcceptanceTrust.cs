using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Release;

internal sealed record ReleaseAcceptanceTrust(
    [property: JsonPropertyName("macos")] string Macos,
    [property: JsonPropertyName("windows_community_msix")] string WindowsCommunityMsix,
    [property: JsonPropertyName("windows_portable")] string WindowsPortable,
    [property: JsonPropertyName("server")] string Server);
