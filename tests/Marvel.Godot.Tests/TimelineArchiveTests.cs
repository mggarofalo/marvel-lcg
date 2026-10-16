using Marvel.Protocol;
using Marvel.Godot;
using Marvel.Server;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class TimelineArchiveTests
{
    private static readonly EventPresentation Draw = new("Spider-Man drew 3 cards.", "Nick Fury", [], EventMotionKind.HandGain);

    [Fact]
    public void RepeatedResponseDoesNotDuplicateAndUndoRemovesOnlyTheDiscardedSuffix()
    {
        var archive = new TimelineArchive();
        archive.Record(Response(1, 1), [Draw], false);
        archive.Record(Response(1, 1), [Draw], false);
        archive.Record(Response(2, 2), [Draw with { Summary = "Later effect" }], false);
        Assert.Equal(2, archive.Entries.Count);
        archive.Record(Response(3, 1), [], true);
        Assert.Equal(Draw, Assert.Single(archive.Entries).Event);
    }

    [Fact]
    public void RestoredSummariesAreDistinctAndUseTheActionEndForUndo()
    {
        var archive = new TimelineArchive();
        var response = Response(4, 4) with
        {
            History = new HistoryDescriptor(4, [], [],
                [new(0, "First action", []), new(2, "Second action", [])], false),
        };
        archive.Record(response, [], true);
        Assert.Equal([2, 4], archive.Entries.Select(entry => entry.Cursor));
        Assert.All(archive.Entries, entry => Assert.Equal("Earlier action summary", entry.Event.Cause));
        archive.Record(Response(5, 2), [], true);
        Assert.Equal("First action", Assert.Single(archive.Entries).Event.Summary);
        archive.Record(response with { Revision = 6 }, [], true);
        Assert.Equal(["First action", "Second action"], archive.Entries.Select(entry => entry.Event.Summary));
    }

    [Fact]
    public void NewCapabilityClearsSameGamePrivateHistoryEvenAtTheSameRevision()
    {
        var archive = new TimelineArchive();
        archive.Record(Response(1, 1), [Draw], false);
        archive.Clear();
        archive.Record(Response(1, 1), [], true);
        Assert.Empty(archive.Entries);
        archive.Record(Response(2, 2), [Draw], false);
        archive.Record(Response(2, 0) with { GameId = "different" }, [], true);
        Assert.Empty(archive.Entries);
    }

    private static EngineResponse Response(long revision, int cursor) =>
        new(EngineProtocol.Version, "test", "game", null, null, [])
        { Revision = revision, History = new HistoryDescriptor(cursor, [], [], [], false) };

    [Fact]
    public void AuthoritativeRewriteAtTheSameCursorReplacesOutdatedLocalConsequences()
    {
        var archive = new TimelineArchive();
        var first = Response(1, 1) with
        { History = new HistoryDescriptor(1, [], [], [new(0, "First action", ["Old consequence"])], false) };
        archive.Record(first, [Draw], false);
        var rewrite = Response(2, 1) with
        { History = new HistoryDescriptor(1, [], [], [new(0, "Replacement action", ["New consequence"])], false) };
        archive.Record(rewrite, [], true);
        Assert.Equal("Replacement action", Assert.Single(archive.Entries).Event.Summary);
    }
}
