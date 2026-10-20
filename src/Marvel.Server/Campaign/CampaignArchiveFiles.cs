using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>Checksummed generation publication; a single lease excludes concurrent process writers.</summary>
internal sealed class CampaignArchiveFiles : IDisposable
{
    private readonly string root;
    private readonly FileStream lease;
    private readonly object publication = new();

    internal CampaignArchiveFiles(string root)
    {
        this.root = Path.GetFullPath(root);
        DurableFiles.EnsurePrivateDirectory(this.root);
        lease = new FileStream(Path.Combine(this.root, ".writer"), FileMode.OpenOrCreate,
            FileAccess.ReadWrite, FileShare.None);
    }

    internal IEnumerable<string> Ids() => Directory.EnumerateDirectories(root)
        .Select(Path.GetFileName).OfType<string>().Where(ValidId).Order(StringComparer.Ordinal);

    internal void Purge(string id)
    {
        RequireId(id);
        string directory = Path.Combine(root, id);
        if (!Directory.Exists(directory)) return;
        if ((File.GetAttributes(directory) & FileAttributes.ReparsePoint) != 0)
            throw new IOException("campaign directory must not be a link");
        foreach (string file in Directory.EnumerateFiles(directory)) File.Delete(file);
        Directory.Delete(directory);
        DurableFiles.FlushDirectory(root);
    }

    internal byte[] Read(string id)
    {
        RequireId(id);
        string directory = Path.Combine(root, id);
        CampaignGeneration generation = ReadManifest(directory);
        string path = Path.Combine(directory, generation.Generation + ".archive");
        var info = new FileInfo(path);
        if (info.Length != generation.Length || info.Length > CampaignSaveJson.MaximumBytes)
            throw new SessionSaveException("campaign generation length is invalid");
        byte[] bytes = File.ReadAllBytes(path);
        if (!string.Equals(Convert.ToHexString(SHA256.HashData(bytes)), generation.Sha256, StringComparison.Ordinal))
            throw new SessionSaveException("campaign generation checksum failed");
        return bytes;
    }

    internal string Commit(string id, ReadOnlySpan<byte> bytes)
    {
        lock (publication) return CommitGeneration(id, bytes);
    }

    private string CommitGeneration(string id, ReadOnlySpan<byte> bytes)
    {
        RequireId(id);
        if (bytes.Length > CampaignSaveJson.MaximumBytes) throw new SessionSaveException("campaign archive exceeds size limit");
        string directory = Path.Combine(root, id);
        DurableFiles.EnsurePrivateDirectory(directory);
        string manifestPath = Path.Combine(directory, "current");
        CampaignGeneration? previous = File.Exists(manifestPath) ? ReadManifest(directory) : null;
        string generation = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        string payload = Path.Combine(directory, generation + ".archive");
        DurableFiles.WriteFlushed(payload, bytes);
        DurableFiles.FlushDirectory(directory);
        var manifest = new CampaignGeneration(generation, bytes.Length,
            Convert.ToHexString(SHA256.HashData(bytes)), previous?.Generation);
        string temporary = Path.Combine(directory, ".current-" + generation);
        DurableFiles.WriteFlushed(temporary, System.Text.Encoding.UTF8.GetBytes(CampaignSaveJson.Canonical(manifest)));
        try { DurableFiles.MoveDurably(temporary, manifestPath, overwrite: true); }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        { throw new CampaignCommitUncertainException(failure); }
        Cleanup(directory, manifest);
        return generation;
    }

    internal string CommitContentUpgrade(string id, StoredCampaign previous, StoredCampaign candidate)
    {
        lock (publication)
        {
            byte[] original = Read(id);
            if (CampaignSaveJson.Canonical(CampaignSaveJson.ReadDocument<StoredCampaign>(original))
                != CampaignSaveJson.Canonical(previous) || candidate.Save.Identity != previous.Save.Identity)
                throw new SessionSaveException("campaign content upgrade predecessor changed");
            string directory = Path.Combine(root, id);
            var manifest = ReadManifest(directory);
            ContentUpgradeBackup.Preserve(directory, manifest.Generation + ".archive");
            ContentUpgradeBackup.Preserve(directory, "current", manifest.Generation + ".manifest");
            return CommitGeneration(id, System.Text.Encoding.UTF8.GetBytes(CampaignSaveJson.Canonical(candidate)));
        }
    }

    private static CampaignGeneration ReadManifest(string directory)
    {
        string path = Path.Combine(directory, "current");
        if (new FileInfo(path).Length > 1024) throw new SessionSaveException("campaign manifest is oversized");
        byte[] bytes = File.ReadAllBytes(path);
        StrictJsonMembers.Validate(bytes);
        var generation = CampaignSaveJson.ReadDocument<CampaignGeneration>(bytes);
        if (!ValidId(generation.Generation) || generation.Length < 1 || generation.Sha256 is not { Length: 64 })
            throw new SessionSaveException("campaign manifest is invalid");
        if (generation.Previous is not null && !ValidId(generation.Previous))
            throw new SessionSaveException("campaign previous generation is invalid");
        return generation;
    }

    private static void Cleanup(string directory, CampaignGeneration manifest)
    {
        try
        {
            foreach (string file in Directory.EnumerateFiles(directory, "*.archive"))
            {
                string id = Path.GetFileNameWithoutExtension(file);
                if (ValidId(id) && id != manifest.Generation && id != manifest.Previous) File.Delete(file);
            }
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        { /* Obsolete payloads cannot change the authority selected by the durable manifest. */ }
    }

    private static bool ValidId(string? id) => id is { Length: 32 }
        && id.All(c => c is >= '0' and <= '9' or >= 'a' and <= 'f');
    private static void RequireId(string id)
    {
        if (!ValidId(id)) throw new SessionSaveException("campaign storage id is invalid");
    }

    /// <summary>Releases the process writer lease.</summary>
    public void Dispose() => lease.Dispose();
}
