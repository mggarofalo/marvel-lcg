using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Marvel.MarvelCdb.Harvest;
using Marvel.Tests;
using Xunit;
using FaqHarvest = Marvel.MarvelCdb.Harvest.Harvest;

namespace Marvel.Rules.Tests.Rules;

public sealed class MarvelCdbHarvestTests
{
    [Fact]
    public void TheCommittedSnapshotIsTheCanonicalWireFormat()
    {
        string path = RepositoryPaths.Dataset("marvelcdb-faq", "faq.json");
        string committed = File.ReadAllText(path);
        Snapshot snapshot = Snapshot.Read(committed);

        Assert.Equal(committed, snapshot.Json());
        Assert.Equal(4456, snapshot.Queried.Count);
        Assert.Equal(63, snapshot.Entries.Count);
        Assert.False(snapshot.CandidateComplete);
    }

    [Fact]
    public void TheCommittedAcquisitionMatchesItsIndependentPin()
    {
        byte[] acquisition = File.ReadAllBytes(
            RepositoryPaths.Dataset("marvelcdb-faq", "acquisition.json"));
        Snapshot snapshot = Snapshot.Read(Encoding.UTF8.GetString(acquisition));
        using var manifest = JsonDocument.Parse(File.ReadAllBytes(
            RepositoryPaths.Dataset("marvelcdb-faq", "acquisition.manifest.json")));
        JsonElement query = manifest.RootElement.GetProperty("query");
        using var digest = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        Span<byte> length = stackalloc byte[sizeof(long)];
        foreach (string code in snapshot.Queried.Order(StringComparer.Ordinal))
        {
            byte[] bytes = Encoding.UTF8.GetBytes(code);
            BinaryPrimitives.WriteInt64BigEndian(length, bytes.LongLength);
            digest.AppendData(length);
            digest.AppendData(bytes);
        }

        Assert.Equal(1, manifest.RootElement.GetProperty("version").GetInt32());
        Assert.Equal("marvelcdb-faq-candidate-v1",
            manifest.RootElement.GetProperty("format").GetString());
        Assert.Equal(acquisition.LongLength, manifest.RootElement.GetProperty("bytes").GetInt64());
        Assert.Equal(
            "sha256:" + Convert.ToHexString(SHA256.HashData(acquisition)).ToLowerInvariant(),
            manifest.RootElement.GetProperty("hash").GetString());
        Assert.Equal("sha256-length-prefixed-utf8",
            query.GetProperty("algorithm").GetString());
        Assert.Equal(snapshot.Queried.Count, query.GetProperty("count").GetInt32());
        Assert.Equal(
            "sha256:" + Convert.ToHexString(digest.GetHashAndReset()).ToLowerInvariant(),
            query.GetProperty("hash").GetString());
        snapshot.VerifyPublishable();
    }

    [Fact]
    public void WriteRejectsThePinnedQueryUniverseWithoutAcquisitionOutcomes()
    {
        string temporary = Path.Combine(Path.GetTempPath(), $"marvel-faq-write-{Guid.NewGuid():N}");
        string candidate = Path.Combine(temporary, "candidate.json");
        string output = Path.Combine(temporary, "output");
        try
        {
            Directory.CreateDirectory(temporary);
            Snapshot committed = Snapshot.Read(File.ReadAllText(
                RepositoryPaths.Dataset("marvelcdb-faq", "faq.json")));
            var forged = new Snapshot(
                "2026-08-31",
                "forged",
                committed.Queried,
                [],
                CandidateComplete: true,
                Outcomes: committed.Queried.Select(code => new QueryOutcome(code, "none")).ToList());
            File.WriteAllText(candidate, forged.CandidateJson());

            Assert.Equal(1, RunTool("write", candidate, output));
            Assert.False(File.Exists(Path.Combine(output, "faq.json")));
        }
        finally
        {
            if (Directory.Exists(temporary))
            {
                Directory.Delete(temporary, recursive: true);
            }
        }
    }

    [Fact]
    public void TheExactPinnedAcquisitionWritesAndChecksOffline()
    {
        string temporary = Path.Combine(Path.GetTempPath(), $"marvel-faq-pinned-{Guid.NewGuid():N}");
        string acquisition = RepositoryPaths.Dataset("marvelcdb-faq", "acquisition.json");
        try
        {
            Assert.Equal(0, RunTool("write", acquisition, temporary));
            Assert.Equal(
                File.ReadAllText(RepositoryPaths.Dataset("marvelcdb-faq", "faq.json")),
                File.ReadAllText(Path.Combine(temporary, "faq.json")));
            Assert.Equal(0, RunTool("check", acquisition));
        }
        finally
        {
            if (Directory.Exists(temporary))
            {
                Directory.Delete(temporary, recursive: true);
            }
        }
    }

    [Fact]
    public void DuplicateEntriesKeepTheFirstAndAreReported()
    {
        Snapshot snapshot = Snapshot.Read(File.ReadAllText(
            RepositoryPaths.Dataset("marvelcdb-faq", "faq.json")));
        IReadOnlyDictionary<string, JsonElement> entries = snapshot.FirstEntries(out var duplicates);

        Assert.Equal(62, entries.Count);
        Assert.Equal(["05005"], duplicates);
        Assert.Equal(
            snapshot.Entries.First(entry => entry.GetProperty("code").GetString() == "05005")
                .GetProperty("text").GetString(),
            entries["05005"].GetProperty("text").GetString());
    }

    [Fact]
    public void WholeCardCodesFanOutToEveryPrintedFace()
    {
        IReadOnlySet<string> cards = new HashSet<string>(
            ["01001a", "01001b", "01050", "01097a", "01097b", "01144a", "01144b", "01144c"],
            StringComparer.Ordinal);

        Assert.Equal(["01050"], Snapshot.Faces("01050", cards));
        Assert.Equal(["01097a", "01097b"], Snapshot.Faces("01097", cards));
        Assert.Equal(["01144a", "01144b", "01144c"], Snapshot.Faces("01144", cards));
        Assert.Equal(["01001a"], Snapshot.Faces("01001a", cards));
        Assert.Empty(Snapshot.Faces("99999", cards));
    }

    [Fact]
    public void EveryVendoredRulingMapsToTheGeneratedCardDataset()
    {
        Snapshot snapshot = Snapshot.Read(File.ReadAllText(
            RepositoryPaths.Dataset("marvelcdb-faq", "faq.json")));
        using var cardsJson = JsonDocument.Parse(File.ReadAllBytes(
            RepositoryPaths.Dataset("cards", "cards.json")));
        var cards = cardsJson.RootElement.GetProperty("cards").EnumerateArray()
            .Select(card => card.GetProperty("card_id").GetString()!)
            .ToHashSet(StringComparer.Ordinal);

        Assert.Empty(snapshot.Unmapped(cards));
        Assert.NotEmpty(snapshot.ByCard(cards));
        Assert.True(snapshot.WasAsked("01097b"));
        Assert.False(snapshot.WasAsked("99999"));
    }

    [Fact]
    public void OneResultAndSeveralResultsHaveDifferentAcceptedShapes()
    {
        Assert.Single(FaqHarvest.ParseEntries("{\"code\":\"01001a\"}"));
        Assert.Equal(2, FaqHarvest.ParseEntries(
            "[{\"code\":\"01001a\"},{\"code\":\"01135\"}]").Count);
        Assert.Empty(FaqHarvest.ParseEntries(" \n"));
    }

    [Fact]
    public void EveryRequestedCodeMustBeAccountedForEvenAtExitOne()
    {
        var runner = new FakeRunner(new CommandResult(
            1,
            "{\"code\":\"01001a\"}",
            "no FAQ entries for 01050\n"));

        BatchResult observed = FaqHarvest.ObserveBatch(runner, ["01001a", "01050"]);
        JsonElement entry = Assert.Single(observed.Entries);
        Assert.Equal("01001a", entry.GetProperty("code").GetString());
        Assert.Equal(
            [new QueryOutcome("01001a", "entry"), new QueryOutcome("01050", "none")],
            observed.Outcomes);
        Assert.DoesNotContain("-q", runner.Arguments);
    }

    [Fact]
    public void ACodeMissingFromBothStreamsFailsClosed()
    {
        var runner = new FakeRunner(new CommandResult(
            0,
            "{\"code\":\"01001a\"}",
            string.Empty));

        InvalidDataException error = Assert.Throws<InvalidDataException>(() =>
            FaqHarvest.Batch(runner, ["01001a", "01050"]));
        Assert.Contains("01050", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void AnUnrequestedEntryFailsClosed()
    {
        var runner = new FakeRunner(new CommandResult(
            0,
            "[{\"code\":\"01001a\"},{\"code\":\"99999\"}]",
            string.Empty));

        InvalidDataException error = Assert.Throws<InvalidDataException>(() =>
            FaqHarvest.Batch(runner, ["01001a"]));
        Assert.Contains("99999", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void APartialCandidateRecordsThatItCannotBePublished()
    {
        using var entry = JsonDocument.Parse("{\"code\":\"01001a\"}");
        var snapshot = new Snapshot(
            "2026-08-31",
            "marvelcdb v0.1.0",
            ["01001a"],
            [entry.RootElement.Clone()],
            CandidateComplete: false);

        Snapshot read = Snapshot.Read(snapshot.CandidateJson());
        Assert.False(read.CandidateComplete);
        Assert.DoesNotContain("candidate_complete", read.Json(), StringComparison.Ordinal);
    }

    [Fact]
    public void CardListingIncludesEncounterCardsAndReprints()
    {
        var runner = new FakeRunner(new CommandResult(0, "01002\n01001a\n01002\n", string.Empty));

        Assert.Equal(["01001a", "01002"], FaqHarvest.Codes(runner));
        Assert.Equal(
            ["cards", "list", "--encounter", "--duplicates", "-o", "ids"],
            runner.Arguments);
    }

    private sealed class FakeRunner(params CommandResult[] results) : ICommandRunner
    {
        private readonly Queue<CommandResult> results = new(results);

        public IReadOnlyList<string> Arguments { get; private set; } = [];

        public CommandResult Run(IReadOnlyList<string> arguments)
        {
            Arguments = arguments;
            return results.Dequeue();
        }
    }

    private static int RunTool(params string[] arguments)
    {
        var start = new ProcessStartInfo("dotnet")
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
        };
        start.ArgumentList.Add(typeof(QueryPin).Assembly.Location);
        foreach (string argument in arguments)
        {
            start.ArgumentList.Add(argument);
        }

        using Process process = Process.Start(start)!;
        process.WaitForExit();
        return process.ExitCode;
    }
}
