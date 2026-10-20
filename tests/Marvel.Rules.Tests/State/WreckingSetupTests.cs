using Marvel.Core.Random;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Rules.Tests.State;

/// <summary>Runtime setup contract from pack:mc03:additional-setup and adjustable-difficulty.</summary>
public sealed class WreckingSetupTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SetupPlacesFourGroupsAndShufflesEachFifteenCardDeckInRecipeOrder(bool extreme)
    {
        var fixture = new WreckingSetupFixture(extreme);
        var recorder = new Recorder();
        var world = WorldSetup.DealConfigured(fixture.Facts, fixture.Cards, ["Player"], 8123,
            recorder, new(EncounterGroups: fixture.Plan));
        Assert.Equal(fixture.Cards.Count, world.Cards.Count);
        Assert.Equal(4, world.AreaOf(DeckType.VillainArea).Cards.Count);
        Assert.Equal(4, world.AreaOf(DeckType.SideSchemesArea).Cards.Count);
        Assert.Equal(extreme ? 4 : 0, world.AreaOf(DeckType.VillainDeck).Cards.Count);
        Assert.Equal(fixture.Plan.ActiveVillain, EncounterRouting.Villain(world)!.ObjectId);
        var expectedRandom = new EngineRandom(8123);
        foreach (var group in world.EncounterTopology!.Groups)
        {
            var expected = group.Members.Select(card => card.ObjectId).ToList();
            expectedRandom.Shuffle(expected);
            Assert.Equal(expected, group.Deck.Cards.Select(card => card.ObjectId));
            Assert.Empty(group.Discard.Cards);
            Assert.Equal(world.Facts.PrintedValue(group.Scheme.FaceId, "StartingThreat", 1),
                group.Scheme.Tokens["k_threat"]);
        }
        Assert.Equal(expectedRandom.Generator.WordsConsumed, world.Random.Generator.WordsConsumed);
        Assert.Equal(["villain-0-a", "villain-1-a", "villain-2-a", "villain-3-a"],
            recorder.Setups.Where(face => face.StartsWith("villain", StringComparison.Ordinal)));
        Assert.Equal(["villain-0-a", "villain-1-a", "villain-2-a", "villain-3-a"],
            recorder.Reveals.Where(face => face.StartsWith("villain", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData(SetupSlot.Obligation)]
    [InlineData(SetupSlot.Nemesis)]
    public void ExcludedNemesisAndObligationBlueprintsFailBeforeAnyAbilityRuns(SetupSlot slot)
    {
        var fixture = new WreckingSetupFixture(false);
        fixture.Cards.Add(new("excluded", slot, 0));
        var recorder = new Recorder();
        Assert.Throws<ArgumentException>(() => WorldSetup.DealConfigured(
            fixture.Facts, fixture.Cards, ["Player"], 0, recorder, new(EncounterGroups: fixture.Plan)));
        Assert.Empty(recorder.Setups);
        Assert.Empty(recorder.Reveals);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(999)]
    public void OutOfRangeComponentIdsFailBeforeAnyAbilityRuns(int invalid)
    {
        var fixture = new WreckingSetupFixture(false);
        var groups = fixture.Plan.Groups.ToArray();
        groups[0] = groups[0] with { Scheme = invalid };
        var recorder = new Recorder();
        Assert.Throws<ArgumentException>(() => WorldSetup.DealConfigured(
            fixture.Facts, fixture.Cards, ["Player"], 0, recorder,
            new(EncounterGroups: fixture.Plan with { Groups = groups })));
        Assert.Empty(recorder.Setups);
    }

    [Fact]
    public void DuplicatedMembershipAndMixedExtremeStagesAreRejected()
    {
        var fixture = new WreckingSetupFixture(false);
        var groups = fixture.Plan.Groups.ToArray();
        groups[1] = groups[1] with { Encounters = groups[0].Encounters };
        Assert.Throws<ArgumentException>(() => WorldSetup.DealConfigured(
            fixture.Facts, fixture.Cards, ["Player"], 0, new NoCardAbilities(),
            new(EncounterGroups: fixture.Plan with { Groups = groups })));
        var extreme = new WreckingSetupFixture(true);
        groups = extreme.Plan.Groups.ToArray();
        groups[0] = groups[0] with { Stages = [groups[0].Stages[0]] };
        Assert.Throws<ArgumentException>(() => WorldSetup.DealConfigured(
            extreme.Facts, extreme.Cards, ["Player"], 0, new NoCardAbilities(),
            new(EncounterGroups: extreme.Plan with { Groups = groups })));
    }

    private sealed class Recorder : NoCardAbilities
    {
        internal List<string> Setups { get; } = [];
        internal List<string> Reveals { get; } = [];
        public override AbilityExecutionResult Setup(World world, AbilityExecution entry, Card card)
        {
            Setups.Add(card.FaceId);
            return base.Setup(world, entry, card);
        }
        public override AbilityExecutionResult WhenRevealed(World world, AbilityExecution entry, Card card, int player)
        {
            Reveals.Add(card.FaceId);
            return base.WhenRevealed(world, entry, card, player);
        }
    }
}
