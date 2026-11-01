using Marvel.Cards.Dsl;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;
using static Marvel.Content.Tests.Play.BasicAttackOverkillFixture;

namespace Marvel.Content.Tests.Play;

public sealed class BasicAttackOverkillTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    [Rule("rr:overkill.1")]
    [Rule("rr:lasting-effects.1")]
    public void SettledPaymentGrantsOnlyThatAttackEvenAfterChoice(bool mixed, bool suspend)
    {
        // "If this attack defeats a minion, deal the damage beyond its hit points to the villain."
        // A lasting effect persists "for a specified duration": this synthetic bonus is for one attack.
        var runner = Runner(suspend);
        var (world, source, physical, other, minion) = Board(runner, mixed);
        var hero = world.Seats[0].IdentityCard;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, minion, []);
        Pay(world, runner, source, physical, other);
        if (suspend)
        {
            var choice = Sequence.Work(world, world.Facts, runner, [])!;
            Assert.Equal(0, villain.Damage);
            Sequence.Answer(world, world.Facts, runner, choice, Decision.Take(choice.Affordances[0].Id), []);
        }
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(mixed ? 0 : 9, villain.Damage);
        Assert.Equal(DeckType.DiscardPile, source.Area.Type);
        Assert.Equal(2, StateFields.Modified(world, hero, "attack", world.Facts, world.Players));
        Assert.False(AttackKeywords.Has(world, hero, hero, Keywords.Overkill, basic: true));
        var next = Minion(world);
        next.TakeDamage(2);
        hero.Refresh();
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, next, []);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.EncounterDiscardPile, next.Area.Type);
        Assert.Equal(mixed ? 0 : 9, villain.Damage);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:overkill.1")]
    public void ToughPreventsDefeatAndThereforeAllSpill()
    {
        // "If this attack defeats a minion"; Tough prevents all damage from this attack.
        var runner = Runner();
        var (world, source, physical, other, minion) = Board(runner);
        Statuses.Give(world, minion, Statuses.Tough);
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, minion, []);
        Pay(world, runner, source, physical, other);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.Equal(0, minion.Damage);
        Assert.False(Statuses.Has(world, minion, Statuses.Tough));
        Assert.Equal(0, world.TheCardIn(DeckType.VillainArea)!.Damage);
    }

    [Fact]
    [Rule("rr:lasting-effects.1")]
    public void NestedCardAttackDoesNotInheritOuterBonusOrOverkill()
    {
        // The "specified duration" is the original attack, not a nested card attack.
        const string attack = """{"attack":{"target":{"titled":"Weapons Runner"},"effect":{"dealAttackDamage":{"cards":{"titled":"Weapons Runner"},"amount":4}}}}""";
        var runner = Runner(after: attack);
        var (world, source, physical, other, minion) = Board(runner);
        World.MoveToTop(minion, world.AreaOf(DeckType.EncounterDiscardPile));
        minion = world.CreateCard("01121", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        BasicPowerInitiation.BasicAttack(world, world.Facts, 0, villain, []);
        Pay(world, runner, source, physical, other);
        Sequence.Finish(world, world.Facts, runner, []);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(12, villain.Damage);
    }

    [Theory]
    [InlineData("WhenHeroBasicUseInitiated", "Interrupt")]
    [InlineData("WhenActionTriggered", "Action")]
    [InlineData("WhenHeroBasicAttackInitiated", "Response")]
    [InlineData("WhenAllyBasicUseInitiated", "Interrupt")]
    [InlineData("WhenHeroBasicThwartInitiated", "Interrupt")]
    public void KeywordOnlyInstructionRequiresExactBasicAttackInterrupt(string occurrence, string timing)
    {
        var text = """{"cards":[{"card":"10003","abilities":[{"trigger":{"event":"EVENT","timing":"TIMING"},"effect":{"boostBasicUse":"overkill"}}]}]}"""
            .Replace("EVENT", occurrence, StringComparison.Ordinal).Replace("TIMING", timing, StringComparison.Ordinal);
        var error = Assert.Throws<AbilityException>(() => new Marvel.Cards.Run.AbilityRunner(AbilityCatalog.Parse(text)));
        Assert.Contains("basic-attack interrupt", error.Message);
    }

    [Theory]
    [InlineData("{\"keyword\":\"overkill\"}")]
    [InlineData("\"Overkill\"")]
    [InlineData("-1")]
    [InlineData("0")]
    [InlineData("true")]
    public void UnknownKeywordFormsAndNonpositiveAmountsAreRejected(string instruction)
    {
        var text = """{"cards":[{"card":"10003","abilities":[{"trigger":{"event":"WhenHeroBasicAttackInitiated","timing":"Interrupt"},"effect":{"boostBasicUse":BODY}}]}]}"""
            .Replace("BODY", instruction, StringComparison.Ordinal);
        Assert.Throws<AbilityException>(() => new Marvel.Cards.Run.AbilityRunner(AbilityCatalog.Parse(text)));
    }
}
