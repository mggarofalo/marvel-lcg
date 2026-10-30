using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Prepared nonpublic topology probes; these do not admit or play Kang.
public sealed class KangJoinMatchingReviewTests : CoreCardTestBase
{
    [Theory]
    [InlineData("01001a", false)]
    [InlineData("01001b", false)]
    [InlineData("01001a", true)]
    [InlineData("01001b", true)]
    [Rule("rr:unique-icon.1.2")]
    public void IdentityMatchesItsAllySubtitleInEitherFormAndJoinDirection(
        string form, bool reverse)
    {
        // "The subtitle or alter-ego title of one matches the title, subtitle,
        // or alter-ego title of the other." The same hero title with a
        // different person's subtitle does not qualify.
        // pack:mc11:rules-clarifications requires matching unique copies to be
        // reconciled when players combine game areas.
        var world = Hero("01001a,01001b", 2);
        var source = world.CreateGameArea();
        var destination = world.CreateGameArea();
        world.Join(PlayArea.Of(0), source, "prepared split", []);
        world.Join(PlayArea.Of(1), destination, "prepared split", []);
        World.MoveToTop(world.Seats[1].IdentityCard, world.Seats[1].Hand);
        var identity = world.Seats[0].IdentityCard;
        identity.TurnTo(form);
        // Owner zero deliberately differs from the destination's play area.
        var peter = world.CreateCard("04045",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 0));
        world.CreateCard("13019", world.Seats[1].Hero);

        var conflicts = reverse
            ? Uniqueness.ConflictsAfterJoin(world, Cards, destination, source)
            : Uniqueness.ConflictsAfterJoin(world, Cards, source, destination);

        Assert.Equal(new[] { (identity, peter) }, conflicts);
    }

    [Fact]
    [Rule("rr:unique-icon.1.1")]
    public void DifferentPrintedFacesWithTheSameBareUniqueTitleConflict()
    {
        // "The two cards share a title, and both have no subtitle and no
        // alter-ego title." Matching is independent of printed card ID.
        var world = Hero("01001a", 2);
        var source = world.CreateGameArea();
        var destination = world.CreateGameArea();
        world.Join(PlayArea.Of(0), source, "prepared split", []);
        world.Join(PlayArea.Of(1), destination, "prepared split", []);
        foreach (var seat in world.Seats)
            World.MoveToTop(seat.IdentityCard, seat.Hand);
        var coreNick = world.CreateCard("01084", world.Seats[0].Hero);
        var reprintedNick = world.CreateCard("08019", world.Seats[1].Hero);

        Assert.Equal(new[] { (coreNick, reprintedNick) },
            Uniqueness.ConflictsAfterJoin(world, Cards, source, destination));

        World.MoveToTop(reprintedNick, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1)));
        Assert.Empty(Uniqueness.ConflictsAfterJoin(world, Cards, source, destination));
    }
}
