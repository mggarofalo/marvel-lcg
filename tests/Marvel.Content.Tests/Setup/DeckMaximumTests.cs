using Marvel.Cards.Dsl;
using Marvel.Content.Setup;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Setup;

public sealed class DeckMaximumTests
{
    private static readonly CardCatalog Cards =
        CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:max-maximum")]
    [Rule("rr:max-maximum.2")]
    [Fact]
    public void APlayerDeckCannotExceedAPrintedPerDeckMaximum()
    {
        // Energy is “Max 1 per deck.” This is checked before any blueprint or
        // object id is allocated, so invalid deck construction cannot become a
        // partially dealt game.
        var dealt = new[]
        {
            new Creation("01088", CreationSource.PlayerDeck, 0),
            new Creation("01088", CreationSource.PlayerDeck, 0),
        };

        var refused = Assert.Throws<ArgumentException>(() => Blueprints.From(dealt, Cards));

        Assert.Contains("Energy", refused.Message, StringComparison.Ordinal);
        Assert.Contains("maximum is 1", refused.Message, StringComparison.Ordinal);
    }

    [Rule("rr:max-maximum")]
    [Rule("rr:max-maximum.2")]
    [Fact]
    public void APerDeckMaximumIsSeparateForEachPlayer()
    {
        var blueprints = Blueprints.From(
        [
            new Creation("01088", CreationSource.PlayerDeck, 0),
            new Creation("01088", CreationSource.PlayerDeck, 1),
        ], Cards);

        Assert.Equal(2, blueprints.Count);
        Assert.Equal([0, 1], blueprints.Select(card => card.Seat));
    }
}
