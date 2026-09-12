using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using Xunit;

namespace Marvel.Release.Tests;

public sealed class ReleaseAcceptanceRecordTests
{
    private const string Version = "0.1.0-preview.1";
    private const string Commit = "0123456789abcdef0123456789abcdef01234567";
    private const string Digest =
        "sha256:aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa";

    [Fact]
    public void RecordBindsPassedJourneyToExactArtifactsAndExplicitCommunityTrust()
    {
        string root = CreateFixture();
        try
        {
            ReleaseAcceptanceRecord record = ReleaseAcceptanceRecord.Create(
                ReleaseVersion.Parse(Version), Commit, root);

            using JsonDocument json = JsonDocument.Parse(record.Json());
            JsonElement result = json.RootElement;
            Assert.Equal("marvel-community-release-acceptance",
                result.GetProperty("format").GetString());
            Assert.Equal("passed", result.GetProperty("status").GetString());
            Assert.Equal(Commit, result.GetProperty("commit").GetString());
            Assert.Equal(5, result.GetProperty("artifacts").GetArrayLength());
            Assert.Equal(Digest, result.GetProperty("server").GetProperty("digest").GetString());
            Assert.Contains("not-notarized",
                result.GetProperty("trust").GetProperty("macos").GetString(),
                StringComparison.Ordinal);
            Assert.Contains("self-signed",
                result.GetProperty("trust").GetProperty("windows_community_msix").GetString(),
                StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RecordRejectsAnArtifactChangedAfterItsPublishedHash()
    {
        string root = CreateFixture();
        try
        {
            string archive = Path.Combine(root,
                $"MarvelChampions-{Version}-macos-adhoc.zip");
            File.AppendAllText(archive, "changed");

            InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
                ReleaseAcceptanceRecord.Create(ReleaseVersion.Parse(Version), Commit, root));

            Assert.Contains("hash sidecar", failure.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void DeveloperBuildCannotClaimReleaseCandidateAcceptance()
    {
        ArgumentException failure = Assert.Throws<ArgumentException>(() =>
            ReleaseAcceptanceRecord.Create(
                ReleaseVersion.Parse("0.1.0-dev.1"), Commit, "absent"));

        Assert.Contains("developer builds", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void RecordRejectsAFailedServerJourney()
    {
        string root = CreateFixture();
        try
        {
            string journey = Path.Combine(root,
                $"MarvelServer-{Version}-release-candidate.json");
            string contents = File.ReadAllText(journey).Replace(
                "\"two_clients_completed\":\"passed\"",
                "\"two_clients_completed\":\"failed\"",
                StringComparison.Ordinal);
            File.WriteAllText(journey, contents);

            InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() =>
                ReleaseAcceptanceRecord.Create(ReleaseVersion.Parse(Version), Commit, root));

            Assert.Contains("two_clients_completed", failure.Message, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private static string CreateFixture()
    {
        string root = Path.Combine(
            Path.GetTempPath(), $"marvel-release-acceptance-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);

        var manifest = new ReleaseManifest(
            "marvel-release",
            1,
            Version,
            "preview",
            Commit,
            "0.1.0.0",
            new ReleaseEngineIdentity(
                "engine-replay-v2", "mt19937-iso-cxx", "state-digest-v2", 14, 3),
            new ReleaseDatasetIdentity("cards", "setup", "abilities"));
        string manifestJson = manifest.Json();

        string macos = Zip(root,
            $"MarvelChampions-{Version}-macos-adhoc.zip",
            "Marvel Champions.app/Contents/Resources/release-manifest.json",
            manifestJson);
        string unsigned = Zip(root,
            $"MarvelChampions-{Version}-windows-x64-unsigned.msix",
            "release-manifest.json",
            manifestJson);
        string portable = Zip(root,
            $"MarvelChampions-{Version}-windows-x64-portable-unsigned.zip",
            "release-manifest.json",
            manifestJson);
        string community = Zip(root,
            $"MarvelChampions-{Version}-windows-x64-community.msix",
            "release-manifest.json",
            manifestJson);
        string certificate = Path.Combine(root,
            $"MarvelChampions-{Version}-windows-x64-community.cer");
        File.WriteAllText(certificate, "public certificate only");
        Sidecar(macos, marker: " ");
        foreach (string artifact in new[] { unsigned, portable, community, certificate })
        {
            Sidecar(artifact);
        }

        File.WriteAllText(
            community + ".provenance.json",
            JsonSerializer.Serialize(new
            {
                format = "marvel-desktop-community-signing",
                schema = 1,
                product_version = Version,
                commit = Commit,
                trust = "self-signed-public-certificate-required",
                timestamp = "none",
                publisher = "CN=Marvel Champions Community",
                unsigned_input_sha256 = Hash(unsigned),
                artifact_sha256 = Hash(community),
                public_certificate_sha256 = Hash(certificate),
            }));

        string digestPath = Path.Combine(root,
            $"MarvelServer-{Version}-linux-amd64.digest");
        File.WriteAllText(digestPath, Digest + "\n");
        File.WriteAllText(
            Path.Combine(root, $"MarvelServer-{Version}-linux-amd64.provenance.json"),
            JsonSerializer.Serialize(new
            {
                format = "marvel-server-release",
                schema = 1,
                product_version = Version,
                commit = Commit,
                image = "ghcr.io/example/marvel-server",
                digest = Digest,
                engine_replay = "engine-replay-v2",
                rng = "mt19937-iso-cxx",
                state_digest = "state-digest-v2",
                protocol = 14,
                save_schema = 3,
                datasets = new
                {
                    cards_sha256 = "cards",
                    setup_sha256 = "setup",
                    abilities_sha256 = "abilities",
                },
            }));
        File.WriteAllText(
            Path.Combine(root, $"MarvelServer-{Version}-linux-amd64.sigstore.json"),
            "{}");

        string incident = Path.Combine(root,
            $"MarvelServer-{Version}-release-candidate-incident.json");
        File.WriteAllText(incident, JsonSerializer.Serialize(new
        {
            format = "marvel-incident",
            schema = 1,
            health = "offline_evidence_only",
            runtime = new { product_version = Version, commit = Commit },
            diagnostics = new[] { new { name = "operational.jsonl" } },
            save_generations = new[] { new { selected = true } },
        }));
        File.WriteAllText(
            Path.Combine(root, $"MarvelServer-{Version}-release-candidate.json"),
            JsonSerializer.Serialize(new
            {
                format = "marvel-server-release-candidate",
                schema = 1,
                status = "passed",
                product_version = Version,
                image = "ghcr.io/example/marvel-server@" + Digest,
                image_digest = Digest,
                incident = new { name = Path.GetFileName(incident), sha256 = Hash(incident) },
                checks = new
                {
                    two_clients_completed = "passed",
                    graceful_restart_restored = "passed",
                    both_clients_reconnected = "passed",
                    monotonic_upgrade_replay_verified = "passed",
                    redacted_diagnostics_retained = "passed",
                    read_only_incident_export = "passed",
                },
            }));
        return root;
    }

    private static string Zip(string root, string name, string entryName, string content)
    {
        string path = Path.Combine(root, name);
        using ZipArchive archive = ZipFile.Open(path, ZipArchiveMode.Create);
        ZipArchiveEntry entry = archive.CreateEntry(entryName);
        using var writer = new StreamWriter(entry.Open());
        writer.Write(content);
        return path;
    }

    private static void Sidecar(string path, string marker = "*") => File.WriteAllText(
        path + ".sha256",
        $"{Hash(path)} {marker}{Path.GetFileName(path)}\n");

    private static string Hash(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}
