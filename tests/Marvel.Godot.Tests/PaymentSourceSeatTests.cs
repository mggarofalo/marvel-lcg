using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class PaymentSourceSeatTests
{
    [Theory]
    [InlineData(0, 1, 0)]
    [InlineData(1, 1, null)]
    [InlineData(null, 1, null)]
    public void OnlyAnotherSeatsSourceNamesItsController(int? controller, int payer, int? named)
    {
        // rr:ability.8: any player may use the first player's Milano (16142); the payer's own card needs no seat.
        var source = new PaymentSourcePresentation(7, "Milano", "G", false, "") { Controller = controller };
        Assert.Equal(named, PaymentSourceSeat.Other(source, payer));
    }

    [Theory]
    [InlineData(true, "first_player", "On Spider-Man's table, the first player. Spider-Man controls this card.")]
    [InlineData(false, "source_seat", "On Spider-Man's table. Spider-Man controls this card.")]
    public void TheSeatChipNamesTheSeatAndWhetherItHoldsTheToken(bool first, string icon, string tooltip)
    {
        Wording wording = TableWording.SourceSeat("Spider-Man", first);
        Assert.Equal("Spider-Man", wording.Label);
        Assert.Equal(icon, wording.Icon);
        Assert.Equal(tooltip, wording.Tooltip);
    }

    [Fact]
    public void TheSeatIsNamedByItsIdentityCardAsTheRailNamesIt()
    {
        var face = new CardFaceDescriptor("01001b", "Peter Parker", "", CardKind.AlterEgo, new Dictionary<string, long>());
        var world = new WorldDescriptor([new PlayerDescriptor(0, "Spider-Man", false), new PlayerDescriptor(1, "Captain Marvel", false)],
            [new(1, "HeroArea", 0, -1, [new CardDescriptor(9, CardBack.Player, true, true, -1, face)], [])], [], Outcome.Unfinished)
        {
            PlayerSummaries = [new(0, 9, "AlterEgo", 10, [], [], []), new(1, 404, "Hero", 12, [], [], [])],
        };
        Assert.Equal("Peter Parker", PaymentSourceSeat.Name(world, 0));
        Assert.Equal("Captain Marvel", PaymentSourceSeat.Name(world, 1));
    }
}
