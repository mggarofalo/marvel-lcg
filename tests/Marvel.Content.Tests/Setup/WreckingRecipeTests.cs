using Marvel.Content.Setup;
using Marvel.Core.Random;
using Marvel.Rules.Play;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class WreckingRecipeTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
    private static readonly string[] Heroes = ["spider_man", "captain_marvel", "she_hulk", "iron_man"];

    public static IEnumerable<object[]> Modes() => Enumerable.Range(0, 16).Select(mode => new object[] { mode, false })
        .Append([0, true]);

    public static IEnumerable<object[]> ModePlayers() => Modes().SelectMany(mode => Enumerable.Range(1, 4)
        .Select(players => new object[] { players, mode[0], mode[1] }));

    [Theory]
    [MemberData(nameof(Modes))]
    public void EveryCustomCombinationAndExtremeUsesExactlyItsPrintedGroupComponents(int versions, bool extreme)
    {
        // pack:mc03:adjustable-difficulty: "some version-A and some version-B villains";
        // Extreme starts with A and "each villain's version B under it".
        var setup = WreckingRecipeFixture.Catalog(versions, extreme);
        var plan = ScenarioDealer.Prepare(setup, new("wrecking_fixture", [Heroes[0]]), Cards);
        var groups = Assert.IsType<EncounterSetupPlan>(plan.EncounterGroups);
        Assert.Equal(4, groups.Groups.Count);
        Assert.Equal(groups.Groups[0].Stages[0], groups.ActiveVillain);
        Assert.Equal(extreme ? 115 : 111, plan.Blueprints.Count);
        Assert.DoesNotContain(plan.Blueprints, card => card.Slot is SetupSlot.Obligation or SetupSlot.Nemesis);
        for (int index = 0; index < 4; index++)
        {
            var group = groups.Groups[index];
            string[] stages = extreme ? WreckingRecipeFixture.Stages[index] : [WreckingRecipeFixture.Stages[index][(versions >> index) & 1]];
            Assert.Equal(stages, group.Stages.Select(id => plan.Blueprints[id].Spec));
            Assert.Equal(WreckingRecipeFixture.Schemes[index], plan.Blueprints[group.Scheme].Spec);
            Assert.Equal(WreckingRecipeFixture.Decks[index], group.Encounters.Select(id => plan.Blueprints[id].Spec));
        }
    }

    [Theory]
    [MemberData(nameof(ModePlayers))]
    [Rule("rr:appendix-ii-setup.step.9")]
    public void RealPrintedSetupKeepsHealthThreatAndIndependentShufflesCorrectAtEveryPlayerCount(int players, int versions, bool extreme)
    {
        // pack:mc03:additional-setup: "Each villain ... has its own encounter deck of 15 cards";
        // "Nemesis cards and obligations are not used"; "Wrecker should have the active villain counter".
        // "Set the villain's hit point dial to the value indicated by the villain card."
        var setup = WreckingRecipeFixture.Catalog(versions, extreme);
        var plan = ScenarioDealer.Prepare(setup, new("wrecking_fixture", Heroes.Take(players).ToArray()), Cards);
        var world = WorldSetup.DealConfigured(Cards, plan.Blueprints, Heroes.Take(players).ToArray(), 8123,
            new NoCardAbilities(), new(Expert: versions == 15, EncounterGroups: plan.EncounterGroups));
        var topology = Assert.IsType<EncounterTopology>(world.EncounterTopology);
        Assert.Equal(extreme || (versions & 1) == 0 ? "07002" : "07003", topology.Active.CurrentVillain!.FaceId);
        var random = new EngineRandom(8123);
        for (int seat = 0; seat < players; seat++) random.Shuffle(Enumerable.Range(0, 40).ToList());
        int[][] health = [[14, 18], [13, 16], [11, 14], [12, 15]];
        int[] threat = [6, 5, 3, 4];
        for (int index = 0; index < 4; index++)
        {
            var group = topology.Groups[index];
            int version = extreme ? 0 : (versions >> index) & 1;
            Assert.Equal(health[index][version] * players, CardValues.MaximumHealth(world, group.Villain, Cards).CurrentValue);
            Assert.Equal(threat[index], group.Scheme.Tokens["k_threat"]);
            var shuffled = plan.EncounterGroups!.Groups[index].Encounters.ToList();
            random.Shuffle(shuffled);
            Assert.Equal(shuffled, group.Deck.Cards.Select(card => card.ObjectId));
            Assert.Empty(group.Discard.Cards);
        }
        Assert.Equal(random.Generator.WordsConsumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(4, topology.Groups.Select(group => group.Deck.Id).Distinct().Count());
        Assert.Equal(4, topology.Groups.Select(group => group.Discard.Id).Distinct().Count());
    }

    [Theory]
    [InlineData("rhino")]
    [InlineData("ultron_expert")]
    [InlineData("crossbones")]
    [InlineData("red_skull_expert")]
    public void OrdinaryRecipesKeepTheirExactBlueprintAllocation(string scenario)
    {
        var setup = WreckingRecipeFixture.Catalog();
        var original = Blueprints.From(Dealer.DealOrder(setup, scenario, [Heroes[0]], facts: Cards), Cards);
        var plan = ScenarioDealer.Prepare(setup, new(scenario, [Heroes[0]]), Cards);
        Assert.Null(plan.EncounterGroups);
        Assert.Equal(original, plan.Blueprints);
        Assert.Contains(plan.Blueprints, card => card.Slot == SetupSlot.Obligation);
        Assert.Contains(plan.Blueprints, card => card.Slot == SetupSlot.Nemesis);
    }
}
