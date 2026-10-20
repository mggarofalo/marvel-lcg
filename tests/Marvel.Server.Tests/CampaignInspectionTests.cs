using Marvel.Rules.Campaign;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Server.Tests;

/// <summary>Synthetic terminal boards prove fact extraction without claiming a played RoRS scenario.</summary>
public sealed class CampaignInspectionTests
{
    [Fact]
    public void InspectionRetainsRemovedRewardsAndSeparatesPrisonCaptivesFromInPlayAttachments()
    {
        var world = new World(new Printed(), 1);
        world.CreateSeat("player");
        world.Seats[0].IdentityCard = world.CreateCard("hero", world.Seats[0].Hero);
        var main = world.CreateCard("main", world.AreaOf(DeckType.MainSchemesArea));
        main.PlaceTokens("k_delay", 4);
        var prison = world.CreateCard("prison", world.AreaOf(DeckType.SideSchemesArea));
        world.CreateCard("ally", world.CreateArea(DeckType.AsideDeck, 0, PlayArea.Of(0), prison.ObjectId));
        world.CreateCard("ally", world.CreateArea(DeckType.UpgradesArea, 0, PlayArea.Of(0), prison.ObjectId));
        var tech = world.CreateCard("tech", world.Seats[0].SetAside);
        var obligation = world.CreateCard("obligation", world.Seats[0].SetAside);
        var assigned = new CampaignCard("obligation", "obligation", CampaignCardRole.Obligation);
        var reward = new CampaignCard("tech", "tech", CampaignCardRole.Tech);
        var condition = new CampaignCard("condition", "condition", CampaignCardRole.Condition);
        var input = new CampaignGameSetup("synthetic", true, CampaignStage.Zola, 4,
            [new(0, [reward, condition], 5, false, [assigned], false) { CampaignSeat = 2 }], [], 0);
        new CampaignAttemptState(input, [new(tech.ObjectId, 0, reward), new(obligation.ObjectId, 0, assigned)]).Bind(world);
        world.Campaign!.Remove(tech, "synthetic", []);
        world.Campaign.AssignObligation(obligation, 0, "synthetic", []);
        var definition = new CampaignDefinition("synthetic", [], [reward], [new(condition, "improved", 2)], [], [], [])
        { HydraPrisonFace = "prison" };
        var game = Game.BeginWithoutCardAbilities(world, world.Facts);
        var before = world.Digest().Canonical();
        var rngBefore = world.Random.Generator.GetState();
        int agendaBefore = world.Agenda.Count;
        Assert.Null(CampaignInspection.Read(game, definition).Outcome);
        world.Finish(Outcome.PlayersWin);
        var first = CampaignInspection.Read(game, definition);
        var second = CampaignInspection.Read(game, definition);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(rngBefore.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(rngBefore.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(agendaBefore, world.Agenda.Count);
        Assert.Equal(first.Removals.ToArray(), second.Removals.ToArray());
        Assert.Equal("tech", Assert.Single(first.Removals).CardKey);
        Assert.Equal(2, Assert.Single(first.Removals).Seat);
        Assert.Equal(2, Assert.Single(first.Obligations).Seat);
        Assert.True(first.Outcome!.Won);
        Assert.Equal(4, first.Outcome.DelayCounters);
        Assert.True(first.Outcome.HydraPrisonInPlay);
        Assert.Equal(["ally"], first.Outcome.ImprisonedAllyFaces);
        Assert.Equal(10, Assert.Single(first.Outcome.Seats).BaseHitPoints);
        Assert.Equal(10, Assert.Single(first.Outcome.Seats).RemainingHitPoints);
        Assert.Equal(2, Assert.Single(first.Outcome.Seats).Seat);
    }

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => faceId switch
        {
            "hero" => CardKind.Hero,
            "main" => CardKind.MainScheme,
            "prison" => CardKind.EncounterSideScheme,
            "ally" => CardKind.Ally,
            _ => CardKind.Upgrade,
        };

        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) =>
            attribute == "HP" ? 10 : fallback;
    }
}
