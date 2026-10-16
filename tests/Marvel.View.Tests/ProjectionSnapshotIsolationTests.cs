using System.Text.Json;
using Marvel.Rules.Events;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.View.Tests;

public sealed class ProjectionSnapshotIsolationTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void PromptCollectionsAreIsolatedInBothDirections(bool editExport)
    {
        // Synthetic contract fixture: snapshot isolation is an engine boundary choice,
        // independent of whether one printed ability uses every selection/payment shape.
        World world = VisibilityFixture.Board();
        int source = world.TheCardIn(DeckType.VillainArea)!.ObjectId;
        Prompt original = Offered(source);
        ViewScope scope = new PermissiveVisibilityPolicy().Authorize(null, world.Players);
        Prompt exported = WorldProjection.For(world, original, [], scope).Prompt!;
        string expected = JsonSerializer.Serialize(original);
        Assert.Equal(expected, JsonSerializer.Serialize(exported));
        Assert.True(exported.ExposesConcealedCandidates);

        CorruptCollections(editExport ? exported : original);

        Prompt unchanged = editExport ? original : exported;
        Assert.Equal(expected, JsonSerializer.Serialize(unchanged));
        Assert.True(unchanged.Affordances[0].Targets!.Allows([source]));
        Assert.False(unchanged.Affordances[0].Targets!.Allows([-999]));
        if (editExport)
        {
            Assert.Equal(expected, JsonSerializer.Serialize(
                WorldProjection.For(world, original, [], scope).Prompt));
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void EventOrderAndPublicTitlesAreIsolatedInBothDirections(bool editExport)
    {
        World world = VisibilityFixture.Board();
        int source = world.TheCardIn(DeckType.VillainArea)!.ObjectId;
        var reordered = new AreaReordered(AreaRef.Scenario(nameof(DeckType.VillainArea)), new[] { source });
        var shuffled = new CardsShuffledIntoDeck(0, 1, new List<string> { "Disclosed card" });
        VisibleResult visible = WorldProjection.For(world, null, [reordered, shuffled], ViewScope.None);
        var exportedOrder = Assert.IsType<AreaReordered>(visible.Events[0]);
        var exportedShuffle = Assert.IsType<CardsShuffledIntoDeck>(visible.Events[1]);

        ((IList<int>)(editExport ? exportedOrder : reordered).Order)[0] = -999;
        ((IList<string>)(editExport ? exportedShuffle : shuffled).PublicTitles)[0] = "Changed";

        Assert.Equal([source], (editExport ? reordered : exportedOrder).Order);
        Assert.Equal(["Disclosed card"], (editExport ? shuffled : exportedShuffle).PublicTitles);
    }

    private static Prompt Offered(int source)
    {
        var targets = new TargetRequest(new[] { source }, 1, 1,
            new IReadOnlyList<int>[] { new[] { source } }, new List<string> { "BRUTE" },
            MaximumOccurrences: new Dictionary<int, int> { [source] = 1 })
        {
            Details = new Dictionary<int, string> { [source] = "Known consequence" },
            ExclusiveSets = new IReadOnlyList<int>[] { new[] { source } },
        };
        var cost = new CostOption(source, "X", new List<string> { "p" }, "2", new List<string> { "m" },
            new[] { new ResourceSource(source, "w") { AbilityName = "Resource", CostDescription = "Exhaust" } },
            new[] { new VariableRequest("X", 1, 2) },
            new[] { new ResourceCost("X", new List<string> { "p" }, Printed: true) { RepeatedResource = 'p' } },
            DeclarationSensitive: true, PreferredResourceTypes: "p");
        var option = new Affordance(7, "Action", source, 0, "Synthetic", targets, new[] { cost })
        {
            Timing = AbilityType.Action,
            WindowChoices = new[] { new WindowChoice(source, "Recipient", "Description") { RecipientIds = new[] { source } } },
            DisplayLabel = "Display", CommitLabel = "Commit", CostDescription = "Exhaust source",
            DeferredTargetSelection = true, DirectRecipientCommit = true,
        };
        return new Prompt(0, Question.TurnOption, TimingPriority.Untimed, "Trigger", "Prompt", true, new[] { option })
        {
            ContextCardIds = new[] { source }, CauseCardIds = new[] { source },
            Description = "Situation", DisplayQuestion = "Intention?", ExposesConcealedCandidates = true,
        };
    }

    private static void CorruptCollections(Prompt prompt)
    {
        Affordance offer = prompt.Affordances[0];
        TargetRequest targets = offer.Targets!;
        int source = targets.Legal[0];
        ((IList<int>)targets.Legal)[0] = -999;
        ((IList<int>)targets.Groups![0])[0] = -999;
        ((IList<int>)targets.ExclusiveSets![0])[0] = -999;
        ((IList<string>)targets.MustIncludeTraits!)[0] = "Changed";
        ((IDictionary<int, int>)targets.MaximumOccurrences!)[source] = 0;
        ((IDictionary<int, string>)targets.Details!)[source] = "Changed";
        ((IList<IReadOnlyList<int>>)targets.Groups)[0] = new[] { -999 };
        ((IList<IReadOnlyList<int>>)targets.ExclusiveSets)[0] = new[] { -999 };
        CostOption cost = offer.CostOptions[0];
        ((IList<string>)cost.Rule!)[0] = "Changed";
        ((IList<string>)cost.OrRule!)[0] = "Changed";
        ((IList<ResourceSource>)cost.Sources!)[0] = new ResourceSource(-999, "");
        ((IList<VariableRequest>)cost.Variables!)[0] = new VariableRequest("Changed", 0, 0);
        ((IList<string>)cost.Components![0].Rule!)[0] = "Changed";
        ((IList<ResourceCost>)cost.Components)[0] = new ResourceCost("0");
        ((IList<CostOption>)offer.Costs!)[0] = new CostOption(-999, "0");
        ((IList<int>)offer.WindowChoices[0].RecipientIds)[0] = -999;
        ((IList<WindowChoice>)offer.WindowChoices)[0] = new WindowChoice(null, "Changed", null);
        ChangeOrRequireReadOnly(prompt.ContextCardIds);
        ChangeOrRequireReadOnly(prompt.CauseCardIds);
        ((IList<Affordance>)prompt.Affordances)[0] = offer with { Id = -999 };
    }

    private static void ChangeOrRequireReadOnly(IReadOnlyList<int> values)
    {
        var list = Assert.IsAssignableFrom<IList<int>>(values);
        try { list[0] = -999; }
        catch (NotSupportedException) { Assert.True(list.IsReadOnly); }
    }
}
