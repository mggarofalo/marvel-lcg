using System.ComponentModel;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.InteropServices;
using Marvel.Session;

namespace Marvel.Server;

/// <summary>A non-secret verifier and its server-authorized visibility scope.</summary>

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
    public string? Commit(StoredSession session)
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
        return generation;
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
