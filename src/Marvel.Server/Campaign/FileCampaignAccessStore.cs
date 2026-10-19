namespace Marvel.Server;

/// <summary>Durable private local access documents, kept separately from deterministic campaign archives.</summary>
public sealed class FileCampaignAccessStore : IDisposable
{
    private readonly CampaignArchiveFiles files;
    /// <summary>Owns the supplied per-user access directory for one process lifetime.</summary>
    public FileCampaignAccessStore(string root) => files = new(root);
    /// <summary>Lists opaque access-document ids in stable order.</summary>
    public IReadOnlyList<string> Ids() => files.Ids().ToArray();
    /// <summary>Reads one checksum-verified private document.</summary>
    public byte[] Read(string id)
    {
        try { return files.Read(id); }
        catch (Marvel.Session.SessionSaveException failure) { throw new IOException("campaign access document failed verification", failure); }
    }
    /// <summary>Durably publishes one bounded private document.</summary>
    public void Write(string id, byte[] value)
    {
        if (value.Length > 1024 * 1024) throw new ArgumentException("access document exceeds size limit", nameof(value));
        _ = files.Commit(id, value);
    }
    /// <summary>Releases the directory's process lease.</summary>
    public void Dispose() => files.Dispose();
}
