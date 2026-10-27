using System.Text.Json;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DoctorStrangeFinalSupportInventoryTests
{
    private static readonly string[] FinalFaces = ["09013", "09019", "09039"];
    [Fact]
    public void ExactlyThreeFinalSupportFacesAreAppendedWithOnlyMedicalUses()
    {
        using var document = JsonDocument.Parse(AuthoredCards.Text);
        var rows = document.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.Equal(719, rows.Length);
        Assert.Equal(FinalFaces, rows.Skip(672).Take(3).Select(row => row.GetProperty("card").GetString()));
        Assert.Equal(40, AuthoredCards.Book.Authored.Count(face => face.StartsWith("09", StringComparison.Ordinal)));
        Assert.False(rows[672].TryGetProperty("startingCounters", out _));
        Assert.False(rows[674].TryGetProperty("startingCounters", out _));
        var counters = rows[673].GetProperty("startingCounters");
        Assert.Equal("medical", counters.GetProperty("type").GetString());
        Assert.Equal(3, counters.GetProperty("count").GetInt32());
        Assert.True(counters.GetProperty("uses").GetBoolean());
        Assert.DoesNotContain("11001", AuthoredCards.Book.Authored);
    }

    [Fact]
    public void PlayingActualNightNursePaysOneAndEntersWithThreeMedicalUses()
    {
        Card? nurse = null, resource = null;
        var (game, world) = Playing(board =>
        {
            nurse = board.CreateCard("09019", board.Seats[0].Hand);
            resource = board.CreateCard("01089", board.Seats[0].Hand);
        }, abilities: AuthoredCards.Runner());
        var play = Assert.Single(game.Pending!.Affordances, option => option.PlaysCard && option.AnchorId == nurse!.ObjectId);
        var cost = Assert.Single(play.CostOptions);
        Assert.Equal("1", cost.Cost);
        int[] paid = [resource!.ObjectId];
        game.Resolve(new Decision(play.Id, [world.Seats[0].IdentityCard.ObjectId], paid, Allocations: ResourcePayment.Allocate(cost, paid)));
        Assert.Equal(DeckType.SupportsArea, nurse!.Area.Type);
        Assert.Equal(3, nurse.Tokens["c_medical"]);
        Assert.True(nurse.Ready);
        Assert.Equal(DeckType.DiscardPile, resource.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
