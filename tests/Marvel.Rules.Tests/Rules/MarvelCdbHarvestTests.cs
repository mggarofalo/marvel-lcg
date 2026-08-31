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

        JsonElement entry = Assert.Single(FaqHarvest.Batch(runner, ["01001a", "01050"]));
        Assert.Equal("01001a", entry.GetProperty("code").GetString());
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
}
