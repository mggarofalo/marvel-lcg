using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>A non-secret verifier and its server-authorized visibility scope.</summary>
public sealed record StoredAuthority(
    [property: JsonRequired] string Verifier,
    [property: JsonRequired] IReadOnlyList<int> Seats,
    [property: JsonRequired] bool Owner,
    [property: JsonRequired] bool Invitation);

/// <summary>The deterministic save plus separate protected operational authority.</summary>
public sealed record StoredSession(
    [property: JsonRequired] SessionSave Save,
    [property: JsonRequired] IReadOnlyList<StoredAuthority> Authorities);

/// <summary>Atomically persists complete hosted-session generations.</summary>
public interface ISessionStore
{
    /// <summary>Loads only complete generations selected by committed manifests.</summary>
    IReadOnlyList<StoredSession> Load();

    /// <summary>Loads independent candidates so one corrupt session can be quarantined.</summary>
    IReadOnlyList<SessionLoadResult> LoadForRestore() =>
        [.. Load().Select(session => new SessionLoadResult(session, null, null))];

    /// <summary>Commits a complete generation before returning.</summary>
    void Commit(StoredSession session);

    /// <summary>Returns the selected opaque generation, when the store exposes one.</summary>
    string? CurrentGeneration(string storageId) => null;
}

/// <summary>One independently loadable session or its bounded quarantine result.</summary>
public sealed record SessionLoadResult(
    StoredSession? Session,
    string? StorageId,
    string? ErrorCode,
    string? Generation = null);

/// <summary>An isolated store used when a host has no filesystem authority.</summary>
public sealed class MemorySessionStore : ISessionStore
{
    private readonly Dictionary<string, string> generations = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public IReadOnlyList<StoredSession> Load() =>
        [.. generations.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .Select(pair => StoredSessionJson.Read(pair.Value))];

    /// <inheritdoc />
    public void Commit(StoredSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.Save is null)
        {
            throw new SessionSaveException("stored generation has no save");
        }

        generations[session.Save.Session.StorageId] = StoredSessionJson.Write(session);
    }
}

/// <summary>Generation-based filesystem persistence with one atomic manifest switch.</summary>
public sealed class FileSessionStore : ISessionStore
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
            if (storageId.StartsWith(".creating-", StringComparison.Ordinal))
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
                if (storageId.StartsWith(".creating-", StringComparison.Ordinal))
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
    public void Commit(StoredSession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        string storageId = session.Save.Session.StorageId;
        RequireStorageId(storageId);
        string directory = Path.Combine(root, storageId);
        EnsurePrivateDirectory(root);
        EnsurePrivateDirectory(directory);
        FlushDirectory(root);

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
        WriteFlushed(temporarySave, SessionSaveJson.Write(session.Save));
        MoveDurably(temporarySave, finalSave, overwrite: false);

        string finalAuthority = Path.Combine(directory, generation + ".authority.json");
        string temporaryAuthority = finalAuthority + ".tmp";
        WriteFlushed(
            temporaryAuthority,
            StoredAuthorityJson.Write(session.Authorities));
        MoveDurably(temporaryAuthority, finalAuthority, overwrite: false);

        string temporaryManifest = Path.Combine(directory, ".current-" + generation + ".tmp");
        WriteFlushed(temporaryManifest, generation + "\n");
        MoveDurably(temporaryManifest, manifest, overwrite: true);
        RemoveObsoleteGenerationsBestEffort(directory, generation, previous);
    }

    /// <inheritdoc />
    public string? CurrentGeneration(string storageId)
    {
        RequireStorageId(storageId);
        string manifest = Path.Combine(root, storageId, Current);
        if (!File.Exists(manifest))
        {
            return null;
        }

        string generation = ReadStrict(manifest).Trim();
        return ValidGeneration(generation) ? generation : null;
    }

    private static void WriteFlushed(string path, string value)
    {
        byte[] bytes = StrictUtf8.GetBytes(value);
        using var stream = new FileStream(
            path, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            bufferSize: 4096, FileOptions.WriteThrough);
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(path, UnixFileMode.UserRead | UnixFileMode.UserWrite);
        }

        stream.Write(bytes);
        stream.Flush(flushToDisk: true);
    }

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

    private static void EnsurePrivateDirectory(
        string path,
        bool setModeWhenAlreadyPresent = true)
    {
        if (!Directory.Exists(path))
        {
            string? parent = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(parent))
            {
                throw new IOException("session directory has no durable parent");
            }

            // Existing ancestors may be shared system directories. They provide the
            // durable rename boundary, but the server does not own their permissions.
            EnsurePrivateDirectory(parent, setModeWhenAlreadyPresent: false);
            string temporary = Path.Combine(
                parent,
                ".creating-" + Path.GetFileName(path) + "-"
                + Convert.ToHexString(RandomNumberGenerator.GetBytes(8)).ToLowerInvariant());
            Directory.CreateDirectory(temporary);
            SetPrivateDirectoryMode(temporary);
            try
            {
                MoveDurably(temporary, path, overwrite: false);
            }
            finally
            {
                if (Directory.Exists(temporary))
                {
                    Directory.Delete(temporary);
                }
            }
        }

        if (setModeWhenAlreadyPresent)
        {
            SetPrivateDirectoryMode(path);
        }
    }

    private static void SetPrivateDirectoryMode(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(
                path,
                UnixFileMode.UserRead
                | UnixFileMode.UserWrite
                | UnixFileMode.UserExecute);
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

    private static void MoveDurably(string source, string destination, bool overwrite)
    {
        if (OperatingSystem.IsWindows())
        {
            uint flags = 0x00000008;
            if (overwrite)
            {
                flags |= 0x00000001;
            }

            if (!MoveFileEx(source, destination, flags))
            {
                throw new IOException(
                    "could not durably publish a session generation",
                    new Win32Exception(Marshal.GetLastWin32Error()));
            }
            return;
        }

        if (Directory.Exists(source))
        {
            if (overwrite)
            {
                throw new IOException("session directories cannot be replaced");
            }

            Directory.Move(source, destination);
        }
        else
        {
            File.Move(source, destination, overwrite);
        }
        FlushDirectory(Path.GetDirectoryName(destination)!);
    }

    private static void FlushDirectory(string path)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        int descriptor = OpenDirectory(path, 0);
        if (descriptor < 0)
        {
            throw new IOException(
                "could not open session directory for durability",
                new Win32Exception(Marshal.GetLastWin32Error()));
        }

        try
        {
            if (FlushDescriptor(descriptor) != 0)
            {
                throw new IOException(
                    "could not flush session directory metadata",
                    new Win32Exception(Marshal.GetLastWin32Error()));
            }
        }
        finally
        {
            _ = CloseDescriptor(descriptor);
        }
    }

    [DllImport("kernel32.dll", EntryPoint = "MoveFileExW", SetLastError = true,
        CharSet = CharSet.Unicode)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool MoveFileEx(
        string existingFileName,
        string newFileName,
        uint flags);

    [DllImport("libc", EntryPoint = "open", SetLastError = true)]
    private static extern int OpenDirectory(string path, int flags);

    [DllImport("libc", EntryPoint = "fsync", SetLastError = true)]
    private static extern int FlushDescriptor(int descriptor);

    [DllImport("libc", EntryPoint = "close", SetLastError = true)]
    private static extern int CloseDescriptor(int descriptor);

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

internal static class StoredAuthorityJson
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static string Write(IReadOnlyList<StoredAuthority> authorities) =>
        JsonSerializer.Serialize(authorities, Options);

    public static IReadOnlyList<StoredAuthority> Read(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<StoredAuthority>>(json, Options)
                ?? throw new SessionSaveException("stored authority is empty");
        }
        catch (JsonException failure)
        {
            throw new SessionSaveException("stored authority is not valid JSON", failure);
        }
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}

internal static class StoredSessionJson
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    public static string Write(StoredSession session)
    {
        Validate(session);
        return JsonSerializer.Serialize(session, Options);
    }

    public static StoredSession Read(string json)
    {
        try
        {
            var session = JsonSerializer.Deserialize<StoredSession>(json, Options)
                ?? throw new SessionSaveException("stored generation is empty");
            Validate(session, readable: true);
            return session;
        }
        catch (JsonException failure)
        {
            throw new SessionSaveException("stored generation is not valid JSON", failure);
        }
    }

    public static void ValidateLoaded(StoredSession session) =>
        Validate(session, readable: true);

    private static void Validate(StoredSession session, bool readable = false)
    {
        ArgumentNullException.ThrowIfNull(session);
        if (session.Save is null)
        {
            throw new SessionSaveException("stored generation has no save");
        }

        if (readable)
        {
            SessionSaveJson.ValidateReadable(session.Save);
        }
        else
        {
            SessionSaveJson.Validate(session.Save);
        }
        if (session.Authorities is null
            || session.Authorities.Any(authority => authority is null
                || authority.Verifier is not { Length: 64 }
                || authority.Verifier.Any(character =>
                    character is not (>= '0' and <= '9')
                    and not (>= 'a' and <= 'f'))
                || authority.Seats is null
                || authority.Seats.Any(seat => seat < 0)))
        {
            throw new SessionSaveException("stored authority is invalid");
        }

        if (session.Authorities.Select(authority => authority.Verifier)
            .Distinct(StringComparer.Ordinal).Count() != session.Authorities.Count)
        {
            throw new SessionSaveException("stored authority verifier is duplicated");
        }

        int owners = session.Authorities.Count(authority => authority.Owner);
        if (session.Authorities.Any(authority => authority.Owner && authority.Invitation)
            || (session.Save.Session.Lifecycle == "active" && owners != 1)
            || (session.Save.Session.Lifecycle == "retired"
                && session.Authorities.Count != 0))
        {
            throw new SessionSaveException("stored lifecycle authority is invalid");
        }
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
            WriteIndented = false,
            UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        };
        options.Converters.Add(new ResourceAllocationJsonConverter());
        options.MakeReadOnly(populateMissingResolver: true);
        return options;
    }
}
