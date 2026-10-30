using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Session;

namespace Marvel.Acceptance.Tests;

/// <summary>
/// The one prompt-spelling difference these research journals tolerate. The Power of
/// Justice (01062): "Double the number of resources this card generates while paying for
/// a Justice (yellow) card." Replay v29 records that as one {G} with a conditional {G};
/// the journals record the unconditional "GG". Only prompts are projected back to that
/// spelling; events, RNG and state fingerprints remain strict.
/// </summary>
internal static class FrozenPromptCompatibility
{
    public static void Require(PromptRecord recorded, Prompt actual, World world, string context) =>
        JournalReplay.RequirePromptRecords(recorded, Recorded(PromptRecord.From(actual), world), context);

    private static PromptRecord Recorded(PromptRecord prompt, World world) => prompt with
    {
        Affordances = [.. prompt.Affordances.Select(affordance => affordance with
        {
            Costs = [.. affordance.Costs.Select(cost => cost with
            {
                Sources = cost.Sources?.Select(source => Recorded(source, world)).ToList(),
            })],
        })],
    };

    private static ResourceSourceRecord Recorded(ResourceSourceRecord source, World world) =>
        source is { Generates: "G", ConditionalBonus.Generates: "G" }
            && world.Cards[source.Effect].FaceId == "01062"
            ? source with { Generates = "GG", ConditionalBonus = null }
            : source;
}
