using System.IO.Compression;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Marvel.Release;

internal sealed record ReleaseAcceptanceRecord(
    [property: JsonPropertyName("format")] string Format,
    [property: JsonPropertyName("schema")] int Schema,
    [property: JsonPropertyName("status")] string Status,
    [property: JsonPropertyName("product_version")] string ProductVersion,
    [property: JsonPropertyName("commit")] string Commit,
    [property: JsonPropertyName("trust")] ReleaseAcceptanceTrust Trust,
    [property: JsonPropertyName("engine")] ReleaseEngineIdentity Engine,
    [property: JsonPropertyName("datasets")] ReleaseDatasetIdentity Datasets,
    [property: JsonPropertyName("artifacts")] IReadOnlyList<ReleaseAcceptedArtifact> Artifacts,
    [property: JsonPropertyName("server")] ReleaseAcceptedServer Server,
    [property: JsonPropertyName("evidence")] ReleaseAcceptanceEvidence Evidence,
    [property: JsonPropertyName("results")] ReleaseAcceptanceResults Results)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = null,
    };

    public static ReleaseAcceptanceRecord Create(
        ReleaseVersion version,
        string commit,
        string artifactRoot)
    {
        ArgumentNullException.ThrowIfNull(version);
        if (version.Channel == ReleaseChannel.Developer)
        {
            throw new ArgumentException(
                "developer builds cannot produce a community release acceptance record");
        }
        ValidateCommit(commit);
        if (!Directory.Exists(artifactRoot))
        {
            throw new ArgumentException("release artifact directory does not exist");
        }

        string macos = Artifact(artifactRoot,
            $"MarvelChampions-{version.Value}-macos-adhoc.zip");
        string windowsUnsigned = Artifact(artifactRoot,
            $"MarvelChampions-{version.Value}-windows-x64-unsigned.msix");
        string windowsPortable = Artifact(artifactRoot,
            $"MarvelChampions-{version.Value}-windows-x64-portable-unsigned.zip");
        string windowsCommunity = Artifact(artifactRoot,
            $"MarvelChampions-{version.Value}-windows-x64-community.msix");
        string windowsCertificate = Artifact(artifactRoot,
            $"MarvelChampions-{version.Value}-windows-x64-community.cer");
        string serverDigestPath = Artifact(artifactRoot,
            $"MarvelServer-{version.Value}-linux-amd64.digest");
        string serverProvenancePath = Artifact(artifactRoot,
            $"MarvelServer-{version.Value}-linux-amd64.provenance.json");
        string sigstoreBundle = Artifact(artifactRoot,
            $"MarvelServer-{version.Value}-linux-amd64.sigstore.json");
        string serverJourneyPath = Artifact(artifactRoot,
            $"MarvelServer-{version.Value}-release-candidate.json");
        string incidentPath = Artifact(artifactRoot,
            $"MarvelServer-{version.Value}-release-candidate-incident.json");

        foreach (string path in new[]
        {
            macos,
            windowsUnsigned,
            windowsPortable,
            windowsCommunity,
            windowsCertificate,
        })
        {
            VerifySidecar(path);
        }

        ReleaseManifest macosManifest = ReadManifest(
            macos, entry => entry.FullName.EndsWith("/release-manifest.json", StringComparison.Ordinal));
        ReleaseManifest unsignedManifest = ReadManifest(
            windowsUnsigned, entry => entry.FullName == "release-manifest.json");
        ReleaseManifest portableManifest = ReadManifest(
            windowsPortable, entry => entry.FullName == "release-manifest.json");
        ReleaseManifest communityManifest = ReadManifest(
            windowsCommunity, entry => entry.FullName == "release-manifest.json");
        foreach (ReleaseManifest manifest in new[]
        {
            macosManifest,
            unsignedManifest,
            portableManifest,
            communityManifest,
        })
        {
            VerifyManifest(manifest, version, commit, macosManifest);
        }

        using JsonDocument signing = ReadJson(Artifact(
            artifactRoot, Path.GetFileName(windowsCommunity) + ".provenance.json"));
        JsonElement signingRoot = signing.RootElement;
        Require(signingRoot, "format", "marvel-desktop-community-signing");
        Require(signingRoot, "product_version", version.Value);
        Require(signingRoot, "commit", commit);
        Require(signingRoot, "trust", "self-signed-public-certificate-required");
        Require(signingRoot, "timestamp", "none");
        Require(signingRoot, "publisher", "CN=Marvel Champions Community");
        Require(signingRoot, "unsigned_input_sha256", Hash(windowsUnsigned));
        Require(signingRoot, "artifact_sha256", Hash(windowsCommunity));
        Require(signingRoot, "public_certificate_sha256", Hash(windowsCertificate));

        string serverDigest = File.ReadAllText(serverDigestPath).Trim();
        if (!ValidSha256(serverDigest))
        {
            throw new InvalidOperationException("server digest is not one lowercase SHA-256 value");
        }

        using JsonDocument serverProvenance = ReadJson(serverProvenancePath);
        using JsonDocument _ = ReadJson(sigstoreBundle);
        JsonElement server = serverProvenance.RootElement;
        Require(server, "format", "marvel-server-release");
        Require(server, "product_version", version.Value);
        Require(server, "commit", commit);
        Require(server, "digest", serverDigest);
        Require(server, "engine_replay", macosManifest.Engine.ReplayContract);
        Require(server, "rng", macosManifest.Engine.RngContract);
        Require(server, "state_digest", macosManifest.Engine.StateDigest);
        Require(server, "protocol", macosManifest.Engine.Protocol);
        Require(server, "save_schema", macosManifest.Engine.SaveSchema);
        VerifyDatasets(server.GetProperty("datasets"), macosManifest.Datasets);
        string image = RequiredString(server, "image");

        using JsonDocument journey = ReadJson(serverJourneyPath);
        JsonElement journeyRoot = journey.RootElement;
        Require(journeyRoot, "format", "marvel-server-release-candidate");
        Require(journeyRoot, "schema", 1);
        Require(journeyRoot, "status", "passed");
        Require(journeyRoot, "product_version", version.Value);
        Require(journeyRoot, "image", $"{image}@{serverDigest}");
        Require(journeyRoot, "image_digest", serverDigest);
        JsonElement checks = journeyRoot.GetProperty("checks");
        foreach (string check in new[]
        {
            "two_clients_completed",
            "graceful_restart_restored",
            "both_clients_reconnected",
            "monotonic_upgrade_replay_verified",
            "redacted_diagnostics_retained",
            "read_only_incident_export",
        })
        {
            Require(checks, check, "passed");
        }

        JsonElement incidentReference = journeyRoot.GetProperty("incident");
        Require(incidentReference, "name", Path.GetFileName(incidentPath));
        Require(incidentReference, "sha256", Hash(incidentPath));
        using JsonDocument incident = ReadJson(incidentPath);
        JsonElement incidentRoot = incident.RootElement;
        Require(incidentRoot, "format", "marvel-incident");
        Require(incidentRoot, "schema", 1);
        Require(incidentRoot, "health", "offline_evidence_only");
        JsonElement incidentRuntime = incidentRoot.GetProperty("runtime");
        Require(incidentRuntime, "product_version", version.Value);
        Require(incidentRuntime, "commit", commit);
        if (incidentRoot.GetProperty("diagnostics").GetArrayLength() == 0
            || !incidentRoot.GetProperty("save_generations").EnumerateArray()
                .Any(generation => generation.GetProperty("selected").GetBoolean()))
        {
            throw new InvalidOperationException("incident evidence is incomplete");
        }

        return new ReleaseAcceptanceRecord(
            "marvel-community-release-acceptance",
            1,
            "passed",
            version.Value,
            commit,
            new ReleaseAcceptanceTrust(
                "ad-hoc-signature-user-approved-first-launch-not-notarized",
                "self-signed-certificate-required-no-trusted-timestamp",
                "unsigned-no-certificate-store-change",
                "keyless-sigstore-workflow-identity"),
            macosManifest.Engine,
            macosManifest.Datasets,
            new[]
            {
                Accepted("macos_desktop", macos),
                Accepted("windows_unsigned_msix_input", windowsUnsigned),
                Accepted("windows_portable", windowsPortable),
                Accepted("windows_community_msix", windowsCommunity),
                Accepted("windows_public_certificate", windowsCertificate),
            },
            new ReleaseAcceptedServer(
                image,
                serverDigest,
                Path.GetFileName(sigstoreBundle),
                Hash(sigstoreBundle),
                Path.GetFileName(serverProvenancePath),
                Hash(serverProvenancePath)),
            new ReleaseAcceptanceEvidence(
                Path.GetFileName(serverJourneyPath),
                Hash(serverJourneyPath),
                Path.GetFileName(incidentPath),
                Hash(incidentPath)),
            new ReleaseAcceptanceResults(
                "passed",
                "passed",
                "passed",
                "passed"));
    }

    public string Json() => JsonSerializer.Serialize(this, Options) + "\n";

    private static ReleaseAcceptedArtifact Accepted(string kind, string path) =>
        new(kind, Path.GetFileName(path), Hash(path));

    private static string Artifact(string root, string name)
    {
        string path = Path.Combine(root, name);
        if (!File.Exists(path))
        {
            throw new ArgumentException($"release artifact is absent: {name}");
        }
        return path;
    }

    private static void VerifySidecar(string path)
    {
        string sidecar = Artifact(Path.GetDirectoryName(path)!, Path.GetFileName(path) + ".sha256");
        string line = File.ReadAllText(sidecar).Trim();
        string hash = line.Length >= 64 ? line[..64] : "";
        string name = line.Length > 64 ? line[64..].TrimStart() : "";
        if (name.StartsWith('*'))
        {
            name = name[1..];
        }
        if (!string.Equals(hash, Hash(path), StringComparison.Ordinal)
            || !string.Equals(name, Path.GetFileName(path), StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"artifact hash sidecar does not match {Path.GetFileName(path)}");
        }
    }

    private static ReleaseManifest ReadManifest(
        string archivePath,
        Func<ZipArchiveEntry, bool> selects)
    {
        using var archive = ZipFile.OpenRead(archivePath);
        ZipArchiveEntry[] matches = archive.Entries.Where(selects).ToArray();
        if (matches.Length != 1)
        {
            throw new InvalidOperationException(
                $"artifact does not contain one release manifest: {Path.GetFileName(archivePath)}");
        }
        using Stream stream = matches[0].Open();
        ReleaseManifest? manifest = JsonSerializer.Deserialize<ReleaseManifest>(stream, Options);
        return manifest ?? throw new InvalidOperationException("release manifest is empty");
    }

    private static void VerifyManifest(
        ReleaseManifest manifest,
        ReleaseVersion version,
        string commit,
        ReleaseManifest expected)
    {
        string channel = version.Channel switch
        {
            ReleaseChannel.Developer => "developer",
            ReleaseChannel.Preview => "preview",
            ReleaseChannel.Stable => "stable",
            _ => throw new InvalidOperationException("release channel is unsupported"),
        };
        if (manifest.Format != "marvel-release"
            || manifest.Schema != 1
            || manifest.ProductVersion != version.Value
            || manifest.Channel != channel
            || manifest.Commit != commit
            || manifest.AssemblyVersion != version.AssemblyVersion
            || manifest.Engine != expected.Engine
            || manifest.Datasets != expected.Datasets)
        {
            throw new InvalidOperationException("desktop artifact release identities disagree");
        }
    }

    private static JsonDocument ReadJson(string path)
    {
        try
        {
            return JsonDocument.Parse(File.ReadAllBytes(path));
        }
        catch (JsonException failure)
        {
            throw new InvalidOperationException(
                $"release evidence is invalid JSON: {Path.GetFileName(path)}", failure);
        }
    }

    private static void VerifyDatasets(JsonElement datasets, ReleaseDatasetIdentity expected)
    {
        Require(datasets, "cards_sha256", expected.CardsSha256);
        Require(datasets, "setup_sha256", expected.SetupSha256);
        Require(datasets, "abilities_sha256", expected.AbilitiesSha256);
    }

    private static void Require(JsonElement element, string property, string expected)
    {
        if (!element.TryGetProperty(property, out JsonElement actual)
            || actual.ValueKind != JsonValueKind.String
            || !string.Equals(actual.GetString(), expected, StringComparison.Ordinal))
        {
            throw new InvalidOperationException($"release evidence disagrees at {property}");
        }
    }

    private static void Require(JsonElement element, string property, int expected)
    {
        if (!element.TryGetProperty(property, out JsonElement actual)
            || !actual.TryGetInt32(out int value)
            || value != expected)
        {
            throw new InvalidOperationException($"release evidence disagrees at {property}");
        }
    }

    private static string RequiredString(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out JsonElement value)
            || value.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(value.GetString()))
        {
            throw new InvalidOperationException($"release evidence is missing {property}");
        }
        return value.GetString()!;
    }

    private static void ValidateCommit(string commit)
    {
        if (commit.Length != 40
            || commit.Any(character => character is not (>= '0' and <= '9')
                and not (>= 'a' and <= 'f')))
        {
            throw new ArgumentException("commit must be 40 lowercase hexadecimal characters");
        }
    }

    private static bool ValidSha256(string value) =>
        value.Length == 71
        && value.StartsWith("sha256:", StringComparison.Ordinal)
        && value[7..].All(character => character is >= '0' and <= '9' or >= 'a' and <= 'f');

    private static string Hash(string path)
    {
        using FileStream stream = File.OpenRead(path);
        return Convert.ToHexString(SHA256.HashData(stream)).ToLowerInvariant();
    }
}

internal sealed record ReleaseAcceptanceTrust(
    [property: JsonPropertyName("macos")] string Macos,
    [property: JsonPropertyName("windows_community_msix")] string WindowsCommunityMsix,
    [property: JsonPropertyName("windows_portable")] string WindowsPortable,
    [property: JsonPropertyName("server")] string Server);

internal sealed record ReleaseAcceptedArtifact(
    [property: JsonPropertyName("kind")] string Kind,
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("sha256")] string Sha256);

internal sealed record ReleaseAcceptedServer(
    [property: JsonPropertyName("image")] string Image,
    [property: JsonPropertyName("digest")] string Digest,
    [property: JsonPropertyName("sigstore_bundle")] string SigstoreBundle,
    [property: JsonPropertyName("sigstore_bundle_sha256")] string SigstoreBundleSha256,
    [property: JsonPropertyName("provenance")] string Provenance,
    [property: JsonPropertyName("provenance_sha256")] string ProvenanceSha256);

internal sealed record ReleaseAcceptanceEvidence(
    [property: JsonPropertyName("server_journey")] string ServerJourney,
    [property: JsonPropertyName("server_journey_sha256")] string ServerJourneySha256,
    [property: JsonPropertyName("incident_manifest")] string IncidentManifest,
    [property: JsonPropertyName("incident_manifest_sha256")] string IncidentManifestSha256);

internal sealed record ReleaseAcceptanceResults(
    [property: JsonPropertyName("macos_install_and_first_launch")] string MacosInstallAndFirstLaunch,
    [property: JsonPropertyName("windows_community_install_and_trust")] string WindowsCommunityInstallAndTrust,
    [property: JsonPropertyName("windows_portable_install")] string WindowsPortableInstall,
    [property: JsonPropertyName("server_restart_upgrade_and_reconnect")] string ServerRestartUpgradeAndReconnect);
