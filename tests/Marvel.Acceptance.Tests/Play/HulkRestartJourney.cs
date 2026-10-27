using System.Text.Json;
using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Server;
using Marvel.Session;
using Xunit;

namespace Marvel.Acceptance.Tests.Play;

/// <summary>A public session and its serialized fresh-host continuation.</summary>
internal sealed class HulkRestartJourney
{
    internal HulkRestartFactory Factory { get; } = new();
    internal MemorySessionStore Store { get; } = new();
    internal EngineHost Host { get; }
    internal EngineResponse Current { get; private set; }
    private readonly string capability;
    private readonly string gameId;

    internal HulkRestartJourney(GameSpecification specification)
    {
        Host = new EngineHost(Factory, store: Store);
        Current = Host.Exchange(EngineRequest.OpenGame("open", "hulk-restart", specification));
        Assert.Null(Current.Error);
        capability = Current.Capability!;
        gameId = Current.GameId;
    }

    private HulkRestartJourney(HulkRestartJourney original)
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

    internal HulkRestartJourney Restart() => new(this);

    internal Decision Next(HulkGamePolicy policy)
    {
        string before = Observation();
        var answer = policy.Answer(Factory.Game);
        Assert.Equal(before, Observation());
        return answer;
    }

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
        Assert.NotNull(rejected.Error);
        Assert.Equal(bytes, Stored());
        Assert.Equal(state, Observation());
    }

    internal void Equivalent(HulkRestartJourney other)
    {
        Assert.Equal(Observation(), other.Observation());
        Assert.Equal(SessionSaveJson.Write(Assert.Single(Store.Load()).Save),
            SessionSaveJson.Write(Assert.Single(other.Store.Load()).Save));
        Assert.Equal(Current.Revision, other.Current.Revision);
        Assert.Equal(JsonSerializer.Serialize(Current.World), JsonSerializer.Serialize(other.Current.World));
        Assert.Equal(JsonSerializer.Serialize(Current.History), JsonSerializer.Serialize(other.Current.History));
    }

    internal void Continue(HulkRestartJourney other, Decision answer)
    {
        Answer(answer);
        other.Answer(answer);
        Assert.Equal(JsonSerializer.Serialize(Current.Events), JsonSerializer.Serialize(other.Current.Events));
        Equivalent(other);
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
