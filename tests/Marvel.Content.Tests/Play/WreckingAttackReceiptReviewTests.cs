using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class WreckingAttackReceiptReviewTests
{
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [Rule("rr:defend-defense.3.1")]
    [Rule("rr:overkill.2")]
    [Rule("rr:damage.3.2")]
    public void HelmetCountsAllyAttackOnceAndUsesTheFinalAttackedPlayer(
        int controller, bool toughIdentity)
    {
        // The ally's controller "becomes the target player for that attack".
        // Overkill is "damage from an attack"; prevention leaves "damage dealt" unchanged.
        // Bulldozer's printed forced interrupt gives his attack overkill.
        var (world, groups, helmet) = WreckingEncounterCardFixture.Create("07049", 3, players: 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var attacker = groups[3].Villain;
        var identity = world.Seats[controller].IdentityCard;
        var ally = world.CreateCard("01002", world.AreaOf(
            DeckType.AlliesArea, PlayArea.Of(controller), cardOwner: controller));
        for (int seat = 0; seat < 2; seat++)
            for (int index = 0; index < 9; index++) world.CreateCard("01088", world.Seats[seat].Deck);
        Reveal.Resolve(world, world.Facts, helmet, 0, []);
        if (toughIdentity) Statuses.Give(world, identity, Statuses.Tough);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 1,
            Subject: attacker.ObjectId, Seat: 0));
        var events = Finish(world, runner, ally);

        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(toughIdentity ? 0 : 2, identity.Damage);
        Assert.Equal(4, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
        var discarded = world.AreaOf(DeckType.DiscardPile, PlayArea.Of(controller)).Cards;
        Assert.Equal(4, discarded.Count(card => card.FaceId == "01088"));
        Assert.Equal(0, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(1 - controller))
            .Cards.Count(card => card.FaceId == "01088"));
        Assert.Equal((controller == 0 ? 10 : 9) - 4, world.Seats[controller].Deck.Cards.Count);
    }

    [Fact]
    public void HelmetExcludesBoostAbilityDamageFromItsDiscardCount()
    {
        // Synthetic cross-deck boost isolates damage provenance using real printed abilities.
        // Energy Projectiles deals 1 to the defender; Helmet counts damage "by this attack".
        var (world, groups, helmet) = WreckingEncounterCardFixture.Create("07049", 3, boostFace: "07027");
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        for (int index = 0; index < 8; index++) world.CreateCard("01088", world.Seats[0].Deck);
        Reveal.Resolve(world, world.Facts, helmet, 0, []);
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 1,
            Subject: groups[3].Villain.ObjectId, Seat: 0));
        var events = Finish(world, runner, world.Seats[0].IdentityCard);

        // Bulldozer's 3 ATK + Helmet's 1 - Spider-Man's 3 DEF = 1 attack damage,
        // plus the boost ability's separate 1 damage.
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, Assert.Single(events.OfType<AttackCompleted>()).DamageDealt);
        Assert.Single(world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0)).Cards);
        Assert.Equal(8, world.Seats[0].Deck.Cards.Count);
    }

    private static List<GameEvent> Finish(World world, AbilityRunner runner, Card defender)
    {
        var events = new List<GameEvent>();
        for (int step = 0; ; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) return events;
            Assert.True(step < 25, prompt.Label);
            bool choose = prompt.Asking == Question.Defender
                && prompt.Affordances.Any(offer => offer.Id == defender.ObjectId);
            if (!choose) Assert.True(prompt.Cancellable, prompt.Label);
            Sequence.Answer(world, world.Facts, runner, prompt,
                choose ? Decision.Take(defender.ObjectId) : Decision.Decline, events);
        }
    }
}
