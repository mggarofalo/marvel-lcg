using System.Text.Json;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DoctorStrangeSupportReprintTests
{
    [Theory]
    [InlineData(1, false)]
    [InlineData(3, false)]
    [InlineData(3, true)]
    [Rule("rr:uses-x-type")]
    public void MedTeamHealsAnotherPlayersFriendlyCharacterAfterPayingItsCounters(int counters, bool allyTarget)
    {
        // rr:uses-x-type: "When the last all-purpose counter is removed from a card with uses, discard that card."
        Card source = null!;
        Card target = null!;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, "09018");
            source.PlaceTokens("c_medical", counters);
            target = allyTarget
                ? board.CreateCard("01083", board.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1))
                : board.Seats[1].IdentityCard;
            if (!allyTarget) target.TurnTo("01010a");
            target.TakeDamage(2);
        }, heroes: ["spider_man", "captain_marvel"]);
        var action = Assert.Single(game.Pending!.Affordances, item => item.AnchorId == source.ObjectId);
        game.Resolve(Decision.Take(action.Id));
        Assert.Equal(counters - 1, source.Tokens["c_medical"]);
        Assert.Equal(counters == 1 ? DeckType.DiscardPile : DeckType.SupportsArea, source.Area.Type);
        if (counters > 1) Assert.False(source.Ready);
        Assert.DoesNotContain(game.Pending!.Affordances,
            item => item.AnchorId == world.TheCardIn(DeckType.VillainArea)!.ObjectId);
        game.Resolve(Decision.Take(Assert.Single(game.Pending.Affordances,
            item => item.AnchorId == target.ObjectId).Id));
        Assert.Equal(0, target.Damage);
        Assert.DoesNotContain(game.Pending!.Affordances, item => item.AnchorId == source.ObjectId);
    }

    [Theory]
    [InlineData(3, true, false)]
    [InlineData(0, true, true)]
    [InlineData(3, false, true)]
    [Rule("rr:target.3")]
    public void MedTeamCannotSpendWithoutDamageOrPayableCosts(int counters, bool ready, bool damaged)
    {
        // rr:target.3: "A target is valid ... if any part of that ability can affect that target."
        Card source = null!;
        var (game, world) = Playing(board =>
        {
            source = InPlay(board, "09018");
            source.PlaceTokens("c_medical", counters);
            if (!ready) source.Exhaust();
            if (damaged) board.Seats[0].IdentityCard.TakeDamage(2);
        });
        string before = world.Digest().Canonical();
        Assert.DoesNotContain(game.Pending!.Affordances, item => item.AnchorId == source.ObjectId);
        Assert.Equal(counters, source.Tokens["c_medical"]);
        Assert.Equal(ready, source.Ready);
        Assert.Equal(before, world.Digest().Canonical());
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void NonFirstMansionControllerChoosesEitherPlayersDraw(int recipient)
    {
        Card source = null!;
        var (_, world) = Playing(board => source = board.CreateCard("09025",
            board.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 0)),
            heroes: ["spider_man", "captain_marvel"]);
        var runner = AuthoredCards.Runner();
        var top = world.Seats[recipient].Deck.Cards[^1];
        int[] held = world.Seats.Select(seat => seat.Hand.Cards.Count).ToArray();
        Assert.DoesNotContain(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        var action = Assert.Single(runner.Actions(world, 1), item => item.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        runner.Chose(world, Assert.Single(world.Agenda.Outstanding),
            Decision.Take(world.Seats[recipient].IdentityCard.ObjectId));
        Assert.Same(world.Seats[recipient].Hand, top.Area);
        for (int player = 0; player < 2; player++)
            Assert.Equal(held[player] + (player == recipient ? 1 : 0), world.Seats[player].Hand.Cards.Count);
        Assert.False(source.Ready);
        Assert.DoesNotContain(runner.Actions(world, 1), item => item.Card == source.ObjectId);
    }

    [Fact]
    public void ReprintInventoryIsExactlySixResearchFaces()
    {
        using var book = JsonDocument.Parse(AuthoredCards.Text);
        var rows = book.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        string[] faces = ["09017", "09018", "09022", "09023", "09024", "09025"];
        Assert.Equal(AuthoredCards.ExpectedFaceCount, rows.Length);
        Assert.Equal(faces, rows.Skip(645).Take(6).Select(row => row.GetProperty("card").GetString()));
        Assert.Equal(faces.Concat(new List<string> { "09009", "09011", "09026", "09001a", "09001b", "09002", "09005", "09036", "09008", "09012", "09016", "09021", "09037", "09038", "09033", "09035", "09006", "09010", "09032", "09034", "09020", "09013", "09019", "09039", "09003", "09004", "09007", "09014", "09015", "09027", "09029", "09028", "09031", "09030" }).Order(), AuthoredCards.Book.Authored.Where(face => face.StartsWith("09", StringComparison.Ordinal)).Order());
        Assert.DoesNotContain("11007a", AuthoredCards.Book.Authored);
    }
}
