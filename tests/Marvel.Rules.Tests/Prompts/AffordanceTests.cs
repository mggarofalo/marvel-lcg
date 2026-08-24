using System.Text.Json;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Xunit;

namespace Marvel.Rules.Tests.Prompts;

/// <summary>
/// The affordance shape holds what the engine actually has to say at a prompt.
/// </summary>
/// <remarks>
/// The numbers here come from <c>py_src/tools/affordances/census.py</c> over 30
/// games, 1,997 prompts and 6,351 options — see <c>docs/affordances.md</c>. The
/// tests are about shape rather than about the measurement, but the measurement
/// is why the shape is this size.
/// </remarks>
public sealed class AffordanceTests
{
    private static Affordance Play(int id = 49, int anchor = 37) =>
        new(id, "Play", anchor, AnchorPlayer: 0, Label: "Play Enhanced Spider-Sense");

    [Fact]
    public void AnAffordanceNamesTheObjectThePlayerClicks()
    {
        // The whole point of the type. A list of option strings could say
        // "Play Enhanced Spider-Sense"; only this can say which card that is.
        Assert.Equal(37, Play().AnchorId);
    }

    [Fact]
    public void NoTargetsAndNoCostsAreTheCommonCase()
    {
        var affordance = Play();
        Assert.Null(affordance.Targets);
        Assert.Empty(affordance.CostOptions);
        Assert.True(affordance.IsLegal);
    }

    [Fact]
    public void AnIllegalAffordanceCarriesItsReason()
    {
        // The engine offers what cannot be taken rather than omitting it, so a
        // client can grey the card out and say why. "Pay cost, need 3, but only
        // have 2" beats a card that is silently not clickable.
        var affordance = Play() with { Illegal = "pay cost, need 3, but only have 2" };

        Assert.False(affordance.IsLegal);
        Assert.Contains("need 3", affordance.Illegal, StringComparison.Ordinal);
    }

    [Fact]
    public void AGroupedSelectionIsAuthoritativeOverTheFlatList()
    {
        // `VillainAndMinionsEngagedWithYou` pools every player's minions but
        // accepts one villain plus one player's whole group. A client obeying
        // the flat list and the count would build an illegal selection.
        var targets = new TargetRequest(
            Legal: [49, 51, 52, 60],
            Min: 2,
            Max: 3,
            Groups: [[49, 51, 52], [49, 60]],
            Rule: "VillainAndMinionsEngagedWithYou");

        Assert.True(targets.IsGrouped);
        Assert.All(targets.Groups!, group =>
            Assert.Subset(new HashSet<int>(targets.Legal), new HashSet<int>(group)));
    }

    [Fact]
    public void AnUngroupedRequestSaysSo()
    {
        Assert.False(new TargetRequest([1, 2], Min: 1, Max: 1).IsGrouped);
    }

    [Fact]
    public void GenerationIsPluralAndIsNotThePayment()
    {
        // A cost of 3 with six generators is not "spend 3"; it is a choice of
        // which resources to generate, and only then a single consumption.
        // 22.1% of priced affordances offer five ways to pay and 21.6% offer
        // six, so this is the normal case rather than a corner.
        var cost = new CostOption(
            Target: 0,
            Cost: "3",
            Sources:
            [
                new ResourceSource(1, "B"),
                new ResourceSource(3, "R"),
                new ResourceSource(38, "YY"),
                new ResourceSource(41, "B"),
                new ResourceSource(42, "R"),
                new ResourceSource(43, "Y"),
            ]);

        Assert.Equal(6, cost.Generators.Count);
        // Seven resources available against a cost of three. The engine cannot
        // pick for the player, and the total is not the answer either.
        Assert.Equal(7, cost.Generators.Sum(source => source.Generates.Length));
        Assert.False(cost.HasAlternative);
    }

    [Fact]
    public void AnAlternativeCostIsAdditiveNotAReplacement()
    {
        // Flattening "a mental resource or two of any type" to a bare "2" is
        // what corrupted a corpus in MARVEL-158: the payer met the number with
        // the wrong types and the ability failed mid-resolution.
        var cost = new CostOption(0, "1", Rule: ["mental"], OrCost: "2", OrRule: []);

        Assert.True(cost.HasAlternative);
        Assert.Equal("1", cost.Cost);
        Assert.Equal(["mental"], cost.Rule);
    }

    [Fact]
    public void APromptSaysWhetherDecliningIsLegal()
    {
        // 34.8% of prompts offer exactly one affordance and 81% are
        // cancellable. Without this a client cannot tell "your only move" from
        // "your only move, or pass".
        var prompt = new Prompt(0, PromptKind.Normal, "WhenPlayerInTurn",
                                "Spider-Man's turn", Cancellable: true, [Play()]);

        Assert.Single(prompt.Affordances);
        Assert.True(prompt.Cancellable);
    }

    [Theory]
    [InlineData(PromptKind.Normal)]
    [InlineData(PromptKind.Response)]
    [InlineData(PromptKind.Interrupt)]
    [InlineData(PromptKind.ForcedInterrupt)]
    public void EveryPromptKindIsOneTheEngineProduces(PromptKind kind)
    {
        // All four were observed in the census. None is speculative.
        Assert.True(Enum.IsDefined(kind));
    }

    [Fact]
    public void ThePromptTriggerIsTheSameStringTheEventStreamCarries()
    {
        // So a client can tie an event to the decision it came out of.
        const string Trigger = "WhenUnitBeingAttack";
        var prompt = new Prompt(0, PromptKind.Interrupt, Trigger, "Defend?",
                                Cancellable: true, [Play()]);
        var moved = new CardsMoved(AreaRef.Player("HandsArea", 0),
                                   AreaRef.Player("DiscardPile", 0),
                                   [new Landing(7, 0)]) { Trigger = Trigger };

        Assert.Equal(prompt.Trigger, moved.Trigger);
    }

    [Fact]
    public void APromptRoundTripsThroughTheWire()
    {
        var prompt = new Prompt(
            1, PromptKind.Response, "AfterCardEnterPlay", "Respond?",
            Cancellable: true,
            [
                Play() with
                {
                    Targets = new TargetRequest([49], Min: 1, Max: 1),
                    Costs = [new CostOption(0, "3", Sources: [new ResourceSource(1, "B")])],
                },
            ]);

        string json = JsonSerializer.Serialize(prompt, EventJson.Options);
        var restored = JsonSerializer.Deserialize<Prompt>(json, EventJson.Options);

        Assert.NotNull(restored);
        Assert.Equal(json, JsonSerializer.Serialize(restored, EventJson.Options));
        Assert.Equal(37, restored.Affordances[0].AnchorId);
        Assert.Equal("B", restored.Affordances[0].CostOptions[0].Generators[0].Generates);
    }
}
