using System.Text.Json;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.BlackWidowIdentitySearchFixture;

namespace Marvel.Content.Tests.Play;

// Independent actual-row checks on prepared nonpublic boards, outside pack admission.
public sealed class DoctorStrangeSearchReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SanctumOfferQueriesPreserveStateAndMatchAnUnprobedContinuation(bool spell)
    {
        var probed = Prepare();
        var control = Prepare();
        string baseline = SpellMasteryFixture.Snapshot(probed);
        Assert.Equal(baseline, SpellMasteryFixture.Snapshot(control));
        var invocation = probed.Seats[1].InvocationDeck!;
        var invocationCards = invocation.Deck.Cards.Concat(invocation.Discard.Cards)
            .Select(card => (card.ObjectId, card.Area)).ToArray();
        var source = Assert.Single(probed.Cards, card => card.FaceId == "09008");
        for (int i = 0; i < 3; i++)
        {
            var action = Assert.Single(probed.Abilities!.Actions(probed, 1), a => a.Card == source.ObjectId);
            Assert.Equal(source.ObjectId, probed.Abilities.Describe(probed, action).AnchorId);
            Assert.DoesNotContain(probed.Abilities.Actions(probed, 0), a => a.Card == source.ObjectId);
            Assert.Equal(baseline, SpellMasteryFixture.Snapshot(probed));
        }

        Resolve(probed);
        Resolve(control);
        Assert.Equal(SpellMasteryFixture.Snapshot(control), SpellMasteryFixture.Snapshot(probed));
        Assert.Single(probed.Seats[1].Hand.Cards);
        Assert.Empty(probed.Seats[0].Hand.Cards);
        Assert.Equal(invocationCards, invocation.Deck.Cards.Concat(invocation.Discard.Cards)
            .Select(card => (card.ObjectId, card.Area)));
        Assert.False(source.Ready);
        Assert.Empty(probed.Agenda.Outstanding);

        World Prepare()
        {
            var world = DoctorStrangeSearchFixture.Board();
            DoctorStrangeSearchFixture.Sanctum(world);
            if (spell)
                for (int i = 0; i < 2; i++)
                    world.CreateCard("09004", DoctorStrangeSearchFixture.Discard(world));
            return world;
        }

        void Resolve(World world)
        {
            DoctorStrangeSearchFixture.Activate(world, Assert.Single(world.Cards, card => card.FaceId == "09008"));
            if (spell)
            {
                var selection = Prompt(world);
                Assert.Equal(1, selection.Player);
                Assert.Equal(2, selection.Affordances.Count);
                Answer(world, selection, selection.Affordances[^1].Id);
            }
            Finish(world);
        }
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:play-put-into-play.3")]
    public void BrotherEntryWithoutPlayingOffersAnOptionalSearch(bool accept)
    {
        // "A card that is put into play is not considered to have been played."
        // Printed 09012 responds after entry, and its Response may be declined.
        var world = DoctorStrangeSearchFixture.Board();
        var seat = world.Seats[1];
        var match = world.CreateCard("01005", seat.Deck);
        var brother = world.CreateCard("09012", seat.Hand);
        CardPlay.PutAllyIntoPlay(world, world.Facts, world.Abilities!, brother, 1, "test entry", []);
        var response = Prompt(world);
        Assert.Equal(1, response.Player);
        string digest = world.Digest().Canonical();
        string random = JsonSerializer.Serialize(world.Random.Generator.GetState());
        long words = world.Random.Generator.WordsConsumed;
        if (accept)
        {
            Answer(world, response, Assert.Single(response.Affordances, a => a.AnchorId == brother.ObjectId).Id);
            var selection = Prompt(world);
            Assert.Equal(match.ObjectId, Assert.Single(selection.Affordances).AnchorId);
            Answer(world, selection, Assert.Single(selection.Affordances).Id);
        }
        else
            Sequence.Answer(world, world.Facts, world.Abilities!, response, Decision.Decline, []);
        Finish(world);
        Assert.Same(accept ? seat.Hand : seat.Deck, match.Area);
        if (accept) Assert.True(world.Random.Generator.WordsConsumed > words);
        else
        {
            Assert.Equal(digest, world.Digest().Canonical());
            Assert.Equal(random, JsonSerializer.Serialize(world.Random.Generator.GetState()));
            Assert.Equal(words, world.Random.Generator.WordsConsumed);
        }
        Assert.Empty(world.Agenda.Outstanding);
    }
}
