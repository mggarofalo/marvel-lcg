using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkFinalCardReviewTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:attack-enemy-activation.step.2")]
    [Rule("rr:attack-player-ability-type.2")]
    public void ChosenOtherSeatsMinionAttacksYouButItsAllyDefeatCannotEnableChase(bool lethal)
    {
        // An enemy attacks the specified player; another player's defender becomes
        // its target. Resolving a labeled attack "is considered to attack the specified target."
        // Printed 01052 requires the hero's own attack to defeat an enemy.
        var f = new HulkFinalCardsFixture();
        var enemy = f.Minion();
        World.MoveToTop(enemy, f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        if (!lethal) f.World.Effects.Register(new(EffectSource.LastingEffect,
            "health", 10, Affects: enemy.ObjectId));
        int incarnation = enemy.Incarnation;
        var ally = f.World.CreateCard("01002", f.World.AreaOf(DeckType.AlliesArea,
            PlayArea.Of(0), cardOwner: 0));
        var source = f.World.CreateCard("10015", f.World.Seats[1].Hand);
        var payment = f.World.CreateCard("01088", f.World.Seats[1].Hand);
        var chase = f.World.CreateCard("01052", f.World.Seats[1].Hand);
        f.Start(source, 0, payment);
        f.Answer(Decision.Take(enemy.ObjectId));
        Assert.Equal(Question.Defender, f.Pending!.Asking);
        f.Answer(Decision.Take(ally.ObjectId));
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(0, f.Hero.Damage);
        Assert.Equal(0, f.World.Seats[0].IdentityCard.Damage);
        var activation = Assert.Single(f.Events.OfType<AttackCompleted>());
        Assert.Equal(ally.ObjectId, activation.Defender);
        if (lethal)
        {
            Assert.Equal(Question.Opportunity, f.Pending!.Asking);
            var occurrence = f.World.Agenda.Occurrence!;
            var defeat = Assert.Single(occurrence.Defeats);
            Assert.Equal(enemy.ObjectId, defeat.Card);
            Assert.Equal(incarnation, defeat.Incarnation);
            Assert.Equal(1, defeat.By);
            Assert.Equal("Attack", defeat.How);
            var response = Assert.Single(f.Pending.Affordances, a => a.AnchorId == chase.ObjectId);
            f.Answer(Decision.Take(response.Id));
            f.Answer(Decision.Take(f.Scheme.ObjectId));
        }
        else
        {
            Assert.True(f.Pending is null || f.Pending.Affordances.All(a => a.AnchorId != chase.ObjectId));
            Assert.Equal(5, enemy.Damage);
            Assert.Contains(chase, f.World.Seats[1].Hand.Cards);
        }
        f.Finish();
        Assert.Equal(lethal ? 2 : 4, f.Scheme.Tokens["k_threat"]);
    }

    [Fact]
    [Rule("rr:initiating-abilities.3")]
    public void CrossOwnedBeatCopUsesOnlyCapturedThreatAfterDiscardAndRejectedTarget()
    {
        // Initiation "does not stop from completing if that card leaves play";
        // printed 10029 counts threat here, independently of other token kinds.
        var f = new HulkFinalCardsFixture();
        var source = f.World.CreateCard("10029", f.World.AreaOf(DeckType.SupportsArea,
            PlayArea.Of(1), cardOwner: 0));
        source.PlaceTokens("k_threat", 2);
        source.PlaceTokens("c_review", 9);
        var minion = f.Minion();
        Assert.DoesNotContain(f.Runner.Actions(f.World, 0), a => a.Card == source.ObjectId);
        f.Start(source, 1);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(0, source.Tokens.GetValueOrDefault("k_threat"));
        var prompt = f.Pending;
        var baseline = (f.World.Digest().Canonical(), f.World.Random.Generator.WordsConsumed,
            f.World.Agenda.Outstanding.ToArray(), f.Events.ToArray());
        Assert.Throws<ArgumentException>(() => f.Answer(Decision.Take(f.Villain.ObjectId)));
        Assert.Same(prompt, f.Pending);
        Assert.Equal(baseline.Item1, f.World.Digest().Canonical());
        Assert.Equal(baseline.Item2, f.World.Random.Generator.WordsConsumed);
        Assert.Equal(baseline.Item3, f.World.Agenda.Outstanding);
        Assert.Equal(baseline.Item4, f.Events);
        f.Answer(Decision.Take(minion.ObjectId));
        f.Finish();
        Assert.Equal(2, minion.Damage);
        Assert.Equal(0, source.Tokens.GetValueOrDefault("k_threat"));
        Assert.Empty(f.Events.OfType<AttackBoundary>());
    }
}
