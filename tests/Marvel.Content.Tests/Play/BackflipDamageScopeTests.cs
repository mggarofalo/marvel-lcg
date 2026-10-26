using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
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

public sealed class BackflipDamageScopeTests
{
    [Theory]
    [InlineData("01103")]
    [InlineData("01154")]
    public void ActualNonattackRevealDoesNotOfferBackflip(string face)
    {
        // Printed 01003 requires "damage from an attack"; these reveals deal damage directly.
        var f = new BlackWidowPreventionFixture();
        var backflip = f.World.CreateCard("01003", f.World.Seats[1].Hand);
        var reveal = f.World.CreateCard(face, f.World.AreaOf(DeckType.RevealingArea));
        f.Runner.ExecuteReveal(f.World, reveal, 1);
        f.Finish(backflip);
        Assert.Equal(1, f.Hero.Damage);
        Assert.Same(f.World.Seats[1].Hand, backflip.Area);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:defend-defense.4.8")]
    public void GenericPreventionStillOffersForDirectDamageDuringAnAttack(bool stance)
    {
        // "A player can trigger abilities labeled as a defense outside of an attack"
        // when its own condition holds. Printed Backflip alone requires attack damage.
        var f = new BlackWidowPreventionFixture();
        var backflip = f.World.CreateCard("01003", f.World.Seats[1].Hand);
        var prevention = f.Upgrade(stance ? "08032" : "05017");
        if (!stance) prevention.PlaceTokens("c_reflection", 1);
        f.World.Attack = new(f.Villain.ObjectId, 1, f.Hero.ObjectId);
        var result = DamagePlacement.Resolve(f.World, f.World.Facts,
            new(f.Villain, f.Hero, 2, "nested", "Attack"), f.Events);
        var prompt = f.Find(prevention);
        Assert.DoesNotContain(prompt.Affordances, a => a.AnchorId == backflip.ObjectId);
        Assert.False(f.World.Agenda.Occurrence!.DamageFromAttack);
        f.Answer(prompt, Decision.Decline);
        f.Finish(backflip);
        Assert.True(result.IsComplete);
        Assert.Equal(2, result.Taken);
        Assert.Same(f.World.Seats[1].Hand, backflip.Area);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    [Rule("rr:overkill.2")]
    public void ActualAttackAndItsOverkillOfferBackflip(bool overkill)
    {
        // Overkill "is considered damage from an attack", including an ally's excess.
        var f = new BlackWidowPreventionFixture();
        var backflip = f.World.CreateCard("01003", f.World.Seats[1].Hand);
        Card? ally = null;
        if (overkill)
        {
            ally = f.World.CreateCard("01020", f.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
            ally.TakeDamage(2);
            f.World.Effects.Register(new(EffectSource.LastingEffect, Keywords.Overkill, 1,
                Card: f.Villain.ObjectId, Affects: f.Villain.ObjectId));
        }
        f.World.Agenda.Now(new PhaseStep(new EnemyAttackOperation(), 1, 0, Subject: f.Villain.ObjectId, Seat: 1));
        Prompt? prevention = null;
        for (int count = 0; count < 12; count++)
        {
            var prompt = f.Work();
            if (prompt.Affordances.Any(a => a.AnchorId == backflip.ObjectId)) { prevention = prompt; break; }
            f.Answer(prompt, ally is not null && prompt.Asking == Question.Defender
                && prompt.Affordances.Any(a => a.Id == ally.ObjectId) ? Decision.Take(ally.ObjectId) : Decision.Decline);
        }
        Assert.NotNull(prevention);
        Assert.True(f.World.Agenda.Occurrence!.DamageFromAttack);
        f.Use(prevention, backflip);
        f.Finish(backflip);
        Assert.Equal(0, f.Hero.Damage);
        Assert.Equal(DeckType.DiscardPile, backflip.Area.Type);
        var completed = Assert.Single(f.Events.OfType<AttackCompleted>());
        Assert.True(completed.DamageDealt > 0);
        if (overkill) Assert.Equal(DeckType.DiscardPile, ally!.Area.Type);
    }

    [Fact]
    public void ActualBoostDamageCannotUseBackflipBeforeTheAttackDamage()
    {
        // Printed 01154 Boost deals 1 damage separately from the enemy's attack damage.
        var f = new BlackWidowPreventionFixture();
        var backflip = f.World.CreateCard("01003", f.World.Seats[1].Hand);
        var boost = f.World.CreateCard("01154", f.World.AreaOf(DeckType.EncounterDeck));
        World.MoveToTop(boost, boost.Area);
        f.World.Agenda.Now(new PhaseStep(new EnemyAttackOperation(), 1, 0, Subject: f.Villain.ObjectId, Seat: 1));
        var prompt = f.Find(backflip);
        Assert.Equal(1, f.Hero.Damage);
        Assert.True(f.World.Agenda.Occurrence!.DamageFromAttack);
        f.Use(prompt, backflip);
        f.Finish(backflip);
        Assert.Equal(1, f.Hero.Damage);
        Assert.Equal(DeckType.DiscardPile, backflip.Area.Type);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedDamageAndDamageCostsCannotBorrowTheOuterAttack(bool cost)
    {
        // Synthetic interrupt keeps an actual attack suspended while direct damage or a cost resolves.
        var book = JsonNode.Parse(AuthoredCards.Text)!;
        var row = book["cards"]!.AsArray().Single(r => r!["card"]!.GetValue<string>() == "01008")!;
        row["abilities"] = JsonNode.Parse(cost ? """
            [{"trigger":{"event":"WhenDamageWouldBeDealt","timing":"Interrupt","target":"you"},
              "cost":{"seq":[{"discard":"this"},{"takeDamage":{"cards":"you","amount":1}}]},
              "effect":{"draw":{"player":"you","count":1}}}]
            """ : """
            [{"trigger":{"event":"WhenDamageWouldBeDealt","timing":"Interrupt","target":"you"},
              "cost":{"discard":"this"},"effect":{"dealDamage":{"cards":"you","amount":2}}}]
            """);
        var f = new BlackWidowPreventionFixture(runner: new AbilityRunner(AbilityCatalog.Parse(book.ToJsonString())));
        var source = f.Upgrade("01008");
        var barrier = cost ? null : f.Upgrade("05017");
        barrier?.PlaceTokens("c_reflection", 1);
        var backflip = f.World.CreateCard("01003", f.World.Seats[1].Hand);
        f.World.Agenda.Now(new PhaseStep(new EnemyAttackOperation(), 1, 0, Subject: f.Villain.ObjectId, Seat: 1));
        f.Use(f.Find(source), source);
        if (barrier is not null)
        {
            var nested = f.Find(barrier);
            Assert.NotNull(f.World.Attack);
            Assert.False(f.World.Agenda.Occurrence!.DamageFromAttack);
            Assert.DoesNotContain(nested.Affordances, a => a.AnchorId == backflip.ObjectId);
            f.Answer(nested, Decision.Decline);
        }
        var outer = f.Find(backflip);
        Assert.Equal(cost ? 1 : 2, f.Hero.Damage);
        Assert.True(f.World.Agenda.Occurrence!.DamageFromAttack);
        f.Use(outer, backflip);
        f.Finish(backflip);
        Assert.Equal(cost ? 1 : 2, f.Hero.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(DeckType.DiscardPile, backflip.Area.Type);
    }

    [Fact]
    [Rule("rr:overkill.2")]
    public void EnergyBarrierPreventsOnlyItsOwnOverkillShareAndKeepsAttackDamageDealt()
    {
        // Overkill is "damage from an attack"; prevention does not change its dealt receipt.
        var f = new BlackWidowPreventionFixture();
        var barrier = f.Upgrade("05017");
        barrier.PlaceTokens("c_reflection", 1);
        var ally = f.World.CreateCard("01020", f.World.AreaOf(DeckType.AlliesArea, PlayArea.Of(1), cardOwner: 1));
        ally.TakeDamage(2);
        f.World.Effects.Register(new(EffectSource.LastingEffect, Keywords.Overkill, 1,
            Card: f.Villain.ObjectId, Affects: f.Villain.ObjectId));
        f.World.Agenda.Now(new PhaseStep(new EnemyAttackOperation(), 1, 0, Subject: f.Villain.ObjectId, Seat: 1));
        Prompt? prevention = null;
        for (int count = 0; count < 12; count++)
        {
            var prompt = f.Work();
            if (prompt.Affordances.Any(a => a.AnchorId == barrier.ObjectId)) { prevention = prompt; break; }
            f.Answer(prompt, prompt.Asking == Question.Defender && prompt.Affordances.Any(a => a.Id == ally.ObjectId)
                ? Decision.Take(ally.ObjectId) : Decision.Decline);
        }
        Assert.NotNull(prevention);
        long remaining = f.World.Agenda.Occurrence!.DirectDamage!.Remaining;
        Assert.True(remaining > 0);
        f.Use(prevention, barrier);
        var target = f.Work();
        f.Answer(target, Decision.Take(Assert.Single(target.Affordances).Id));
        f.Finish(barrier);
        Assert.Equal(remaining - 1, f.Hero.Damage);
        Assert.Equal(1, f.Villain.Damage);
        Assert.Equal(remaining + 1, Assert.Single(f.Events.OfType<AttackCompleted>()).DamageDealt);
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
    }
}
