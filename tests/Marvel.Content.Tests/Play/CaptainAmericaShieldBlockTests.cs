using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CaptainAmericaShieldBlockTests
{
    [Theory]
    [InlineData("missing")]
    [InlineData("exhausted")]
    [InlineData("other-player")]
    [InlineData("wrong-title")]
    [Rule("rr:cost.7")]
    public void BlockRequiresYourReadyNamedShield(string state)
    {
        // "that player must pay costs with cards and/or game elements they control."
        Card? block = null;
        var (_, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            block = board.CreateCard("03005", board.Seats[0].Hand);
            if (state == "missing") return;
            int owner = state == "other-player" ? 1 : 0;
            var shield = board.CreateCard(state == "wrong-title" ? "03010" : "03009",
                board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(owner), cardOwner: owner));
            if (state == "exhausted") shield.Exhaust();
        }, twoPlayers: true);
        var identity = world.Seats[0].IdentityCard;
        var occurrence = new Occurrence(1, [Steps.DamageWouldBeDealt],
            Subject: identity.ObjectId, Player: 0, Target: identity.ObjectId,
            TargetFacts: new OccurrenceCard(identity.ObjectId, CardKind.Hero, 0, 0));
        Assert.DoesNotContain(AuthoredCards.Runner().Waiting(world, occurrence, WindowKind.Interrupt),
            item => item.Card == block!.ObjectId);
        Assert.Same(world.Seats[0].Hand, block!.Area);
    }

    [Fact]
    [Rule("rr:defend-defense.4.4")]
    public void ActualBlockDuringAttackExhaustsShieldAndMakesIdentityDefenderWithoutBasicDefense()
    {
        // "Unless specified by the ability’s text, a hero does not exhaust when using a defense-labeled ability."
        Card? block = null;
        Card? shield = null;
        var (_, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            block = board.CreateCard("03005", board.Seats[0].Hand);
            shield = board.CreateCard("03009", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        });
        var runner = AuthoredCards.Runner();
        var events = new List<GameEvent>();
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), Round: 1, Number: 2,
            Subject: villain.ObjectId, Seat: 0));
        var defend = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        Assert.Equal(Question.Defender, defend.Asking);
        Sequence.Answer(world, world.Facts, runner, defend, Decision.Decline, events);
        var prevent = Assert.IsType<Prompt>(Sequence.Work(world, world.Facts, runner, events));
        var blockOffer = Assert.Single(prevent.Affordances, option => option.AnchorId == block!.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prevent, Decision.Take(blockOffer.Id), events);
        for (int answered = 0; Sequence.Work(world, world.Facts, runner, events) is { } remaining; answered++)
        {
            Assert.True(answered < 8);
            Assert.Equal(Question.Opportunity, remaining.Asking);
            Sequence.Answer(world, world.Facts, runner, remaining, Decision.Decline, events);
        }
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.True(world.Seats[0].IdentityCard.Ready);
        Assert.False(shield!.Ready);
        Assert.Equal(DeckType.DiscardPile, block!.Area.Type);
        Assert.Single(events.OfType<AttackCompleted>());
        Assert.Equal(1, villain.Damage);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:defend-defense.4.8")]
    public void ActualBlockPreventsNonAttackDamageInEitherForm(bool hero)
    {
        // "A player can trigger abilities labeled as a defense outside of an attack if the ability’s triggering condition is met."
        // Printed 03005 is an Interrupt, not a Hero Interrupt.
        Card? block = null;
        Card? shield = null;
        var (_, world) = CaptainAmericaFixture.PlayingCaptain(board =>
        {
            block = board.CreateCard("03005", board.Seats[0].Hand);
            shield = board.CreateCard("03009", board.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        }, hero);
        var runner = AuthoredCards.Runner();
        var events = new List<GameEvent>();
        var identity = world.Seats[0].IdentityCard;
        Assert.Same(world.Seats[0].Hand, block!.Area);
        Assert.True(shield!.Ready);
        var occurrence = new Occurrence(1, [Steps.DamageWouldBeDealt],
            Subject: identity.ObjectId, Player: 0, Target: identity.ObjectId,
            TargetFacts: new OccurrenceCard(identity.ObjectId, hero ? CardKind.Hero : CardKind.AlterEgo, 0, 0));
        var option = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt),
            item => item.Card == block!.ObjectId);
        runner.ResolveWindow(world, occurrence, option, new Decision(option.Card, [], []));
        var result = DamagePlacement.Resolve(world, world.Facts,
            new DamageRequest(world.TheCardIn(DeckType.VillainArea)!, identity, 4, Steps.DamageWouldBeDealt, "Damage"), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.True(result.IsComplete);
        Assert.Equal(0, identity.Damage);
        Assert.False(shield!.Ready);
        Assert.Equal(DeckType.DiscardPile, block!.Area.Type);
        Assert.DoesNotContain(events, item => item is AttackCompleted);
    }
}
