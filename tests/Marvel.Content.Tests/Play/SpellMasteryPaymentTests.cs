using static Marvel.Content.Tests.Play.SpellMasteryFixture;
using Marvel.Cards.Run;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic paid Special controls; actual Winds retains its printed zero cost.</summary>
public sealed class SpellMasteryPaymentTests
{
    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void SyntheticPaidInvocationRejectsInsufficientPaymentWithoutCommittingAnything()
    {
        // "Pay the cost(s)." Actual Crimson Bands costs two; its draw/discard
        // Special is deliberately synthetic here to isolate dynamic payment.
        var (world, game, deck, _) = Board(1, paid: true);
        var hero = world.Seats[1].IdentityCard;
        var top = deck.PublicTop!;
        var offer = Mastery(game, hero);
        Assert.Equal("2", Assert.Single(offer.CostOptions).Cost);
        var prompt = game.Pending;
        string before = Snapshot(world);
        var rejected = Assert.Throws<ArgumentException>(() =>
            game.Resolve(Decision.Take(offer.Id, [top.ObjectId], [])));
        Assert.Contains("InvalidPayment", rejected.Message, StringComparison.Ordinal);
        Assert.Equal(before, Snapshot(world));
        Assert.Same(prompt, game.Pending);
        Assert.True(hero.Ready);
        var payment = world.Seats[1].Hand.Cards[0];
        int hand = world.Seats[1].Hand.Cards.Count;
        game.Resolve(PaidAnswer(offer, top, payment));
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Same(deck.Discard, top.Area);
        Assert.False(hero.Ready);
        Assert.Equal(hand - 1 + 3, world.Seats[1].Hand.Cards.Count);
        Assert.Empty(world.Agenda.Outstanding);
        var nextPrompt = game.Pending;
        string completed = Snapshot(world);
        Assert.Throws<ArgumentException>(() => game.Resolve(PaidAnswer(offer, top, payment)));
        Assert.Equal(completed, Snapshot(world));
        Assert.Same(nextPrompt, game.Pending);
    }

    [Fact]
    [Rule("rr:cost.5")]
    public void SyntheticPaidInvocationPreservesLiveResourceWorkAcrossSuspension()
    {
        // Multiple costs "must be paid simultaneously". The synthetic generator
        // defeats itself, opens an interrupt, and generates two resources once.
        var (world, game, deck, runner) = Board(1, paid: true, live: true);
        var hero = world.Seats[1].IdentityCard;
        var top = deck.PublicTop!;
        var generator = Generator(world);
        var offer = Mastery(game, hero);
        int hand = world.Seats[1].Hand.Cards.Count;
        var beginning = game.Resolve(PaidAnswer(offer, top, generator));
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        Assert.Equal(1, game.Pending.Player);
        Assert.Same(top, deck.PublicTop);
        Assert.Equal(hand, world.Seats[1].Hand.Cards.Count);
        var interrupt = Assert.Single(game.Pending.Affordances);
        var ending = game.Resolve(Decision.Take(interrupt.Id));
        Assert.Equal(hand + 1 + 3, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(DeckType.DiscardPile, generator.Area.Type);
        Assert.Same(deck.Discard, top.Area);
        Assert.False(hero.Ready);
        Assert.DoesNotContain(runner.ResourceAbilities(world, 1), source => source.Effect == generator.ObjectId);
        Assert.DoesNotContain(beginning.Events.Concat(ending.Events), happened => happened.Verb == CardPlay.Verb);
        Assert.Equal(Question.TurnOption, game.Pending!.Asking);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Theory]
    [InlineData("owner")]
    [InlineData("incarnation")]
    [InlineData("replacement")]
    public void SyntheticSuspendedPaymentCannotResolveAChangedCapturedInvocation(string change)
    {
        var (world, game, deck, _) = Board(1, paid: true, live: true);
        var hero = world.Seats[1].IdentityCard;
        var top = deck.PublicTop!;
        int hand = world.Seats[1].Hand.Cards.Count;
        game.Resolve(PaidAnswer(Mastery(game, hero), top, Generator(world)));
        Assert.Equal(Question.Opportunity, game.Pending!.Asking);
        switch (change)
        {
            case "owner": top.TransferScenarioOwnership(0); break;
            case "incarnation":
                int incarnation = top.Incarnation;
                World.MoveToTop(top, world.Seats[1].Hero);
                World.MoveToTop(top, deck.Deck);
                top.TurnFaceUp();
                Assert.NotEqual(incarnation, top.Incarnation);
                break;
            case "replacement": deck.DiscardTop(top, "synthetic-probe", []); break;
        }
        var error = Assert.Throws<RulesNotImplementedException>(() => game.Resolve(Decision.Decline));
        Assert.Contains("captured Invocation top is no longer current", error.Message, StringComparison.Ordinal);
        Assert.Equal(hand, world.Seats[1].Hand.Cards.Count);
        Assert.Equal(change == "replacement" ? 1 : 0, deck.Discard.Cards.Count);
    }

    [Fact]
    public void AnUnauthoredActualInvocationSpecialRaisesExplicitly()
    {
        var (world, game, deck, _) = Board(1, paid: true, unsupported: true);
        var top = deck.PublicTop!;
        var hero = world.Seats[1].IdentityCard;
        var offer = Mastery(game, hero);
        var payment = world.Seats[1].Hand.Cards[0];
        int hand = world.Seats[1].Hand.Cards.Count;
        Assert.Equal("09032", top.FaceId);
        var error = Assert.Throws<RulesNotImplementedException>(() => game.Resolve(
            PaidAnswer(offer, top, payment)));
        Assert.Contains("09032' has no authored Special ability", error.Message, StringComparison.Ordinal);
        Assert.False(hero.Ready);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Equal(hand - 1, world.Seats[1].Hand.Cards.Count);
        Assert.Empty(deck.Discard.Cards);
        Assert.Same(top, deck.PublicTop);
    }

    private static Card Generator(World world) => Assert.Single(world.Cards,
        card => card.FaceId == "01030" && card.Owner == 1);

    private static Decision PaidAnswer(Affordance offer, Card top, Card generator)
    {
        int[] paying = [generator.ObjectId];
        var allocation = Assert.IsAssignableFrom<IReadOnlyList<ResourceAllocation>>(
            ResourcePayment.Allocate(Assert.Single(offer.CostOptions), paying));
        return new Decision(offer.Id, [top.ObjectId], paying, Allocations: allocation);
    }
}
