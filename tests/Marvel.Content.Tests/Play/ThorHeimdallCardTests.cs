using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual full-book entry response on nonpublic supported boards.
public sealed class ThorHeimdallCardTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [Rule("rr:look-looked-at.1")]
    [Rule("rr:discard.5")]
    public void EntryRetainsTheLookedAtPhysicalGroupAndChosenOrder(int count)
    {
        // Looked-at cards "are still considered part of that deck"; a selected
        // looked-at card is discarded "from the top of that deck".
        var world = ThorActualFixture.Board("06001b", 2);
        var runner = AuthoredCards.Runner();
        var seat = world.Seats[1];
        var deck = world.AreaOf(DeckType.EncounterDeck);
        foreach (var card in deck.Cards.ToArray())
            World.MoveToTop(card, world.AreaOf(DeckType.EncounterDiscardPile));
        for (int i = 0; i < count; i++) world.CreateCard(i % 2 == 0 ? "01104" : "01121", deck);
        int[] looked = deck.Cards.Reverse().Take(3).Select(c => c.ObjectId).ToArray();
        var ally = world.CreateCard("06020", seat.Hand);
        var payment = Enumerable.Range(0, 5).Select(_ => world.CreateCard("01003", seat.Hand)).ToArray();
        CardPlay.Play(world, world.Facts, runner, seat, ally, payment.Select(c => c.ObjectId).ToArray(), []);
        var prompt = Sequence.Work(world, world.Facts, runner, []);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.All(payment, c => Assert.Equal(DeckType.DiscardPile, c.Area.Type));
        if (count == 0)
        {
            Assert.Null(prompt);
            Assert.Empty(world.Agenda.Outstanding);
            return;
        }
        var response = Assert.Single(prompt!.Affordances, a => a.AnchorId == ally.ObjectId);
        Assert.Equal(1, prompt.Player);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(response.Id), []);
        prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Empty(prompt.ContextCardIds);
        Assert.Equal(looked, deck.Cards.Reverse().Take(3).Select(c => c.ObjectId));
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(0), []);
        prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Equal(looked, prompt.ContextCardIds);
        int discarded = looked[looked.Length / 2];
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var outstanding = world.Agenda.Outstanding.ToArray();
        Assert.Throws<ArgumentException>(() => Sequence.Answer(world, world.Facts, runner,
            prompt, Decision.Take(ally.ObjectId), []));
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(outstanding, world.Agenda.Outstanding);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(discarded), []);
        int[] remaining = looked.Where(id => id != discarded).Reverse().ToArray();
        if (remaining.Length > 0)
        {
            prompt = Sequence.Work(world, world.Facts, runner, [])!;
            Assert.Equal(Question.Order, prompt.Asking);
            Assert.Equal(remaining.Order(), prompt.ContextCardIds.Order());
            Sequence.Answer(world, world.Facts, runner, prompt, new Decision(ally.ObjectId, remaining), []);
        }
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(count == 1 ? DeckType.EncounterDeck : DeckType.EncounterDiscardPile, world.Cards[discarded].Area.Type);
        Assert.Equal(remaining, deck.Cards.Reverse().Take(remaining.Length).Select(c => c.ObjectId));
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    public void EntryResponseCanBeDeclinedWithoutInspectingOrMovingCards()
    {
        var world = ThorActualFixture.Board("06001b");
        var runner = AuthoredCards.Runner();
        var ally = world.CreateCard("06020", world.Seats[0].Hand);
        CardPlay.PutAllyIntoPlay(world, world.Facts, runner, ally, 0, "fixture", []);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Contains(prompt.Affordances, a => a.AnchorId == ally.ObjectId);
        Assert.DoesNotContain(prompt.ContextCardIds, id => world.Cards[id].Area.Type == DeckType.EncounterDeck);
        string before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Decline, []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Empty(world.Agenda.Outstanding);
    }
}
