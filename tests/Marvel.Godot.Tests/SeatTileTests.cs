using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class SeatTileTests
{
    [Theory]
    [InlineData("Spider-Man", "SM")]
    [InlineData("Captain Marvel", "CM")]
    [InlineData("Thor", "TH")]
    [InlineData("X", "X")]
    [InlineData("", "?")]
    public void APortraitShowsTheHerosInitials(string hero, string initials) => Assert.Equal(initials, SeatTile.Initials(hero));

    [Fact]
    public void ASeatsRolesAreItsBadgesInOrder()
    {
        var selection = new DisplayedSeatSelection(1, ActivePlayer: 1, PromptOwner: 1, ViewedPrivateSeat: 1, PublicFocusSeat: 1);
        Assert.Equal([TableFact.ActiveSeat, TableFact.AnsweringSeat, TableFact.YourSeat, TableFact.PublicFocus],
            SeatTile.Roles(1, selection));
        Assert.Empty(SeatTile.Roles(0, selection));
    }

    [Theory]
    [InlineData("Hero", "HeroForm")]
    [InlineData("AlterEgo", "AlterEgoForm")]
    [InlineData("Ally", null)]
    [InlineData(null, null)]
    public void TheFormBadgeComesFromTheIdentityKind(string? kind, string? form) =>
        Assert.Equal(form, SeatTile.Form(kind)?.ToString());

    [Fact]
    public void TheHeroIsTheIdentityCardsPublishedTitle()
    {
        var identity = new BoardCardPresentation(9, 1, false, "Spider-Man · Peter", "", "HERO", "", []);
        var board = new BoardPresentation([new BoardAreaPresentation(1, "HeroArea", "", [identity], []) { Zone = "HeroArea" }]);
        Assert.Equal("Spider-Man · Peter", SeatTile.Hero(board, Summary(2, 9)));
        Assert.Equal("Player 3", SeatTile.Hero(board, Summary(2, null)));
        Assert.Equal("Player 3", SeatTile.Hero(board, Summary(2, 404)));
    }

    [Theory]
    [InlineData(7L, 10L, 0.7f, "7/10")]
    [InlineData(12L, 10L, 1f, "12/10")]
    [InlineData(7L, null, null, "HP 7")]
    [InlineData(7L, 0L, null, "HP 7")]
    [InlineData(null, 10L, null, "HP —")]
    public void TheHealthBarNeedsBothValues(long? health, long? maximum, float? fraction, string text)
    {
        Assert.Equal(fraction, SeatHealth.Fraction(health, maximum));
        Assert.Equal(text, TableWording.Health(health, maximum));
    }

    [Fact]
    public void TheMaximumIsTheIdentityCardsCurrentHitPoints()
    {
        var identity = new BoardCardPresentation(9, 1, false, "Spider-Man", "", "HERO", "", [])
        {
            EffectiveValues = new Dictionary<string, CardEffectiveValue>
            {
                ["HP"] = new(10, 12, "printed", true, []),
            },
        };
        var board = new BoardPresentation([new BoardAreaPresentation(1, "HeroArea", "", [identity], []) { Zone = "HeroArea" }]);
        Assert.Equal(12, SeatHealth.Maximum(board, 9));
        Assert.Null(SeatHealth.Maximum(board, 8));
        Assert.Null(SeatHealth.Maximum(board, null));
    }

    private static PlayerSummaryDescriptor Summary(int seat, int? identity) => new(seat, identity, "Hero", 7, [], [], []);
}
