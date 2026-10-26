using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Synthetic window costs isolate answer acceptance from committed payment.</summary>
public sealed class WindowPaymentAtomicityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:initiating-abilities.step.5")]
    [Rule("rr:triggering-condition.1")]
    public void InvalidOrdinaryWindowPaymentPreservesOpportunityForRetryOrDecline(bool decline)
    {
        // "Pay the cost(s)." Each response "can only be triggered once" per occurrence.
        // A rejected answer has not initiated payment or triggered the ability.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01007","abilities":[{
              "trigger":{"event":"WhenCardDefeated","timing":"Response","subject":"game"},
              "cost":{"seq":[{"exhaust":"this"},{"spendDistinct":2}]},
              "effect":{"placeCounters":{"card":"you","counter":"test","count":1}}
            }]}]}
            """));
        var (_, world) = Playing(_ => { }, heroes: ["spider_man", "captain_marvel"], abilities: runner);
        foreach (var seat in world.Seats)
            foreach (var held in seat.Hand.Cards.ToArray()) Discard.Card(world, held, "test", []);
        var source = world.CreateCard("01007", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(1), cardOwner: 1));
        var first = world.CreateCard("01055", world.Seats[1].Hand);
        var second = world.CreateCard("01055", world.Seats[1].Hand);
        var opponent = world.CreateCard("01055", world.Seats[0].Hand);
        var defeated = world.CreateCard("01101", world.AreaOf(DeckType.EncounterDiscardPile));
        world.Agenda.Add(new PhaseStep(new CompletedCardEffectOperation(), 1, 1));
        var origin = world.Agenda.Begin(world, world.Facts);
        world.Agenda.Advance();
        world.Agenda.Advance();
        origin.Also(new Defeated(defeated.ObjectId, 1, "Attack") { Kind = CardKind.Minion });
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        var offer = Assert.Single(prompt.Affordances);
        Assert.Equal(1, prompt.Player);
        var ability = Assert.Single(runner.Waiting(world, origin, WindowKind.Response));
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        var pending = world.Agenda.Outstanding.ToArray();

        Reject([first.ObjectId, second.ObjectId], [new(first.ObjectId, 0, "Y"), new(second.ObjectId, 0, "Y")]);
        Reject([first.ObjectId, opponent.ObjectId], [new(first.ObjectId, 0, "Y"), new(opponent.ObjectId, 0, "B")]);
        Assert.Same(world.Seats[0].Hand, opponent.Area);

        Sequence.Answer(world, world.Facts, runner, prompt,
            decline ? Decision.Decline : new(offer.Id, [], [first.ObjectId, second.ObjectId],
                Allocations: [new(first.ObjectId, 0, "Y"), new(second.ObjectId, 0, "B")]), []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(decline ? 0 : 1, world.Seats[1].IdentityCard.Tokens.GetValueOrDefault("c_test"));
        Assert.Equal(decline, source.Ready);
        Assert.Equal(decline, origin.MayTrigger(WindowKind.Response, ability));
        Assert.Equal(decline ? DeckType.HandsArea : DeckType.DiscardPile, first.Area.Type);

        void Reject(int[] spent, ResourceAllocation[] allocations)
        {
            Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(world, world.Facts, runner,
                prompt, new(offer.Id, [], spent, Allocations: allocations), []));
            Assert.Equal(digest, world.Digest().Canonical());
            Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
            Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(pending, world.Agenda.Outstanding.ToArray());
            Assert.True(origin.MayTrigger(WindowKind.Response, ability));
            Assert.Empty(origin.Resolutions);
            Assert.True(source.Ready);
        }
    }
}
