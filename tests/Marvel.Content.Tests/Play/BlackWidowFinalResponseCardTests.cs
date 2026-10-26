using System.Text.Json;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class BlackWidowFinalResponseCardTests
{
    [Fact]
    public void FinalResponseRowsCompleteThisPacketsExactInventory()
    {
        using var book = JsonDocument.Parse(AuthoredCards.Text);
        var rows = book.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.Equal(645, rows.Length);
        string[] added = ["08030", "08031"];
        Assert.Equal(added,
            rows.TakeLast(2).Select(row => row.GetProperty("card").GetString()));
        Assert.Equal(34, AuthoredCards.Book.Authored.Count(face => face.StartsWith("08", StringComparison.Ordinal)));
        Assert.All(rows.TakeLast(2), row => Assert.False(row.TryGetProperty("startingCounters", out _)));
        Assert.DoesNotContain("09020", AuthoredCards.Book.Authored);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void RapidResponseRejectsExtraneousPaymentBeforeAnyCostOrEffect(bool validGenerator)
    {
        var f = new RapidResponseFixture();
        var resource = f.World.CreateCard("01093", f.World.Seats[0].Hand);
        var prompt = f.Defeat();
        int offer = Assert.Single(prompt.Affordances, a => a.AnchorId == f.Rapid.ObjectId).Id;
        string digest = f.World.Digest().Canonical();
        var pending = f.World.Agenda.Outstanding.ToArray();
        var occurrence = f.World.Agenda.Occurrence;
        var random = f.World.Random.Generator.GetState();
        long words = f.World.Random.Generator.WordsConsumed;
        int eventCount = f.Events.Count;
        Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(f.World, f.World.Facts, f.Runner,
            prompt, Decision.Take(offer, [], [validGenerator ? resource.ObjectId : int.MaxValue]), f.Events));
        Assert.Equal(digest, f.World.Digest().Canonical());
        Assert.Equal(pending, f.World.Agenda.Outstanding);
        Assert.Same(occurrence, f.World.Agenda.Occurrence);
        Assert.Equal(random.Words, f.World.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, f.World.Random.Generator.GetState().Index);
        Assert.Equal(words, f.World.Random.Generator.WordsConsumed);
        Assert.Equal(eventCount, f.Events.Count);
        Assert.Equal(DeckType.UpgradesArea, f.Rapid.Area.Type);
        Assert.Equal(DeckType.DiscardPile, f.Ally.Area.Type);
        Assert.Same(f.World.Seats[0].Hand, resource.Area);
        f.Answer(prompt, f.Rapid.ObjectId);
        f.Work();
        Assert.Equal(1, f.Ally.Damage);
        Assert.Equal(DeckType.DiscardPile, f.Rapid.Area.Type);
        Assert.Same(f.World.Seats[0].Hand, resource.Area);
    }

    [Fact]
    [Rule("rr:leaves-play.1")]
    [Rule("rr:resolve.2")]
    public void PaidRapidResponseRetainsItsSourceReceiptThroughNestedEntryAndSourceReentry()
    {
        // Departure makes "a new copy of the card"; resolution requires
        // "one or more of its effects resolve", after the nested entry finishes.
        var f = new RapidResponseFixture(entryChoice: true);
        int incarnation = f.Rapid.Incarnation;
        var area = f.Rapid.Area;
        f.Answer(f.Defeat(), f.Rapid.ObjectId);
        var entry = f.Work();
        Assert.Equal(0, f.Ally.Damage);
        Assert.Equal(DeckType.DiscardPile, f.Rapid.Area.Type);
        f.Answer(entry, f.Ally.ObjectId);
        var choice = f.Work();
        World.MoveToTop(f.Rapid, area);
        Assert.True(f.Rapid.Incarnation > incarnation);
        Sequence.Answer(f.World, f.World.Facts, f.Runner, choice,
            Decision.Take(choice.Affordances[0].Id), f.Events);
        var completion = f.Work();
        var receipt = Assert.IsType<ResolvedPreparationAbility>(f.World.Agenda.Occurrence!.ActorFacts!.PreparationAbility);
        Assert.Equal("08031", receipt.Face);
        Assert.Equal(incarnation, receipt.Incarnation);
        Assert.Equal(0, receipt.Controller);
        Assert.Equal(1, f.Ally.Damage);
        Assert.Contains(completion.Affordances, a => a.AnchorId == f.World.Seats[0].IdentityCard.ObjectId);
        Assert.Equal(DeckType.UpgradesArea, f.Rapid.Area.Type);
    }
}
