using Marvel.Decisions;
using Marvel.Session;
using System.Security.Cryptography;
using Xunit;

namespace Marvel.Server.Tests;

public sealed class SessionPersistenceTests
{
    [Fact]
    public void DatasetCompatibilityUsesTheExactVendoredBytes()
    {
        DatasetGameFactory factory = DatasetGameFactory.Load(
            Marvel.Tests.RepositoryPaths.Root);

        Assert.Equal(Hash("cards", "cards.json"), factory.Compatibility.CardsSha256);
        Assert.Equal(Hash("setup", "setup.json"), factory.Compatibility.SetupSha256);
        Assert.Equal(
            Hash("abilities", "abilities.json"),
            factory.Compatibility.AbilitiesSha256);
    }

    [Fact]
    public void AFilesystemRestartRestoresOwnerAndAttachedSeatWithoutPlaintextSecrets()
    {
        string root = Path.Combine(
            Path.GetTempPath(), $"marvel-session-host-{Guid.NewGuid():N}");
        try
        {
            var factory = DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root);
            var first = new EngineHost(
                factory,
                new FixedCapabilities("owner-secret", "seat-invite", "seat-secret"),
                new Marvel.View.RestrictedVisibilityPolicy(0),
                new FileSessionStore(root));
            EngineResponse opened = first.Exchange(EngineRequest.OpenGame(
                "open",
                "persistent-table",
                new GameSpecification(
                    "rhino", ["spider_man", "captain_marvel"], [], Seed: 91)));
            SeatInvitation invitation = Assert.Single(opened.Invitations!);
            EngineResponse attached = first.Exchange(EngineRequest.AttachGame(
                "attach", "persistent-table", invitation.Invitation));
            var mulligan = Assert.Single(Assert.IsType<Marvel.Rules.Prompts.Prompt>(
                opened.Prompt).Affordances);
            Assert.Null(first.Exchange(EngineRequest.ResolveGame(
                "keep",
                "persistent-table",
                opened.Capability!,
                new EngineDecision(mulligan.Id, []),
                opened.Revision)).Error);

            // Simulate the exact predecessor generation written before schema 2.
            // The next host must verify it and atomically publish the migrated save.
            string directory = Assert.Single(Directory.GetDirectories(root));
            string generation = File.ReadAllText(Path.Combine(directory, "current")).Trim();
            string predecessor = Path.Combine(directory, generation + ".session.json");
            File.WriteAllText(
                predecessor,
                File.ReadAllText(predecessor).Replace(
                    "\"schema\":2",
                    "\"schema\":1",
                    StringComparison.Ordinal).Replace(
                    ",\"exposures\":[]",
                    string.Empty,
                    StringComparison.Ordinal));

            var restarted = new EngineHost(
                factory,
                visibility: new Marvel.View.RestrictedVisibilityPolicy(0),
                store: new FileSessionStore(root));
            EngineResponse owner = restarted.Exchange(EngineRequest.SyncGame(
                "owner", "persistent-table", opened.Capability!));
            EngineResponse seat = restarted.Exchange(EngineRequest.SyncGame(
                "seat", "persistent-table", attached.Capability!));

            Assert.Null(owner.Error);
            Assert.Null(seat.Error);
            Assert.Equal(
                SessionSave.CurrentSchema,
                Assert.Single(new FileSessionStore(root).Load()).Save.Schema);
            Assert.Single(Assert.Single(new FileSessionStore(root).Load()).Save.Units);
            Assert.NotEqual(
                EngineJson.Write(owner with { RequestId = "same" }),
                EngineJson.Write(seat with { RequestId = "same" }));
            string persisted = string.Join(
                "\n",
                Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                    .Select(File.ReadAllText));
            Assert.DoesNotContain("owner-secret", persisted, StringComparison.Ordinal);
            Assert.DoesNotContain("seat-invite", persisted, StringComparison.Ordinal);
            Assert.DoesNotContain("seat-secret", persisted, StringComparison.Ordinal);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void CredentialRevocationAndOwnerRetirementSurviveRestart()
    {
        var store = new MemorySessionStore();
        var factory = DatasetGameFactory.Load(Marvel.Tests.RepositoryPaths.Root);
        var first = new EngineHost(
            factory,
            new FixedCapabilities("owner", "invite", "seat"),
            new Marvel.View.RestrictedVisibilityPolicy(0),
            store);
        EngineResponse opened = first.Exchange(EngineRequest.OpenGame(
            "open",
            "reusable-label",
            new GameSpecification(
                "rhino", ["spider_man", "captain_marvel"], [], Seed: 91)));
        EngineResponse attached = first.Exchange(EngineRequest.AttachGame(
            "attach",
            "reusable-label",
            Assert.Single(opened.Invitations!).Invitation));
        Assert.Null(first.Exchange(EngineRequest.CloseGame(
            "leave", "reusable-label", attached.Capability!)).Error);

        var afterSeatClose = new EngineHost(factory, store: store);
        Assert.Null(afterSeatClose.Exchange(EngineRequest.SyncGame(
            "owner", "reusable-label", opened.Capability!)).Error);
        Assert.Equal(
            "session_not_found",
            afterSeatClose.Exchange(EngineRequest.SyncGame(
                "seat", "reusable-label", attached.Capability!)).Error?.Code);
        Assert.Null(afterSeatClose.Exchange(EngineRequest.CloseGame(
            "retire", "reusable-label", opened.Capability!)).Error);

        var afterRetirement = new EngineHost(
            factory,
            new FixedCapabilities("replacement-owner"),
            store: store);
        Assert.Equal(
            "session_not_found",
            afterRetirement.Exchange(EngineRequest.SyncGame(
                "old", "reusable-label", opened.Capability!)).Error?.Code);
        Assert.Null(afterRetirement.Exchange(EngineRequest.OpenGame(
            "replacement",
            "reusable-label",
            new GameSpecification("rhino", ["spider_man"], [], Seed: 92))).Error);
    }

    [Fact]
    public void ACommittedManifestIgnoresAnUnselectedInterruptedGeneration()
    {
        string root = Path.Combine(
            Path.GetTempPath(), $"marvel-session-store-{Guid.NewGuid():N}");
        try
        {
            var store = new FileSessionStore(root);
            StoredSession first = Stored(revision: 0);
            store.Commit(first);
            string directory = Assert.Single(Directory.GetDirectories(root));
            File.WriteAllText(
                Path.Combine(directory, new string('e', 32) + ".json"),
                "not a complete generation");
            File.WriteAllText(Path.Combine(directory, ".current-interrupted.tmp"), "broken");

            StoredSession loaded = Assert.Single(store.Load());

            Assert.Equal(0, loaded.Save.Revision);
            StoredAuthority authority = Assert.Single(loaded.Authorities);
            Assert.Equal(first.Authorities[0].Verifier, authority.Verifier);
            Assert.Equal([0], authority.Seats);
            Assert.True(authority.Owner);
            Assert.False(authority.Invitation);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void TheManifestSwitchPublishesTheWholeNextGeneration()
    {
        string root = Path.Combine(
            Path.GetTempPath(), $"marvel-session-store-{Guid.NewGuid():N}");
        try
        {
            var store = new FileSessionStore(root);
            store.Commit(Stored(revision: 0));
            store.Commit(Stored(revision: 1));
            store.Commit(Stored(revision: 2));

            StoredSession loaded = Assert.Single(store.Load());

            Assert.Equal(2, loaded.Save.Revision);
            Assert.Single(loaded.Authorities);
            string sessionDirectory = Assert.Single(Directory.GetDirectories(root));
            Assert.Equal(
                2,
                Directory.GetFiles(sessionDirectory, "*.session.json").Length);
            Assert.Equal(
                2,
                Directory.GetFiles(sessionDirectory, "*.authority.json").Length);
            if (!OperatingSystem.IsWindows())
            {
                Assert.Equal(
                    UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute,
                    File.GetUnixFileMode(sessionDirectory));
                foreach (string file in Directory.GetFiles(sessionDirectory))
                {
                    Assert.Equal(
                        UnixFileMode.UserRead | UnixFileMode.UserWrite,
                        File.GetUnixFileMode(file));
                }
            }
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void CreatingAStoreDoesNotChangeAnExistingParentsPermissions()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        string parent = Path.Combine(
            Path.GetTempPath(), $"marvel-session-parent-{Guid.NewGuid():N}");
        Directory.CreateDirectory(parent);
        UnixFileMode original = UnixFileMode.UserRead
            | UnixFileMode.UserWrite
            | UnixFileMode.UserExecute
            | UnixFileMode.GroupRead
            | UnixFileMode.GroupExecute;
        File.SetUnixFileMode(parent, original);
        try
        {
            var store = new FileSessionStore(Path.Combine(parent, "nested", "sessions"));

            store.Commit(Stored(revision: 0));

            Assert.Equal(original, File.GetUnixFileMode(parent));
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    [Fact]
    public void InvalidUtf8InASelectedGenerationFailsClosed()
    {
        string root = Path.Combine(
            Path.GetTempPath(), $"marvel-session-utf8-{Guid.NewGuid():N}");
        try
        {
            var store = new FileSessionStore(root);
            store.Commit(Stored(revision: 0));
            string save = Assert.Single(Directory.GetFiles(
                root, "*.session.json", SearchOption.AllDirectories));
            byte[] bytes = File.ReadAllBytes(save);
            int application = bytes.AsSpan().IndexOf("1.0.0"u8);
            Assert.True(application >= 0);
            bytes[application] = 0xff;
            File.WriteAllBytes(save, bytes);

            Assert.Throws<SessionSaveException>(() => store.Load());
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void RestoreCandidatesKeepAHealthyGenerationBesideACorruptOne()
    {
        string root = Path.Combine(
            Path.GetTempPath(), $"marvel-session-quarantine-{Guid.NewGuid():N}");
        try
        {
            var store = new FileSessionStore(root);
            string badId = new('a', 32);
            string healthyId = new('b', 32);
            store.Commit(Stored(revision: 0, badId, "bad-table"));
            store.Commit(Stored(revision: 1, healthyId, "healthy-table"));
            string badSave = Assert.Single(Directory.GetFiles(
                Path.Combine(root, badId), "*.session.json"));
            File.WriteAllText(badSave, "not-json");

            IReadOnlyList<SessionLoadResult> candidates = store.LoadForRestore();

            SessionLoadResult failed = Assert.Single(
                candidates, result => result.StorageId == badId);
            Assert.Null(failed.Session);
            Assert.Equal("restore_failed", failed.ErrorCode);
            SessionLoadResult loaded = Assert.Single(
                candidates, result => result.StorageId == healthyId);
            Assert.Equal("healthy-table", loaded.Session?.Save.Session.Label);
            Assert.Null(loaded.ErrorCode);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void MissingAuthorityMembersFailClosedInsteadOfChangingOwnership()
    {
        string json = StoredAuthorityJson.Write(Stored(revision: 0).Authorities);
        string missingOwner = json.Replace(
            "\"owner\":true,",
            string.Empty,
            StringComparison.Ordinal);

        Assert.Throws<SessionSaveException>(() => StoredAuthorityJson.Read(missingOwner));
        string generation = StoredSessionJson.Write(Stored(revision: 0));
        Assert.Throws<SessionSaveException>(() => StoredSessionJson.Read(
            generation.Replace(
                "\"owner\":true",
                "\"owner\":false",
                StringComparison.Ordinal)));
    }

    private static StoredSession Stored(
        long revision,
        string? storageId = null,
        string label = "table")
    {
        var save = new SessionSave(
            SessionSave.FormatName,
            SessionSave.CurrentSchema,
            new SessionCompatibility(
                "1.0.0",
                "engine-replay-v1",
                "mt19937-iso-cxx",
                "state-digest-v2",
                new string('a', 64),
                new string('b', 64),
                new string('c', 64)),
            new SessionIdentity(storageId ?? new string('d', 32), label, "active"),
            new SessionSetup("rhino", ["spider_man"], [], 7),
            new InitialRecord([], 0, "digest"),
            revision,
            Cursor: 0,
            EditFrontier: 0,
            CurrentPrompt: null,
            Units: []);
        return new StoredSession(
            save,
            [new StoredAuthority(new string('f', 64), [0], Owner: true, Invitation: false)]);
    }

    private static string Hash(string dataset, string file) =>
        Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(Path.Combine(
                Marvel.Tests.RepositoryPaths.Root, "datasets", dataset, file))))
            .ToLowerInvariant();

    private sealed class FixedCapabilities(params string[] values) : ISessionCapabilityIssuer
    {
        private readonly Queue<string> values = new(values);

        public string Issue() => values.Dequeue();
    }
}
