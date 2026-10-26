using Marvel.Cards.Run;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Marvel.Content.Tests.Cards;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual authored rows on a nonpublic board; Thor product admission remains closed.
public sealed class ThorGetOverHereCardTests
{
    [Fact]
    [Rule("rr:quickstrike.1")]
    public void ActualTransferQuickstrikesBeforeThorDrawsWithoutReentering()
    {
        // Quickstrike: "After this minion engages a player, it attacks that player."
        var (world, runner, source, decoy) = Board(1, true);
        var target = world.CreateCard("01167", decoy.Area);
        int incarnation = target.Incarnation;
        Start(world, runner, source, target);
        int responses = 0;
        for (int step = 0; step < 40; step++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) break;
            var thor = prompt.Affordances.SingleOrDefault(a =>
                a.AnchorId == world.Seats[0].IdentityCard.ObjectId && a.Timing == AbilityType.Response);
            if (thor is not null)
            {
                responses++;
                Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
                Assert.False(world.Agenda.Occurrence!.Is(Steps.CardEntersPlay));
                Assert.False(world.Agenda.Occurrence.Is(Steps.CardRevealed));
            }
            Sequence.Answer(world, world.Facts, runner, prompt,
                thor is not null ? Decision.Take(thor.Id) : Decision.Decline, []);
        }
        Assert.Equal(1, responses);
        Assert.Equal(2, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, target.Damage);
        Assert.Equal(incarnation, target.Incarnation);
        Assert.Equal(PlayArea.Of(0), target.Area.PlayArea);
        Assert.Equal(PlayArea.Of(1), decoy.Area.PlayArea);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Fact]
    [Rule("rr:status-cards.2")]
    public void StunCancelsActualAttackAndItsEngagementFollowUp()
    {
        // "A stunned status card cancels a character's next attack."
        var (world, runner, source, target) = Board(1, true);
        Statuses.Give(world, world.Seats[0].IdentityCard, Statuses.Stunned);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        Assert.Equal(0, Finish(world, runner));
        Assert.False(Statuses.Has(world, world.Seats[0].IdentityCard, Statuses.Stunned));
        Assert.Equal(0, target.Damage);
        Assert.Equal(PlayArea.Of(1), target.Area.PlayArea);
        Assert.Empty(world.Seats[0].Hand.Cards);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
    }

    [Theory]
    [InlineData(2, false, false, true, 1, false)]
    [InlineData(0, false, false, true, 1, true)]
    [InlineData(2, true, false, true, 1, true)]
    [InlineData(2, false, true, true, 1, true)]
    [InlineData(0, false, false, false, 1, false)]
    [InlineData(0, false, false, true, 0, false)]
    [Rule("rr:in-play-and-out-of-play.4")]
    [Rule("rr:engage.3")]
    public void ActualAttackEngagesOnlyTheSurvivingChosenMinion(
        int damage, bool tough, bool prevention, bool aerial, int engaged, bool transfers)
    {
        // Abilities "can only target, cards that are in play"; a minion already
        // engaged cannot "engage with the same player again" (also FAQ 06014).
        var (world, runner, source, target) = Board(engaged, aerial);
        target.TakeDamage(damage);
        if (tough) Statuses.Give(world, target, Statuses.Tough);
        if (prevention) world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: "preventDamage", Amount: 1, Affects: target.ObjectId, Lasts: new Duration(Uses: 1)));
        int incarnation = target.Incarnation;
        var decoy = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        Start(world, runner, source, target);
        int responses = Finish(world, runner);
        AssertTarget(target, damage, tough, prevention, engaged, transfers, incarnation);
        Assert.Equal(transfers ? 1 : 0, responses);
        Assert.Equal(transfers ? 2 : 0, world.Seats[0].Hand.Cards.Count);
        Assert.Equal(0, decoy.Damage);
        Assert.Equal(PlayArea.Of(1), decoy.Area.PlayArea);
        Assert.False(Statuses.Has(world, target, Statuses.Tough));
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Empty(world.Agenda.Outstanding);
    }

    private static void AssertTarget(Card target, int damage, bool tough, bool prevention,
        int engaged, bool transfers, int incarnation)
    {
        bool lethal = damage == 2 && !tough && !prevention;
        Assert.Equal(lethal ? DeckType.EncounterDiscardPile : DeckType.EngagedEnemiesArea, target.Area.Type);
        if (lethal) return;
        Assert.Equal(PlayArea.Of(transfers ? 0 : engaged), target.Area.PlayArea);
        Assert.Equal(incarnation, target.Incarnation);
        Assert.Equal(damage + (tough || prevention ? 0 : 1), target.Damage);
    }

    private static (World World, AbilityRunner Runner, Card Source, Card Target) Board(
        int engaged, bool aerial)
    {
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        if (aerial) world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect,
            Kind: Traits.Granted + "AERIAL", Affects: world.Seats[0].IdentityCard.ObjectId));
        var source = world.CreateCard("06014", world.Seats[0].Hand);
        var target = world.CreateCard("01101", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(engaged)));
        return (world, runner, source, target);
    }

    private static void Start(World world, AbilityRunner runner, Card source, Card target)
    {
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        runner.ExecuteAction(world, new PlayerAction(action, [], []));
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        Assert.Contains(prompt.Affordances, a => a.AnchorId == target.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(target.ObjectId), []);
    }

    private static int Finish(World world, AbilityRunner runner)
    {
        int responses = 0;
        for (int i = 0; i < 40; i++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, []);
            if (prompt is null) { Assert.Empty(world.Agenda.Outstanding); return responses; }
            var response = prompt.Affordances.SingleOrDefault(a =>
                a.AnchorId == world.Seats[0].IdentityCard.ObjectId && a.Timing == AbilityType.Response);
            if (response is not null) responses++;
            Sequence.Answer(world, world.Facts, runner, prompt,
                response is not null ? Decision.Take(response.Id) : Decision.Decline, []);
        }
        Assert.Fail("Actual Get Over Here composition did not complete");
        return responses;
    }

}
