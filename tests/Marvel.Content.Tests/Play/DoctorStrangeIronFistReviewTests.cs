using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

/// <summary>Independent actual-book attack evidence on nonpublic multiplayer boards.</summary>
public sealed class DoctorStrangeIronFistReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:triggering-condition.1")]
    [Rule("rr:consequential-damage.1")]
    [Rule("rr:ownership-and-control.7.2")]
    public void ReadiedForeignOwnedIronFistUsesBothCountersAcrossDistinctAttacks(bool reverse)
    {
        // Each interrupt can trigger "once per occurrence of its triggering condition".
        // Consequential damage follows "abilities that are triggered by the ally attacking".
        // A departing card uses "its owner's equivalent out-of-play area".
        // Printed 09014 has two ordinary counters and no per-round ability limit.
        var world = ThorActualFixture.Board("06001b", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var ally = world.CreateCard("09014",
            world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 0));
        ally.PlaceTokens("c_mystic", 2);
        var target = world.TheCardIn(DeckType.VillainArea)!;
        var events = new List<GameEvent>();

        for (int attack = 0; attack < 3; attack++)
        {
            Assert.True(ally.Ready);
            AllyBasicPowers.AllyPower(world, world.Facts, ally, target, BasicPowers.AttackVerb, events);
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (attack < 2)
            {
                Assert.NotNull(prompt);
                Assert.Equal(1, prompt.Player);
                Assert.True(prompt.Cancellable);
                Assert.Equal(attack * 3, target.Damage);
                Assert.Equal(attack, ally.Damage);
                Sequence.Answer(world, world.Facts, runner, prompt,
                    Decision.Take(Assert.Single(prompt.Affordances).Id), events);
                Assert.Equal(1 - attack, ally.Tokens["c_mystic"]);
                prompt = Sequence.Work(world, world.Facts, runner, events)!;
                Assert.Equal(Question.Order, prompt.Asking);
                Sequence.Answer(world, world.Facts, runner, prompt,
                    new Decision(Assert.Single(prompt.Affordances).Id, reverse ? [1, 0] : [0, 1]), events);
            }
            else Assert.Null(prompt);
            Sequence.Finish(world, world.Facts, runner, events);
            Assert.Equal(attack < 2 ? (attack + 1) * 3 : 8, target.Damage);
            Assert.True(Statuses.Has(world, target, Statuses.Stunned));
            Assert.False(world.Agenda.IsBusy);
            Assert.Empty(world.Agenda.Outstanding);
            if (attack == 2) break;
            Assert.False(ally.Ready);
            Assert.Equal(attack + 1, ally.Damage);
            Assert.Equal(DeckType.AlliesArea, ally.Area.Type);

            // Actual printed Get Ready provides the next legal attack in the same round.
            var ready = world.CreateCard("01069", world.Seats[1].Hand);
            var action = Assert.Single(runner.Actions(world, 1), item => item.Card == ready.ObjectId);
            events.AddRange(runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events);
            prompt = Sequence.Work(world, world.Facts, runner, events)!;
            Assert.Equal(1, prompt.Player);
            var option = Assert.Single(prompt.Affordances, item => item.AnchorId == ally.ObjectId);
            Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(option.Id), events);
            Sequence.Finish(world, world.Facts, runner, events);
            Assert.True(ally.Ready);
            Assert.Equal(DeckType.DiscardPile, ready.Area.Type);
            Assert.Empty(world.Agenda.Outstanding);
        }

        Assert.Equal(0, ally.Tokens["c_mystic"]);
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(PlayArea.Of(0), ally.Area.PlayArea);
    }
}
