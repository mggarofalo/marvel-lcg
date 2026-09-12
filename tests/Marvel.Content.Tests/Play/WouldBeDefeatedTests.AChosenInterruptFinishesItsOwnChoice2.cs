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
public sealed class WouldBeDefeatedAChosenInterruptFinishesItsOwnChoiceTests : WouldBeDefeatedTestBase
{
    [Rule("rr:forced.6")]
    [Rule("rr:damage.step.6")]
    [Fact]
    public void AChosenInterruptFinishesItsOwnChoiceBeforeDefeatContinues()
    {
        var world = Empty("""
            {"cards":[{"card":"01185","abilities":[{
              "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt",
                         "subject":"attachedTo"},
              "effect":{"choose":{"options":[
                {"heal":{"card":"attachedTo","amount":{"damageOn":"attachedTo"}}},
                {"placeThreat":{"scheme":{"query":"mainScheme"},"amount":1}}
              ]}}
            }]}]}
            """);
        var minion = world.CreateCard(Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard(AuthoredCards.BiomechanicalUpgrades, world.AreaOf(DeckType.UpgradesArea, minion.Area.PlayArea, minion.ObjectId));
        var events = new List<GameEvent>();
        DamagePlacement.Deal(world, Cards, minion, minion, 3, "test", "test", events);
        var interrupt = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Sequence.Answer(world, Cards, world.Abilities, interrupt, Decision.Take(interrupt.Affordances[0].Id), events);
        var inner = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(Question.Option, inner.Asking);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        var heal = Assert.Single(inner.Affordances, option => option.Label == "heal");
        Sequence.Answer(world, Cards, world.Abilities, inner, Decision.Take(heal.Id), events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(0, minion.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
    }

    [Rule("rr:damage.step.6")]
    [Rule("rr:ability.11")]
    [Fact]
    public void AnOptionalInterruptCanBeDeclinedAndDefeatResumes()
    {
        var world = Empty("""
            {"cards":[{"card":"01185","abilities":[{
              "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt",
                         "subject":"attachedTo"},
              "effect":{"heal":{"card":"attachedTo","amount":{"damageOn":"attachedTo"}}}
            }]}]}
            """);
        var minion = world.CreateCard(Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard(AuthoredCards.BiomechanicalUpgrades, world.AreaOf(DeckType.UpgradesArea, minion.Area.PlayArea, minion.ObjectId));
        var events = new List<GameEvent>();
        DamagePlacement.Deal(world, Cards, minion, minion, 3, "test", "test", events);
        var asked = Sequence.Work(world, Cards, world.Abilities, events);
        Assert.NotNull(asked);
        Sequence.Answer(world, Cards, world.Abilities, asked, Decision.Decline, events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(1, events.Count(happened => happened is FieldSet set && set.Card == minion.ObjectId && set.Field == "health" && set.To == 0));
    }

    [Rule("rr:damage.step.6")]
    [Rule("rr:retaliate-x.2")]
    [Fact]
    public void ADefeatDecisionFinishesBeforeAnAttackChecksRetaliate()
    {
        var world = Empty("""
            {"cards":[{"card":"01185","abilities":[{
              "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt",
                         "subject":"attachedTo"},
              "effect":{"heal":{"card":"attachedTo","amount":1}}
            }]}]}
            """);
        var engaged = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0));
        var attacker = world.CreateCard(Mercenary, engaged);
        var minion = world.CreateCard(Mercenary, engaged);
        world.CreateCard(AuthoredCards.BiomechanicalUpgrades, world.AreaOf(DeckType.UpgradesArea, minion.Area.PlayArea, minion.ObjectId));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: "retaliate", Amount: 3, Affects: minion.ObjectId));
        var events = new List<GameEvent>();
        var result = DamageAttacks.Attack(world, Cards, attacker, minion, 3, "test", "Attack", events);
        var asked = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.True(result.Suspended);
        Assert.Equal(0, attacker.Damage);
        Sequence.Answer(world, Cards, world.Abilities, asked, Decision.Decline, events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(0, attacker.Damage);
    }

    [Rule("rr:forced.4")]
    [Rule("rr:would.1")]
    [Fact]
    public void AForcedReplacementInvalidatesALaterOptionalInterrupt()
    {
        var world = Empty("""
            {"cards":[
              {"card":"01185","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"ForcedInterrupt",
                           "subject":"attachedTo"},
                "effect":{"heal":{"card":"attachedTo","amount":{"damageOn":"attachedTo"}}}
              }]},
              {"card":"01101","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt",
                           "subject":"this"},
                "effect":{"discard":"this"}
              }]}
            ]}
            """);
        var minion = world.CreateCard(Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard(AuthoredCards.BiomechanicalUpgrades, world.AreaOf(DeckType.UpgradesArea, minion.Area.PlayArea, minion.ObjectId));
        bool defeated = DamagePlacement.Deal(world, Cards, minion, minion, 3, "test", "test", []);
        Assert.False(defeated);
        Assert.Equal(0, minion.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
    }

    [Rule("rr:damage.step.6")]
    [Fact]
    public void AForcedInterruptThatDoesNotPreventDefeatLeavesItImminent()
    {
        var world = Empty("""
            {"cards":[{"card":"01185","abilities":[{
              "trigger":{"event":"WhenCardWouldBeDefeated","timing":"ForcedInterrupt",
                         "subject":"attachedTo"},
              "effect":{"discard":"this"}
            }]}]}
            """);
        var minion = world.CreateCard(Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var upgrade = world.CreateCard(AuthoredCards.BiomechanicalUpgrades, world.AreaOf(DeckType.UpgradesArea, minion.Area.PlayArea, minion.ObjectId));
        Agendas.Happening(world);
        bool defeated = DamagePlacement.Deal(world, Cards, minion, minion, 3, "test", "test", []);
        Assert.True(defeated);
        Assert.Equal(DeckType.EncounterDiscardPile, minion.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, upgrade.Area.Type);
    }

    [Rule("rr:forced.5")]
    [Fact]
    public void TheFirstPlayerOrdersTwoForcedInterrupts()
    {
        var world = Empty("""
            {"cards":[
              {"card":"01185","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"ForcedInterrupt",
                           "subject":"attachedTo"},
                "effect":{"heal":{"card":"attachedTo","amount":{"damageOn":"attachedTo"}}}
              }]},
              {"card":"01101","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"ForcedInterrupt",
                           "subject":"this"},
                "effect":{"discard":"this"}
              }]}
            ]}
            """);
        var minion = world.CreateCard(Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var upgrade = world.CreateCard(AuthoredCards.BiomechanicalUpgrades, world.AreaOf(DeckType.UpgradesArea, minion.Area.PlayArea, minion.ObjectId));
        var events = new List<GameEvent>();
        DamagePlacement.Deal(world, Cards, minion, minion, 3, "test", "test", events);
        var asked = Sequence.Work(world, Cards, world.Abilities, events);
        Assert.NotNull(asked);
        Assert.Equal(Question.Order, asked.Asking);
        Assert.Equal(world.FirstPlayer, asked.Player);
        Sequence.Answer(world, Cards, world.Abilities, asked, Decision.Take(upgrade.ObjectId), events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(0, minion.Damage);
        Assert.Equal(DeckType.UpgradesArea, upgrade.Area.Type);
    }

    [Rule("rr:triggering-condition.1")]
    [Fact]
    public void OneCardTriggersOnceWhenOneOccurrenceWouldDefeatTwoCharacters()
    {
        var world = Empty("""
            {"cards":[{"card":"01140","abilities":[{
              "trigger":{"event":"WhenCardWouldBeDefeated","timing":"ForcedInterrupt",
                         "subject":"game"},
              "effect":{"placeAccelerationToken":1}
            }]}]}
            """);
        var scheme = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        world.CreateCard("01140", world.AreaOf(DeckType.EnvironmentArea));
        var first = world.CreateCard(Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var second = world.CreateCard(Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Agendas.Happening(world);
        DamagePlacement.Deal(world, Cards, first, first, 3, "test", "test", []);
        DamagePlacement.Deal(world, Cards, second, second, 3, "test", "test", []);
        Assert.Equal(1, scheme.Tokens.GetValueOrDefault(EncounterDeck.AccelerationToken));
    }
}
