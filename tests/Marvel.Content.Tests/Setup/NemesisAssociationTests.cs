using System.Text.Json;
using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class NemesisAssociationTests
{
    private static readonly SetupCatalog Setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
    private static readonly CardCatalog Cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Fact]
    [Rule("rr:nemesis-encounter-set.3")]
    public void DealtAssociationSurvivesMovementAndReconstructsFromSetup()
    {
        // “An identity’s ‘nemesis minion’ is the minion belonging to that identity’s nemesis set.”
        var blueprints = Blueprints.From(Dealer.DealOrder(Setup, "rhino",
            ["spider_man", "black_widow"]), Cards);
        World Deal() => WorldSetup.DealWithoutCardAbilities(Cards, blueprints,
            ["Spider-Man", "Black Widow"], 832);
        var first = Deal();
        var second = Deal();
        var control = Deal();
        (string Digest, string Random, long Words, bool Pending) Snapshot(World world) =>
            (world.Digest().Canonical(), JsonSerializer.Serialize(world.Random.Generator.GetState()),
                world.Random.Generator.WordsConsumed, world.Agenda.IsBusy);
        var firstBefore = Snapshot(first);
        var secondBefore = Snapshot(second);
        Assert.Equal(firstBefore, secondBefore);
        Assert.Equal(["01167", "08026"], first.Seats.Select(seat =>
            Assert.Single(seat.NemesisSet, card => Cards.IsNemesisMinion(card.FaceId)).FaceId));
        Assert.Equal(first.Seats.SelectMany(seat => seat.NemesisSet.Select(card => card.ObjectId)),
            second.Seats.SelectMany(seat => seat.NemesisSet.Select(card => card.ObjectId)));
        Assert.Equal(firstBefore, Snapshot(first));
        Assert.Equal(secondBefore, Snapshot(second));
        Assert.False(first.Agenda.IsBusy);
        Assert.Equal(control.Agenda.IsBusy, first.Agenda.IsBusy);
        first.Shuffle(first.AreaOf(DeckType.EncounterDeck));
        control.Shuffle(control.AreaOf(DeckType.EncounterDeck));
        second.Shuffle(second.AreaOf(DeckType.EncounterDeck));
        Assert.Equal(Snapshot(control), Snapshot(first));
        for (int seat = 0; seat < 2; seat++)
        {
            Assert.Equal(first.Seats[seat].Nemesis.Cards.Select(card => card.ObjectId),
                first.Seats[seat].NemesisSet.Select(card => card.ObjectId));
            var minion = Assert.Single(first.Seats[seat].NemesisSet,
                card => Cards.IsNemesisMinion(card.FaceId));
            Assert.Equal(World.Scenario, minion.Owner);
            var counterpart = Assert.Single(second.Seats[seat].Nemesis.Cards, card => card.ObjectId == minion.ObjectId);
            World.MoveToTop(counterpart, second.AreaOf(seat == 0
                ? DeckType.EncounterDeck : DeckType.EncounterDiscardPile));
            World.MoveToTop(minion, first.AreaOf(seat == 0
                ? DeckType.EncounterDeck : DeckType.EncounterDiscardPile));
            Assert.Contains(minion, first.Seats[seat].NemesisSet);
            Assert.DoesNotContain(minion, first.Seats[1 - seat].NemesisSet);
            Assert.Equal(first.Seats[seat].NemesisSet.Select(card => card.ObjectId),
                second.Seats[seat].NemesisSet.Select(card => card.ObjectId));
        }
        var engaged = Assert.Single(first.Seats[0].NemesisSet, card => Cards.IsNemesisMinion(card.FaceId));
        var engagedCopy = Assert.Single(second.Seats[0].NemesisSet, card => card.ObjectId == engaged.ObjectId);
        World.MoveToTop(engaged, first.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        World.MoveToTop(engagedCopy, second.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Assert.Contains(engaged, first.Seats[0].NemesisSet);
        Assert.DoesNotContain(engaged, first.Seats[1].NemesisSet);
        Assert.Equal(Snapshot(second), Snapshot(first));
    }
}
