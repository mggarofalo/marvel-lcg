using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual obligation on a prepared board with distinct identities, outside public admission.
public sealed class CareForCassieCardTests
{
    [Theory]
    [InlineData("12001a")]
    [InlineData("12001c")]
    [Rule("rr:obligation.4")]
    [Rule("rr:form-change-form.3")]
    public void GivesToScottWhoMayFlipExhaustAndRemoveIt(string form)
    {
        // Obligation "you" applies only to its player; an ability change does not use the voluntary change.
        var world = CareForCassieFixture.Board(form);
        var runner = AuthoredCards.Runner();
        var scott = world.Seats[1];
        int changed = scott.FormChangedInRound;
        var card = new RedSkullFixture(world, runner).Reveal("12025", 0);
        var flip = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(1, flip.Player);
        Assert.Equal(PlayArea.Of(1), card.Area.PlayArea);
        Assert.Equal("Change to alter-ego form", flip.Affordances.Single(item => item.Id == 0).Description);
        Sequence.Answer(world, world.Facts, runner, flip, Decision.Take(0), []);
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(1, choice.Player);
        Assert.Contains("Exhaust Scott Lang", choice.Affordances.Single(item => item.Id == 0).Description!);
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(0), []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal("12001b", scott.IdentityCard.FaceId);
        Assert.False(scott.IdentityCard.Ready);
        Assert.Equal(changed, scott.FormChangedInRound);
        Assert.True(world.Seats[0].IdentityCard.Ready);
        Assert.True(FormChangePermissions.CanChange(world, scott.IdentityCard));
        Assert.Equal(DeckType.RemovedArea, card.Area.Type);
    }

    [Theory]
    [InlineData("12001a", 2)]
    [InlineData("12001c", 0)]
    [Rule("rr:obligation.4")]
    [Rule("rr:ability.4")]
    [Rule("rr:choose-option.1")]
    [Rule("rr:lasting-effects.1")]
    public void DiscardChoiceUsesScottsHandAndStillProhibitsWithNoHand(string form, int handSize)
    {
        // "Resolve the ability one sentence at a time"; the option still affects Scott and itself.
        // A lasting effect continues "whether or not the card" that created it is in play.
        var world = CareForCassieFixture.Board(form, handSize);
        var runner = AuthoredCards.Runner();
        var scott = world.Seats[1];
        var card = new RedSkullFixture(world, runner).Reveal("12025", 0);
        var flip = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Sequence.Answer(world, world.Facts, runner, flip, Decision.Take(1), []);
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.DoesNotContain(choice.Affordances, item => item.Id == 0);
        Assert.Contains("until your next turn ends", Assert.Single(choice.Affordances).Description!);
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(1), []);
        var discard = Sequence.Work(world, world.Facts, runner, []);
        if (handSize > 0)
        {
            Assert.NotNull(discard);
            Assert.Equal(1, discard.Player);
            var offer = Assert.Single(discard.Affordances);
            var own = scott.Hand.Cards[0];
            var foreign = world.Seats[0].Hand.Cards[0];
            var digest = world.Digest().Canonical();
            var random = world.Random.Generator.GetState();
            long consumed = world.Random.Generator.WordsConsumed;
            var pending = world.Agenda.Outstanding.ToArray();
            Assert.Throws<ArgumentException>(() => Sequence.Answer(world, world.Facts, runner, discard,
                new Decision(offer.Id, [foreign.ObjectId], []), []));
            Assert.Equal(digest, world.Digest().Canonical());
            Assert.Equal(random.Words, world.Random.Generator.GetState().Words);
            Assert.Equal(random.Index, world.Random.Generator.GetState().Index);
            Assert.Equal(consumed, world.Random.Generator.WordsConsumed);
            Assert.Equal(pending, world.Agenda.Outstanding);
            Sequence.Answer(world, world.Facts, runner, discard, new Decision(offer.Id, [own.ObjectId], []), []);
            Assert.Equal(DeckType.DiscardPile, own.Area.Type);
            Assert.Equal(DeckType.HandsArea, foreign.Area.Type);
        }
        else Assert.Null(discard);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(Math.Max(0, handSize - 1), scott.Hand.Cards.Count);
        Assert.Equal(form, scott.IdentityCard.FaceId);
        Assert.True(scott.IdentityCard.Ready);
        Assert.False(FormChangePermissions.CanChange(world, scott.IdentityCard));
        Assert.True(FormChangePermissions.CanChange(world, world.Seats[0].IdentityCard));
        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
        World.MoveToTop(card, world.AreaOf(DeckType.RemovedArea));
        Assert.False(FormChangePermissions.CanChange(world, scott.IdentityCard));
    }

    [Fact]
    [Rule("rr:cost-arrow-icon")]
    [Rule("rr:exhausted.1")]
    public void ExhaustedScottCannotPayTheRemovalArrowCost()
    {
        // Text before the arrow must be "paid and/or resolved in full"; an exhausted card cannot exhaust again.
        var world = CareForCassieFixture.Board("12001b", 0);
        var runner = AuthoredCards.Runner();
        world.Seats[1].IdentityCard.Exhaust();
        var card = new RedSkullFixture(world, runner).Reveal("12025", 0);
        var flip = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(1, Assert.Single(flip.Affordances).Id);
        Sequence.Answer(world, world.Facts, runner, flip, Decision.Take(1), []);
        var choice = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(1, Assert.Single(choice.Affordances).Id);
        Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(1), []);
        Assert.Null(Sequence.Work(world, world.Facts, runner, []));
        Assert.Equal(DeckType.EncounterDiscardPile, card.Area.Type);
        Assert.False(FormChangePermissions.CanChange(world, world.Seats[1].IdentityCard));
    }
}
