using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Server;

/// <summary>One read-only operator evidence manifest.</summary>
public sealed record IncidentManifest(
    string Format,
    int Schema,
    DateTimeOffset CreatedUtc,
    RuntimeIdentity Runtime,
    string Health,
    IReadOnlyList<IncidentDiagnosticFile> Diagnostics,
    IReadOnlyList<IncidentSaveGeneration> SaveGenerations);
