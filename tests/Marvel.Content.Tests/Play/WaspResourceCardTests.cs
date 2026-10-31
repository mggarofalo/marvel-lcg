using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual production rows on synthetic boards; no public pack admission.
public sealed class WaspResourceCardTests : CoreCardTestBase
{
    private const string Energy = "01088";
    private const string Genius = "01089";
    private const string PowerOfAggression = "01055";
    private const string AvengersMansion = "01091";

    [Theory]
    [InlineData(new[] { Energy }, 2)]
    [InlineData(new[] { Energy, Genius }, 2)]
    [InlineData(new[] { Energy, Energy }, 3)]
    [InlineData(new[] { Genius }, 0)]
    [Rule("rr:cost.4.1")]
    public void JanetCountsOnlyOverpaidEnergyAndCapsAtThree(string[] faces, long expected)
    {
        // rr:cost.4.1: resources beyond a cost "were not paid for that cost"; printed
        // cost 0 makes every generated icon overpaid. Only energy places counters.
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001a");
        var seat = world.Seats[0];
        var janet = world.CreateCard("13012", seat.Hand);
        var payments = faces.Select(face => world.CreateCard(face, seat.Hand)).ToArray();
        CardPlay.Play(world, world.Facts, runner, seat, janet, [.. payments.Select(card => card.ObjectId)], []);
        var interrupt = Sequence.Work(world, world.Facts, runner, []);
        // An interrupt that would place no counter changes nothing and is not offered.
        Assert.Equal(expected > 0, interrupt is not null);
        if (interrupt is not null)
        {
            var offer = Assert.Single(interrupt.Affordances, item => item.AnchorId == janet.ObjectId);
            Sequence.Answer(world, world.Facts, runner, interrupt, Decision.Take(offer.Id), []);
            Sequence.Finish(world, world.Facts, runner, []);
        }
        Assert.Equal(expected, janet.Tokens.GetValueOrDefault("c_pym"));
        // rr:hit-points.3: with no counter Janet has 0 hit points and is defeated.
        Assert.Equal(expected > 0 ? DeckType.AlliesArea : DeckType.DiscardPile, janet.Area.Type);
        if (expected > 0) Assert.Equal(expected, DamagePlacement.Health(world, world.Facts, janet));
    }

    [Fact]
    [Rule("rr:wild-resource.1")]
    public void PowerOfAggressionOverpaysOneWildCountedAsEnergy()
    {
        // rr:wild-resource.1: "Wild resources can be spent to pay the resource cost of cards and abilities."
        // Ruling 5e6ac90dc4187eef: "The Power Of cards will only generate one resource
        // for these allies." The engine counts the overpaid wild as declared energy.
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001a");
        var seat = world.Seats[0];
        var janet = world.CreateCard("13012", seat.Hand);
        var power = world.CreateCard(PowerOfAggression, seat.Hand);
        CardPlay.Play(world, world.Facts, runner, seat, janet, [power.ObjectId], []);
        var interrupt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, interrupt,
            Decision.Take(Assert.Single(interrupt.Affordances, item => item.AnchorId == janet.ObjectId).Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(1, janet.Tokens.GetValueOrDefault("c_pym"));
        Assert.Equal(DeckType.AlliesArea, janet.Area.Type);
    }

    [Fact]
    [Rule("rr:wild-resource.3")]
    public void NadiaShufflesOnlyCardsWithAPrintedMentalResourceOncePerRound()
    {
        // rr:wild-resource.3: "When resources are not being generated for a cost, a wild resource does not have any characteristic other than “wild resource.” In such contexts, wild resources cannot be interpreted as any of the other resource types."
        // A wild icon is not a mental icon outside payment (rr:wild-resource.3).
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001b");
        var discard = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0);
        var genius = world.CreateCard(Genius, discard);
        var mansion = world.CreateCard(AvengersMansion, discard);
        var energy = world.CreateCard(Energy, discard);
        var power = world.CreateCard(PowerOfAggression, discard);
        var seat = world.Seats[0];
        int deck = seat.Deck.Cards.Count;
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == seat.IdentityCard.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        var offer = Assert.Single(prompt.Affordances);
        Assert.Equal([genius.ObjectId, mansion.ObjectId], offer.Targets!.Legal.Order());
        Assert.Equal(0, offer.Targets.Min);
        Assert.Equal(2, offer.Targets.Max);
        Sequence.Answer(world, world.Facts, runner, prompt,
            Decision.Take(offer.Id) with { Targets = [genius.ObjectId, mansion.ObjectId] }, []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(seat.Deck, genius.Area);
        Assert.Equal(seat.Deck, mansion.Area);
        Assert.Equal(DeckType.DiscardPile, energy.Area.Type);
        Assert.Equal(DeckType.DiscardPile, power.Area.Type);
        Assert.Equal(deck + 2, seat.Deck.Cards.Count);
        Assert.DoesNotContain(runner.Actions(world, 0), a => a.Card == seat.IdentityCard.ObjectId);
    }

    [Fact]
    [Rule("rr:status-cards.2")]
    public void AthleticConditioningDiscardsOnlyAStunOrConfuse()
    {
        // rr:status-cards.2: "Status card abilities have timing priority over all conflicting triggered abilities."
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001a");
        var hero = world.Seats[0].IdentityCard;
        var tough = Statuses.Give(world, hero, Statuses.Tough);
        var stunned = Statuses.Give(world, hero, Statuses.Stunned);
        var confused = Statuses.Give(world, hero, Statuses.Confused);
        var card = world.CreateCard("13034", world.Seats[0].Hand);
        var resource = world.CreateCard(Genius, world.Seats[0].Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == card.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [resource.ObjectId], []));
        var prompt = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal([stunned.ObjectId, confused.ObjectId],
            prompt.Affordances.Select(item => item.AnchorId).Order());
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(confused.ObjectId), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(0, Statuses.Count(world, hero, Statuses.Confused));
        Assert.Equal(1, Statuses.Count(world, hero, Statuses.Stunned));
        Assert.Equal(1, Statuses.Count(world, hero, Statuses.Tough));
        Assert.Equal(DeckType.DiscardPile, card.Area.Type);
        _ = tough;
    }

    [Fact]
    [Rule("rr:status-cards.2")]
    public void AthleticConditioningIsNotOfferedWithOnlyATough()
    {
        // rr:status-cards.2: "Status card abilities have timing priority over all conflicting triggered abilities."
        var runner = AuthoredCards.Runner();
        var world = WaspBoards.Board("13001a");
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Tough);
        var card = world.CreateCard("13034", world.Seats[0].Hand);
        world.CreateCard(Genius, world.Seats[0].Hand);
        Assert.DoesNotContain(runner.Actions(world, 0), a => a.Card == card.ObjectId);
    }
}
