using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Server;

/// <summary>Hashes identifying one save generation without exporting its contents.</summary>
public sealed record IncidentSaveGeneration(
    string StorageId,
    string Generation,
    bool Selected,
    string? SessionSha256,
    string? AuthoritySha256,
    string? ErrorCode);
