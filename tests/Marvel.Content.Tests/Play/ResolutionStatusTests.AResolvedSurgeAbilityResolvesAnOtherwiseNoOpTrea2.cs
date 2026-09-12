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
public sealed class ResolutionStatusAResolvedSurgeAbilityResolvesAnOtherwiseNoOpTreaTests : ResolutionStatusTestBase
{
    [Rule("rr:resolve.2")]
    [Rule("rr:resolve.4")]
    [Fact]
    public void AResolvedSurgeAbilityResolvesAnOtherwiseNoOpTreachery()
    {
        var world = Deal();
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        world.Seats[0].IdentityCard.Exhaust();
        var card = world.CreateCard(AuthoredCards.Exhaustion, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)));
        card.TurnFaceDown();
        world.Agenda.Add(new PhaseStep(Steps.RevealEncounterCard, 1, 4, Subject: card.ObjectId, Seat: 0));
        var occurrence = world.Agenda.Begin(world, Cards);
        var events = new List<GameEvent>();
        var asked = Sequence.Work(world, Cards, runner, events);
        Assert.NotNull(asked);
        var surge = Assert.Single(asked.Affordances, option => option.Label == "Surge");
        Sequence.Answer(world, Cards, runner, asked, Decision.Take(surge.Id), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(ResolutionStatus.Unresolved, occurrence.StatusOf(new PendingAbility(card.ObjectId, AbilityType.WhenRevealed, 0)));
        Assert.Equal(ResolutionStatus.Resolved, occurrence.CardStatus(card.ObjectId));
    }

    [Rule("rr:cancel.1")]
    [Rule("rr:resolve.7")]
    [Rule("rr:resolve.8")]
    [Fact]
    public void CancelingWhenRevealedCancelsSurgeBeforeItDealsACard()
    {
        var world = Deal();
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var card = world.CreateCard(AuthoredCards.Exhaustion, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)));
        card.TurnFaceDown();
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: "cancelWhenRevealed", Affects: card.ObjectId, Lasts: new Duration(Uses: 1)));
        world.Agenda.Add(new PhaseStep(Steps.RevealEncounterCard, 1, 4, Subject: card.ObjectId, Seat: 0));
        var occurrence = world.Agenda.Begin(world, Cards);
        var events = new List<GameEvent>();
        Sequence.Finish(world, Cards, runner, events);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Equal(ResolutionStatus.Unresolved, occurrence.CardStatus(card.ObjectId));
        Assert.All(occurrence.Resolutions.Where(entry => entry.Card == card.ObjectId), entry => Assert.Equal(ResolutionStatus.Unresolved, entry.Status));
    }

    [Rule("rr:replacement-effect.1")]
    [Rule("rr:resolve.1")]
    [Rule("rr:resolve.2")]
    [Rule("rr:resolve.3")]
    [Fact]
    public void ReplacingThreatResolvesEvenWhenTheReplacementDamageIsPrevented()
    {
        var world = Deal();
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var card = world.CreateCard("01061", world.Seats[0].Hand);
        var hero = world.Seats[0].IdentityCard;
        hero.TurnTo(AuthoredCards.SpiderMan);
        var scheme = world.TheCardIn(DeckType.MainSchemesArea)!;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var occurrence = Occurrence.ForThreat(9, [Steps.ThreatWouldBePlaced], world, Cards, new ThreatPlacement(scheme.ObjectId, villain.ObjectId, 4, ThreatCause.CardAbility, "test", 0));
        var ability = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Interrupt), pending => pending.Card == card.ObjectId);
        world.Effects.Register(new ContinuousEffect(EffectSource.LastingEffect, Kind: "preventDamage", Amount: long.MaxValue, Affects: hero.ObjectId, Lasts: new Duration(Uses: 1)));
        runner.Resolve(world, occurrence, ability, [], []);
        Assert.True(occurrence.Threat!.Replaced);
        Assert.Equal(0, hero.Damage);
        Assert.Equal(ResolutionStatus.Resolved, occurrence.StatusOf(ability));
        Assert.Equal(ResolutionStatus.Resolved, occurrence.CardStatus(card.ObjectId));
    }

    [Rule("rr:shuffle")]
    [Rule("rr:resolve.1")]
    [Rule("rr:resolve.2")]
    [Fact]
    public void ShuffleIntoResolvesWhenItsCardQueryIsEmpty()
    {
        var(world, card, occurrence, runner) = Revealing(AuthoredCards.ShadowOfThePast, """
            { "shuffleInto": {
              "cards": { "query": "yourAsidePile" },
              "deck": "encounterDeck"
            } }
            """);
        foreach (var aside in world.Seats[0].Nemesis.Cards.ToList())
        {
            World.MoveToTop(aside, world.AreaOf(DeckType.EncounterDeck));
        }

        runner.WhenRevealed(world, card, 0, occurrence);
        Assert.Equal(ResolutionStatus.Resolved, occurrence.StatusOf(new PendingAbility(card.ObjectId, AbilityType.WhenRevealed, 0)));
    }

    [Rule("rr:resolve.1")]
    [Rule("rr:resolve.2")]
    [Fact]
    public void ShuffleIntoIsUnresolvedForAnEmptyQueryAndOneCardDeck()
    {
        var(world, card, occurrence, runner) = Revealing(AuthoredCards.ShadowOfThePast, """
            { "shuffleInto": {
              "cards": { "query": "yourAsidePile" },
              "deck": "encounterDeck"
            } }
            """);
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var discard = world.AreaOf(DeckType.EncounterDiscardPile);
        foreach (var aside in world.Seats[0].Nemesis.Cards.ToList())
        {
            World.MoveToTop(aside, discard);
        }

        foreach (var extra in deck.Cards.Skip(1).ToList())
        {
            World.MoveToTop(extra, discard);
        }

        runner.WhenRevealed(world, card, 0, occurrence);
        Assert.Equal(ResolutionStatus.Unresolved, occurrence.StatusOf(new PendingAbility(card.ObjectId, AbilityType.WhenRevealed, 0)));
    }

    [Rule("rr:resolve.1")]
    [Rule("rr:resolve.2")]
    [Rule("rr:search.3")]
    [Fact]
    public void DoomsdayChairShuffleIsUnresolvedForAOneCardDeck()
    {
        var(world, card, occurrence, runner) = Revealing(AuthoredCards.DoomsdayChair, """{ "shuffle": "encounterDeck" }""");
        var deck = world.AreaOf(DeckType.EncounterDeck);
        var discard = world.AreaOf(DeckType.EncounterDiscardPile);
        foreach (var extra in deck.Cards.Skip(1).ToList())
        {
            World.MoveToTop(extra, discard);
        }

        runner.WhenRevealed(world, card, 0, occurrence);
        Assert.Equal(ResolutionStatus.Unresolved, occurrence.StatusOf(new PendingAbility(card.ObjectId, AbilityType.WhenRevealed, 0)));
    }

    [Rule("rr:resolve.2")]
    [Fact]
    public void AChoiceResumesByItsSameTierOrdinalRatherThanItsEventFilterPosition()
    {
        var world = Deal();
        var card = world.CreateCard(AuthoredCards.ImTough, world.AreaOf(DeckType.RevealingArea));
        var occurrence = new Occurrence(8, [Steps.CardRevealed], Subject: card.ObjectId, Player: 0);
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            { "cards": [ { "card": "01105", "abilities": [
              {
                "trigger": { "event": "WhenCardEntersPlay", "timing": "WhenRevealed",
                               "subject": "this" },
                "effect": { "seq": [] }
              },
              {
                "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                               "subject": "this" },
                "effect": { "choose": { "options": [
                  { "giveStatus": {
                    "card": { "query": "villain" }, "status": "tough"
                  } },
                  { "seq": [] }
                ] } }
              }
            ] } ] }
            """));
        world.Abilities = runner;
        var events = runner.WhenRevealed(world, card, 0, occurrence).ToList();
        var question = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, question, Decision.Take(0), events);
        Sequence.Finish(world, Cards, runner, events);
        Assert.Equal(ResolutionStatus.Resolved, occurrence.StatusOf(new PendingAbility(card.ObjectId, AbilityType.WhenRevealed, 0, 1)));
    }

    [Rule("rr:resolve.2")]
    [Rule("rr:resolve.4")]
    [Fact]
    public void RevealStatusRemainsOnTheOccurrenceThroughItsResponseWindow()
    {
        var world = Deal();
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var card = world.CreateCard(AuthoredCards.ImTough, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)));
        card.TurnFaceDown();
        world.Agenda.Add(new PhaseStep(Steps.RevealEncounterCard, 1, 4, Subject: card.ObjectId, Seat: 0));
        var occurrence = world.Agenda.Begin(world, Cards);
        var events = new List<GameEvent>();
        Sequence.Finish(world, Cards, runner, events);
        var ability = new PendingAbility(card.ObjectId, AbilityType.WhenRevealed, 0);
        Assert.Equal(ResolutionStatus.Resolved, occurrence.StatusOf(ability));
        Assert.Equal(ResolutionStatus.Resolved, occurrence.CardStatus(card.ObjectId));
    }

    [Rule("rr:resolve.5")]
    [Rule("rr:resolve.6")]
    [Fact]
    public void ConstantsAndOtherCardTypesNeverBecomeResolvedCards()
    {
        var occurrence = new Occurrence(1, [Steps.TurnAction]);
        occurrence.Begin(new PendingAbility(4, AbilityType.Constant, 0));
        occurrence.Resolve(new PendingAbility(4, AbilityType.Constant, 0));
        Assert.Equal(ResolutionStatus.Unresolved, occurrence.StatusOf(new PendingAbility(4, AbilityType.Constant, 0)));
        Assert.Equal(ResolutionStatus.Unresolved, occurrence.CardStatus(4));
        Assert.Equal(ResolutionStatus.Unresolved, occurrence.CardStatus(5));
    }

    [Rule("rr:resolve.2")]
    [Fact]
    public void ResolutionStateSurvivesSerialization()
    {
        var occurrence = new Occurrence(7, [Steps.CardRevealed]);
        var ability = new PendingAbility(4, AbilityType.WhenRevealed, 0);
        occurrence.BeginCard(4, [ability]);
        occurrence.Resolve(ability);
        occurrence.Complete(ability);
        string json = JsonSerializer.Serialize(occurrence, EventJson.Options);
        var restored = JsonSerializer.Deserialize<Occurrence>(json, EventJson.Options)!;
        Assert.Equal(ResolutionStatus.Resolved, restored.StatusOf(ability));
        Assert.Equal(ResolutionStatus.Resolved, restored.CardStatus(4));
    }
}
