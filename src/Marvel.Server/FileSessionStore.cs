using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>Generation-based filesystem persistence with one atomic manifest switch.</summary>
public sealed class FileSessionStore : IContentUpgradeStore
{
    private const string Current = "current";
    private static readonly Encoding StrictUtf8 = new UTF8Encoding(
        encoderShouldEmitUTF8Identifier: false,
        throwOnInvalidBytes: true);
    private readonly string root;

    /// <summary>Uses one private directory beneath the supplied server storage root.</summary>
    public FileSessionStore(string root)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        this.root = Path.GetFullPath(root);
    }

    /// <inheritdoc />
    public IReadOnlyList<StoredSession> Load()
    {
        if (!Directory.Exists(root))
        {
            return [];
        }

        var loaded = new List<StoredSession>();
        foreach (string directory in Directory.EnumerateDirectories(root)
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            string storageId = Path.GetFileName(directory);
            if (ReservedDirectory(storageId))
            {
                continue;
            }

            RequireStorageId(storageId);
            string manifest = Path.Combine(directory, Current);
            if (!File.Exists(manifest))
            {
                continue;
            }

            string generation = ReadStrict(manifest).Trim();
            if (!ValidGeneration(generation))
            {
                throw new SessionSaveException(
                    $"session {storageId} has an invalid generation manifest");
            }

            SessionSave save = SessionSaveJson.Read(ReadStrict(
                Path.Combine(directory, generation + ".session.json")));
            IReadOnlyList<StoredAuthority> authorities = StoredAuthorityJson.Read(
                ReadStrict(Path.Combine(
                    directory, generation + ".authority.json")));
            var session = new StoredSession(save, authorities);
            StoredSessionJson.ValidateLoaded(session);
            if (!string.Equals(
                    storageId, session.Save.Session.StorageId, StringComparison.Ordinal))
            {
                throw new SessionSaveException(
                    $"session {storageId} generation names another storage id");
            }

            loaded.Add(session);
        }

        return loaded;
    }

    /// <inheritdoc />
    public IReadOnlyList<SessionLoadResult> LoadForRestore()
    {
        if (!Directory.Exists(root))
        {
            return [];
        }

        var results = new List<SessionLoadResult>();
        foreach (string directory in Directory.EnumerateDirectories(root)
                     .OrderBy(path => path, StringComparer.Ordinal))
        {
            string storageId = Path.GetFileName(directory);
            string? generation = null;
            try
            {
                if (ReservedDirectory(storageId))
                {
                    continue;
                }

                RequireStorageId(storageId);
                string manifest = Path.Combine(directory, Current);
                if (!File.Exists(manifest))
                {
                    continue;
                }

                generation = ReadStrict(manifest).Trim();
                if (!ValidGeneration(generation))
                {
                    throw new SessionSaveException(
                        $"session {storageId} has an invalid generation manifest");
                }

                SessionSave save = SessionSaveJson.Read(ReadStrict(
                    Path.Combine(directory, generation + ".session.json")));
                IReadOnlyList<StoredAuthority> authorities = StoredAuthorityJson.Read(
                    ReadStrict(Path.Combine(directory, generation + ".authority.json")));
                var session = new StoredSession(save, authorities);
                StoredSessionJson.ValidateLoaded(session);
                if (!string.Equals(
                        storageId, session.Save.Session.StorageId, StringComparison.Ordinal))
                {
                    throw new SessionSaveException(
                        $"session {storageId} generation names another storage id");
                }

                results.Add(new SessionLoadResult(session, storageId, null, generation));
            }
            catch (Exception failure) when (failure is IOException
                or UnauthorizedAccessException
                or JsonException
                or SessionSaveException)
            {
                results.Add(new SessionLoadResult(
                    null,
                    storageId,
                    "restore_failed",
                    ValidGeneration(generation ?? string.Empty) ? generation : null));
            }
        }

        return results;
    }

    /// <inheritdoc />
    public string? Commit(StoredSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        string directory = PrepareDirectory(session.Save.Session.StorageId);
        using var lease = WriterLease(directory);
        return CommitGeneration(session);
    }

    /// <inheritdoc />
    public string? CommitContentUpgrade(StoredSession previous, StoredSession candidate, string? generation)
    {
        string directory = PrepareDirectory(previous.Save.Session.StorageId);
        using var lease = WriterLease(directory);
        string actualGeneration = ReadStrict(Path.Combine(directory, Current)).Trim();
        if (!ValidGeneration(actualGeneration) || actualGeneration != generation
            || candidate.Save.Session != previous.Save.Session)
            throw new SessionSaveException("content upgrade predecessor changed");
        var current = new StoredSession(
            SessionSaveJson.Read(ReadStrict(Path.Combine(directory, actualGeneration + ".session.json"))),
            StoredAuthorityJson.Read(ReadStrict(Path.Combine(directory, actualGeneration + ".authority.json"))));
        if (StoredSessionJson.Write(current) != StoredSessionJson.Write(previous))
            throw new SessionSaveException("content upgrade predecessor changed");
        ContentUpgradeBackup.Preserve(directory, actualGeneration + ".session.json");
        ContentUpgradeBackup.Preserve(directory, actualGeneration + ".authority.json");
        return CommitGeneration(candidate);
    }

    private string PrepareDirectory(string storageId)
    {
        RequireStorageId(storageId);
        DurableFiles.EnsurePrivateDirectory(root);
        string directory = Path.Combine(root, storageId);
        DurableFiles.EnsurePrivateDirectory(directory);
        return directory;
    }

    private static FileStream WriterLease(string directory) =>
        new(Path.Combine(directory, ".writer"), FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);

    private string? CommitGeneration(StoredSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        string storageId = session.Save.Session.StorageId;
        RequireStorageId(storageId);
        string directory = Path.Combine(root, storageId);
        DurableFiles.EnsurePrivateDirectory(root);
        DurableFiles.EnsurePrivateDirectory(directory);
        DurableFiles.FlushDirectory(root);

        string manifest = Path.Combine(directory, Current);
        string? previous = File.Exists(manifest)
            ? ReadStrict(manifest).Trim()
            : null;
        if (previous is not null && !ValidGeneration(previous))
        {
            throw new SessionSaveException(
                $"session {storageId} has an invalid generation manifest");
        }

        string generation = Convert.ToHexString(RandomNumberGenerator.GetBytes(16))
            .ToLowerInvariant();
        _ = StoredSessionJson.Write(session);
        string finalSave = Path.Combine(directory, generation + ".session.json");
        string temporarySave = finalSave + ".tmp";
        WriteTextFlushed(temporarySave, SessionSaveJson.Write(session.Save));
        DurableFiles.MoveDurably(temporarySave, finalSave, overwrite: false);

        string finalAuthority = Path.Combine(directory, generation + ".authority.json");
        string temporaryAuthority = finalAuthority + ".tmp";
        WriteTextFlushed(
            temporaryAuthority,
            StoredAuthorityJson.Write(session.Authorities));
        DurableFiles.MoveDurably(temporaryAuthority, finalAuthority, overwrite: false);

        string temporaryManifest = Path.Combine(directory, ".current-" + generation + ".tmp");
        WriteTextFlushed(temporaryManifest, generation + "\n");
        DurableFiles.MoveDurably(temporaryManifest, manifest, overwrite: true);
        RemoveObsoleteGenerationsBestEffort(directory, generation, previous);
        return generation;
    }

    private static void WriteTextFlushed(string path, string value) =>
        DurableFiles.WriteFlushed(path, StrictUtf8.GetBytes(value));

    private static string ReadStrict(string path)
    {
        try
        {
            return File.ReadAllText(path, StrictUtf8);
        }
        catch (DecoderFallbackException failure)
        {
            throw new SessionSaveException(
                $"stored session file {Path.GetFileName(path)} is not valid UTF-8",
                failure);
        }
    }

    private static void RemoveObsoleteGenerationsBestEffort(
        string directory,
        string current,
        string? previous)
    {
        try
        {
            foreach (string file in Directory.EnumerateFiles(directory))
            {
                string name = Path.GetFileName(file);
                int separator = name.IndexOf('.', StringComparison.Ordinal);
                if (separator != 32)
                {
                    continue;
                }

                string generation = name[..separator];
                if (ValidGeneration(generation)
                    && generation != current
                    && generation != previous)
                {
                    File.Delete(file);
                }
            }
        }
        catch (Exception failure) when (failure is IOException or UnauthorizedAccessException)
        {
            // The manifest is already durable. Obsolete generations are not
            // authority, so cleanup failure cannot turn a committed gameplay
            // command into a reported failure. A later commit tries again.
        }
    }

    private static bool ReservedDirectory(string storageId) => storageId == ".campaigns"
        || storageId.StartsWith(".creating-", StringComparison.Ordinal);

    private static void RequireStorageId(string value)
    {
        if (value.Length != 32 || value.Any(character =>
                character is not (>= '0' and <= '9') and not (>= 'a' and <= 'f')))
        {
            throw new SessionSaveException("session storage id is invalid");
        }
    }

    private static bool ValidGeneration(string value) =>
        value.Length == 32 && value.All(character =>
            character is (>= '0' and <= '9') or (>= 'a' and <= 'f'));
}
