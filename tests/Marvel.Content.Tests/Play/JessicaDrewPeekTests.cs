using Marvel.Content.Tests.Cards;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class JessicaDrewPeekTests : CoreCardTestBase
{
    [Theory]
    [InlineData(DeckType.PlayerDeck)]
    [InlineData(DeckType.EncounterDeck)]
    [InlineData(DeckType.ExperimentalWeaponsDeck)]
    [InlineData(DeckType.SideSchemeDeck)]
    public void DeckSelectionPrecedesPrivateInspectionAndDoesNotChangeDeckOrder(DeckType kind)
    {
        var world = Hero("04031b", 2);
        var area = kind == DeckType.PlayerDeck ? world.Seats[1].Deck : world.AreaOf(kind);
        world.CreateCard("01088", area);
        var top = world.CreateCard("01089", area);
        var runner = Runner();
        world.Abilities = runner;
        string original = world.Digest().Canonical();
        var action = Assert.Single(runner.Actions(world, 0));
        Assert.Equal(original, world.Digest().Canonical());
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        var decks = Sequence.Work(world, Cards, runner, events)!;
        Assert.False(decks.ExposesConcealedCandidates);
        Assert.All(decks.Affordances, option => Assert.Equal(AffordanceAnchorKind.Area, option.AnchorKind));
        var option = Assert.Single(decks.Affordances, option => option.AnchorId == area.Id);
        string beforePeek = world.Digest().Canonical();
        Sequence.Answer(world, Cards, runner, decks, Decision.Take(option.Id), events);
        var inspection = Sequence.Work(world, Cards, runner, events)!;
        Assert.True(inspection.ExposesConcealedCandidates);
        Assert.Equal(PublicDecisionKind.CardLook, inspection.PublicKind);
        Assert.Equal(top.ObjectId, Assert.Single(inspection.Affordances).AnchorId);
        Assert.Equal([top.ObjectId], inspection.ContextCardIds);
        Assert.Equal(beforePeek, world.Digest().Canonical());
        Sequence.Answer(world, Cards, runner, inspection,
            Decision.Take(Assert.Single(inspection.Affordances).Id), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(beforePeek, world.Digest().Canonical());
        Assert.False(top.FaceUp);
        Assert.Equal(["01088", "01089"], area.Cards.Select(card => card.FaceId));
        Assert.Empty(runner.Actions(world, 0));
    }

    [Fact]
    public void EmptyDecksAndUnrevealedVillainStagesAreNotOfferedAsDeckTops()
    {
        var world = Hero("04031b");
        var current = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        world.CreateCard("01095", world.AreaOf(DeckType.VillainDeck));
        var runner = Runner();
        world.Abilities = runner;
        var action = Assert.Single(runner.Actions(world, 0));
        var events = runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events.ToList();
        var decks = Sequence.Work(world, Cards, runner, events)!;
        var only = Assert.Single(decks.Affordances);
        Assert.Equal(current.Area.Id, only.AnchorId);
        Sequence.Answer(world, Cards, runner, decks, Decision.Take(only.Id), events);
        var inspection = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(current.ObjectId, Assert.Single(inspection.ContextCardIds));
        Assert.False(inspection.ExposesConcealedCandidates);
    }

    [Fact]
    public void NoNonemptyDeckMeansNoActionAndNoStateMutation()
    {
        var world = Hero("04031b");
        var runner = Runner();
        world.Abilities = runner;
        string before = world.Digest().Canonical();
        Assert.Empty(runner.Actions(world, 0));
        Assert.Equal(before, world.Digest().Canonical());
    }

    internal static AbilityRunner Runner() => AuthoredCards.Runner();
}
