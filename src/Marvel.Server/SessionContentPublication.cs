using Marvel.Session;

namespace Marvel.Server;

/// <summary>Reserves changes of persisted content provenance for verified upgrade publication.</summary>
internal static class SessionContentPublication
{
    internal static void RequireUnchanged(string directory, SessionCompatibility candidate)
    {
        string manifest = Path.Combine(directory, "current");
        if (!File.Exists(manifest))
            return;
        string generation = FileSessionStore.ReadStrict(manifest).Trim();
        if (!FileSessionStore.ValidGeneration(generation))
            throw new SessionSaveException("session has an invalid generation manifest");
        var previous = SessionSaveJson.Read(FileSessionStore.ReadStrict(
            Path.Combine(directory, generation + ".session.json"))).Compatibility;
        SessionPublication.RequireCompatible(previous, candidate);
    }
}
