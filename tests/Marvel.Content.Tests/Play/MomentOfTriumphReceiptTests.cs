using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Actual Moment of Triumph with synthetic observers; this does not admit the pack.
public sealed class MomentOfTriumphReceiptTests
{
    [Fact]
    public void DefaultExcessIsOmittedAndNonzeroExcessRoundTrips()
    {
        var world = ThorActualFixture.Board("06001a");
        var enemy = world.TheCardIn(DeckType.VillainArea)!;
        var recipient = Attacked.Capture(enemy, world.Facts);
        string baseline = JsonSerializer.Serialize(recipient);
        Assert.DoesNotContain("ExcessDamage", baseline, StringComparison.Ordinal);
        var settled = recipient with { ExcessDamage = 3 };
        string json = JsonSerializer.Serialize(settled);
        Assert.Contains("ExcessDamage", json, StringComparison.Ordinal);
        var restored = JsonSerializer.Deserialize<Attacked>(json)!;
        Assert.Equal(settled.Card, restored.Card);
        Assert.Equal(settled.Incarnation, restored.Incarnation);
        Assert.Equal(3, restored.ExcessDamage);
        Assert.Equal(0, JsonSerializer.Deserialize<Attacked>(baseline)!.ExcessDamage);
    }

    [Fact]
    [Rule("rr:overkill.3")]
    [Rule("rr:leaves-play.1")]
    public void DefeatedRecipientReenteringBeforeResponseKeepsOriginalExcess()
    {
        // rr:leaves-play.1: reentry is a new copy; the response still concerns
        // the defeated original, not that new copy's health or incarnation.
        var book = AbilityCatalog.Parse("""
            {"cards":[
              {"card":"01092","abilities":[{
                "trigger":{"event":"WhenAttackEnds","alsoHappened":"WhenCardDefeated","timing":"ForcedResponse","target":"enemy"},
                "effect":{"putIntoPlay":{"card":{"cardsIn":{"areas":["encounterDiscardPile"],"title":"Armored Guard"}},"where":"engagedWithYou"}}
              }]}
            ]}
            """);
        var runner = WithMoment(book);
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var hero = world.Seats[0].IdentityCard;
        hero.TakeDamage(4);
        long attack = StateFields.Modified(world, hero, "attack", world.Facts, world.Players);
        world.Effects.Register(new(EffectSource.LastingEffect, "attack", 5 - attack, Affects: hero.ObjectId));
        var enemy = world.CreateCard("01120", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Effects.Register(new(EffectSource.LastingEffect, "health", 10, Affects: enemy.ObjectId));
        enemy.TakeDamage(DamagePlacement.Health(world, world.Facts, enemy) - 2);
        int incarnation = enemy.Incarnation;
        world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var moment = world.CreateCard("12030", world.Seats[0].Hand);
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, enemy, []);
        var prompt = Sequence.Work(world, world.Facts, runner, []);
        Assert.True(enemy.Incarnation > incarnation);
        Assert.Equal(DeckType.EngagedEnemiesArea, enemy.Area.Type);
        Assert.Equal(0, enemy.Damage);
        var response = Assert.Single(prompt!.Affordances, option => option.AnchorId == moment.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(response.Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(1, hero.Damage);
        Assert.Equal(DeckType.DiscardPile, moment.Area.Type);
    }

    [Rule("rr:overkill.3")]
    [Rule("rr:overkill.1")]
    [Theory]
    [InlineData(1, false, false, 4, 2)]
    [InlineData(2, false, false, 3, 4)]
    [InlineData(2, false, true, 3, 4)]
    [InlineData(5, false, false, 0, 4)]
    [InlineData(6, false, false, -1, 4)]
    [InlineData(2, true, false, -1, 4)]
    public void BasicAttackResponseUsesSettledExcess(int remaining, bool tough, bool overkill, int expected, int damage)
    {
        // rr:overkill.3: an ability counts "the same value of excess damage"
        // as overkill; rr:overkill.1 requires that the attack defeats the enemy.
        var book = AbilityCatalog.Parse("""
            {"cards":[{"card":"01092","abilities":[{
              "trigger":{"event":"WhenAttackEnds","timing":"ForcedResponse","target":"enemy"},
              "effect":{"placeCounters":{"card":"you","counter":"excess_seen","count":{"add":[1,{"attackExcessDamage":"trigger.target"}]}}}
            }]}]}
            """);
        var runner = WithMoment(book);
        var world = ThorActualFixture.Board("06001a");
        world.Abilities = runner;
        var hero = world.Seats[0].IdentityCard;
        hero.TakeDamage(damage);
        long attack = StateFields.Modified(world, hero, "attack", world.Facts, world.Players);
        world.Effects.Register(new(EffectSource.LastingEffect, "attack", 5 - attack, Affects: hero.ObjectId));
        var enemy = world.CreateCard("01120", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Effects.Register(new(EffectSource.LastingEffect, "health", 10, Affects: enemy.ObjectId));
        enemy.TakeDamage(DamagePlacement.Health(world, world.Facts, enemy) - remaining);
        if (tough) Statuses.Give(world, enemy, Statuses.Tough);
        if (overkill) world.Effects.Register(new(EffectSource.LastingEffect, Keywords.Overkill, 1, Affects: hero.ObjectId));
        world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var eventCard = world.CreateCard("12030", world.Seats[0].Hand);
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, enemy, []);
        var prompt = Sequence.Work(world, world.Facts, runner, []);
        Assert.Equal(Math.Max(0, expected) + 1, hero.Tokens.GetValueOrDefault("c_excess_seen"));
        if (expected <= 0)
        {
            Assert.True(prompt is null || prompt.Affordances.All(option => option.AnchorId != eventCard.ObjectId));
            Assert.Equal(damage, hero.Damage);
            if (expected == 0) Assert.Equal(DeckType.EncounterDiscardPile, enemy.Area.Type);
            return;
        }
        var response = Assert.Single(prompt!.Affordances, option => option.AnchorId == eventCard.ObjectId);
        Sequence.Answer(world, world.Facts, runner, prompt, Decision.Take(response.Id), []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(Math.Max(0, damage - expected), hero.Damage);
        Assert.Equal(overkill ? 3 : 0, world.TheCardIn(DeckType.VillainArea)!.Damage);
        Assert.Equal(DeckType.EncounterDiscardPile, enemy.Area.Type);
        Assert.Equal(DeckType.DiscardPile, eventCard.Area.Type);
    }
    [Fact]
    [Rule("rr:overkill.3")]
    public void ResponseOfferExplainsKnownHealingQuantity()
    {
        // Excess-counting abilities use the "same value of excess damage" as overkill.
        var world = ThorActualFixture.Board("06001a");
        var runner = AuthoredCards.Runner();
        var hero = world.Seats[0].IdentityCard;
        hero.TakeDamage(4);
        long attack = StateFields.Modified(world, hero, "attack", world.Facts, world.Players);
        world.Effects.Register(new(EffectSource.LastingEffect, "attack", 5 - attack, Affects: hero.ObjectId));
        var enemy = world.CreateCard("01120", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        enemy.TakeDamage(DamagePlacement.Health(world, world.Facts, enemy) - 2);
        var moment = world.CreateCard("12030", world.Seats[0].Hand);
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, enemy, []);
        var prompt = Sequence.Work(world, world.Facts, runner, [])!;
        var response = Assert.Single(prompt.Affordances, option => option.AnchorId == moment.ObjectId);
        Assert.Contains("3", response.Description ?? "", StringComparison.Ordinal);
    }

    private static AbilityRunner WithMoment(AbilityBook book) => new(book with
    {
        Abilities = [.. book.Abilities, .. AuthoredCards.Book.On("12030")],
        Authored = book.Authored.Append("12030").ToHashSet(StringComparer.Ordinal),
    });

}
