using Marvel.Content.Setup;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DirectDamageEventProvenanceTests
{
    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ResumedLethalDamageOwnsPlacementAndDisposalWithoutBorrowingForcedAncestors(bool simultaneous, bool optional)
    {
        // Synthetic invocations using real printed recipients and prevention. Receipt
        // association is an engine choice; optional children retain their own boundary.
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
        seat.IdentityCard.TakeDamage(8);
        world.CreateCard("01088", seat.Deck);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var source = world.CreateCard("04093", world.AreaOf(DeckType.VillainArea));
        var barrier = world.CreateCard("05017", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        barrier.PlaceTokens("c_reflection", 3);
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var occurrence = new Occurrence(123, [], Player: 0);
        var events = new List<GameEvent>();
        AbilityExecutionResult Deal(AbilityExecution owner)
        {
            DamageRequest Request(Card target) => new(source, target, 3, "synthetic", "Deal_Damage") { RecordDefeatOn = occurrence };
            if (simultaneous) SimultaneousDirectDamage.Resolve(world, facts, [Request(seat.IdentityCard), Request(ally)], events);
            else DamagePlacement.Resolve(world, facts, Request(seat.IdentityCard), events);
            return owner.Finish(events);
        }
        world.Agenda.Executions.Run(occurrence, new(source.ObjectId, AbilityType.ForcedResponse, 0), forced =>
        {
            if (!optional) return Deal(forced);
            world.Agenda.Executions.Run(new Occurrence(124, [], Player: 0), new(barrier.ObjectId, AbilityType.Interrupt, 0), Deal);
            return forced.Finish(events);
        });
        var prompt = Sequence.Work(world, facts, runner, events)!;
        Assert.Contains(prompt.Affordances, action => action.AnchorId == barrier.ObjectId);
        Sequence.Answer(world, facts, runner, prompt, Decision.Decline, events);
        Sequence.Finish(world, facts, runner, events);
        Assert.True(seat.Eliminated);
        Assert.False(world.Agenda.IsBusy);
        Assert.Contains(events.OfType<CardsMoved>(), moved => moved.Cards.Any(card => card.Card == barrier.ObjectId));
        Assert.Contains(events.OfType<CardsMoved>(), moved => moved.Cards.Any(card => card.Card == ally.ObjectId));
        Assert.Contains(events.OfType<FieldSet>(), damage => damage.Card == seat.IdentityCard.ObjectId && damage.Verb == "Deal_Damage");
        AssertOwner(events, optional, source.ObjectId);
    }

    private static void AssertOwner(List<GameEvent> events, bool optional, int source)
    {
        Assert.All(events, happened =>
        {
            Assert.Equal(optional ? (int?)null : source, happened.AbilitySource);
            Assert.Equal(optional ? (int?)null : 123, happened.AbilityOccurrence);
            Assert.Equal(optional ? (AbilityType?)null : AbilityType.ForcedResponse, happened.AbilityTiming);
            Assert.Equal(optional ? (int?)null : 0, happened.AbilityOrdinal);
        });
    }

    [Fact]
    public void HulkResumedAreaDamageDefeatAndSiblingDiscardRetainTheForcedReceipt()
    {
        // Actual Hulk wild branch damages every other character, then discards Hulk.
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 1);
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
        world.CreateCard("01088", seat.Deck);
        world.CreateCard("01011", seat.Deck);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var hulk = world.CreateCard("01050", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        var ally = world.CreateCard("01002", world.AreaOf(DeckType.AlliesArea, PlayArea.Of(0), cardOwner: 0));
        ally.TakeDamage(1);
        var enemy = world.CreateCard("01094", world.AreaOf(DeckType.VillainArea));
        var barrier = world.CreateCard("05017", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        barrier.PlaceTokens("c_reflection", 3);
        var occurrence = Occurrence.ForAttack(72, [Steps.AttackEnds], world, facts,
            hulk.ObjectId, enemy.ObjectId, 0);
        var response = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Response));
        var events = new List<GameEvent>(runner.ResolveWindow(world, occurrence, response, new Decision(response.Card, [], [])).Events);
        for (int remaining = 0; remaining < 10; remaining++)
        {
            var prompt = Sequence.Work(world, facts, runner, events);
            if (prompt is null) break;
            Sequence.Answer(world, facts, runner, prompt, prompt.Cancellable ? Decision.Decline : Decision.Take(Assert.Single(prompt.Affordances).Id), events);
        }
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(1, seat.IdentityCard.Damage);
        Assert.Equal(DeckType.DiscardPile, hulk.Area.Type);
        Assert.Equal(DeckType.DiscardPile, ally.Area.Type);
        var aftermath = events.OfType<CardsMoved>().Where(moved => moved.Cards.Any(card => card.Card == hulk.ObjectId || card.Card == ally.ObjectId)).ToArray();
        Assert.Equal(2, aftermath.Length);
        Assert.All(aftermath, happened =>
        {
            Assert.Equal(hulk.ObjectId, happened.AbilitySource);
            Assert.Equal(72, happened.AbilityOccurrence);
            Assert.Equal(AbilityType.ForcedResponse, happened.AbilityTiming);
            Assert.Equal(response.Ordinal, happened.AbilityOrdinal);
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void TaskmasterPlacementRetainsItsReceiptAfterEnergyBarrierChoice(bool prevent)
    {
        // Controlled board with the actual authored Taskmaster and Energy Barrier books.
        // Event association is an engine choice; the card text determines the damage/prevention.
        var facts = CardCatalog.Parse(File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));
        var world = new World(facts, 1);
        world.CreateCard("rule_a", world.AreaOf(DeckType.AsideDeck));
        var seat = world.CreateSeat("p0");
        seat.IdentityCard = world.CreateCard("01001a,01001b", seat.Hero);
        var runner = AuthoredCards.Runner();
        world.Abilities = runner;
        var taskmaster = world.CreateCard("04093", world.AreaOf(DeckType.VillainArea));
        var barrier = world.CreateCard("05017", world.AreaOf(DeckType.UpgradesArea, PlayArea.Of(0), cardOwner: 0));
        barrier.PlaceTokens("c_reflection", 1);
        world.CreateCard("01088", seat.Deck);
        world.CreateCard("01102", world.AreaOf(DeckType.EncounterDeck));
        world.CreateCard("04064", world.AreaOf(DeckType.EncounterDeck));
        var occurrence = new Occurrence(71, [Steps.FormChanged], Subject: seat.IdentityCard.ObjectId, Player: 0);
        var response = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Response));
        var initial = runner.ResolveWindow(world, occurrence, response, new Decision(response.Card, [], []));
        var events = new List<GameEvent>(initial.Events);
        var prompt = Sequence.Work(world, facts, runner, events)!;
        var offer = Assert.Single(prompt.Affordances, action => action.AnchorId == barrier.ObjectId);
        Sequence.Answer(world, facts, runner, prompt, prevent ? Decision.Take(offer.Id) : Decision.Decline, events);
        if (prevent)
        {
            var reflection = Sequence.Work(world, facts, runner, events)!;
            Sequence.Answer(world, facts, runner, reflection, Decision.Take(Assert.Single(reflection.Affordances).Id), events);
        }
        for (int remaining = 0; remaining < 10; remaining++)
        {
            var pending = Sequence.Work(world, facts, runner, events);
            if (pending is null) break;
            Assert.True(pending.Cancellable);
            Sequence.Answer(world, facts, runner, pending, Decision.Decline, events);
        }
        Assert.False(world.Agenda.IsBusy);
        Assert.Equal(prevent ? 2 : 3, seat.IdentityCard.Damage);
        var damage = Assert.Single(events.OfType<FieldSet>(), happened => happened.Card == seat.IdentityCard.ObjectId
            && happened.Verb == "Deal_Damage");
        Assert.Equal(taskmaster.ObjectId, damage.AbilitySource);
        Assert.Equal(71, damage.AbilityOccurrence);
        Assert.Equal(AbilityType.ForcedResponse, damage.AbilityTiming);
        Assert.Equal(response.Ordinal, damage.AbilityOrdinal);
        if (prevent)
        {
            var reflected = Assert.Single(events.OfType<FieldSet>(), happened => happened.Card == taskmaster.ObjectId
                && happened.Verb == "Deal_Damage");
            Assert.Null(reflected.AbilitySource);
            Assert.Null(reflected.AbilityOccurrence);
            Assert.Equal(1, taskmaster.Damage);
        }
    }
}
