using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.View.Tests;

// Authorized descriptor fixtures test source copy identity and state formatting.
public sealed class AffordanceSourceStateTests
{
    [Fact]
    public void DuplicateSupportActionsRetainTheirExactSourceCopyAndMandatoryCosts()
    {
        var world = Board("SupportsArea", [Card(7, "Surveillance Team", CardKind.Support),
            Card(8, "Surveillance Team", CardKind.Support)]);
        var prompt = new Prompt(0, Question.TurnOption, TimingPriority.Untimed, "Turn", "Turn", true,
            [Offer(7), Offer(8)]);

        AffordancePresentation[] choices = [.. PromptPresentation.From(prompt, world).Affordances];

        Assert.Equal([7, 8], choices.Select(choice => choice.CardAnchorId));
        Assert.Equal(["Surveillance Team (copy 1 of 2)", "Surveillance Team (copy 2 of 2)"],
            choices.Select(choice => choice.SourceName));
        Assert.All(choices, choice => Assert.Equal("Exhaust Surveillance Team; Remove 1 snoop counter", choice.CostDescription));
        Assert.All(choices, choice => Assert.Contains("3 snoop counters", choice.SourceState));
    }

    [Fact]
    public void OneHitPointAllyExposesCurrentHealthBeforeThePlayerChoosesItsConsequentialPower()
    {
        var world = Board("AlliesArea", [Card(7, "Jessica Jones", CardKind.Ally, health: 1)]);
        var prompt = new Prompt(0, Question.TurnOption, TimingPriority.Untimed, "Turn", "Turn", true,
            [new Affordance(7, BasicPowers.ThwartVerb, 7, 0, BasicPowers.ThwartVerb)
                { AnchorKind = AffordanceAnchorKind.Card, Description = "Take 1 consequential damage after thwarting" }]);

        AffordancePresentation choice = Assert.Single(PromptPresentation.From(prompt, world).Affordances);

        Assert.Contains("HP 1", choice.SourceState);
        Assert.Contains("1 consequential damage", choice.Description);
        Assert.DoesNotContain("defeated", choice.SourceState);
    }

    private static Affordance Offer(int id) => new(id, "Action", id, 0, "Action")
    {
        AnchorKind = AffordanceAnchorKind.Card,
        CostDescription = "Exhaust Surveillance Team; Remove 1 snoop counter",
        Description = "Remove 1 threat from a scheme",
    };

    private static WorldDescriptor Board(string zone, IReadOnlyList<CardDescriptor> cards) => new(
        [new PlayerDescriptor(0, "Spider-Man", false)],
        [new AreaDescriptor(3, zone, 0, -1, cards, [])], [], Outcome.Unfinished);

    private static CardDescriptor Card(int id, string title, CardKind kind, long health = 0) => new(
        id, CardBack.Player, true, true, -1,
        new CardFaceDescriptor("source", title, "", kind,
            new Dictionary<string, long>(StringComparer.Ordinal) { ["health"] = health })
        { Counters = new Dictionary<string, long>(StringComparer.Ordinal) { ["snoop"] = 3 } })
    {
        Location = new CardLocationDescriptor(3, "SupportsArea", 0, -1),
        State = new CardStateDescriptor(true, 0, null,
            new Dictionary<string, long>(StringComparer.Ordinal) { ["snoop"] = 3 },
            new Dictionary<string, long>(StringComparer.Ordinal) { ["health"] = health }, []),
    };
}
