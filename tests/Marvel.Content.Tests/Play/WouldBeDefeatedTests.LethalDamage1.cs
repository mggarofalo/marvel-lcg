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
public sealed class WouldBeDefeatedLethalDamageTests : WouldBeDefeatedTestBase
{
    [Rule("rr:damage.step.6")]
    [Rule("rr:would.1")]
    [Fact]
    public void LethalDamageIsHealedAndTheAttachmentIsDiscardedInstead()
    {
        var(world, minion, upgrade) = Board();
        bool defeated = DamagePlacement.Deal(world, Cards, minion, minion, 3, "test", "test", []);
        Assert.False(defeated);
        Assert.Equal(0, minion.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, upgrade.Area.Type);
    }

    [Rule("rr:damage.step.6")]
    [Rule("rr:would.1")]
    [Fact]
    public void LethalPreviewIncludesTheForcedDefeatReplacement()
    {
        var(world, minion, upgrade) = Board();
        string preview = Damage.PreviewAttack(world, Cards, minion, minion, minion, 3);
        Assert.Equal("3/3 → 3/3 HP · Biomechanical Upgrades heals all damage instead " + "and will be discarded", preview);
        Assert.Equal(0, minion.Damage);
        Assert.Equal(DeckType.UpgradesArea, upgrade.Area.Type);
    }

    [Rule("rr:hit-points.3.1")]
    [Rule("rr:would.1")]
    [Fact]
    public void HealthGrantEndingStillOpensTheWouldBeDefeatedInterrupt()
    {
        // Biomechanical Upgrades is not limited to damage-caused defeat. When
        // +3 HP ends, its forced interrupt heals the now-lethal minion and
        // discards itself, invalidating the imminent defeat.
        var(world, minion, upgrade) = Board();
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, "health", Amount: 3, Affects: minion.ObjectId, Lasts: Duration.UntilEndOf(TimingPoints.EndOfRound)));
        minion.TakeDamage(3);
        Agendas.Happening(world);
        world.Effects.Expire(TimingPoints.EndOfRound, []);
        Assert.Equal(0, minion.Damage);
        Assert.Equal(DeckType.EngagedEnemiesArea, minion.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, upgrade.Area.Type);
    }

    [Rule("rr:hit-points.3.1")]
    [Rule("rr:damage.step.7")]
    [Rule("rr:interrupt.1")]
    [Fact]
    public void AStepSevenChoiceCanOpenAHealthLossDefeatInterrupt()
    {
        // Spider-Tracer resolves in the defeated minion's procedure-local
        // occurrence. Its chosen threat removal turns off Infinite Soldier's
        // +3 HP, but the resulting optional defeat interrupt belongs ahead of
        // the containing agenda frame. Both identities must survive suspension.
        var world = Empty("""
            {"cards":[
              {"card":"01007","abilities":[{
                "trigger":{"event":"WhenCardDefeated","timing":"ForcedInterrupt",
                           "subject":"attachedTo"},
                "effect":{"seq":[
                  {"chooseCard":{
                    "from":{"query":"schemes"},
                    "effect":{"removeThreat":{"scheme":"chosen","amount":3}}
                  }},
                  {"placeThreat":{"scheme":{"titled":"Gene Pool"},"amount":1}}
                ]}
              }]},
              {"card":"45069","abilities":[{
                "trigger":{"timing":"Constant","subject":"this"},
                "effect":{"if":{
                  "test":{"atLeast":{
                    "value":{"tokensOn":{"titled":"Gene Pool"}},"count":9
                  }},
                  "then":{"grant":{
                    "card":"this","keyword":"health","amount":3
                  }}
                }}
              }]},
              {"card":"01098","abilities":[{
                "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt",
                           "subject":"attachedTo"},
                "effect":{"heal":{
                  "card":"attachedTo","amount":{"damageOn":"attachedTo"}
                }}
              }]},
              {"card":"01101","abilities":[{
                "trigger":{"event":"WhenCardDefeated","timing":"WhenDefeated",
                           "subject":"this"},
                "effect":{"if":{
                    "test":{"atLeast":{
                      "value":{"tokensOn":{"titled":"Gene Pool"}},"count":7
                    }},
                    "then":{"placeThreat":{
                      "scheme":{"titled":"Gene Pool"},"amount":10
                    }},
                    "else":{"placeThreat":{
                      "scheme":{"titled":"Gene Pool"},"amount":20
                    }}
                  }}
              }]}
            ]}
            """);
        var pool = world.CreateCard("45071", world.AreaOf(DeckType.SideSchemesArea));
        pool.PlaceTokens("k_threat", 9);
        var soldier = world.CreateCard("45069", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        soldier.TakeDamage(3);
        world.CreateCard("01098", world.AreaOf(DeckType.UpgradesArea, soldier.Area.PlayArea, soldier.ObjectId));
        var defeated = world.CreateCard(Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var tracer = world.CreateCard("01007", world.AreaOf(DeckType.UpgradesArea, defeated.Area.PlayArea, defeated.ObjectId, cardOwner: 0));
        Agendas.Happening(world);
        Assert.Equal(6, DamagePlacement.Health(world, Cards, soldier));
        var events = new List<GameEvent>();
        DamagePlacement.Deal(world, Cards, defeated, defeated, 3, "test", "test", events);
        var order = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(Question.Order, order.Asking);
        Sequence.Answer(world, Cards, world.Abilities, order, Decision.Take(tracer.ObjectId), events);
        var chooseScheme = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Contains(chooseScheme.Affordances, option => option.Id == pool.ObjectId);
        Sequence.Answer(world, Cards, world.Abilities, chooseScheme, Decision.Take(pool.ObjectId), events);
        var saveSoldier = Assert.IsType<Prompt>(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(Question.Opportunity, saveSoldier.Asking);
        Assert.Equal(6, pool.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(DeckType.EngagedEnemiesArea, soldier.Area.Type);
        Sequence.Answer(world, Cards, world.Abilities, saveSoldier, Decision.Decline, events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.NotEqual(DeckType.EngagedEnemiesArea, soldier.Area.Type);
        Assert.Equal(17, pool.Tokens.GetValueOrDefault("k_threat"));
    }

    [Rule("rr:damage.step.6")]
    [Fact]
    public void NonlethalDamageDoesNotTriggerTheInterrupt()
    {
        var(world, minion, upgrade) = Board();
        bool defeated = DamagePlacement.Deal(world, Cards, minion, minion, 2, "test", "test", []);
        Assert.False(defeated);
        Assert.Equal(2, minion.Damage);
        Assert.Equal(DeckType.UpgradesArea, upgrade.Area.Type);
    }

    [Rule("rr:damage.step.2")]
    [Rule("rr:damage.step.6")]
    [Fact]
    public void ToughPreventsDamageBeforeDefeatBecomesImminent()
    {
        var(world, minion, upgrade) = Board();
        Statuses.Give(world, minion, Statuses.Tough);
        DamagePlacement.Deal(world, Cards, minion, minion, 3, "test", "test", []);
        Assert.Equal(0, minion.Damage);
        Assert.False(Statuses.Has(world, minion, Statuses.Tough));
        Assert.Equal(DeckType.UpgradesArea, upgrade.Area.Type);
    }

    [Rule("rr:damage.step.6")]
    [Fact]
    public void TheInterruptOnlyAnswersForItsHost()
    {
        var(world, _, upgrade) = Board();
        var other = world.CreateCard(Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        Agendas.Happening(world);
        bool defeated = DamagePlacement.Deal(world, Cards, other, other, 3, "test", "test", []);
        Assert.True(defeated);
        Assert.Equal(DeckType.EncounterDiscardPile, other.Area.Type);
        Assert.Equal(DeckType.UpgradesArea, upgrade.Area.Type);
    }

    [Rule("rr:attach-to")]
    [Fact]
    public void ItAttachesToTheHighestPrintedHitPointsWithoutAnotherCopy()
    {
        var world = Empty();
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var lower = world.CreateCard(Sandman, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var higher = world.CreateCard(Modok, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: "health", Amount: 10, Card: lower.ObjectId, Affects: lower.ObjectId));
        var first = world.CreateCard(AuthoredCards.BiomechanicalUpgrades, world.AreaOf(DeckType.RevealingArea));
        Assert.Equal(higher.ObjectId, world.Abilities.AttachesTo(world, first));
        World.MoveToTop(first, world.AreaOf(DeckType.UpgradesArea, higher.Area.PlayArea, higher.ObjectId));
        var second = world.CreateCard(AuthoredCards.BiomechanicalUpgrades, world.AreaOf(DeckType.RevealingArea));
        Assert.Equal(lower.ObjectId, world.Abilities.AttachesTo(world, second));
    }

    [Rule("rr:first-player.1")]
    [Fact]
    public void TheFirstPlayerBreaksAHighestHitPointAttachmentTie()
    {
        var world = Empty();
        world.CreateCard(Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        world.CreateCard(Mercenary, world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var minions = world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)).Cards.ToList();
        var upgrade = world.CreateCard(AuthoredCards.BiomechanicalUpgrades, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)));
        world.Agenda.Add(new PhaseStep(Steps.RevealEncounterCard, 1, 4, Subject: upgrade.ObjectId, Seat: 0));
        var events = new List<GameEvent>();
        var asked = Sequence.Work(world, Cards, world.Abilities, events);
        Assert.NotNull(asked);
        Assert.Equal(world.FirstPlayer, asked.Player);
        Assert.Equal(Question.Element, asked.Asking);
        Assert.Equal(minions.Select(minion => minion.ObjectId), asked.Affordances.Select(option => option.Id));
        Sequence.Answer(world, Cards, world.Abilities, asked, Decision.Take(minions[1].ObjectId), events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(DeckType.UpgradesArea, upgrade.Area.Type);
        Assert.Equal(minions[1].ObjectId, upgrade.Area.Host);
    }

    [Rule("rr:attach-to.3")]
    [Fact]
    public void WithNoMinionThereIsNothingToAttachTo()
    {
        var world = Empty();
        world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var upgrade = world.CreateCard(AuthoredCards.BiomechanicalUpgrades, world.AreaOf(DeckType.RevealingArea));
        Assert.Null(world.Abilities.AttachesTo(world, upgrade));
    }

    [Rule("rr:damage.step.6")]
    [Fact]
    public void AnOptionalInterruptCanBeAcceptedWithoutReplayingDamage()
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
        bool defeated = DamagePlacement.Deal(world, Cards, minion, minion, 3, "test", "test", events);
        var asked = Sequence.Work(world, Cards, world.Abilities, events);
        Assert.False(defeated);
        Assert.NotNull(asked);
        Assert.True(asked.Cancellable);
        Assert.Equal(Question.Opportunity, asked.Asking);
        Sequence.Answer(world, Cards, world.Abilities, asked, Decision.Take(asked.Affordances[0].Id), events);
        Assert.Null(Sequence.Work(world, Cards, world.Abilities, events));
        Assert.Equal(0, minion.Damage);
        Assert.Equal(1, events.Count(happened => happened is FieldSet set && set.Card == minion.ObjectId && set.Field == "health" && set.To == 0));
    }
}
