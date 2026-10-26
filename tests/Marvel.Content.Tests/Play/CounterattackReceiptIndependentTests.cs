using System.Text.Json;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CounterattackReceiptIndependentTests
{
    [Fact]
    [Rule("rr:attack-enemy-activation.step.6.a")]
    [Rule("rr:attack-enemy-activation.step.6.b")]
    public void EnemyAttackAndItsForcedRetaliationFinishBeforeCounterattackIsOffered()
    {
        // "The attack finishes resolving"; forced abilities including retaliate precede
        // "Non-forced abilities with the triggers listed above", including taking damage.
        var f = new CounterattackReceiptFixture();
        GiveRetaliationAndPrevention(f);
        f.Attack();
        var prompt = f.CounterPrompt();
        Assert.Equal(1, f.Enemy.Damage);
        Assert.Equal(f.Enemy.ObjectId, Assert.Single(f.Events.OfType<AttackCompleted>()).Enemy);
        var counter = Assert.Single(prompt.Affordances, offer => offer.AnchorId == f.Counter.ObjectId);
        f.Answer(prompt, Decision.Take(counter.Id));
        f.Finish();
        Assert.Equal(1, f.Hero.Damage);
        Assert.Equal(2, f.Enemy.Damage);
        Assert.Equal(DeckType.DiscardPile, f.Counter.Area.Type);
    }

    [Fact]
    [Rule("rr:attack-enemy-activation.step.6.a")]
    [Rule("rr:villain-defeat.3.1")]
    public void ForcedRetaliationPromotionKeepsCounterattackBoundToTheSameVillainCharacter()
    {
        // Forced retaliate precedes non-forced responses; the new same-title stage
        // "is considered the same character ... for purposes of card abilities".
        var f = new CounterattackReceiptFixture(villain: true);
        var next = f.World.CreateCard("01095", f.World.AreaOf(DeckType.VillainDeck));
        var side = f.World.CreateCard("01107", f.World.AreaOf(DeckType.EncounterDeck));
        f.Enemy.TakeDamage(Damage.Health(f.World, f.World.Facts, f.Enemy) - 1);
        GiveRetaliationAndPrevention(f);
        f.Attack();
        var prompt = f.CounterPrompt();
        Assert.Equal(DeckType.RemovedArea, f.Enemy.Area.Type);
        Assert.Equal(DeckType.VillainArea, next.Area.Type);
        Assert.Equal(DeckType.SideSchemesArea, side.Area.Type);
        var counter = Assert.Single(prompt.Affordances, offer => offer.AnchorId == f.Counter.ObjectId);
        f.Answer(prompt, Decision.Take(counter.Id));
        f.Finish();
        Assert.Equal(1, f.Hero.Damage);
        Assert.Equal(1, next.Damage);
        Assert.Equal(DeckType.DiscardPile, f.Counter.Area.Type);
    }

    [Fact]
    public void OccurrenceJsonRetainsItsExistingShapeWithAReceivedDamageReceipt()
    {
        // Engine choice: receipts reconstruct from decisions and are not persisted occurrence members.
        var f = new CounterattackReceiptFixture();
        f.Attack();
        f.CounterPrompt();
        var occurrence = f.World.Agenda.Occurrence!;
        Assert.Single(occurrence.ReceivedAttackDamage);
        using var document = JsonDocument.Parse(JsonSerializer.Serialize(occurrence));
        Assert.DoesNotContain(document.RootElement.EnumerateObject(),
            property => property.Name.Contains("ReceivedAttackDamage", StringComparison.OrdinalIgnoreCase));
        var reconstructed = JsonSerializer.Deserialize<Occurrence>(document.RootElement.GetRawText())!;
        Assert.Empty(reconstructed.ReceivedAttackDamage);
        Assert.Equal(JsonSerializer.Serialize(occurrence), JsonSerializer.Serialize(reconstructed));
        Assert.Equal(1, f.Finish());
        Assert.Equal(2, f.Enemy.Damage);
    }

    private static void GiveRetaliationAndPrevention(CounterattackReceiptFixture f)
    {
        f.World.Effects.Register(new(EffectSource.LastingEffect, "retaliate", 1,
            Affects: f.Hero.ObjectId));
        f.World.Effects.Register(new(EffectSource.LastingEffect, "preventDamage", 1,
            Affects: f.Hero.ObjectId, Lasts: Duration.NextUses(1)));
    }
}
