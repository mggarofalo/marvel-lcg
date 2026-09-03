using System.Security.Cryptography;
using System.Text.Json;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class IncidentExportTests
{
    [Fact]
    public void ExportIncludesRedactedLogsRuntimeAndGenerationHashesWithoutChangingSources()
    {
        string root = Path.Combine(
            Path.GetTempPath(), $"marvel-incident-{Guid.NewGuid():N}");
        string saves = Path.Combine(root, "sessions");
        string diagnostics = Path.Combine(root, "diagnostics");
        string output = Path.Combine(root, "incident.json");
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
                sink.Write(new OperationalRecord(
                    OperationalEventIds.RequestCompleted,
                    "Marvel.Server",
                    DateTimeOffset.UnixEpoch,
                    7,
                    1,
                    "accepted",
                    RequestId: "pseudonymous-request",
                    GameId: "pseudonymous-game",
                    Operation: EngineProtocol.Open,
                    Revision: 0,
                    SaveCommitted: true));
            }
            File.AppendAllText(
                Path.Combine(diagnostics, "operational.jsonl"),
                "not-json owner-secret private-table-label\n");
            Dictionary<string, string> before = Snapshot(saves, diagnostics);

            IncidentExporter.Export(
                output,
                Marvel.Tests.RepositoryPaths.Root,
                saves,
                diagnostics,
                () => DateTimeOffset.UnixEpoch);

            Assert.Equal(before, Snapshot(saves, diagnostics));
            using JsonDocument document = JsonDocument.Parse(File.ReadAllText(output));
            JsonElement manifest = document.RootElement;
            Assert.Equal("marvel-incident", manifest.GetProperty("format").GetString());
            Assert.Equal(EngineBuildIdentity.ProductVersion,
                manifest.GetProperty("runtime").GetProperty("product_version").GetString());
            JsonElement log = Assert.Single(
                manifest.GetProperty("diagnostics").EnumerateArray());
            Assert.Equal(1, log.GetProperty("invalid_records").GetInt32());
            Assert.Single(log.GetProperty("records").EnumerateArray());
            JsonElement generation = Assert.Single(
                manifest.GetProperty("save_generations").EnumerateArray(),
                item => item.GetProperty("selected").GetBoolean());
            Assert.Equal(64, generation.GetProperty("session_sha256").GetString()!.Length);
            Assert.Equal(64, generation.GetProperty("authority_sha256").GetString()!.Length);
            string exported = File.ReadAllText(output);
            Assert.DoesNotContain("owner-secret", exported, StringComparison.Ordinal);
            Assert.DoesNotContain("private-table-label", exported, StringComparison.Ordinal);
            if (!OperatingSystem.IsWindows())
            {
                Assert.Equal(
                    UnixFileMode.UserRead | UnixFileMode.UserWrite,
                    File.GetUnixFileMode(output));
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ExportRefusesToOverwriteAndTheCommandDoesNotStartAServer()
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
