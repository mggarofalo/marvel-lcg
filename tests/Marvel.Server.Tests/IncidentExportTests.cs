using System.Security.Cryptography;
using System.Text.Json;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class IncidentExportTests
{
    [Fact]
    public void ManifestIncludesRedactedLogsRuntimeAndGenerationHashesWithoutChangingSources()
    {
        string root = Path.Combine(
            Path.GetTempPath(), $"marvel-incident-{Guid.NewGuid():N}");
        string saves = Path.Combine(root, "sessions");
        string diagnostics = Path.Combine(root, "diagnostics");
        Directory.CreateDirectory(root);
        try
        {
            var host = new EngineHost(
                DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root),
                new FixedCapabilities("owner-secret"),
                store: new FileSessionStore(saves));
            EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
                "open-request",
                "private-table-label",
                new GameSpecification("rhino", ["spider_man"], [], Seed: 73)));
            Assert.Null(opened.Error);
            using (var sink = new RotatingJsonFileOperationalSink(diagnostics))
            {
                var emittedLog = new OperationalLog(
                    sink, "Marvel.Server", () => DateTimeOffset.UnixEpoch);
                emittedLog.Write(
                    OperationalEventIds.RequestCompleted,
                    "accepted",
                    durationMilliseconds: 1,
                    requestId: "pseudonymous-request",
                    gameId: "pseudonymous-game",
                    operation: EngineProtocol.Open,
                    revision: 0,
                    saveCommitted: true);
                emittedLog.Flush(TimeSpan.FromSeconds(2));
            }
            File.AppendAllText(
                Path.Combine(diagnostics, "operational.jsonl"),
                OperationalJson.Serialize(new OperationalRecord(
                    OperationalEventIds.TransportCompleted,
                    "Marvel.Server",
                    DateTimeOffset.UnixEpoch,
                    Environment.ProcessId,
                    0,
                    "uncertain",
                    ProductVersion: "0.0.9",
                    Commit: new string('a', 40),
                    Runtime: "v0.0.9 · engine engine-replay-v1 · protocol 1 · save 2"))
                + "\n" + OperationalJson.Serialize(new OperationalRecord(
                    OperationalEventIds.RequestCompleted,
                    "Marvel.Server",
                    DateTimeOffset.UnixEpoch,
                    Environment.ProcessId,
                    0,
                    "stale",
                    ProductVersion: EngineBuildIdentity.ProductVersion,
                    Commit: EngineBuildIdentity.Commit,
                    Runtime: EngineBuildIdentity.Display))
                + "\n" + OperationalJson.Serialize(new OperationalRecord(
                    OperationalEventIds.TransportCompleted,
                    "Marvel.Godot",
                    DateTimeOffset.UnixEpoch,
                    Environment.ProcessId,
                    0,
                    "cancelled",
                    ProductVersion: EngineBuildIdentity.ProductVersion,
                    Commit: EngineBuildIdentity.Commit,
                    Runtime: EngineBuildIdentity.Display))
                + "\n" + OperationalJson.Serialize(new OperationalRecord(
                    OperationalEventIds.RequestCompleted,
                    "owner-secret",
                    DateTimeOffset.UnixEpoch,
                    Environment.ProcessId,
                    0,
                    "accepted",
                    ProductVersion: EngineBuildIdentity.ProductVersion,
                    Commit: EngineBuildIdentity.Commit,
                    Runtime: EngineBuildIdentity.Display))
                + "\n" + OperationalJson.Serialize(new OperationalRecord(
                    OperationalEventIds.RequestCompleted,
                    "Marvel.Server",
                    DateTimeOffset.UnixEpoch,
                    Environment.ProcessId,
                    0,
                    "accepted",
                    ProductVersion: EngineBuildIdentity.ProductVersion,
                    Commit: new string('b', 64),
                    Runtime: EngineBuildIdentity.Display))
                + "\n{}\nnot-json owner-secret private-table-label\n");
            Dictionary<string, string> before = Snapshot(saves, diagnostics);

            string exported = IncidentExporter.Serialize(IncidentExporter.Build(
                Marvel.Tests.RepositoryPaths.Root,
                saves,
                diagnostics,
                () => DateTimeOffset.UnixEpoch));

            Assert.Equal(before, Snapshot(saves, diagnostics));
            using JsonDocument document = JsonDocument.Parse(exported);
            JsonElement manifest = document.RootElement;
            Assert.Equal("marvel-incident", manifest.GetProperty("format").GetString());
            Assert.Equal(EngineBuildIdentity.ProductVersion,
                manifest.GetProperty("runtime").GetProperty("product_version").GetString());
            JsonElement log = Assert.Single(
                manifest.GetProperty("diagnostics").EnumerateArray());
            Assert.Equal(4, log.GetProperty("invalid_records").GetInt32());
            Assert.Equal(4, log.GetProperty("records").GetArrayLength());
            JsonElement generation = Assert.Single(
                manifest.GetProperty("save_generations").EnumerateArray(),
                item => item.GetProperty("selected").GetBoolean());
            Assert.Equal(64, generation.GetProperty("session_sha256").GetString()!.Length);
            Assert.Equal(64, generation.GetProperty("authority_sha256").GetString()!.Length);
            Assert.DoesNotContain("owner-secret", exported, StringComparison.Ordinal);
            Assert.DoesNotContain("private-table-label", exported, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void CommandRefusesAFileDestinationAndDoesNotStartAServer()
    {
        string root = Path.Combine(
            Path.GetTempPath(), $"marvel-incident-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        string output = Path.Combine(root, "existing.json");
        File.WriteAllText(output, "keep");
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        try
        {
            int result = Program.ExportIncident(
                [
                    "--export-incident", output,
                    "--data-root", Marvel.Tests.RepositoryPaths.Root,
                    "--save-root", Path.Combine(root, "sessions"),
                    "--diagnostics-root", Path.Combine(root, "diagnostics"),
                ],
                stdout,
                stderr);

            Assert.Equal(2, result);
            Assert.Equal("keep", File.ReadAllText(output));
            Assert.Empty(stdout.ToString());
            Assert.Equal(
                "incident export failed without changing server state" + Environment.NewLine,
                stderr.ToString());
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void CommandCanStreamTheManifestWithoutAWritableContainerMount()
    {
        string root = Path.Combine(
            Path.GetTempPath(), $"marvel-incident-stdout-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        using var stdout = new StringWriter();
        using var stderr = new StringWriter();
        try
        {
            int result = Program.ExportIncident(
                [
                    "--export-incident", "-",
                    "--data-root", Marvel.Tests.RepositoryPaths.Root,
                    "--save-root", Path.Combine(root, "sessions"),
                    "--diagnostics-root", Path.Combine(root, "diagnostics"),
                ],
                stdout,
                stderr);

            Assert.Equal(0, result);
            Assert.Equal("marvel-incident",
                JsonDocument.Parse(stdout.ToString()).RootElement
                    .GetProperty("format").GetString());
            Assert.Empty(stderr.ToString());
            Assert.Empty(Directory.GetFiles(root));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void OversizedCorruptEvidenceIsBoundedWhileGenerationHashesRemainAvailable()
    {
        string root = Path.Combine(
            Path.GetTempPath(), $"marvel-incident-bounds-{Guid.NewGuid():N}");
        string saves = Path.Combine(root, "sessions");
        string diagnostics = Path.Combine(root, "diagnostics");
        string storage = Path.Combine(saves, new string('a', 32));
        string generation = new('b', 32);
        Directory.CreateDirectory(storage);
        Directory.CreateDirectory(diagnostics);
        try
        {
            File.WriteAllText(Path.Combine(storage, "current"), new string('c', 4096));
            using (FileStream session = File.Create(
                       Path.Combine(storage, generation + ".session.json")))
            {
                session.SetLength(2 * 1024 * 1024);
            }
            using (FileStream authority = File.Create(
                       Path.Combine(storage, generation + ".authority.json")))
            {
                authority.SetLength(2 * 1024 * 1024);
            }
            File.WriteAllText(
                Path.Combine(diagnostics, "operational.jsonl"),
                new string('x', 1024 * 1024));

            IncidentManifest manifest = IncidentExporter.Build(
                Marvel.Tests.RepositoryPaths.Root, saves, diagnostics);

            Assert.Equal("offline_evidence_has_invalid_selected_generation", manifest.Health);
            Assert.Equal(1, Assert.Single(manifest.Diagnostics).InvalidRecords);
            IncidentSaveGeneration saved = Assert.Single(
                manifest.SaveGenerations, value => !value.Selected);
            Assert.Equal(64, saved.SessionSha256!.Length);
            Assert.Equal(64, saved.AuthoritySha256!.Length);
            Assert.Contains(
                manifest.SaveGenerations,
                value => value.Selected && value.ErrorCode == "invalid_manifest");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static Dictionary<string, string> Snapshot(params string[] roots) =>
        roots.Where(Directory.Exists)
            .SelectMany(root => Directory.GetFiles(root, "*", SearchOption.AllDirectories))
            .ToDictionary(
                path => path,
                path => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path))),
                StringComparer.Ordinal);

    private sealed class FixedCapabilities(params string[] capabilities)
        : ISessionCapabilityIssuer
    {
        private readonly Queue<string> capabilities = new(capabilities);

        public string Issue() => capabilities.Dequeue();
    }
}
