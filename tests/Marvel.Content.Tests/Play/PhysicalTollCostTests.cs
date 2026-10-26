using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic 09027 candidate with actual production event and Invocation programs.</summary>
public sealed class PhysicalTollCostTests
{
    [Fact]
    [Rule("rr:obligation.1")]
    [Rule("rr:obligation.4")]
    public void RevealAssociatesStephenAndOptionalFlipCanExhaustAndRemove()
    {
        // "apply only to the player whose play area the obligation is in."
        var (world, _, source) = PhysicalTollFixture.Reveal(flip: true, remove: true);
        Assert.Equal("09001b", world.Seats[1].IdentityCard.FaceId);
        Assert.False(world.Seats[1].IdentityCard.Ready);
        Assert.True(world.Seats[0].IdentityCard.Ready);
        Assert.Equal(DeckType.RemovedArea, source.Area.Type);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Card == source.ObjectId);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.4")]
    public void SurchargePersistsAndQueriesDoNotSpendItOrAffectAnotherSeat()
    {
        // "Apply any modifiers to the cost(s)."
        var (world, runner, source) = PhysicalTollFixture.Reveal();
        var master = world.Seats[1].Hand.Cards.Single(card => card.FaceId == "09005");
        world.Effects.Expire(TimingPoints.EndOfPlayerPhase);
        world.Effects.Expire(TimingPoints.EndOfRound);
        string before = Snapshot(world);
        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(4, CardPayment.CostOf(world, world.Facts, world.Seats[1], master).Amount);
            Assert.Equal(1, CardPayment.CostOf(world, world.Facts, world.Seats[0], master).Amount);
            Assert.Contains(runner.Actions(world, 1), action => action.Card == master.ObjectId);
        }
        Assert.Equal(before, Snapshot(world));
        Assert.Equal(PlayArea.Of(1), source.Area.PlayArea);
        Assert.Equal("09001a", world.Seats[1].IdentityCard.FaceId);
    }

    [Fact]
    [Rule("rr:event.2")]
    public void SpecialDoesNotConsumeButActualMasterEventPaysAndDiscardsObligation()
    {
        // "the card is still considered to have been played" concerns events, not Special resolution.
        var (world, runner, source) = PhysicalTollFixture.Reveal();
        var hero = world.Seats[1].IdentityCard;
        var mastery = Assert.Single(runner.Actions(world, 1), action => action.Card == hero.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(mastery, [], [world.Seats[1].InvocationDeck!.PublicTop!.ObjectId]));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.ObligationsArea, source.Area.Type);
        Assert.Single(world.Effects.Registered, effect => effect.Card == source.ObjectId);
        (world, runner, source) = PhysicalTollFixture.Reveal();
        var master = world.Seats[1].Hand.Cards.Single(card => card.FaceId == "09005");
        // The newly exposed Invocation can add its own cost; supply the complete quoted payment.
        var action = Assert.Single(runner.Actions(world, 1), item => item.Card == master.ObjectId);
        var price = Assert.Single(runner.Describe(world, action).CostOptions);
        int[] paying = price.Sources!.Where(item => item.Effect != master.ObjectId)
            .Select(item => item.Effect).ToArray();
        PhysicalTollFixture.Play(world, runner, master, paying);
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, master.Area.Type);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Card == source.ObjectId);
    }

    [Fact]
    public void DepartureAndReentryDoNotApplyAnOldSurcharge()
    {
        var (world, _, source) = PhysicalTollFixture.Reveal();
        var master = world.Seats[1].Hand.Cards.Single(card => card.FaceId == "09005");
        Discard.Card(world, source, "probe", []);
        World.MoveToTop(source, world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)));
        Assert.Equal(1, CardPayment.CostOf(world, world.Facts, world.Seats[1], master).Amount);
    }

    [Fact]
    [Rule("rr:initiating-abilities.step.5")]
    public void InsufficientCombinedPaymentIsPureAndSufficientPaymentCompletes()
    {
        // "abort this process without paying any costs" when the full payment cannot be made.
        var (world, runner, source) = PhysicalTollFixture.Reveal();
        var seat = world.Seats[1];
        var master = seat.Hand.Cards.Single(card => card.FaceId == "09005");
        foreach (var card in seat.Hand.Cards.Where(card => card != master).ToArray())
            World.MoveToTop(card, seat.Deck);
        var first = world.CreateCard("01088", seat.Hand);
        string before = Snapshot(world);
        Assert.DoesNotContain(runner.Actions(world, 1), action => action.Card == master.ObjectId);
        Assert.Throws<RulesNotImplementedException>(() => runner.ExecuteAction(world,
            new PlayerAction(new PendingAbility(master.ObjectId, AbilityType.Action, 0),
                [first.ObjectId], [seat.InvocationDeck!.PublicTop!.ObjectId])));
        Assert.Equal(before, Snapshot(world));
        var second = world.CreateCard("01088", seat.Hand);
        PhysicalTollFixture.Play(world, runner, master, first.ObjectId, second.ObjectId);
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, first.Area.Type);
        Assert.Equal(DeckType.DiscardPile, second.Area.Type);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:event.2")]
    public void StatusCanceledAttackStillPaysAndCompletesObligation(bool stunned)
    {
        // "Only the effects are canceled"; the event's costs remain paid.
        var (world, runner, source) = PhysicalTollFixture.Reveal();
        var hero = world.Seats[1].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var attack = world.CreateCard("01005", world.Seats[1].Hand);
        if (stunned) Statuses.Give(world, hero, Statuses.Stunned);
        var paying = Enumerable.Range(0, 3).Select(_ => world.CreateCard("01088", world.Seats[1].Hand)).ToArray();
        long damage = villain.Damage;
        var action = Assert.Single(runner.Actions(world, 1), item => item.Card == attack.ObjectId);
        Assert.Equal("6", Assert.Single(runner.Describe(world, action).CostOptions).Cost);
        runner.ExecuteAction(world, new PlayerAction(action, paying.Select(card => card.ObjectId).ToArray(),
            [villain.ObjectId]));
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(damage + (stunned ? 0 : 8), villain.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, attack.Area.Type);
        Assert.All(paying, card => Assert.Equal(DeckType.DiscardPile, card.Area.Type));
        Assert.False(Statuses.Has(world, hero, Statuses.Stunned));
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Card == source.ObjectId);
    }

    [Fact]
    public void QueriedAndUnprobedReconstructionsProduceIdenticalEventCompletion()
    {
        var left = PhysicalTollFixture.Reveal();
        var right = PhysicalTollFixture.Reveal();
        Assert.Equal(Snapshot(left.World), Snapshot(right.World));
        var card = left.World.Seats[1].Hand.Cards.Single(item => item.FaceId == "09005");
        for (int i = 0; i < 3; i++)
            _ = left.Runner.Actions(left.World, 1).Select(action => left.Runner.Describe(left.World, action)).ToArray();
        int[] paying = left.World.Seats[1].Hand.Cards.Where(item => item.FaceId == "01088").Take(2)
            .Select(item => item.ObjectId).ToArray();
        var leftEvents = PhysicalTollFixture.Play(left.World, left.Runner, card, paying);
        var rightEvents = PhysicalTollFixture.Play(right.World, right.Runner, right.World.Cards[card.ObjectId], paying);
        Assert.Equal(JsonSerializer.Serialize(leftEvents), JsonSerializer.Serialize(rightEvents));
        Assert.Equal(Snapshot(left.World), Snapshot(right.World));
    }

    [Fact]
    public void ReservedReceiptDoesNotDiscardAReenteredSource()
    {
        var (world, _, source) = PhysicalTollFixture.Reveal();
        var card = world.Seats[1].Hand.Cards.Single(item => item.FaceId == "09005");
        var cost = CardPayment.CostOf(world, world.Facts, world.Seats[1], card);
        NextEventCost.Reserve(world, card, cost);
        CardPayment.UseCostModifiers(world, cost);
        Assert.Empty(NextEventCost.Applied(world, world.Seats[1], card));
        Discard.Card(world, source, "probe", []);
        World.MoveToTop(source, world.AreaOf(DeckType.ObligationsArea, PlayArea.Of(1)));
        NextEventCost.Complete(world, card, []);
        Assert.Equal(DeckType.ObligationsArea, source.Area.Type);
        Assert.DoesNotContain(world.Effects.Registered, effect => effect.Card == source.ObjectId);
    }

    private static string Snapshot(World world) => SpellMasteryFixture.Snapshot(world)
        + JsonSerializer.Serialize(world.Effects.Registered);
}


