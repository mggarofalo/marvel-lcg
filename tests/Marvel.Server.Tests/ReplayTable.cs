using Marvel.Protocol;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Server.Tests;

/// <summary>One seat's view of a hosted game, advanced by a crude deterministic policy.</summary>
internal sealed class ReplayTable(EngineHost host, EngineResponse opened)
{
    private const int DecisionLimit = 200;

    public EngineResponse Current { get; private set; } = opened;

    public string Capability { get; } = opened.Capability!;

    public int Accepted { get; private set; }

    public static ReplayTable Open(EngineHost host, string gameId)
    {
        EngineResponse opened = host.Exchange(EngineRequest.OpenGame(
            "open", gameId,
            new GameSpecification("rhino", ["spider_man"], [], Seed: 12345)));
        Assert.Null(opened.Error);
        return new ReplayTable(host, opened);
    }

    public void Continue(EngineResponse response) => Current = response;

    public EngineResponse Sync(string requestId) =>
        host.Exchange(EngineRequest.SyncGame(requestId, Current.GameId, Capability));

    /// <summary>Answers with the first candidate the host accepts.</summary>
    public void Accept()
    {
        Assert.True(Accepted < DecisionLimit, $"still playing after {Accepted} decisions");
        EngineResponse response = Attempt();
        Assert.Null(response.Error);
        Current = response;
        Accepted++;
    }

    /// <summary>The first response that accepts a candidate or fails to save it.</summary>
    public EngineResponse Attempt()
    {
        Prompt prompt = Current.Prompt ?? throw new InvalidOperationException("the game is over");
        EngineResponse? last = null;
        foreach (Decision decision in CrudePolicy.Candidates(prompt))
        {
            last = host.Exchange(EngineRequest.ResolveGame(
                "step-" + Accepted, Current.GameId, Capability, decision, Current.Revision));
            if (last.Error is null || last.Error.Code == "save_failed")
            {
                return last;
            }
        }

        throw new InvalidOperationException(
            $"no candidate answer was accepted at decision {Accepted}: {last?.Error?.Code}");
    }
}
