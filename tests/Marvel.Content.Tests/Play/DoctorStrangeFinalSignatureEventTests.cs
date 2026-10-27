using System.Text.Json;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DoctorStrangeFinalSignatureEventTests
{
    private static readonly string[] FinalFaces = ["09003", "09004", "09007"];
    [Fact]
    public void ThreeEventsAppendWithoutStartingCountersOrProductAdmission()
    {
        using var book = JsonDocument.Parse(AuthoredCards.Text);
        var rows = book.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.Equal(684, rows.Length);
        Assert.Equal(FinalFaces, rows.Skip(675).Take(3)
            .Select(row => row.GetProperty("card").GetString()));
        Assert.All(rows.Skip(675).Take(3), row => Assert.False(row.TryGetProperty("startingCounters", out _)));
        Assert.Equal(39, AuthoredCards.Book.Authored.Count(face => face.StartsWith("09", StringComparison.Ordinal)));
        Assert.DoesNotContain("09030", AuthoredCards.Book.Authored);
    }

    [Theory]
    [InlineData("09003")]
    [InlineData("09004")]
    public void PrintedHeroActionRequiresHeroFormWithSameCostLegalControl(string face)
    {
        var world = ThorActualFixture.Board("06001b");
        var source = world.CreateCard(face, world.Seats[0].Hand);
        world.CreateCard("01088", world.Seats[0].Hand);
        world.CreateCard("01089", world.Seats[0].Hand);
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 8);
        var runner = AuthoredCards.Runner();
        string before = world.Digest().Canonical();
        Assert.DoesNotContain(runner.Actions(world, 0), action => action.Card == source.ObjectId);
        Assert.Equal(before, world.Digest().Canonical());
        world.Seats[0].IdentityCard.TurnTo("06001a");
        Assert.Single(runner.Actions(world, 0), action => action.Card == source.ObjectId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:initiating-abilities.step.5")]
    public void ActualConfusedAstralStillRequiresItsArrowChoiceBeforePayment(bool nonscheme)
    {
        // "Pay the cost(s)" precedes initiation, including a confused thwart.
        var world = ThorActualFixture.Board("06001a");
        var source = world.CreateCard("09003", world.Seats[0].Hand);
        var payment = world.CreateCard("01088", world.Seats[0].Hand);
        world.TheCardIn(DeckType.MainSchemesArea)!.PlaceTokens("k_threat", 8);
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Confused);
        var runner = AuthoredCards.Runner();
        var action = Assert.Single(runner.Actions(world, 0), item => item.Card == source.ObjectId);
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(action, [payment.ObjectId], nonscheme ? [payment.ObjectId] : [])));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.False(world.Agenda.IsBusy);
    }

    [Theory]
    [InlineData("01004", false, false)]
    [InlineData("01004", true, false)]
    [InlineData("01004", false, true)]
    [InlineData("01075", false, false)]
    [InlineData("01075", true, false)]
    [InlineData("01075", false, true)]
    public void CoreCancellationUsesTheSameOriginRestriction(string face, bool deck, bool dealt)
    {
        // Ruling 7075a85e02f2203e: first-dealt cards qualify regardless of their
        // earlier source; a direct reveal from another game zone does not.
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var source = world.CreateCard(face, face == "01004" ? world.Seats[0].Hand
            : world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        world.CreateCard("01089", world.Seats[0].Hand);
        var card = world.CreateCard("01186", world.AreaOf(DeckType.RevealingArea));
        var origin = new EncounterRevealOrigin(card.ObjectId, Places.Reference(world.AreaOf(
            deck ? DeckType.EncounterDeck : DeckType.EncounterDiscardPile)), Dealt: dealt);
        var occurrence = new Occurrence(1, [Steps.CardRevealed], Subject: card.ObjectId, Player: 0)
        { RevealOrigin = origin };
        string before = world.Digest().Canonical();
        var offers = runner.Waiting(world, occurrence, WindowKind.Interrupt);
        Assert.Equal(deck || dealt, offers.Any(offer => offer.Card == source.ObjectId));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.True(source.Ready);
    }
}
