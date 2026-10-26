using System.Text.Json;
using System.Text.Json.Serialization;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class CounterattackCurrentTimingTests
{
    [Fact]
    [Rule("rr:attack-enemy-activation.step.6.a")]
    public void GenericForcedDamageResponseSharesTheFinishedAttackWindow()
    {
        // "The attack finishes resolving" before forced damage-taking responses.
        // Synthetic marker response distinguishes damage timing without admitting another card.
        var f = new CounterattackReceiptFixture(additionalRow: """
            {"card":"09020","abilities":[{
              "trigger":{"event":"WhenDamageDealt","timing":"ForcedResponse","target":"you"},
              "effect":{"giveStatus":{"card":"you","status":"tough"}}
            }]}
            """);
        f.World.CreateCard("09020", f.World.AreaOf(DeckType.UpgradesArea, PlayArea.Of(1), cardOwner: 1));
        f.Attack();
        var prompt = f.CounterPrompt();
        Assert.Single(f.Events.OfType<AttackCompleted>());
        Assert.True(Statuses.Has(f.World, f.Hero, Statuses.Tough));
        var tough = Assert.Single(f.Events.OfType<CardAttached>(), item =>
            item.Host == f.Hero.ObjectId && item.Verb == "Give_Status"
            && f.World.Cards[item.Card].FaceId == Statuses.Tough);
        Assert.True(f.Events.IndexOf(Assert.Single(f.Events.OfType<AttackCompleted>()))
            < f.Events.IndexOf(tough));
        Assert.Single(f.World.Windows.Current!.Value.Occurrence.ReceivedAttackDamage);
        Assert.Contains(prompt.Affordances, offer => offer.AnchorId == f.Counter.ObjectId);
        Assert.Equal(1, f.Finish());
        Assert.Equal(2, f.Enemy.Damage);
    }

    [Fact]
    [Rule("rr:villain-defeat.4.1")]
    public void DifferentTitlePromotionCannotReceiveTheOriginalCharactersResponse()
    {
        // The new different-title stage "is not considered the same character".
        var f = new CounterattackReceiptFixture(villain: true);
        var next = f.World.CreateCard("01113", f.World.AreaOf(DeckType.VillainDeck));
        f.Enemy.TakeDamage(Damage.Health(f.World, f.World.Facts, f.Enemy) - 1);
        f.World.Effects.Register(new(EffectSource.LastingEffect, "retaliate", 1, Affects: f.Hero.ObjectId));
        f.Attack();
        Assert.Equal(0, f.Finish());
        Assert.Equal(DeckType.RemovedArea, f.Enemy.Area.Type);
        Assert.Equal(DeckType.VillainArea, next.Area.Type);
        Assert.Equal(0, next.Damage);
        Assert.Equal(DeckType.UpgradesArea, f.Counter.Area.Type);
    }

    [Fact]
    public void QueuedAttackDoesNotBorrowTheFirstAttacksPlacement()
    {
        var f = new CounterattackReceiptFixture();
        var next = f.World.CreateCard("01102", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        f.World.Effects.Register(new(EffectSource.LastingEffect, "attack", -10, Affects: next.ObjectId));
        f.Attack();
        f.World.Agenda.Add(new PhaseStep(new EnemyAttackOperation { DealBoostCards = false },
            1, 2, Subject: next.ObjectId, Seat: 1));
        Assert.Equal(1, f.Finish(use: false));
        Assert.Equal(2, f.Events.OfType<AttackCompleted>().Count());
        Assert.Equal(2, f.Hero.Damage);
        Assert.Equal(0, next.Damage);
        Assert.Equal(DeckType.UpgradesArea, f.Counter.Area.Type);
    }

    [Fact]
    public void PendingAttackEndReceiptUsesStrictExistingJsonShape()
    {
        var f = new CounterattackReceiptFixture();
        f.Attack();
        f.CounterPrompt();
        var options = new JsonSerializerOptions { UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow };
        var occurrence = f.World.Agenda.Occurrence!;
        string json = JsonSerializer.Serialize(occurrence, options);
        var restored = JsonSerializer.Deserialize<Occurrence>(json, options)!;
        Assert.Empty(restored.ReceivedAttackDamage);
        Assert.Equal(json, JsonSerializer.Serialize(restored, options));
        Assert.DoesNotContain("ReceivedDamage", JsonSerializer.Serialize(f.World.FinishedAttack, options), StringComparison.Ordinal);
        Assert.Equal(1, f.Finish());
        Assert.Equal(2, f.Enemy.Damage);
    }
}
