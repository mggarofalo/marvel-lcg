using Marvel.Cards.Run;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Arranged topology contracts, independent of full pack admission.</summary>
public sealed class WreckingContinuationReferenceTests : CoreCardTestBase
{
    [Rule("rr:villain-defeat.3.1")]
    [Fact]
    public void InactiveExtremeStageRetainsOnlyItsOwnCapturedLineage()
    {
        // Same-title new stages are "the same character"; the active counter
        // does not make another group's stage that character.
        var world = Hero("01001a");
        string[][] faces = [["07002", "07003", "07004"], ["07017", "07018", "07019"],
            ["07032", "07033", "07034"], ["07046", "07047", "07048"]];
        var groups = faces.Select(pair => Group(world, pair)).ToArray();
        var topology = new EncounterTopology(world, groups, groups[1].Villain);
        world.BindEncounterTopology(topology);
        var original = groups[0].Villain;
        var source = world.CreateCard("04043", world.Seats[0].Hand);
        var reference = new AbilityCardReference(original, original.Area.Id, original.Incarnation);
        var area = original.Area;
        World.MoveToTop(original, world.AreaOf(DeckType.RemovedArea));
        World.MoveToTop(groups[0].Stages[1], area);
        // A same-title extra card is deliberately outside the immutable lineage.
        // It must not steal a selection even though it shares the public area.
        world.CreateCard("07003", area);
        string before = world.Digest().Canonical();
        long words = world.Random.Generator.WordsConsumed;

        Assert.Same(groups[0].Stages[1], reference.Resolve(world, source, "chosen"));
        Assert.Same(groups[1], topology.Active);
        Assert.Same(original, reference.Card);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(words, world.Random.Generator.WordsConsumed);

        World.MoveToTop(groups[0].Stages[1], world.AreaOf(DeckType.RemovedArea));
        Assert.Null(reference.Resolve(world, source, "chosen"));
        Assert.Same(groups[1], topology.Active);
    }

    private static EncounterDeckGroup Group(World world, string[] faces)
    {
        var first = world.CreateCard(faces[0], world.AreaOf(DeckType.VillainArea));
        var second = world.CreateCard(faces[1], world.AreaOf(DeckType.VillainDeck));
        return new(first, world.CreateCard(faces[2], world.AreaOf(DeckType.SideSchemesArea)),
            world.CreateArea(DeckType.EncounterDeck), world.CreateArea(DeckType.EncounterDiscardPile),
            [], [first, second]);
    }
}
