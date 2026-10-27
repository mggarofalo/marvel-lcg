using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Direct negative controls for the actual stage-II capture boundary.
public sealed class KangStageTwoCaptureReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:when-defeated-abilities.1")]
    public void CaptureRejectsAnotherCardOrAnEarlierIncarnation(bool staleIncarnation)
    {
        // When Defeated means "When this card is defeated".
        var world = Hero("01001a,01001b", 2);
        var source = world.CreateGameArea();
        var destination = world.CreateGameArea();
        world.Join(PlayArea.Of(0), source, "prepared split", []);
        world.Join(PlayArea.Of(1), destination, "prepared split", []);
        var kang = world.CreateCard("11037", world.AreaOf(DeckType.VillainArea, PlayArea.Of(0)));
        var other = world.CreateCard("11003", world.AreaOf(DeckType.VillainArea, PlayArea.Of(1)));
        var scheme = world.CreateCard("11011b", world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(0)));
        var defeated = new Defeated(staleIncarnation ? kang.ObjectId : other.ObjectId, 0, "Attack") {
            Kind = CardKind.EncounterVillain, Incarnation = staleIncarnation ? kang.Incarnation - 1 : other.Incarnation };
        var events = new List<GameEvent>();
        string digest = world.Digest().Canonical();
        Assert.Throws<RulesNotImplementedException>(() =>
            world.Agenda.KangTransitions.CaptureDefeat(world, kang, defeated, events));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Empty(world.Agenda.KangTransitions.Pending);
        Assert.Equal(DeckType.MainSchemesArea, scheme.Area.Type);
        Assert.Empty(events);
    }

    [Fact]
    public void MatchingSchemeInAnotherAreaCannotSatisfyTheLocalRequirement()
    {
        var world = Hero("01001a,01001b", 2);
        var source = world.CreateGameArea();
        var destination = world.CreateGameArea();
        world.Join(PlayArea.Of(0), source, "prepared split", []);
        world.Join(PlayArea.Of(1), destination, "prepared split", []);
        var kang = world.CreateCard("11037", world.AreaOf(DeckType.VillainArea, PlayArea.Of(0)));
        var remote = world.CreateCard("11011b", world.AreaOf(DeckType.MainSchemesArea, PlayArea.Of(1)));
        var defeated = new Defeated(kang.ObjectId, 0, "Attack") { Kind = CardKind.EncounterVillain, Incarnation = kang.Incarnation };
        var events = new List<GameEvent>();
        Assert.Throws<RulesNotImplementedException>(() =>
            world.Agenda.KangTransitions.CaptureDefeat(world, kang, defeated, events));
        Assert.Empty(world.Agenda.KangTransitions.Pending);
        Assert.Equal(DeckType.MainSchemesArea, remote.Area.Type);
        Assert.Empty(events);
    }
}
