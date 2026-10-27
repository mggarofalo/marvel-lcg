using static Marvel.Content.Tests.Play.SpellMasteryFixture;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Actual Vapors and Ronan on nonpublic boards; no modular admission.</summary>
public sealed class RonanStatusReplacementReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:cannot")]
    [Rule("rr:status-cards.1")]
    public void VaporsCannotReplaceRonansToughWithStunButCanReplaceItWithConfused(bool master)
    {
        // "Cannot" is absolute. "A character cannot have more than one status
        // card of each type at a time." The local Ronan ruling explicitly
        // applies cannot-be-stunned to Vapors replacement as well as stunning.
        var (world, game, deck, _) = BoardActual("09035", false, master);
        var ronan = world.CreateCard("90001", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var old = Statuses.Give(world, ronan, Statuses.Tough);
        var control = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var other = Statuses.Give(world, control, Statuses.Tough);
        var original = deck.PublicTop!;
        Begin(world, game, original, master);
        Assert.Equal(1, game.Pending!.Player);
        Assert.Contains(game.Pending.Affordances, offer => offer.AnchorId == other.ObjectId);
        game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances,
            offer => offer.AnchorId == old.ObjectId).Id));
        Assert.Equal(1, game.Pending!.Player);
        var replacement = Assert.Single(game.Pending.Affordances);
        Assert.Equal("Replace with confused", replacement.Description);
        Assert.Equal(DeckType.StatusArea, old.Area.Type);
        string before = Snapshot(world);
        Assert.ThrowsAny<Exception>(() => game.Resolve(Decision.Take(int.MaxValue)));
        Assert.Equal(before, Snapshot(world));
        var result = game.Resolve(Decision.Take(replacement.Id));
        Assert.Equal(DeckType.RemovedArea, old.Area.Type);
        Assert.False(Statuses.Has(world, ronan, Statuses.Stunned));
        Assert.Equal(1, Statuses.Count(world, ronan, Statuses.Confused));
        Assert.Equal(0, Statuses.Count(world, ronan, Statuses.Tough));
        Assert.Equal(DeckType.StatusArea, other.Area.Type);
        Assert.Contains(result.Events.OfType<CardAttached>(), item =>
            item.Host == ronan.ObjectId && item.Verb == "Replace_Status");
        Assert.Equal(master ? DeckType.AdditionalDeck : DeckType.AdditionalDiscardPile, original.Area.Type);
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:cannot")]
    [Rule("rr:status-cards.1")]
    public void AlreadyConfusedRonanKeepsHisToughWhileAStunnableControlRemainsSelectable()
    {
        // "Cannot" is absolute, and a character cannot have duplicate statuses.
        // Ronan has no legal destination; an otherwise matching mercenary does.
        var (world, game, deck, _) = BoardActual("09035", false, false);
        var ronan = world.CreateCard("90001", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var tough = Statuses.Give(world, ronan, Statuses.Tough);
        var confused = Statuses.Give(world, ronan, Statuses.Confused);
        var control = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var controlTough = Statuses.Give(world, control, Statuses.Tough);
        Begin(world, game, deck.PublicTop!, false);
        Assert.DoesNotContain(game.Pending!.Affordances, offer => offer.AnchorId == tough.ObjectId);
        Assert.DoesNotContain(game.Pending.Affordances, offer => offer.AnchorId == confused.ObjectId);
        game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances,
            offer => offer.AnchorId == controlTough.ObjectId).Id));
        var stun = Assert.Single(game.Pending!.Affordances, offer => offer.Description == "Replace with stunned");
        game.Resolve(Decision.Take(stun.Id));
        Assert.Equal(DeckType.StatusArea, tough.Area.Type);
        Assert.Equal(DeckType.StatusArea, confused.Area.Type);
        Assert.False(Statuses.Has(world, ronan, Statuses.Stunned));
        Assert.True(Statuses.Has(world, control, Statuses.Stunned));
        Assert.True(game.IsRootPrompt);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static void Begin(World world, Game game, Card original, bool master)
    {
        var offer = master ? Assert.Single(game.Pending!.Affordances,
            item => item.AnchorId == world.Cards.Single(card => card.FaceId == "09005").ObjectId)
            : Mastery(game, world.Seats[1].IdentityCard);
        int[] sources = master ? [world.Seats[1].Hand.Cards.First(card => card.FaceId == "01088").ObjectId] : [];
        var allocations = master ? ResourcePayment.Allocate(Assert.Single(offer.CostOptions), sources) : null;
        game.Resolve(new Decision(offer.Id, [original.ObjectId], sources, Allocations: allocations));
    }
}
