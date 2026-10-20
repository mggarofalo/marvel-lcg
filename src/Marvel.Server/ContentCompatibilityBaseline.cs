using Marvel.Session;

namespace Marvel.Server;

/// <summary>Pinned release input identities and the admitted entries they require unchanged.</summary>
internal sealed record ContentCompatibilityBaseline(
    string SourceRevision,
    SessionCompatibility Source,
    Dictionary<string, string> Cards,
    Dictionary<string, string> Setup,
    Dictionary<string, string> Abilities);
