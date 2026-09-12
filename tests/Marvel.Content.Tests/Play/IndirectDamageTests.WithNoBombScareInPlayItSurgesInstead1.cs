using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;
public sealed class IndirectDamageWithNoBombScareInPlayItSurgesInsteadTests : IndirectDamageTestBase
{
    [Fact]
    public void WithNoBombScareInPlayItSurgesInstead()
    {
        // "If Bomb Scare is not in play, this card gains surge." The scenario
        // deals Bomb Scare into the encounter deck, so a game that has not
        // revealed it is the common case.
        var world = Deal();
        int queued = world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count;
        Reveal(world, AuthoredCards.Explosion);
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(queued + 1, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards.Count);
    }

    [Rule("rr:indirect-damage.1")]
    [Fact]
    public void OneCharacterTakesItAllWithoutBeingAsked()
    {
        // A player with no ally has one character and no division to choose.
        // Being asked a question with one answer is not being given a choice --
        // which is why this asks nothing and simply deals it.
        var world = Deal();
        var scare = BombScare(world, threat: 3);
        Reveal(world, AuthoredCards.Explosion);
        Assert.Equal(3, scare.Tokens.GetValueOrDefault("k_threat"));
        Assert.Equal(3, world.Seats[0].IdentityCard.Damage);
        Assert.Empty(world.Agenda.Outstanding);
    }

    [Rule("rr:indirect-damage.2")]
    [Rule("rr:indirect-damage.3.1")]
    [Rule("rr:you-your.3")]
    [Fact]
    public void WithAnAllyThePlayerIsAskedHowToDivideIt()
    {
        // If an ability deals indirect damage to "you," that player assigns it
        // among characters under their control. "Indirect damage dealt to a group of players can be divided as the
        // group chooses among friendly characters in play." Two characters and
        // three damage is a real division, so this asks.
        var world = Deal();
        BombScare(world, threat: 3);
        var ally = world.CreateCard(Ally, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var identity = world.Seats[0].IdentityCard;
        var card = Reveal(world, AuthoredCards.Explosion);
        var waiting = Assert.Single(world.Agenda.Outstanding);
        Assert.Equal(Steps.ChooseOption, waiting.What);
        var asked = AuthoredCards.Runner().Choosing(world, card, 0, waiting.Index)!;
        Assert.Equal(Question.Element, asked.Asking);
        // One entry per point, so the same character may be named twice.
        var targets = Assert.Single(asked.Affordances).Targets!;
        Assert.Equal(3, targets.Min);
        Assert.Equal(3, targets.Max);
        Assert.Equal([identity.ObjectId, ally.ObjectId], targets.Legal);
        // "A character cannot be assigned more indirect damage than would
        // cause it to be defeated." These exact capacities travel with the
        // choice rather than being reconstructed by a client.
        Assert.Equal(new Dictionary<int, int> { [identity.ObjectId] = 10, [ally.ObjectId] = 3, }, targets.MaximumOccurrences);
        AuthoredCards.Runner().Chose(world, card, 0, waiting.Index, Decision.Take(card.ObjectId, [ally.ObjectId, ally.ObjectId, identity.ObjectId], []));
        Assert.Equal(2, ally.Damage);
        Assert.Equal(1, identity.Damage);
    }

    [Rule("rr:indirect-damage.1")]
    [Fact]
    public void ADiscardedCardBindingShapesAndResolvesTheSuspendedAssignment()
    {
        // The assignment names one character per point. Its amount is known
        // before the question is asked, so the same "discarded this way" card
        // must shape both the prompt and the answer after the ability suspends.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            { "cards": [ { "card": "01111", "abilities": [ {
              "trigger": { "event": "WhenCardRevealed", "timing": "WhenRevealed",
                           "subject": "this" },
              "effect": { "seq": [
                { "discardTop": { "from": "yourDeck", "count": 1 } },
                { "indirectDamage": {
                    "among": { "query": "heroesAndAllies" },
                    "amount": { "printedResourceCountDiscarded": "Y" }
                } }
              ] }
            } ] } ] }
            """));
        var world = Deal();
        world.Abilities = runner;
        var ally = world.CreateCard(Ally, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var energy = world.CreateCard("01002", world.Seats[0].Deck);
        var source = world.CreateCard("01111", world.AreaOf(DeckType.RevealingArea));
        runner.WhenRevealed(world, source, 0);
        var waiting = Assert.Single(world.Agenda.Outstanding);
        var prompt = runner.Choosing(world, source, 0, waiting.Index, waiting.Tier)!;
        var targets = Assert.Single(prompt.Affordances).Targets!;
        Assert.Equal(1, targets.Min);
        Assert.Equal(1, targets.Max);
        Assert.Throws<RulesNotImplementedException>(() => runner.Chose(world, source, 0, waiting.Index, Decision.Take(source.ObjectId, [], []), waiting.Tier));
        runner.Chose(world, source, 0, waiting.Index, Decision.Take(source.ObjectId, [ally.ObjectId], []), waiting.Tier);
        Assert.Equal(1, ally.Damage);
        Assert.Contains(energy, world.AreaOf(DeckType.DiscardPile, PlayArea.Of(0), cardOwner: 0).Cards);
    }

    [Rule("rr:indirect-damage.3.1")]
    [Fact]
    public void NoCharacterIsAssignedMoreThanWouldDefeatIt()
    {
        // "A character cannot be assigned more indirect damage than would cause
        // it to be defeated." Spider-Man has ten hit points and Bomb Scare has
        // more threat than that, so the rest is simply not assigned rather than
        // piling onto him.
        var world = Deal();
        BombScare(world, threat: 40);
        Agendas.Happening(world);
        Reveal(world, AuthoredCards.Explosion);
        var identity = world.Seats[0].IdentityCard;
        Assert.Equal(DamagePlacement.Health(world, Cards, identity), identity.Damage);
    }

    [Rule("rr:indirect-damage.3.1")]
    [Fact]
    public void AnAnswerCannotAssignOneCharacterPastItsRemainingHitPoints()
    {
        // "A character cannot be assigned more indirect damage than would
        // cause it to be defeated." Repeating one legal target id must still
        // respect that character's remaining hit points.
        var world = Deal();
        BombScare(world, threat: 3);
        var ally = world.CreateCard(Ally, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(2);
        var identity = world.Seats[0].IdentityCard;
        var card = Reveal(world, AuthoredCards.Explosion);
        var waiting = Assert.Single(world.Agenda.Outstanding);
        var runner = AuthoredCards.Runner();
        var prompt = runner.Choosing(world, card, 0, waiting.Index)!;
        var targets = Assert.Single(prompt.Affordances).Targets!;
        Assert.Equal(1, targets.MaximumOccurrences![ally.ObjectId]);
        Assert.Equal(10, targets.MaximumOccurrences[identity.ObjectId]);
        Assert.Throws<RulesNotImplementedException>(() => runner.Chose(world, card, 0, waiting.Index, Decision.Take(card.ObjectId, [ally.ObjectId, ally.ObjectId, ally.ObjectId], [])));
        runner.Chose(world, card, 0, waiting.Index, Decision.Take(card.ObjectId, [ally.ObjectId, identity.ObjectId, identity.ObjectId], []));
        Assert.Equal(3, ally.Damage);
        Assert.Equal(2, identity.Damage);
    }

    [Rule("rr:indirect-damage.3.2")]
    [Fact]
    public void ToughDoesNotReduceHowMuchIndirectDamageCanBeAssigned()
    {
        // A tough character can be assigned damage up to its remaining hit
        // points without anticipating the prevention. All three points are
        // assigned to the ally, then its one tough card prevents all of them.
        var world = Deal();
        BombScare(world, threat: 3);
        var ally = world.CreateCard(Ally, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        Statuses.Give(world, ally, Statuses.Tough);
        var card = Reveal(world, AuthoredCards.Explosion);
        var waiting = Assert.Single(world.Agenda.Outstanding);
        AuthoredCards.Runner().Chose(world, card, 0, waiting.Index, Decision.Take(card.ObjectId, [ally.ObjectId, ally.ObjectId, ally.ObjectId], []));
        Assert.Equal(0, ally.Damage);
        Assert.False(Statuses.Has(world, ally, Statuses.Tough));
        Assert.Equal(0, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:indirect-damage.3")]
    [Rule("rr:damage.step.6")]
    [Fact]
    public void IndirectAttackAssignmentWaitsForARecipientsDefeatInterrupt()
    {
        // The ally's optional interrupt suspends damage step 6 after both
        // shares have been placed. The assignment itself must advance exactly
        // once: accepting the heal resumes defeat and then the attack, without
        // replaying the allocation or skipping its live choice.
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01020","abilities":[{
              "trigger":{"event":"WhenCardWouldBeDefeated","timing":"Interrupt",
                         "subject":"this"},
              "effect":{"heal":{"card":"this","amount":{"damageOn":"this"}}}
            }]}]}
            """));
        var world = Deal();
        world.Abilities = runner;
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var ally = world.CreateCard(Ally, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(DamagePlacement.Health(world, Cards, ally) - 1);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var events = new List<Marvel.Rules.Events.GameEvent>();
        world.Agenda.Add(new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0));
        var defend = Sequence.Work(world, Cards, runner, events)!;
        Attack.MakeIndirect(world);
        Sequence.Answer(world, Cards, runner, defend, Decision.Decline, events);
        var assign = Sequence.Work(world, Cards, runner, events)!;
        int amount = Assert.Single(assign.Affordances).Targets!.Min;
        Sequence.Answer(world, Cards, runner, assign, Decision.Take(assign.Affordances[0].Id, [ally.ObjectId, ..Enumerable.Repeat(world.Seats[0].IdentityCard.ObjectId, amount - 1)], []), events);
        var interrupt = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(Question.Opportunity, interrupt.Asking);
        Sequence.Answer(world, Cards, runner, interrupt, Decision.Take(Assert.Single(interrupt.Affordances).Id), events);
        Assert.Null(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(0, ally.Damage);
        Assert.Equal(DeckType.AlliesArea, ally.Area.Type);
        Assert.Equal(amount - 1, world.Seats[0].IdentityCard.Damage);
    }

    [Rule("rr:indirect-damage.3")]
    [Rule("rr:damage.step.6")]
    [Fact]
    public void TwoIndirectRecipientsEachFinishTheirDefeatInterrupt()
    {
        var runner = HealingAllyRunner();
        var world = Deal();
        world.Abilities = runner;
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var first = world.CreateCard(Ally, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var second = world.CreateCard(Ally, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        first.TakeDamage(DamagePlacement.Health(world, Cards, first) - 1);
        second.TakeDamage(DamagePlacement.Health(world, Cards, second) - 1);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var events = new List<Marvel.Rules.Events.GameEvent>();
        world.Agenda.Add(new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0));
        var defend = Sequence.Work(world, Cards, runner, events)!;
        Attack.MakeIndirect(world);
        Sequence.Answer(world, Cards, runner, defend, Decision.Decline, events);
        var assign = Sequence.Work(world, Cards, runner, events)!;
        Sequence.Answer(world, Cards, runner, assign, Decision.Take(Assert.Single(assign.Affordances).Id, [first.ObjectId, second.ObjectId], []), events);
        foreach (var ally in new[]
        {
            first,
            second
        }

        )
        {
            var interrupt = Sequence.Work(world, Cards, runner, events)!;
            Assert.Equal(Question.Opportunity, interrupt.Asking);
            Sequence.Answer(world, Cards, runner, interrupt, Decision.Take(Assert.Single(interrupt.Affordances).Id), events);
        }

        Assert.Null(Sequence.Work(world, Cards, runner, events));
        Assert.Equal(0, first.Damage);
        Assert.Equal(0, second.Damage);
        Assert.Equal(DeckType.AlliesArea, first.Area.Type);
        Assert.Equal(DeckType.AlliesArea, second.Area.Type);
    }

    [Rule("rr:triggering-condition.2")]
    [Rule("rr:indirect-damage.3")]
    [Fact]
    public void IndirectAllyDefeatJoinsTheOuterAttackResponseWindow()
    {
        // `rr:triggering-condition.2` says an attack that both deals damage
        // and defeats a character is handled "with a single interrupt window
        // and a single response window." The assignment plan is internal, so
        // the fixture's forced response belongs to the outer attack-damage
        // occurrence and feeds the side scheme.
        var world = Deal();
        var runner = DeclinedDefeatAndSideSchemeRunner();
        world.Abilities = runner;
        world.Seats[0].IdentityCard.TurnTo(AuthoredCards.SpiderMan);
        var pool = world.CreateCard(AuthoredCards.BombScare, world.AreaOf(DeckType.SideSchemesArea));
        var ally = world.CreateCard(Ally, world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(DamagePlacement.Health(world, Cards, ally) - 1);
        long before = pool.Tokens.GetValueOrDefault("k_threat");
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var events = new List<Marvel.Rules.Events.GameEvent>();
        world.Agenda.Add(new PhaseStep(Steps.Attack, 1, 2, Subject: villain.ObjectId, Seat: 0));
        var defend = Sequence.Work(world, Cards, runner, events)!;
        while (defend.Asking != Question.Defender)
        {
            Sequence.Answer(world, Cards, runner, defend, Decision.Decline, events);
            defend = Sequence.Work(world, Cards, runner, events)!;
        }

        Attack.MakeIndirect(world);
        Sequence.Answer(world, Cards, runner, defend, Decision.Decline, events);
        var assign = Sequence.Work(world, Cards, runner, events)!;
        int amount = Assert.Single(assign.Affordances).Targets!.Min;
        Sequence.Answer(world, Cards, runner, assign, Decision.Take(assign.Affordances[0].Id, [ally.ObjectId, ..Enumerable.Repeat(world.Seats[0].IdentityCard.ObjectId, amount - 1)], []), events);
        var save = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(Question.Opportunity, save.Asking);
        Sequence.Answer(world, Cards, runner, save, Decision.Decline, events);
        var remaining = Sequence.Work(world, Cards, runner, events);
        while (remaining is not null)
        {
            Sequence.Answer(world, Cards, runner, remaining, remaining.Cancellable ? Decision.Decline : Decision.Take(Assert.Single(remaining.Affordances).Id), events);
            remaining = Sequence.Work(world, Cards, runner, events);
        }

        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        Assert.Equal(before + 3, pool.Tokens.GetValueOrDefault("k_threat"));
    }
}
