using System.Text.Json;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Events;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.BlackWidowIdentitySearchFixture;
using static Marvel.Content.Tests.Play.DoctorStrangeSearchFixture;

namespace Marvel.Content.Tests.Play;

public sealed class DoctorStrangeSearchCardTests
{
    private static readonly string[] SearchFaces = ["09008", "09012"];
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:search.1")]
    [Rule("rr:search.3")]
    public void BrotherChoosesExactOwnTopFiveEventBeforeShuffling(bool shortDeck)
    {
        // "If a player finds multiple cards ... the player chooses among those options."
        // "If any portion of a deck is searched ... shuffle that entire deck."
        var world = DoctorStrangeSearchFixture.Board();
        ClearDeck(world);
        var seat = world.Seats[1];
        var sixth = shortDeck ? null : world.CreateCard("01005", seat.Deck);
        var first = world.CreateCard("01005", seat.Deck);
        var second = world.CreateCard("01005", seat.Deck);
        for (int i = 0; i < (shortDeck ? 1 : 3); i++) world.CreateCard("01088", seat.Deck);
        var foreign = world.CreateCard("01005", world.Seats[0].Deck);
        var discard = world.CreateCard("01005", DoctorStrangeSearchFixture.Discard(world));
        var invocation = seat.InvocationDeck!;
        var invocationState = invocation.Deck.Cards.Concat(invocation.Discard.Cards).Select(c => (c.ObjectId, c.Area)).ToArray();
        var brother = PlayCard(world, "09012", 1);
        var response = Prompt(world);
        Answer(world, response, Assert.Single(response.Affordances, a => a.AnchorId == brother.ObjectId).Id);
        var selection = Prompt(world);
        Assert.Equal(1, selection.Player);
        Assert.Equal(new[] { first.ObjectId, second.ObjectId }.Order(), selection.Affordances.Select(a => a.AnchorId).Order());
        Assert.Same(seat.Deck, first.Area);
        Assert.Same(seat.Deck, second.Area);
        string before = SpellMasteryFixture.Snapshot(world);
        Assert.Throws<ArgumentException>(() => Answer(world, selection, foreign.ObjectId));
        Assert.Equal(before, SpellMasteryFixture.Snapshot(world));
        long words = world.Random.Generator.WordsConsumed;
        // The controller remains the answering player after the accepted response source leaves.
        Marvel.Rules.Play.Discard.Card(world, brother, "test departure", []);
        Answer(world, selection, Assert.Single(selection.Affordances, a => a.AnchorId == second.ObjectId).Id);
        Finish(world);
        Assert.Same(seat.Hand, second.Area);
        Assert.Same(seat.Deck, first.Area);
        if (sixth is not null) Assert.Same(seat.Deck, sixth.Area);
        Assert.Same(world.Seats[0].Deck, foreign.Area);
        Assert.Same(DoctorStrangeSearchFixture.Discard(world), discard.Area);
        Assert.True(world.Random.Generator.WordsConsumed > words);
        Assert.Equal(invocationState, invocation.Deck.Cards.Concat(invocation.Discard.Cards).Select(c => (c.ObjectId, c.Area)));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:target.6")]
    [Rule("rr:search.3")]
    public void BrotherNoMatchStillOffersAndShufflesWholeDeck()
    {
        // "An ability with a search effect requires only a searchable game area ... to initiate."
        // "If any portion of a deck is searched ... shuffle that entire deck."
        var world = DoctorStrangeSearchFixture.Board();
        var seat = world.Seats[1];
        var sixth = world.CreateCard("01005", seat.Deck);
        for (int i = 0; i < 5; i++) world.CreateCard("01088", seat.Deck);
        var brother = PlayCard(world, "09012", 1);
        var response = Prompt(world);
        long words = world.Random.Generator.WordsConsumed;
        Answer(world, response, Assert.Single(response.Affordances, a => a.AnchorId == brother.ObjectId).Id);
        Finish(world);
        Assert.True(world.Random.Generator.WordsConsumed > words);
        Assert.Same(seat.Deck, sixth.Area);
        Assert.Empty(seat.Hand.Cards);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:ability.13")]
    [Rule("rr:initiating-abilities.step.5")]
    public void SanctumSelectsOwnPhysicalSpellAndPaysExhaustionOnceAcrossSelection()
    {
        // Form abilities "can only be used ... in the specified form"; "Pay the cost(s)."
        var world = DoctorStrangeSearchFixture.Board();
        var source = Sanctum(world);
        var first = world.CreateCard("09004", DoctorStrangeSearchFixture.Discard(world));
        var second = world.CreateCard("09004", DoctorStrangeSearchFixture.Discard(world));
        var foreign = world.CreateCard("09004", DoctorStrangeSearchFixture.Discard(world, 0));
        var nonSpell = world.CreateCard("09005", DoctorStrangeSearchFixture.Discard(world));
        Assert.DoesNotContain(world.Abilities!.Actions(world, 0), a => a.Card == source.ObjectId);
        world.Seats[1].IdentityCard.TurnTo("09001a");
        Assert.DoesNotContain(world.Abilities.Actions(world, 1), a => a.Card == source.ObjectId);
        world.Seats[1].IdentityCard.TurnTo("09001b");
        Activate(world, source);
        Assert.False(source.Ready);
        var selection = Prompt(world);
        Assert.Equal(1, selection.Player);
        Assert.Equal(new[] { first.ObjectId, second.ObjectId }.Order(), selection.Affordances.Select(a => a.AnchorId).Order());
        string before = SpellMasteryFixture.Snapshot(world);
        Assert.Throws<ArgumentException>(() => Answer(world, selection, foreign.ObjectId));
        Assert.Equal(before, SpellMasteryFixture.Snapshot(world));
        // A ready source at resume distinguishes repayment from a single committed arrow cost.
        source.Refresh();
        Answer(world, selection, Assert.Single(selection.Affordances, a => a.AnchorId == second.ObjectId).Id);
        Finish(world);
        Assert.True(source.Ready);
        Assert.Equal(0, source.Owner);
        Assert.NotSame(DoctorStrangeSearchFixture.Discard(world), second.Area);
        Assert.Same(DoctorStrangeSearchFixture.Discard(world), first.Area);
        Assert.Same(DoctorStrangeSearchFixture.Discard(world, 0), foreign.Area);
        Assert.Same(DoctorStrangeSearchFixture.Discard(world), nonSpell.Area);
        Assert.Single(world.Seats[1].Hand.Cards);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void SanctumShufflesChosenSpellBeforeIndependentDraw()
    {
        var world = DoctorStrangeSearchFixture.Board();
        var source = Sanctum(world);
        var spell = world.CreateCard("09004", DoctorStrangeSearchFixture.Discard(world));
        world.CreateCard("09004", DoctorStrangeSearchFixture.Discard(world));
        Activate(world, source);
        var selection = Prompt(world);
        var events = new List<GameEvent>();
        Sequence.Answer(world, world.Facts, world.Abilities!, selection,
            Decision.Take(Assert.Single(selection.Affordances, a => a.AnchorId == spell.ObjectId).Id), events);
        Sequence.Finish(world, world.Facts, world.Abilities!, events);
        int shuffle = events.FindIndex(e => e is CardsMoved { Verb: "Shuffle_Into" });
        int draw = events.FindIndex(e => e is CardsMoved { Verb: "Draw" });
        Assert.True(shuffle >= 0);
        Assert.True(draw > shuffle);
        Assert.Single(world.Seats[1].Hand.Cards);
        Assert.False(source.Ready);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:target.2.3")]
    [Rule("rr:target.3")]
    public void SanctumWithoutSpellCanDrawOnlyFromNonemptyDeck(bool emptyDeck)
    {
        // Draw abilities "always have a valid target so long as ... at least one card in their deck."
        // "A target is valid ... if any part of that ability can affect that target."
        var world = DoctorStrangeSearchFixture.Board();
        if (emptyDeck) ClearDeck(world);
        var source = Sanctum(world);
        var actions = world.Abilities!.Actions(world, 1).Where(a => a.Card == source.ObjectId).ToArray();
        Assert.Equal(emptyDeck ? 0 : 1, actions.Length);
        if (!emptyDeck)
        {
            Activate(world, source);
            Finish(world);
            Assert.Single(world.Seats[1].Hand.Cards);
            Assert.False(source.Ready);
        }
        else Assert.True(source.Ready);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void SearchPacketAddsOnlyTwoReviewedFacesWithoutStartingCounters()
    {
        using var json = JsonDocument.Parse(AuthoredCards.Text);
        var rows = json.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.Equal(667, rows.Length);
        Assert.Equal(SearchFaces, rows.Skip(659).Take(2).Select(r => r.GetProperty("card").GetString()));
        Assert.All(rows.Skip(659).Take(2), row => Assert.False(row.TryGetProperty("startingCounters", out _)));
        Assert.DoesNotContain("09006", AuthoredCards.Book.Authored);
    }
}
