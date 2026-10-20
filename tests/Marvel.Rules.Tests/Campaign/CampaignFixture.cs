using System.Collections.Immutable;
using Marvel.Rules.Campaign;
using Marvel.Rules.Setup;

namespace Marvel.Rules.Tests.Campaign;

// Synthetic composition isolates campaign rules; it does not admit a playable expansion.
internal static class CampaignFixture
{
    internal static readonly CampaignDefinition Definition = new("trors",
        [.. Enumerable.Range(0, 5).Select(index => new CampaignScenario($"scenario{index}", $"scenario{index}_expert"))],
        [.. Enumerable.Range(0, 4).Select(index => new CampaignCard($"tech{index}", $"tech-face{index}", CampaignCardRole.Tech))],
        [.. Enumerable.Range(0, 4).Select(index => new CampaignCondition(
            new CampaignCard($"condition{index}", $"basic{index}", CampaignCardRole.Condition), $"improved{index}", index + 1))],
        ["obligation0", "obligation1", "obligation2", "obligation3"], ["weapon0", "weapon1"], ["captive0"]);

    internal static CampaignState Create(bool expert = false, int seats = 1) => CampaignRules.Create(
        Definition, [.. Enumerable.Range(0, seats).Select(index => new CampaignRosterEntry(
            $"hero{index}", new PlayerDeckRecipe(["ordinary", "signature"]), 10))], expert, 42);

    internal static CampaignAttemptOutcome Outcome(CampaignState state, bool won = true) => new(won,
        [.. state.Players.Select((player, seat) => new CampaignSeatOutcome(seat, false, true, 7, 10, false, []))],
        ["weapon0"], 3, false, []);

    internal static CampaignState Win(CampaignState state)
    {
        state = CampaignRules.Start(state);
        state = CampaignOutcomeRules.Finish(state, Definition, Outcome(state));
        while (CampaignRewardRules.Choice(state, Definition) is { } choice)
            state = CampaignRewardRules.Choose(state, Definition, choice.Seat, choice.Options[0].Key);
        return state;
    }
}
