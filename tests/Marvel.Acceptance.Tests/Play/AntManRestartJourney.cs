using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.Session;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>A public session and its serialized fresh-host continuation.</summary>
internal sealed class AntManRestartJourney
{
    internal HulkRestartFactory Factory { get; } = new();
    internal MemorySessionStore Store { get; } = new();
    internal EngineHost Host { get; }
    internal EngineResponse Current { get; private set; }
    private readonly string capability;
    private readonly string gameId;

    internal AntManRestartJourney(GameSpecification specification)
    {
        Host = new EngineHost(Factory, store: Store);
        Current = Host.Exchange(EngineRequest.OpenGame("open", "ant-man-restart", specification));
        Assert.Null(Current.Error);
        capability = Current.Capability!;
        gameId = Current.GameId;
    }

    private AntManRestartJourney(AntManRestartJourney original)
    {
        capability = original.capability;
        gameId = original.gameId;
        string bytes = original.Stored();
        var stored = Assert.Single(original.Store.Load());
        // Acceptance tests use the public strict game-save codec and independently
        // copy protected authority metadata; the server's internal envelope codec is private.
        var copied = new StoredSession(SessionSaveJson.Read(SessionSaveJson.Write(stored.Save)),
            JsonSerializer.Deserialize<StoredAuthority[]>(JsonSerializer.Serialize(stored.Authorities))!);
        Assert.Null(Store.Commit(copied));
        Host = new EngineHost(Factory, store: Store);
        Current = Host.Exchange(EngineRequest.SyncGame("restart", gameId, capability));
        Assert.Null(Current.Error);
        Assert.NotSame(original.Factory.Game, Factory.Game);
        Assert.NotSame(original.Factory.Game.State, Factory.Game.State);
        Assert.Equal(bytes, Stored());
        // Compare two sync snapshots: a resolve response additionally describes
        // transient faces from that response's events.
        original.Current = original.Host.Exchange(EngineRequest.SyncGame("control-sync", gameId, capability));
        Assert.Null(original.Current.Error);
        Assert.Equal(bytes, original.Stored());
        original.Equivalent(this);
    }

    internal AntManRestartJourney Restart() => new(this);

    internal void Answer(Decision answer)
    {
        Current = Host.Exchange(EngineRequest.ResolveGame("answer-" + Current.Revision,
            gameId, capability, answer, Current.Revision));
        Assert.Null(Current.Error);
    }

    internal void Reject(Decision answer, bool stale = false)
    {
        string bytes = Stored();
        string state = Observation();
        var rejected = Host.Exchange(EngineRequest.ResolveGame("rejected-" + stale,
            gameId, capability, answer, Current.Revision - (stale ? 1 : 0)));
        Assert.Equal(stale ? "stale_decision" : "invalid_decision", rejected.Error?.Code);
        Assert.Equal(bytes, Stored());
        Assert.Equal(state, Observation());
    }

    internal void Equivalent(AntManRestartJourney other)
    {
        Assert.Equal(Observation(), other.Observation());
        Assert.Equal(SessionSaveJson.Write(Assert.Single(Store.Load()).Save),
            SessionSaveJson.Write(Assert.Single(other.Store.Load()).Save));
        Assert.Equal(Current.Revision, other.Current.Revision);
        Assert.Equal(JsonSerializer.Serialize(Current.World), JsonSerializer.Serialize(other.Current.World));
        Assert.Equal(JsonSerializer.Serialize(Current.History), JsonSerializer.Serialize(other.Current.History));
    }

    internal void Continue(AntManRestartJourney other, Decision answer)
    {
        Answer(answer);
        other.Answer(answer);
        Assert.Equal(JsonSerializer.Serialize(Current.Events), JsonSerializer.Serialize(other.Current.Events));
        Equivalent(other);
    }

    internal void EquivalentState(AntManRestartJourney other) => Assert.Equal(Observation(), other.Observation());

    internal void UndoRedo()
    {
        var saved = Assert.Single(Store.Load()).Save;
        int cursor = saved.Cursor;
        string before = Observation();
        var completedStep = saved.Units[^1].Decisions[^1];
        var undo = Host.Exchange(EngineRequest.UndoGame("undo", gameId, capability, cursor - 1, Current.Revision));
        Assert.Null(undo.Error);
        Assert.NotEqual(before, Observation());
        Assert.Equal("12001b", Factory.Game.State.Seats[0].IdentityCard.FaceId);
        Assert.Equal(cursor - 1, Assert.Single(Store.Load()).Save.Cursor);
        var redo = Host.Exchange(EngineRequest.RedoGame("redo", gameId, capability, cursor, undo.Revision));
        Assert.Null(redo.Error);
        Current = redo;
        Assert.Equal(before, Observation());
        var redoneStep = Assert.Single(Store.Load()).Save.Units[^1].Decisions[^1];
        Assert.Equal(completedStep.Events.Select(item => item.GetRawText()), redoneStep.Events.Select(item => item.GetRawText()));
        Assert.Equal(completedStep.RngWords, redoneStep.RngWords);
        Assert.Equal(completedStep.StateFingerprint, redoneStep.StateFingerprint);
    }

    internal string Stored()
    {
        var stored = Assert.Single(Store.Load());
        return JsonSerializer.Serialize(new
        {
            Save = SessionSaveJson.Write(stored.Save), stored.Authorities,
        });
    }

    private string Observation()
    {
        var game = Factory.Game;
        return JsonSerializer.Serialize(new
        {
            State = game.State.Digest().Canonical(), Rng = game.State.Random.Generator.GetState(),
            game.State.Random.Generator.WordsConsumed, game.Phase, game.Round, game.State.Result,
            Prompt = game.Pending is { } prompt ? PromptRecord.From(prompt) : null,
            Agenda = game.State.Agenda.Outstanding.ToArray(),
            Effects = game.State.Effects.Registered.ToArray(),
        });
    }
}
