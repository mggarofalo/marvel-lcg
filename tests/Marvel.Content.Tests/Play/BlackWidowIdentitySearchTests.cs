using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.BlackWidowIdentitySearchFixture;

namespace Marvel.Content.Tests.Play;

public sealed class BlackWidowIdentitySearchTests
{
    private static readonly string[] PreparationFaces = ["08006", "08008", "08010", "08017", "08018"];

    [Fact]
    [Rule("rr:limit")]
    [Rule("rr:form-change-form.2")]
    [Rule("rr:play-put-into-play.3")]
    public void ActualNatashaDrawIsOwnPlayOnlyAndBothPhaseBoundariesRestoreUse()
    {
        // "When a player changes form, only the form changes." The printed limit is once per phase.
        var world = Board();
        var seat = world.Seats[1];
        var runner = Runner();
        var put = world.CreateCard("08017", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1)));
        Reveal.EnterPlay(world, world.Facts, put, [], abilities: runner);
        // "A card that is put into play is not considered to have been played."
        // The isolated upgrade entry owns an entry lifecycle, without a card-play lifecycle.
        world.Agenda.Then(new PhaseStep(new LifecycleOperation(LifecycleKind.CardEntersPlay),
            0, 0, Subject: put.ObjectId, Seat: 1));
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        PlayCard(world, "08017", 0);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        PlayCard(world, "08015", 1);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        for (int phase = 0; phase < 3; phase++)
        {
            PlayCard(world, "08017", 1);
            var prompt = Prompt(world);
            Assert.Equal(1, prompt.Player);
            int hand = seat.Hand.Cards.Count;
            Answer(world, prompt, Assert.Single(prompt.Affordances, a => a.AnchorId == seat.IdentityCard.ObjectId).Id);
            Finish(world);
            Assert.Equal(hand + 1, seat.Hand.Cards.Count);
            seat.IdentityCard.TurnTo("08001a");
            seat.IdentityCard.TurnTo("08001b");
            PlayCard(world, "08017", 1);
            Assert.Null(Sequence.Work(world, world.Facts, runner, []));
            if (phase == 0) PhaseEnd.EndPlayerPhase(world, []);
            else if (phase == 1) PhaseEnd.EndVillainPhase(world, world.Facts, []);
        }
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(0, 4)]
    [InlineData(2, 2)]
    [InlineData(5, 0)]
    public void ActualWinterSoldierPricesAndPlaysUsingOnlyControlledPreparations(int count, int expected)
    {
        var world = Board();
        var runner = AuthoredCards.Runner();
        var seat = world.Seats[1];
        for (int i = 0; i < count; i++)
            world.CreateCard(PreparationFaces[i], world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1)));
        world.CreateCard("08017", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0)));
        world.CreateCard("08017", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1)));
        world.CreateCard("08011", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1)));
        world.CreateCard("08005", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1)));
        var card = world.CreateCard("08002", seat.Hand);
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        for (int i = 0; i < 3; i++)
            Assert.Equal(expected, CardPayment.CostOf(world, world.Facts, seat, card).Amount);
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Outstanding);
        int[] payment = Enumerable.Range(0, expected).Select(_ => world.CreateCard("01093", seat.Hand).ObjectId).ToArray();
        CardPlay.Play(world, world.Facts, runner, seat, card, payment, []);
        Finish(world);
        Assert.Equal(DeckType.AlliesArea, card.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ActualCoulsonSelectsExactOwnPreparationAndAlwaysShuffles(bool discard)
    {
        var world = Board();
        var seat = world.Seats[1];
        var deck = world.CreateCard("08017", seat.Deck);
        var pile = world.CreateCard("08017", world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1)));
        var foreign = world.CreateCard("08017", world.Seats[0].Deck);
        var coulson = PlayCard(world, "08011", 1);
        var response = Prompt(world);
        Assert.Equal(1, response.Player);
        Assert.Null(Assert.Single(response.Affordances, a => a.AnchorId == coulson.ObjectId).Targets);
        Answer(world, response, Assert.Single(response.Affordances, a => a.AnchorId == coulson.ObjectId).Id);
        var selection = Prompt(world);
        Assert.Equal(1, selection.Player);
        Assert.Equal(new[] { deck.ObjectId, pile.ObjectId }.Order(), selection.Affordances.Select(a => a.AnchorId).Order());
        Assert.DoesNotContain(selection.Affordances, a => a.AnchorId == foreign.ObjectId);
        var chosen = discard ? pile : deck;
        long words = world.Random.Generator.WordsConsumed;
        // The accepted entry response retains its controller even if Coulson leaves before selection.
        Discard.Card(world, coulson, "test departure", []);
        Answer(world, selection, Assert.Single(selection.Affordances, a => a.AnchorId == chosen.ObjectId).Id);
        Finish(world);
        Assert.Same(seat.Hand, chosen.Area);
        Assert.True(world.Random.Generator.WordsConsumed > words);
        Assert.Same(world.Seats[0].Deck, foreign.Area);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void ActualCoulsonNoMatchStillOffersResponseAndShuffles()
    {
        var world = Board();
        var card = PlayCard(world, "08011", 1);
        var prompt = Prompt(world);
        long before = world.Random.Generator.WordsConsumed;
        Answer(world, prompt, Assert.Single(prompt.Affordances, a => a.AnchorId == card.ObjectId).Id);
        Finish(world);
        Assert.True(world.Random.Generator.WordsConsumed > before);
        Assert.Empty(world.Seats[1].Hand.Cards);
        Assert.Empty(world.Agenda.Outstanding);
    }
    [Fact]
    public void HeroSidePlayDoesNotConsumeNatashasLaterAlterEgoResponse()
    {
        var world = Board();
        var seat = world.Seats[1];
        seat.IdentityCard.TurnTo("08001a");
        PlayCard(world, "08017", 1);
        Assert.Null(Sequence.Work(world, world.Facts, world.Abilities!, []));
        seat.IdentityCard.TurnTo("08001b");
        PlayCard(world, "08017", 1);
        var response = Prompt(world);
        Assert.Contains(response.Affordances, a => a.AnchorId == seat.IdentityCard.ObjectId);
        Sequence.Answer(world, world.Facts, world.Abilities!, response, Decision.Decline, []);
        Finish(world);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void CoulsonRejectsForeignSelectionWithoutChangingPendingSearch()
    {
        var world = Board();
        var own = world.CreateCard("08017", world.Seats[1].Deck);
        var foreign = world.CreateCard("08017", world.Seats[0].Deck);
        var coulson = PlayCard(world, "08011", 1);
        var response = Prompt(world);
        Answer(world, response, Assert.Single(response.Affordances, a => a.AnchorId == coulson.ObjectId).Id);
        var selection = Prompt(world);
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();
        Assert.Throws<ArgumentException>(() => Answer(world, selection, foreign.ObjectId));
        Assert.Equal(digest, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(pending, world.Agenda.Outstanding);
        Answer(world, selection, Assert.Single(selection.Affordances).Id);
        Finish(world);
        Assert.Same(world.Seats[1].Hand, own.Area);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void IdentitySearchRowsHaveNoStartingCountersAndUnrelatedContentRemainsUnauthored()
    {
        using var json = System.Text.Json.JsonDocument.Parse(AuthoredCards.Text);
        var rows = json.RootElement.GetProperty("cards").EnumerateArray()
            .Where(row => new[] { "08001b", "08002", "08011" }.Contains(row.GetProperty("card").GetString())).ToArray();
        Assert.Equal(3, rows.Length);
        Assert.All(rows, row => Assert.False(row.TryGetProperty("startingCounters", out _)));
        Assert.DoesNotContain("11007a", AuthoredCards.Book.Authored);
    }

}
