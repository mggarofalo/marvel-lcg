using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Session;
using Marvel.Sim;
using Xunit;

namespace Marvel.Acceptance.Tests;

/// <summary>
/// The in-memory journal of a current legal game. Its facts are recorded from the
/// current engine and replayed independently; no historical archive is rewritten.
/// </summary>
internal sealed class CurrentLegalJournal
{
    private readonly List<JournalStep> steps = [];

    public int Count => steps.Count;

    public void Record(Game game, Prompt prompt, DurableDecision decision, IReadOnlyList<GameEvent> events) =>
        steps.Add(new JournalStep(PromptRecord.From(prompt), decision,
            events.Select(JournalJson.Event).ToList(),
            game.State.Random.Generator.WordsConsumed, game.State.Digest().Fingerprint()));

    /// <summary>
    /// Finishes a game whose recorded intentions no longer apply with the acting
    /// policy, seeded from the recording's policy seed. The continuation is a
    /// generated current game, not a derivation of the historical choices.
    /// </summary>
    public void CompleteWithActingPolicy(Game game, string root, uint policySeed, int decisionLimit)
    {
        var policy = new ActingPolicy(SimulationData.Load(root).Cards,
            SimulationHarnessSupport.SeatPolicySeeds(policySeed, game.State.Seats.Count));
        while (game.Pending is { } prompt)
        {
            Assert.True(steps.Count < decisionLimit, "the continuation exceeds the recorded decision limit");
            var answer = policy.Answer(game);
            var durable = DurableDecision.From(DurableDecision.SimulationActor(prompt, answer), prompt, answer);
            var result = game.Resolve(durable.Resolve(prompt));
            Record(game, prompt, durable, result.Events);
        }
    }

    /// <summary>Replays every recorded prompt, event, RNG count and fingerprint from a fresh game.</summary>
    public void Replay(Game replay)
    {
        foreach (var step in steps)
        {
            JournalReplay.RequirePrompt(step.Prompt, replay.Pending!, "current replay prompt");
            var result = replay.Resolve(step.Decision.Resolve(replay.Pending!));
            JournalReplay.RequireEvents(step.Events, result.Events, "current replay events");
            JournalReplay.RequireRng(step.RngWords, replay.State.Random.Generator.WordsConsumed, "current replay RNG");
            JournalReplay.RequireFingerprint(step.StateFingerprint, replay.State.Digest().Fingerprint(), "current replay state");
        }
        Assert.Null(replay.Pending);
    }
}
