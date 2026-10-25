using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class EventDiscardAnswerAtomicityTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [Rule("rr:event")]
    [Rule("rr:cost.5")]
    [Rule("rr:initiating-abilities.step.5")]
    public void HostileEventDiscardAnswersPreserveThePendingDecision(bool self)
    {
        // An event is placed "faceup on the table" before paying costs.
        // Multiple costs "must be paid simultaneously"; when they cannot,
        // "abort this process without paying any costs."
        // Synthetic unadmitted event cost shape; this does not admit Shield Toss.
        var runner = Runner("01004", "Action",
            """{"placeCounters":{"card":"you","counter":"test","count":1}}""",
            cost: """{"discardFromHand":1}""");
        Card? source = null;
        Card? resource = null;
        Card? discard = null;
        var (game, world) = Playing(board =>
        {
            source = board.CreateCard("01004", board.Seats[0].Hand);
            resource = board.CreateCard("01093", board.Seats[0].Hand);
            discard = board.CreateCard("01093", board.Seats[0].Hand);
        }, abilities: runner);
        var pending = game.Pending;
        var action = Assert.Single(pending!.Affordances, offer => offer.AnchorId == source!.ObjectId);
        int[] paying = [resource!.ObjectId];
        var allocation = ResourcePayment.Allocate(Assert.Single(action.Costs!), paying);
        var before = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var effects = world.Effects.Registered.ToArray();
        var agenda = world.Agenda.Outstanding.ToArray();

        void Attempt() => game.Resolve(new Decision(action.Id,
            [self ? source!.ObjectId : resource.ObjectId], paying, Allocations: allocation));
        if (self) Assert.Throws<ArgumentException>(Attempt);
        else Assert.Throws<RulesNotImplementedException>(Attempt);

        Assert.Equal(before, world.Digest().Canonical());
        Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
        Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
        Assert.Equal(words, world.Random.Generator.WordsConsumed);
        Assert.Equal(effects, world.Effects.Registered);
        Assert.Equal(agenda, world.Agenda.Outstanding);
        Assert.Same(pending, game.Pending);

        game.Resolve(new Decision(action.Id, [discard!.ObjectId], paying, Allocations: allocation));
        Assert.Equal(DeckType.DiscardPile, source!.Area.Type);
        Assert.Equal(DeckType.DiscardPile, resource.Area.Type);
        Assert.Equal(DeckType.DiscardPile, discard.Area.Type);
        Assert.Equal(1, world.Seats[0].IdentityCard.Tokens["c_test"]);
        Assert.False(world.Agenda.IsBusy);
    }
}
