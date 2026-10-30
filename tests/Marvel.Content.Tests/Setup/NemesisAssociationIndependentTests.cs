using Marvel.Content;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Setup;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class NemesisAssociationIndependentTests
{
    [Fact]
    [Rule("rr:nemesis-encounter-set.3")]
    public void PhysicalCopiesRemainAssociatedIndependentlyAndCannotBeRemovedThroughSnapshot()
    {
        // “An identity’s ‘nemesis minion’ is the minion belonging to that identity’s nemesis set.”
        var setup = SetupCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("setup", "setup.json")));
        var cards = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var blueprints = Blueprints.From(Dealer.DealOrder(setup, "rhino", ["black_widow"]), cards);
        var world = WorldSetup.DealWithoutCardAbilities(cards, blueprints, ["Black Widow"], 832);
        var seat = world.Seats[0];
        Assert.Equal(["08026", "08027", "08028", "08028", "08029"],
            seat.NemesisSet.Select(card => card.FaceId));
        var copies = seat.NemesisSet.Where(card => card.FaceId == "08028").ToArray();
        Assert.Equal(2, copies.Length);
        Assert.NotEqual(copies[0].ObjectId, copies[1].ObjectId);
        World.MoveToTop(copies[0], world.AreaOf(DeckType.EncounterDeck));
        World.MoveToTop(copies[1], world.AreaOf(DeckType.EncounterDiscardPile));
        Assert.Contains(copies[0], seat.NemesisSet);
        Assert.Contains(copies[1], seat.NemesisSet);
        Assert.DoesNotContain(seat.NemesisSet, card => card.FaceId == "08025");
        if (seat.NemesisSet is IList<Card> snapshot)
        {
            try { snapshot[0] = copies[0]; }
            catch (NotSupportedException) { }
        }
        Assert.Equal("08026", seat.NemesisSet[0].FaceId);
    }
}
