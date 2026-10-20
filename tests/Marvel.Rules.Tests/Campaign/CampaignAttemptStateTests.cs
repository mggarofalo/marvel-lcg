using Marvel.Rules.Campaign;
using Marvel.Rules.Events;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Rules.Tests.Campaign;

/// <summary>Campaign bookkeeping participates in replay state instead of living in a UI cache.</summary>
public sealed class CampaignAttemptStateTests
{
    [Fact]
    public void RevealingAnExperimentalWeaponDoesNotRecordEntryUntilItActuallyEntersPlay()
    {
        var world = new World(new Printed(), 1);
        var card = world.CreateCard("weapon", world.AreaOf(DeckType.ExperimentalWeaponsDeck));
        var setup = new CampaignGameSetup("synthetic", false, CampaignStage.Crossbones, 1, [], [], 0);
        new CampaignAttemptState(setup, [], [card.ObjectId]).Bind(world);
        World.MoveToTop(card, world.AreaOf(DeckType.RevealingArea));
        Assert.Empty(world.Campaign!.EnteredExperimentalWeapons);
        Assert.Equal(0, Fields(world, card)["campaign_entered_play"]);
        World.MoveToTop(card, world.AreaOf(DeckType.UpgradesArea));
        World.MoveToTop(card, world.AreaOf(DeckType.EncounterDiscardPile));
        Assert.Equal([card], world.Campaign.EnteredExperimentalWeapons);
        Assert.Equal(1, Fields(world, card)["campaign_entered_play"]);
    }

    [Fact]
    public void CampaignRemovalChangesTheDigestEvenWhenTheCardIsAlreadyOutOfPlay()
    {
        var (world, card) = Board(CampaignCardRole.Tech);
        var before = world.Digest().Canonical();
        var events = new List<GameEvent>();
        world.Campaign!.Remove(card, "ability", events);
        Assert.NotEqual(before, world.Digest().Canonical());
        Assert.True(Assert.Single(world.Campaign.Cards).Removed);
        Assert.Equal(1, Fields(world, card)["campaign_removed"]);
        Assert.Single(events.OfType<FieldSet>());
        Assert.Throws<InvalidOperationException>(() => world.Campaign.Remove(card, "ability", events));
    }

    [Fact]
    public void RescuingCaptiveChangesItsCampaignSeatAndPersistsAcrossZones()
    {
        var (world, card) = Board(CampaignCardRole.RescuedAlly, -1);
        world.Campaign!.Rescue(card, 0, "rescue", []);
        World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        Assert.Equal(0, Fields(world, card)["campaign_seat"]);
        Assert.Equal(1, Fields(world, card)["campaign_rescued"]);
        Assert.False(Assert.Single(world.Campaign.Cards).Removed);
    }

    [Fact]
    public void RecoveryAssignmentIsSeatBoundAndCannotBeRecordedTwice()
    {
        var (world, card) = Board(CampaignCardRole.Obligation);
        Assert.Throws<ArgumentOutOfRangeException>(() => world.Campaign!.AssignObligation(card, 1, "recovery", []));
        Assert.Equal(0, Fields(world, card)["campaign_assigned"]);
        world.Campaign!.AssignObligation(card, 0, "recovery", []);
        Assert.Equal(1, Fields(world, card)["campaign_assigned"]);
        Assert.Throws<InvalidOperationException>(() => world.Campaign.AssignObligation(card, 0, "recovery", []));
    }

    [Fact]
    public void UntrackedCardsHaveNoCampaignFields()
    {
        var (world, _) = Board(CampaignCardRole.Condition);
        var ordinary = world.CreateCard("ordinary", world.AreaOf(DeckType.RemovedArea));
        Assert.DoesNotContain(Fields(world, ordinary).Keys, key => key.StartsWith("campaign_", StringComparison.Ordinal));
    }

    private static IReadOnlyDictionary<string, long> Fields(World world, Card card) =>
        StateFields.For(card, world.Facts, 1, false, false, false, world);

    private static (World World, Card Card) Board(CampaignCardRole role, int seat = 0)
    {
        var world = new World(new Printed(), 1);
        var card = world.CreateCard("face", world.AreaOf(DeckType.AsideDeck));
        var setup = new CampaignGameSetup("test", true, CampaignStage.Crossbones, 1,
            [new CampaignSeatSetup(0, [], 5, false, [], false)], [], 0);
        new CampaignAttemptState(setup,
            [new CampaignCardBinding(card.ObjectId, seat, new CampaignCard("physical", "face", role))]).Bind(world);
        return (world, card);
    }

    private sealed class Printed : ICardFacts
    {
        public CardKind Kind(string faceId) => CardKind.Upgrade;
        public IReadOnlyList<string> Traits(string faceId) => [];
        public IReadOnlyDictionary<string, string> Attributes(string faceId) => new Dictionary<string, string>();
        public long PrintedValue(string faceId, string attribute, int players, long fallback = 0) => fallback;
    }
}
