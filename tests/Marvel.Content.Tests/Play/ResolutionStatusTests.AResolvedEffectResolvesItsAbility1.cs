using System.Text.Json;
using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;
public sealed class ResolutionStatusAResolvedEffectResolvesItsAbilityTests : ResolutionStatusTestBase
{
    [Rule("rr:resolve.2")]
    [Rule("rr:resolve.4")]
    [Fact]
    public void AResolvedEffectResolvesItsAbilityAndTreachery()
    {
        // “An ability is resolved when it is triggered and one or more of its
        // effects resolve.” A treachery is resolved by the same test applied
        // to its abilities. Giving Tough changes the villain's game state, so
        // both exact addresses are resolved.
        var(world, card, occurrence, runner) = Revealing(AuthoredCards.ImTough, """{ "giveStatus": { "card": { "query": "villain" }, "status": "stunned" } }""");
        runner.WhenRevealed(world, card, 0, occurrence);
        var ability = new PendingAbility(card.ObjectId, AbilityType.WhenRevealed, 0);
        Assert.Equal(ResolutionStatus.Resolved, occurrence.StatusOf(ability));
        Assert.Equal(ResolutionStatus.Resolved, occurrence.CardStatus(card.ObjectId));
    }

    [Rule("rr:status-cards.1")]
    [Rule("rr:vulnerable.1")]
    [Fact]
    public void InflictingAStatusOnAVulnerableCharacterCreatesItThenDiscardsTheCharacter()
    {
        // `Reveal.Afflict` is the one rules procedure for an inflicted status:
        // it creates the physical status card and then `rr:vulnerable.1`
        // discards the character. Calling `Statuses.Give` directly would make
        // a plausible status board while skipping both observable operations.
        var(world, card, _, runner) = Revealing(AuthoredCards.ImTough, """{ "giveStatus": { "card": { "titled": "A.I.M. Scientist" }, "status": "stunned" } }""");
        var vulnerable = world.CreateCard("50083", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var events = runner.WhenRevealed(world, card, 0);
        Assert.Equal(DeckType.EncounterDiscardPile, vulnerable.Area.Type);
        Assert.Contains(events.OfType<CardsCreated>(), created => created.Cards.Any(status => status.Card == Statuses.Stunned));
    }

    [Rule("rr:resolve.2")]
    [Rule("rr:resolve.4")]
    [Rule("rr:resolve.7")]
    [Rule("rr:resolve.8")]
    [Fact]
    public void NoAppliedEffectLeavesTheAbilityAndTreacheryUnresolved()
    {
        var(world, card, occurrence, runner) = Revealing(AuthoredCards.Exhaustion, """{ "exhaust": "you" }""");
        world.Seats[0].IdentityCard.Exhaust();
        runner.WhenRevealed(world, card, 0, occurrence);
        var ability = new PendingAbility(card.ObjectId, AbilityType.WhenRevealed, 0);
        Assert.Equal(ResolutionStatus.Unresolved, occurrence.StatusOf(ability));
        Assert.Equal(ResolutionStatus.Unresolved, occurrence.CardStatus(card.ObjectId));
    }

    [Rule("rr:resolve.7")]
    [Rule("rr:resolve.8")]
    [Fact]
    public void CancellationLeavesTheWholeTreacheryUnresolved()
    {
        var(world, card, occurrence, runner) = Revealing(AuthoredCards.ImTough, """{ "giveStatus": { "card": { "query": "villain" }, "status": "tough" } }""");
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: "cancelWhenRevealed", Affects: card.ObjectId, Lasts: new Duration(Uses: 1)));
        runner.WhenRevealed(world, card, 0, occurrence);
        var ability = new PendingAbility(card.ObjectId, AbilityType.WhenRevealed, 0);
        Assert.Equal(ResolutionStatus.Unresolved, occurrence.StatusOf(ability));
        Assert.Equal(ResolutionStatus.Unresolved, occurrence.CardStatus(card.ObjectId));
    }

    [Rule("rr:resolve.2")]
    [Rule("rr:resolve.4")]
    [Fact]
    public void OneResolvedAbilityResolvesACardWhoseOtherAbilityDoesNothing()
    {
        var world = Deal();
        world.Seats[0].IdentityCard.Exhaust();
        var card = world.CreateCard(AuthoredCards.ImTough, world.AreaOf(DeckType.RevealingArea));
        var occurrence = new Occurrence(1, [Steps.CardRevealed], Subject: card.ObjectId, Player: 0);
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            { "cards": [ { "card": "01105", "abilities": [
              {
                "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                               "subject": "this" },
                "effect": { "exhaust": "you" }
              },
              {
                "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                               "subject": "this" },
                "effect": { "giveStatus": {
                  "card": { "query": "villain" }, "status": "tough"
                } }
              }
            ] } ] }
            """));
        runner.WhenRevealed(world, card, 0, occurrence);
        Assert.Equal(ResolutionStatus.Unresolved, occurrence.StatusOf(new PendingAbility(card.ObjectId, AbilityType.WhenRevealed, 0, 0)));
        Assert.Equal(ResolutionStatus.Resolved, occurrence.StatusOf(new PendingAbility(card.ObjectId, AbilityType.WhenRevealed, 0, 1)));
        Assert.Equal(ResolutionStatus.Resolved, occurrence.CardStatus(card.ObjectId));
    }

    [Rule("rr:resolve.2")]
    [Rule("rr:resolve.4")]
    [Fact]
    public void ResolutionRemainsPendingAcrossAChoiceAndCompletesAfterTheAnswer()
    {
        var(world, card, occurrence, runner) = Revealing(AuthoredCards.ImTough, """
            { "seq": [
              { "draw": { "player": "you", "count": 1 } },
              { "choose": { "options": [
                { "draw": { "player": "you", "count": 1 } },
                { "seq": [] }
              ] } }
            ] }
            """);
        world.Abilities = runner;
        var events = runner.WhenRevealed(world, card, 0, occurrence).ToList();
        var ability = new PendingAbility(card.ObjectId, AbilityType.WhenRevealed, 0);
        Assert.Equal(ResolutionStatus.Pending, occurrence.StatusOf(ability));
        Assert.Equal(ResolutionStatus.Pending, occurrence.CardStatus(card.ObjectId));
        var question = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, question, Decision.Take(0), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(ResolutionStatus.Resolved, occurrence.StatusOf(ability));
        Assert.Equal(ResolutionStatus.Resolved, occurrence.CardStatus(card.ObjectId));
    }

    [Rule("rr:resolve.2")]
    [Rule("rr:resolve.3")]
    [Fact]
    public void AnEventCardResolvesWithItsAppliedAbility()
    {
        var world = Deal();
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var card = world.CreateCard(AuthoredCards.Backflip, world.Seats[0].Hand);
        var hero = world.Seats[0].IdentityCard;
        var occurrence = Occurrence.ForAttack(2, [Steps.DamageWouldBeDealt], world, Cards, world.TheCardIn(DeckType.VillainArea)!.ObjectId, hero.ObjectId, player: 0);
        var ability = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt), pending => pending.Card == card.ObjectId);
        runner.Resolve(world, occurrence, ability, [], []);
        Assert.Equal(ResolutionStatus.Resolved, occurrence.StatusOf(ability));
        Assert.Equal(ResolutionStatus.Resolved, occurrence.CardStatus(card.ObjectId));
    }

    [Rule("rr:resolve.2")]
    [Rule("rr:resolve.3")]
    [Rule("rr:resolve.7")]
    [Rule("rr:resolve.8")]
    [Fact]
    public void AStatusCanceledEventAttackDoesNotResolveFromRemovingTheStatus()
    {
        var world = Deal();
        var hero = world.Seats[0].IdentityCard;
        Statuses.Give(world, hero, Statuses.Stunned);
        var card = world.CreateCard(AuthoredCards.Backflip, world.Seats[0].Hand);
        var occurrence = new Occurrence(3, [Steps.TurnAction], Subject: card.ObjectId, Player: 0);
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            { "cards": [ { "card": "01003", "abilities": [ {
              "trigger": { "event": "WhenActionTriggered", "timing": "Action",
                             "subject": "game" },
              "effect": { "attack": {
                "target": { "query": "villain" },
                "effect": { "dealAttackDamage": {
                  "cards": { "query": "villain" }, "amount": 1
                } }
              } }
            } ] } ] }
            """));
        world.Abilities = runner;
        var ability = new PendingAbility(card.ObjectId, AbilityType.Action, 0);
        runner.Act(world, ability, [], [], occurrence);
        Assert.False(Statuses.Has(world, hero, Statuses.Stunned));
        Assert.Equal(ResolutionStatus.Unresolved, occurrence.StatusOf(ability));
        Assert.Equal(ResolutionStatus.Unresolved, occurrence.CardStatus(card.ObjectId));
    }

    [Rule("rr:resolve.2")]
    [Rule("rr:resolve.4")]
    [Theory]
    [InlineData(true, ResolutionStatus.Resolved)]
    [InlineData(false, ResolutionStatus.Unresolved)]
    public void ActivationResolutionWaitsForWhetherTheActivationWasMade(bool made, ResolutionStatus expected)
    {
        var(world, card, occurrence, runner) = Revealing(AuthoredCards.Assault, """{ "enemyAttacks": { "enemies": { "query": "villain" } } }""");
        runner.WhenRevealed(world, card, 0, occurrence);
        var ability = new PendingAbility(card.ObjectId, AbilityType.WhenRevealed, 0);
        Assert.Equal(ResolutionStatus.Pending, occurrence.StatusOf(ability));
        Assert.Equal(ResolutionStatus.Pending, occurrence.CardStatus(card.ObjectId));
        var attack = Assert.Single(world.Agenda.Outstanding, step => step.What == Steps.Attack);
        runner.ActivationCompleted(world, new EnemyActivation(attack.Subject, attack.Seat, Attacking: true, attack.ActivationId, Made: made));
        Assert.Equal(expected, occurrence.StatusOf(ability));
        Assert.Equal(expected, occurrence.CardStatus(card.ObjectId));
    }

    [Rule("rr:resolve.1")]
    [Rule("rr:resolve.2")]
    [Rule("rr:resolve.4")]
    [Theory]
    [InlineData(1, ResolutionStatus.Resolved)]
    [InlineData(0, ResolutionStatus.Unresolved)]
    public void ScheduledThreatResolvesOnlyWhenItIsApplied(int amount, ResolutionStatus expected)
    {
        var(world, card, occurrence, runner) = Revealing(AuthoredCards.ImTough, $$"""
            { "placeThreat": {
              "scheme": { "query": "mainScheme" }, "amount": {{amount}}
            } }
            """);
        world.Abilities = runner;
        var events = runner.WhenRevealed(world, card, 0, occurrence).ToList();
        var ability = new PendingAbility(card.ObjectId, AbilityType.WhenRevealed, 0);
        Assert.Equal(amount > 0 ? ResolutionStatus.Pending : ResolutionStatus.Unresolved, occurrence.StatusOf(ability));
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(expected, occurrence.StatusOf(ability));
        Assert.Equal(expected, occurrence.CardStatus(card.ObjectId));
    }

    [Rule("rr:resolve.1")]
    [Rule("rr:resolve.2")]
    [Rule("rr:resolve.4")]
    [Fact]
    public void SchedulingARevealedCardResolvesUnderFire()
    {
        // “An effect is resolved when it is applied to the game state.” Moving
        // the encounter card into the revealing area and scheduling its reveal
        // applies Under Fire even though that operation emits no stream event.
        var(world, card, occurrence, runner) = Revealing(AuthoredCards.UnderFire, """{ "revealTop": 1 }""");
        var top = world.AreaOf(DeckType.EncounterDeck).Cards[^1];
        runner.WhenRevealed(world, card, 0, occurrence);
        var ability = new PendingAbility(card.ObjectId, AbilityType.WhenRevealed, 0);
        Assert.Equal(DeckType.RevealingArea, top.Area.Type);
        Assert.Equal(ResolutionStatus.Resolved, occurrence.StatusOf(ability));
        Assert.Equal(ResolutionStatus.Resolved, occurrence.CardStatus(card.ObjectId));
    }

    [Rule("rr:resolve.1")]
    [Rule("rr:resolve.2")]
    [Fact]
    public void SearchingResolvesWithoutDependingOnAnEventStreamEntry()
    {
        var(world, card, occurrence, runner) = Revealing("01114", """
            { "search": {
              "in": [ { "encounterDeck": 1 }, { "encounterDiscardPile": 1 } ],
              "for": "01116a"
            } }
            """);
        runner.WhenRevealed(world, card, 0, occurrence);
        Assert.Equal(ResolutionStatus.Resolved, occurrence.StatusOf(new PendingAbility(card.ObjectId, AbilityType.WhenRevealed, 0)));
        // A villain is one of rr:resolve.6's other card types.
        Assert.Equal(ResolutionStatus.Unresolved, occurrence.CardStatus(card.ObjectId));
    }

    [Rule("rr:resolve.1")]
    [Rule("rr:resolve.2")]
    [Fact]
    public void SearchingIsUnresolvedWhenNothingIsFoundAndNothingCanShuffle()
    {
        var(world, card, occurrence, runner) = Revealing("01114", """
            { "search": {
              "in": [ { "encounterDeck": 1 }, { "encounterDiscardPile": 1 } ],
              "for": "01116a"
            } }
            """);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var aside = world.Seats[0].Nemesis;
        foreach (var extra in deck.Cards.Skip(1).ToList())
        {
            World.MoveToTop(extra, aside);
        }

        runner.WhenRevealed(world, card, 0, occurrence);
        Assert.Equal(ResolutionStatus.Unresolved, occurrence.StatusOf(new PendingAbility(card.ObjectId, AbilityType.WhenRevealed, 0)));
    }
}
