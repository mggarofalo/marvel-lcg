using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AttackIdentityReceiptTests : CoreCardTestBase
{
    [Theory]
    [InlineData(false, false, 0)]
    [InlineData(true, false, 2)]
    [InlineData(true, true, 0)]
    [Rule("rr:overkill.2")]
    [Rule("rr:overkill.4")]
    [Rule("rr:you-your.2")]
    public void StageThreeResponseCountsOnlyCompletedAttackDamageToTheIdentity(
        bool overkill, bool tough, int threat)
    {
        // 02016: "After Green Goblin attacks and damages you, place 2 threat on the main scheme."
        // rr:overkill.2: overkill "is considered damage from an attack".
        // rr:overkill.4: prevented excess damage "is not dealt to the identity or villain".
        // rr:you-your.2: damage to you applies "to the hit point dial of their identity".
        var world = Hero("01001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var scheme = world.CreateCard("02018b", world.AreaOf(DeckType.MainSchemesArea));
        // Encounter cards support any encounter dealing reached during defeat windows.
        for (int card = 0; card < 8; card++)
            world.CreateCard("01186", world.AreaOf(DeckType.EncounterDeck));
        var villain = world.CreateCard("02016", world.AreaOf(DeckType.VillainArea));
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        if (overkill) world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Keywords.Overkill, Amount: 1, Affects: villain.ObjectId));
        if (tough) Statuses.Give(world, world.Seats[1].IdentityCard, Statuses.Tough);
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation { DealBoostCards = false },
            1, 2, Subject: villain.ObjectId, Seat: 1));
        for (int decision = 0; decision < 30; decision++)
        {
            var prompt = Sequence.Work(world, Cards, runner, events);
            if (prompt is null) break;
            Assert.True(prompt.Cancellable);
            Sequence.Answer(world, Cards, runner, prompt,
                prompt.Asking == Question.Defender && prompt.Player == 1
                    ? Decision.Take(ally.ObjectId) : Decision.Decline, events);
        }
        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.IsOver);
        var completed = Assert.Single(events.OfType<AttackCompleted>());
        Assert.Equal(ally.ObjectId, completed.Target);
        Assert.Equal(overkill && !tough ? 1 : 0, world.Seats[1].IdentityCard.Damage);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(threat, scheme.Tokens.GetValueOrDefault("k_threat"));
    }
}
