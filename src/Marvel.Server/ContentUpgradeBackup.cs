using Marvel.Session;

namespace Marvel.Server;

/// <summary>Retains original archive bytes outside ordinary generation cleanup.</summary>
internal static class ContentUpgradeBackup
{
    internal static void Preserve(string directory, string name, string? backupName = null)
    {
        byte[] original = File.ReadAllBytes(Path.Combine(directory, name));
        string backup = Path.Combine(directory, ".upgrade-" + (backupName ?? name));
        if (File.Exists(backup))
        {
            if (!File.ReadAllBytes(backup).AsSpan().SequenceEqual(original))
                throw new SessionSaveException("content upgrade backup differs from its predecessor");
            return;
        }
        string temporary = backup + ".tmp-" + Guid.NewGuid().ToString("N");
        DurableFiles.WriteFlushed(temporary, original);
        DurableFiles.MoveDurably(temporary, backup, overwrite: false);
    }
}
