using System.Text.Json.Serialization;

namespace Marvel.Protocol;

/// <summary>A rejected request with an explicit recovery contract independent of diagnostic spelling.</summary>
public sealed record EngineError(string Code, string Message,
    [property: JsonRequired] EngineFailureDisposition Disposition = EngineFailureDisposition.Rejected);
