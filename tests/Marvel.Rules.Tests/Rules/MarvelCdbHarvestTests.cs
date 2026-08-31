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
