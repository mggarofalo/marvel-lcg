using System.Text.Json;
using Marvel.Content;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.View.Tests;

public sealed class PrintedValueProjectionTests
{
    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Theory]
    [InlineData("03014", "ATK", "3", true, 1)]
    [InlineData("01002", "ATK", "1", false, 0)]
    [InlineData("01002", "THW", "1", false, 1)]
    [InlineData("01084", "THW", "2", false, 1)]
    [InlineData("27047", "THW", "3", false, 3)]
    [InlineData("41001a", "DEF", "2", true, 0)]
    [InlineData("01029a", "HS", "1", false, 0)]
    [InlineData("01099", "ATK+", "3", true, 0)]
    [InlineData("01153", "ATK+", "1", false, 0)]
    [InlineData("01050", "THW", "—", false, 0)]
    [InlineData("01162", "ATK", "X", false, 0)]
    [InlineData("01121", "Boost", "0", true, 0)]
    [InlineData("01146", "Boost", "1", true, 0)]
    [InlineData("02001a", "ATK", "★", true, 0)]
    public void SourceFactsSurviveProjectionSerializationAndBoardComposition(
        string id, string attribute, string value, bool special, int consequence)
    {
        // Legal projection fixture, not an expansion game: no decisions or
        // expansion mechanics run while copying printed source facts.
        var world = new World(Cards, players: 1, seed: 7);
        Seat seat = world.CreateSeat("Player");
        world.CreateCard(id, seat.Hand);
        WorldDescriptor projected = WorldProjection.For(world, null, [],
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players)).World;
        WorldDescriptor received = JsonSerializer.Deserialize<WorldDescriptor>(
            JsonSerializer.Serialize(projected))!;
        CardFaceDescriptor face = Assert.Single(received.Areas.SelectMany(area => area.Cards)).Face!;
        Assert.Equal(new CardPrintedValue(value, special, false, consequence),
            face.PrintedValues[attribute]);
        BoardCardPresentation card = Assert.Single(BoardPresentation.From(received).Areas
            .SelectMany(area => area.Cards));
        BoardPrintedValueMark mark = Assert.Single(card.PrintedMarks, mark => mark.Attribute == attribute);
        Assert.Equal(value, mark.Value);
        Assert.Equal(special, mark.SpecialStar);
        Assert.Equal(consequence, mark.ConsequentialDamage);
        Assert.False(mark.PerPlayer);
    }

    [Fact]
    public void UnauthorizedHandsDoNotExposeAnnotatedFacts()
    {
        var world = new World(Cards, players: 2, seed: 7);
        world.CreateSeat("Viewer");
        Seat owner = world.CreateSeat("Owner");
        world.CreateCard("03014", owner.Hand);
        WorldDescriptor projected = WorldProjection.For(world, null, [],
            new RestrictedVisibilityPolicy(0).Authorize(null, world.Players)).World;
        CardDescriptor card = Assert.Single(projected.Areas.SelectMany(area => area.Cards));
        Assert.Null(card.Face);
        BoardCardPresentation boardCard = Assert.Single(BoardPresentation.From(projected).Areas
            .SelectMany(area => area.Cards));
        Assert.Empty(boardCard.PrintedMarks);
    }

    [Fact]
    public void BoardCompositionDoesNotReinterpretLegacyPunctuation()
    {
        var face = new CardFaceDescriptor("fixture", "Synthetic", "", CardKind.Ally,
            new Dictionary<string, long>())
        {
            PrintedStats = new Dictionary<string, string> { ["ATK"] = "99***" },
            PrintedValues = new Dictionary<string, CardPrintedValue>
            {
                ["ATK"] = new("2", true, false, 1),
            },
        };
        var world = new WorldDescriptor([], [new AreaDescriptor(1, "HandsArea", 0, -1,
            [new CardDescriptor(1, CardBack.Player, true, true, -1, face)], [])], [],
            Marvel.Rules.Play.Outcome.Unfinished);
        BoardPrintedValueMark mark = Assert.Single(Assert.Single(Assert.Single(
            BoardPresentation.From(world).Areas).Cards).PrintedMarks);
        Assert.Equal("2", mark.Value);
        Assert.True(mark.SpecialStar);
        Assert.Equal(1, mark.ConsequentialDamage);
        Assert.False(mark.PerPlayer);
    }
}
