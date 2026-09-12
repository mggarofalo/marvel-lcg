using System.Collections.Concurrent;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Marvel.Server;

/// <summary>Stable identifiers for machine-readable operational events.</summary>

/// <summary>Retains bounded private JSON-lines diagnostics beside stderr.</summary>
public sealed class RotatingJsonFileOperationalSink : IOperationalSink, IDisposable
{
    private const string ActiveName = "operational.jsonl";
    private readonly Func<DateTimeOffset> clock;
    private readonly long maximumBytes;
    private readonly int maximumArchives;
    private readonly TimeSpan retention;
    private readonly string root;
    private readonly object gate = new();
    private StreamWriter? writer;

    /// <summary>Creates a size- and age-bounded diagnostic destination.</summary>
    public RotatingJsonFileOperationalSink(
        string root,
        long maximumBytes = 10 * 1024 * 1024,
        int maximumArchives = 9,
        TimeSpan? retention = null,
        Func<DateTimeOffset>? clock = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(root);
        if (maximumBytes <= 0 || maximumArchives < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumBytes));
        }

        this.root = Path.GetFullPath(root);
        this.maximumBytes = maximumBytes;
        this.maximumArchives = maximumArchives;
        this.retention = retention ?? TimeSpan.FromDays(30);
        this.clock = clock ?? (() => DateTimeOffset.UtcNow);
        Directory.CreateDirectory(this.root);
        MakePrivate(this.root, directory: true);
        Prune();
        EnsureWriter();
    }

    /// <inheritdoc />
    public void Write(OperationalRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        string line = OperationalJson.Serialize(record);
        lock (gate)
        {
            EnsureWriter();
            if (writer!.BaseStream.Length > 0
                && writer.BaseStream.Length + System.Text.Encoding.UTF8.GetByteCount(line) + 1
                    > maximumBytes)
            {
                Rotate();
                EnsureWriter();
            }

            writer!.WriteLine(line);
            writer.Flush();
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        lock (gate)
        {
            writer?.Dispose();
            writer = null;
        }
    }

    private void EnsureWriter()
    {
        if (writer is not null)
        {
            return;
        }

        string path = Path.Combine(root, ActiveName);
        if (File.Exists(path))
        {
            MakePrivate(path, directory: false);
        }
        var stream = new FileStream(
            path, FileMode.Append, FileAccess.Write, FileShare.Read,
            bufferSize: 4096, FileOptions.WriteThrough);
        MakePrivate(path, directory: false);
        writer = new StreamWriter(stream, new System.Text.UTF8Encoding(false));
    }

    private void Rotate()
    {
        writer?.Dispose();
        writer = null;
        string active = Path.Combine(root, ActiveName);
        for (int suffix = 0; ; suffix++)
        {
            string archive = Path.Combine(
                root,
                $"operational-{clock().UtcDateTime:yyyyMMddHHmmssfff}-{suffix:D3}.jsonl");
            if (!File.Exists(archive))
            {
                File.Move(active, archive);
                break;
            }
        }

        Prune();
    }

    private void Prune()
    {
        DateTimeOffset cutoff = clock() - retention;
        string[] archives = Directory.GetFiles(root, "operational-*.jsonl")
            .OrderByDescending(File.GetLastWriteTimeUtc)
            .ToArray();
        foreach (string archive in archives
                     .Where((path, index) => index >= maximumArchives
                         || File.GetLastWriteTimeUtc(path) < cutoff.UtcDateTime))
        {
            File.Delete(archive);
        }
    }

    private static void MakePrivate(string path, bool directory)
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        File.SetUnixFileMode(
            path,
            directory
                ? UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute
                : UnixFileMode.UserRead | UnixFileMode.UserWrite);
    }
}
