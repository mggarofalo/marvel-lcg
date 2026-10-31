using Marvel.Client;
using Marvel.Rules.Play;
using Marvel.View;
using Xunit;

namespace Marvel.Godot.Tests;

public sealed class GameAreaClientValidationTests
{
    [Theory]
    [InlineData(null, null, true)]
    [InlineData(7, 7, true)]
    [InlineData(8, null, false)]
    [InlineData(null, 8, false)]
    public void SeatsAndCardsMayNameOnlyAListedGameArea(int? seatArea, int? cardArea, bool accepted)
    {
        // Synthetic wire shape: a null game area is the central location of a
        // separated table; any other id must be one the response lists.
        var card = new CardDescriptor(5, CardBack.Encounter, true, true, -1, null)
        {
            Location = new CardLocationDescriptor(2, "VillainArea", -1, -1) { GameArea = cardArea },
        };
        var world = new WorldDescriptor(
            [new PlayerDescriptor(0, "p0", false) { GameArea = seatArea }],
            [new AreaDescriptor(2, "VillainArea", -1, -1, [card], [])],
            [new GameAreaDescriptor(7, [0])],
            Outcome.Unfinished);

        Assert.Equal(accepted, ClientTableValidation.KnownGameAreas(world));
    }
}
