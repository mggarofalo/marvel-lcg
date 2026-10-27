using Marvel.Rules.State;
using Xunit;

namespace Marvel.View.Tests;

public sealed class SupportThreatProjectionReviewTests
{
    [Theory]
    [InlineData("01001a", DeckType.HeroArea)]
    [InlineData("01094", DeckType.VillainArea)]
    [InlineData("01101", DeckType.EngagedEnemiesArea)]
    public void RegisteredCharacterBookkeepingDoesNotBecomeDisplayedThreat(string face, DeckType zone)
    {
        // Presentation policy: a registered engine token pool is bookkeeping;
        // characters do not acquire a displayed threat statistic from registration.
        World world = PersistentFixture.Board();
        Card character = world.CreateCard(face, world.AreaOf(zone,
            zone == DeckType.VillainArea ? PlayArea.Villains : PlayArea.Of(0)));
        CardDescriptor projected = PersistentFixture.Card(world, character);
        Assert.Equal(0, projected.Face!.Fields["k_threat"]);
        Assert.Null(projected.State!.Threat);
        Assert.DoesNotContain(Present(PersistentFixture.Visible(world), character.ObjectId).Fields,
            field => field.Name == "THREAT");
    }

    [Fact]
    public void SuppliedSupportTokenCountWinsOverADifferentRawBookkeepingValue()
    {
        // Synthetic disagreement challenges which contract owns the live value.
        World world = PersistentFixture.Board();
        Card support = world.CreateCard("10029", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0)));
        support.PlaceTokens("k_threat", 3);
        WorldDescriptor visible = PersistentFixture.Visible(world);
        var areas = visible.Areas.Select(area => area with
        {
            Cards = area.Cards.Select(card => card.Id != support.ObjectId ? card : card with
            {
                Face = card.Face! with { Fields = new Dictionary<string, long> { ["k_threat"] = 9 } },
            }).ToArray(),
        }).ToArray();
        Assert.Equal(new BoardFieldPresentation("THREAT", "3"), Assert.Single(
            Present(visible with { Areas = areas }, support.ObjectId).Fields));
    }

    [Theory]
    [InlineData(null)]
    [InlineData(0L)]
    [InlineData(3L)]
    public void ActualSupportProjectionDistinguishesAbsentFromSuppliedThreat(long? threat)
    {
        // Synthetic placement of the actual authored face isolates projection;
        // this is not evidence of paying for or activating Beat Cop.
        World world = PersistentFixture.Board();
        Card support = world.CreateCard("10029", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0)));
        if (threat is { } count) support.PlaceTokens("k_threat", count);
        WorldDescriptor visible = PersistentFixture.Visible(world);
        Assert.Equal(threat, VisibilityFixture.Card(visible, support.ObjectId).State!.Threat);
        BoardFieldPresentation[] displayed = Present(visible, support.ObjectId).Fields
            .Where(field => field.Name == "THREAT").ToArray();
        if (threat is null) Assert.Empty(displayed);
        else Assert.Equal(threat.Value.ToString(System.Globalization.CultureInfo.InvariantCulture),
            Assert.Single(displayed).Value);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void UnauthorizedSupportCountersRemainHiddenAcrossProjectionAndBoard(bool cooperative)
    {
        // A synthetic counter in a concealed pile challenges both disclosure routes.
        World world = PersistentFixture.Board();
        Card support = world.CreateCard("10029", world.Seats[0].Deck);
        support.PlaceTokens("k_threat", 3);
        ViewScope scope = cooperative
            ? new PermissiveVisibilityPolicy().Authorize(null, world.Players)
            : ViewScope.None;
        WorldDescriptor visible = WorldProjection.For(world, null, [], scope).World;
        CardDescriptor hidden = Assert.Single(Assert.Single(visible.Areas,
            area => area.Id == support.Area.Id).Cards);
        Assert.Null(hidden.Id);
        Assert.Null(hidden.Face);
        Assert.Null(hidden.State);
        BoardCardPresentation concealed = Assert.Single(Assert.Single(BoardPresentation.From(visible).Areas,
            area => area.Id == support.Area.Id).Cards);
        Assert.True(concealed.Concealed);
        Assert.Empty(concealed.Fields);
        Assert.Null(concealed.FaceId);
    }

    private static BoardCardPresentation Present(WorldDescriptor visible, int id) =>
        Assert.Single(BoardPresentation.From(visible).Areas.SelectMany(area => area.Cards),
            card => card.TargetId == id);
}
