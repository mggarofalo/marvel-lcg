using Marvel.Rules.Play;
using Marvel.Session;
using Xunit;

namespace Marvel.Server.Tests;

internal static class FrozenReplayPrefixAssertions
{
    internal static Game Before(SessionSave save, DatasetGameFactory factory, int unit, int decision)
    {
        var opened = ReplayContractUpgradeTests.Open(factory, save.Setup);
        var game = opened.Game;
        JournalReplay.RequireEvents(save.Initial.Events, opened.SetupEvents, "initial events");
        JournalReplay.RequireRng(save.Initial.RngWords, game.State.Random.Generator.WordsConsumed, "initial RNG");
        JournalReplay.RequireFingerprint(save.Initial.StateDigest, game.State.Digest().Canonical(), "initial state");
        for (int u = 0; u <= unit; u++)
        {
            int count = u == unit ? decision : save.Units[u].Decisions.Count;
            for (int d = 0; d < count; d++) RequireStep(game, save.Units[u].Decisions[d]);
        }
        JournalReplay.RequirePrompt(save.Units[unit].Decisions[decision].Prompt, game.Pending!, "divergence prompt");
        return game;
    }

    private static void RequireStep(Game game, JournalStep step)
    {
        JournalReplay.RequirePrompt(step.Prompt, game.Pending!, "prefix prompt");
        var result = game.Resolve(step.Decision.Resolve(game.Pending!));
        JournalReplay.RequireEvents(step.Events, result.Events, "prefix events");
        JournalReplay.RequireRng(step.RngWords, game.State.Random.Generator.WordsConsumed, "prefix RNG");
        JournalReplay.RequireFingerprint(step.StateFingerprint, SessionReplay.Fingerprint(game), "prefix state");
        Assert.Equal(step.Result, SessionReplay.Result(game));
    }
}
