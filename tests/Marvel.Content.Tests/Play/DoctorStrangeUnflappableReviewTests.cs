using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DoctorStrangeUnflappableReviewTests
{
    [Theory]
    [InlineData(true, true, true)]
    [InlineData(true, false, false)]
    [InlineData(false, true, false)]
    [Rule("rr:tough.3")]
    [Rule("rr:defend-defense.2")]
    public void PreventedPositiveDamageQualifiesOnlyWhenTheHeroDefended(
        bool defend, bool tough, bool eligible)
    {
        // Tough means the character "is not considered to have taken damage."
        // Basic defense reduces damage by DEF; printed 09020 also requires defending.
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var hero = world.Seats[1].IdentityCard;
        var source = world.CreateCard("09020", world.AreaOf(DeckType.UpgradesArea,
            PlayArea.Of(1), cardOwner: 0));
        var enemy = world.CreateCard("01103", world.AreaOf(DeckType.EngagedEnemiesArea,
            PlayArea.Of(1)));
        world.Effects.Register(new(EffectSource.LastingEffect, "attack", 3, Affects: enemy.ObjectId));
        if (tough) Statuses.Give(world, hero, Statuses.Tough);
        int held = world.Seats[1].Hand.Cards.Count;
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation { DealBoostCards = false },
            1, 2, Subject: enemy.ObjectId, Seat: 1));
        int offers = 0;
        for (int index = 0; index < 40; index++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
            var offer = prompt.Affordances.SingleOrDefault(item => item.AnchorId == source.ObjectId);
            if (offer is not null)
            {
                offers++;
                Assert.True(eligible);
                Assert.Equal(1, prompt.Player);
                Assert.Single(events.OfType<AttackCompleted>());
            }
            Sequence.Answer(world, world.Facts, runner, prompt,
                BasicDefenseAnswer(prompt, offer?.Id, hero.ObjectId, defend), events);
        }
        Assert.Equal(eligible ? 1 : 0, offers);
        Assert.Equal(held + (eligible ? 1 : 0), world.Seats[1].Hand.Cards.Count);
        Assert.Equal(!eligible, source.Ready);
        Assert.Equal(tough ? 0 : 2, hero.Damage);
        Assert.False(Statuses.Has(world, hero, Statuses.Tough));
        Assert.Single(events.OfType<AttackCompleted>());
        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.IsOver);
    }

    private static Decision BasicDefenseAnswer(Prompt prompt, int? response, int hero, bool defend)
    {
        if (response is { } responseId) return Decision.Take(responseId);
        if (defend && prompt.Asking == Question.Defender && prompt.Player == 1)
            return Decision.Take(hero);
        return Decision.Decline;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:defend-defense.4")]
    [Rule("rr:response.4")]
    public void ControllerCanDeclineOrUseResponseAfterEventDefense(bool accept)
    {
        // A defense-labeled ability means the identity "is considered to have defended the attack".
        // Players may decide they "do not wish to resolve any (further) responses".
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var hero = world.Seats[1].IdentityCard;
        var source = world.CreateCard("09020", world.AreaOf(DeckType.UpgradesArea,
            PlayArea.Of(1), cardOwner: 0));
        var defense = world.CreateCard("01003", world.Seats[1].Hand);
        var enemy = world.CreateCard("01103", world.AreaOf(DeckType.EngagedEnemiesArea,
            PlayArea.Of(1)));
        int ownerHeld = world.Seats[0].Hand.Cards.Count;
        int controllerHeld = world.Seats[1].Hand.Cards.Count;
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation { DealBoostCards = false },
            1, 2, Subject: enemy.ObjectId, Seat: 1));
        int offers = 0;
        for (int index = 0; index < 40; index++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
            var response = prompt.Affordances.SingleOrDefault(item => item.AnchorId == source.ObjectId);
            var backflip = prompt.Affordances.SingleOrDefault(item => item.AnchorId == defense.ObjectId);
            if (response is not null)
            {
                offers++;
                Assert.Equal(1, prompt.Player);
                Assert.True(prompt.Cancellable);
                Assert.Single(events.OfType<AttackCompleted>());
                Assert.True(hero.Ready);
                Assert.Equal(0, hero.Damage);
            }
            var answer = response is not null && accept ? Decision.Take(response.Id)
                : backflip is not null ? Decision.Take(backflip.Id) : Decision.Decline;
            Sequence.Answer(world, world.Facts, runner, prompt, answer, events);
        }
        Assert.Equal(1, offers);
        Assert.Equal(controllerHeld - 1 + (accept ? 1 : 0), world.Seats[1].Hand.Cards.Count);
        Assert.Equal(ownerHeld, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(!accept, source.Ready);
        Assert.Equal(DeckType.DiscardPile, defense.Area.Type);
        Assert.True(hero.Ready);
        Assert.Single(events.OfType<AttackCompleted>());
        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.IsOver);
    }
}
