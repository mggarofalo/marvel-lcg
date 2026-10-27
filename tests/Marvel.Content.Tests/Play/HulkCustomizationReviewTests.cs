using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class HulkCustomizationReviewTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void InspiringPresenceUsesTheCurrentIdentityTraitRatherThanAnAllysTrait(bool identityHasTrait)
    {
        var (_, world) = HulkFirstSliceFixture.PlayingHulk(_ => { });
        var seat = world.Seats[0];
        World.MoveToTop(seat.IdentityCard, world.AreaOf(DeckType.RemovedArea));
        seat.IdentityCard = world.CreateCard("05001a", seat.Hero);
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0)));
        ally.Exhaust();
        world.Effects.Register(new(EffectSource.LastingEffect, "trait:AVENGER", 1,
            Affects: identityHasTrait ? seat.IdentityCard.ObjectId : ally.ObjectId));
        var source = world.CreateCard("10030", seat.Hand);
        world.CreateCard("10002", seat.Hand);
        Assert.Equal(identityHasTrait, AuthoredCards.Runner().Actions(world, 0).Any(a => a.Card == source.ObjectId));
    }

    [Theory]
    [InlineData("R", true)]
    [InlineData("Y", false)]
    [InlineData("B", false)]
    [InlineData("G", false)]
    [Rule("rr:wild-resource.1")]
    [Rule("rr:ownership-and-control.7.2")]
    public void ResourcefulUsesItsDeclaredTypeAndReturnsToItsOwnersDiscard(string declaration, bool bonus)
    {
        // A generated wild may specify "which resource type ... it is being used as".
        // A card leaving play "is placed in its owner's equivalent out-of-play area".
        var f = new HulkStarterFixture();
        var resource = f.World.CreateCard("10032",
            f.World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 0));
        resource.Exhaust();
        var strength = f.Held("10022");
        var kick = f.Held("10014");
        int deck = f.World.Seats[1].Deck.Cards.Count;
        var action = Assert.Single(f.Runner.Actions(f.World, 1), a => a.Card == kick.ObjectId);
        f.Events.AddRange(f.Runner.ExecuteAction(f.World, new PlayerAction(action,
            [resource.ObjectId, strength.ObjectId], [f.Villain.ObjectId],
            Allocations: [new(resource.ObjectId, 0, declaration), new(strength.ObjectId, 0, "RR")])).Events);
        f.Finish();
        Assert.Equal(4, f.Villain.Damage);
        Assert.Equal(bonus, Statuses.Has(f.World, f.Villain, Statuses.Stunned));
        Assert.Equal(deck - (bonus ? 1 : 0), f.World.Seats[1].Deck.Cards.Count);
        Assert.Same(f.World.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0), resource.Area);
        Assert.DoesNotContain(f.Runner.ResourceAbilities(f.World, 1), a => a.Effect == resource.ObjectId);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:you-your.6")]
    [Rule("rr:defend-defense.5")]
    [Rule("rr:defend-defense.7")]
    public void ArmorFollowsTheDefendingIdentityAcrossSeatsAndExcludesItsAlly(bool allyDefends)
    {
        // "you" must resolve as "the identity they control performed that game function".
        // A different defending player "becomes the new target of that attack".
        // After-defense abilities "resolve after that attack ends".
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        var armor = world.CreateCard("10031",
            world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 0));
        var ally = world.CreateCard("01083", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(1)));
        var enemy = world.CreateCard("01182", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var defender = allyDefends ? ally : world.Seats[1].IdentityCard;
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation { DealBoostCards = false }, 1, 2,
            Subject: enemy.ObjectId, Seat: 0));
        int responses = 0;
        for (int i = 0; i < 30; i++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
            var response = prompt.Affordances.SingleOrDefault(a => a.AnchorId == armor.ObjectId);
            var decision = Decision.Decline;
            if (response is not null)
            {
                Assert.False(allyDefends);
                Assert.Equal(1, prompt.Player);
                Assert.Single(events.OfType<AttackCompleted>());
                responses++;
                decision = Decision.Take(response.Id);
            }
            else if (prompt.Asking == Question.Defender && prompt.Player == 1)
                decision = Decision.Take(defender.ObjectId);
            Sequence.Answer(world, world.Facts, runner, prompt, decision, events);
        }
        Assert.False(defender.Ready);
        Assert.Equal(allyDefends ? 0 : 1, responses);
        Assert.Equal(allyDefends ? 0 : 1, enemy.Damage);
        Assert.Equal(allyDefends ? 2 : 0, ally.Damage);
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Single(events.OfType<AttackCompleted>());
        Assert.Empty(world.Agenda.Outstanding);
        Assert.False(world.IsOver);
    }
}
