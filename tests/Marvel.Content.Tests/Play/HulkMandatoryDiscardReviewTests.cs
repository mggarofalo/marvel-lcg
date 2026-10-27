using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkMandatoryDiscardReviewTests
{
    [Fact]
    [Rule("rr:forced.1")]
    public void NonFirstHulkMustDiscardItsWholeHandAndInvalidAnswersPermitRetry()
    {
        // Forced abilities "must be resolved when their triggering conditions are met".
        var (game, world) = HulkFirstSliceFixture.PlayingHulk(_ => { }, owner: 1);
        HulkFirstSliceFixture.EmptyHand(world, 1);
        for (int index = 0; index < 3; index++) world.CreateCard("01088", world.Seats[1].Hand);
        var hand = world.Seats[1].Hand.Cards.ToArray();
        var otherHand = world.Seats[0].Hand.Cards.ToArray();
        game.Resolve(Decision.Decline);
        game.Resolve(Decision.Decline);
        var prompt = game.Pending!;
        Assert.Equal(Question.Element, prompt.Asking);
        Assert.Equal(1, prompt.Player);
        var choice = Assert.Single(prompt.Affordances);
        string digest = world.Digest().Canonical();
        var random = world.Random.Generator.GetState();
        long words = world.Random.Generator.WordsConsumed;
        var agenda = world.Agenda.Outstanding.ToArray();
        var effects = world.Effects.Registered.ToArray();
        Decision[] invalid =
        [
            Decision.Decline,
            new(choice.Id, [hand[0].ObjectId]),
            new(choice.Id, [hand[0].ObjectId, hand[1].ObjectId, otherHand[0].ObjectId]),
        ];
        foreach (var answer in invalid)
        {
            Assert.Throws<ArgumentException>(() => game.Resolve(answer));
            Assert.Same(prompt, game.Pending);
            Assert.Equal(1, game.Active);
            Assert.Equal(digest, world.Digest().Canonical());
            Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
            Assert.Equal(words, world.Random.Generator.WordsConsumed);
            Assert.Equal(agenda, world.Agenda.Outstanding);
            Assert.Equal(effects, world.Effects.Registered);
        }
        game.Resolve(new Decision(choice.Id, hand.Select(card => card.ObjectId).ToArray()));
        Assert.Empty(world.Seats[1].Hand.Cards);
        Assert.All(hand, card =>
        {
            Assert.Equal(DeckType.DiscardPile, card.Area.Type);
            Assert.Equal(1, card.Area.CardOwner);
        });
        Assert.Equal(otherHand, world.Seats[0].Hand.Cards);
        Assert.Equal(GamePhase.EndPhase, game.Phase);
    }
}
