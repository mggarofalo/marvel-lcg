using System.Text.Json;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DoctorStrangeUnflappableTests
{
    [Fact]
    public void InventoryKeepsUnflappableAtItsReviewedPosition()
    {
        using var document = JsonDocument.Parse(AuthoredCards.Text);
        var rows = document.RootElement.GetProperty("cards").EnumerateArray().ToArray();
        Assert.Equal(684, rows.Length);
        Assert.Equal("09020", rows[671].GetProperty("card").GetString());
        Assert.False(rows[671].TryGetProperty("startingCounters", out _));
        Assert.Equal(39, AuthoredCards.Book.Authored.Count(face => face.StartsWith("09", StringComparison.Ordinal)));
        string[] blocked = ["09030"];
        Assert.All(blocked, face => Assert.DoesNotContain(face, AuthoredCards.Book.Authored));
    }

    [Theory]
    [InlineData(false, false, false, true, false, false, 1)]
    [InlineData(false, true, false, true, false, false, 1)]
    [InlineData(true, false, false, false, false, false, 1)]
    [InlineData(false, false, true, false, false, false, 1)]
    [InlineData(false, false, false, true, false, true, 1)]
    [InlineData(true, false, false, true, true, false, 1)]
    [InlineData(false, false, false, false, false, false, 0)]
    [Rule("rr:defend-defense.2")]
    [Rule("rr:defend-defense.3")]
    [Rule("rr:defend-defense.7")]
    public void CompletedAttackRequiresControllerHeroDefenseWithoutAttackDamage(
        bool attackDamage, bool priorDamage, bool allyDefense, bool eligible, bool eventDefense, bool boost, int defenderSeat)
    {
        // A hero's basic defense reduces attack damage; an ally becomes the target instead.
        // "Abilities that trigger after a character defends an attack resolve after that attack ends."
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var hero = world.Seats[defenderSeat].IdentityCard;
        var source = world.CreateCard("09020", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 0));
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        var enemy = boost ? world.TheCardIn(DeckType.VillainArea)! : world.CreateCard("01103", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var defense = ConfigureAttack(world, hero, enemy, attackDamage, priorDamage, eventDefense, boost);
        int held = world.Seats[1].Hand.Cards.Count;
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation { DealBoostCards = boost }, 1, 2,
            Subject: enemy.ObjectId, Seat: defenderSeat));
        bool offered = false;
        for (int i = 0; i < 40; i++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
            var offer = prompt.Affordances.SingleOrDefault(item => item.AnchorId == source.ObjectId);
            if (offer is not null)
            {
                Assert.True(eligible);
                Assert.Equal(1, prompt.Player);
                Assert.Single(events.OfType<AttackCompleted>());
                Assert.True(prompt.Cancellable);
                offered = true;
            }
            Sequence.Answer(world, world.Facts, runner, prompt,
                AnswerTo(prompt, source.ObjectId, defense?.ObjectId,
                    allyDefense ? ally.ObjectId : hero.ObjectId, defenderSeat, eventDefense), events);
        }
        Assert.Equal(eligible, offered);
        AssertCompleted(world, source, events, held, eligible, eventDefense, boost);
    }

    private static Card? ConfigureAttack(World world, Card hero, Card enemy,
        bool attackDamage, bool priorDamage, bool eventDefense, bool boost)
    {
        world.Effects.Register(new(EffectSource.LastingEffect, "attack", attackDamage ? 3 : -10, Affects: enemy.ObjectId));
        if (priorDamage) hero.TakeDamage(1);
        var defense = eventDefense ? world.CreateCard("01003", world.Seats[1].Hand) : null;
        if (boost) world.CreateCard("01154", world.AreaOf(DeckType.EncounterDeck));
        return defense;
    }

    private static void AssertCompleted(World world, Card source, List<GameEvent> events,
        int held, bool eligible, bool eventDefense, bool boost)
    {
        Assert.Equal(held + (eligible ? 1 : 0) - (eventDefense ? 1 : 0), world.Seats[1].Hand.Cards.Count);
        Assert.Equal(!eligible, source.Ready);
        Assert.Single(events.OfType<AttackCompleted>());
        if (boost) Assert.Equal(1, world.Seats[1].IdentityCard.Damage);
        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.IsOver);
    }

    private static Decision AnswerTo(Prompt prompt, int source, int? defense,
        int defender, int seat, bool eventDefense)
    {
        var response = prompt.Affordances.SingleOrDefault(item => item.AnchorId == source);
        if (response is not null) return Decision.Take(response.Id);
        if (defense is { } defenseCard)
        {
            var offered = prompt.Affordances.SingleOrDefault(item => item.AnchorId == defenseCard);
            if (offered is not null) return Decision.Take(offered.Id);
        }
        if (!eventDefense && prompt.Asking == Question.Defender && prompt.Player == seat)
            return Decision.Take(defender);
        return Decision.Decline;
    }

    [Fact]
    [Rule("rr:ownership-and-control.2.1")]
    public void PlayCanTransferControlAndMaxOneAppliesToRecipient()
    {
        // An upgrade attached to another player's card "is controlled by that other player."
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var owner = world.Seats[0];
        var source = world.CreateCard("09020", owner.Hand);
        var payment = world.CreateCard("01088", owner.Hand);
        var recipient = world.Seats[1].IdentityCard;
        Assert.Contains(recipient.ObjectId, runner.AttachmentTargets(world, source)!);
        CardPlay.Play(world, world.Facts, runner, owner, source, [payment.ObjectId], [], [recipient.ObjectId]);
        Assert.Equal(0, source.Owner);
        Assert.Equal(1, source.Area.PlayArea.Player);
        var second = world.CreateCard("09020", owner.Hand);
        Assert.DoesNotContain(recipient.ObjectId, runner.AttachmentTargets(world, second)!);
        Assert.Contains(owner.IdentityCard.ObjectId, runner.AttachmentTargets(world, second)!);
    }
}
