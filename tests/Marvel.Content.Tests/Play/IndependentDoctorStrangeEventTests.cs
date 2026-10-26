using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class IndependentDoctorStrangeEventTests
{
    [Fact]
    [Rule("rr:initiating-abilities.step.2")]
    [Rule("rr:prevent.1.2")]
    public void AlterEgoControllerCanUseWarningToPreventAllDamageToAnotherHero()
    {
        // "If the card or ability has a form requirement" it is checked here.
        // Warning has none. "If an effect prevents all damage ... [it] is not
        // considered to have taken damage."
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = AuthoredCards.Runner();
        world.Seats[1].IdentityCard.TurnTo("01001b");
        var hero = world.Seats[0].IdentityCard;
        var warning = world.CreateCard("09021", world.Seats[1].Hand);
        var events = new List<GameEvent>();
        var receipt = DamagePlacement.Resolve(world, world.Facts,
            new(world.TheCardIn(DeckType.VillainArea)!, hero, 1, "Warning control", "Damage"), events);
        var prompt = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(1, prompt.Player);
        Sequence.Answer(world, world.Facts, runner, prompt,
            Decision.Take(Assert.Single(prompt.Affordances, a => a.AnchorId == warning.ObjectId).Id), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.True(receipt.IsComplete);
        Assert.Equal(1, receipt.Dealt);
        Assert.Equal(0, receipt.Taken);
        Assert.Equal(0, hero.Damage);
        Assert.Equal(DeckType.DiscardPile, warning.Area.Type);
        Assert.False(world.Agenda.IsBusy);
    }

    [Fact]
    [Rule("rr:activation.8")]
    [Rule("rr:scheme-enemy-activation.step.2")]
    public void FoiledCancelsAQueuedSchemeOnlyAfterTheOriginatingAttackCompletes()
    {
        // "The newly initiated activation resolves after the current activation
        // has finished resolving." The synthetic Boost body requests a scheme;
        // ordinary scheduling owns its deferral and completion. Foiled is actual.
        var runner = QueuedSchemeRunner();
        var world = ThorActualFixture.Board("06001a", 2);
        world.Abilities = runner;
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        var foiled = world.CreateCard("09038", world.Seats[1].Hand);
        world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var schemeBoost = world.CreateCard("01102", world.AreaOf(DeckType.EncounterDeck));
        var attackBoost = world.CreateCard("01154", world.AreaOf(DeckType.EncounterDeck));
        var events = new List<GameEvent>();
        world.Agenda.Add(new PhaseStep(new EnemyAttackOperation(), 1, 2, Subject: villain.ObjectId, Seat: 0));
        bool cancelled = false;
        bool sawAttackFaceup = false;
        for (int i = 0; i < 40; i++)
        {
            var prompt = Sequence.Work(world, world.Facts, runner, events);
            if (prompt is null) break;
            var offer = prompt.Affordances.SingleOrDefault(a => a.AnchorId == foiled.ObjectId);
            if (world.Agenda.Occurrence?.Is(BoostFaceupOperation.Faceup) == true)
            {
                if (world.Activation!.Attacking)
                {
                    sawAttackFaceup = true;
                    Assert.Equal(attackBoost.ObjectId, world.Agenda.Occurrence.Subject);
                    Assert.Null(offer);
                }
                else if (offer is not null)
                {
                    Assert.Single(events.OfType<AttackCompleted>());
                    Assert.Null(world.Attack);
                    Assert.Equal(schemeBoost.ObjectId, world.Agenda.Occurrence.Subject);
                    Assert.False(cancelled);
                    cancelled = true;
                }
            }
            Sequence.Answer(world, world.Facts, runner, prompt,
                offer is null ? Decision.Decline : Decision.Take(offer.Id), events);
        }
        Assert.True(sawAttackFaceup);
        Assert.True(cancelled);
        Assert.False(world.IsOver);
        Assert.False(world.Agenda.IsBusy);
        Assert.Null(world.Activation);
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, world.TheCardIn(DeckType.MainSchemesArea)!.Tokens["k_threat"]);
        Assert.Equal(new long[] { 0, 0 }, events.OfType<BoostResolved>().Select(e => e.Icons));
        Assert.Equal(DeckType.DiscardPile, foiled.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, attackBoost.Area.Type);
        Assert.Equal(DeckType.EncounterDiscardPile, schemeBoost.Area.Type);
    }

    private static AbilityRunner QueuedSchemeRunner()
    {
        var json = System.Text.Json.Nodes.JsonNode.Parse(AuthoredCards.Text)!;
        var rows = json["cards"]!.AsArray();
        // These explicitly synthetic bodies isolate a legal deferred-activation
        // lifecycle; they do not assert either replaced card's printed behavior.
        foreach (string face in new[] { "01092", "01154" })
            rows.Remove(rows.Single(row => row!["card"]!.GetValue<string>() == face));
        rows.Add(System.Text.Json.Nodes.JsonNode.Parse("""
            {"card":"01092","abilities":[{"trigger":{"event":"WhenBoostCardTurnedFaceup","timing":"Interrupt","subject":"game"},"effect":{"draw":{"player":"you","count":1}}}]}
            """));
        rows.Add(System.Text.Json.Nodes.JsonNode.Parse("""
            {"card":"01154","abilities":[{"trigger":{"event":"WhenCardRevealed","timing":"Boost","subject":"this"},"effect":{"enemySchemes":{"enemies":{"query":"villain"},"against":"you"}}}]}
            """));
        return new(AbilityCatalog.Parse(json.ToJsonString()));
    }
}
