using Marvel.Cards.Dsl;
using Marvel.Cards.Run;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class DefeatTriggerBindingTests
{
    private static readonly CardCatalog Cards = CardCatalog.Parse(
        File.ReadAllText(RepositoryPaths.Dataset("cards", "cards.json")));

    [Rule("rr:triggering-condition.1")]
    [Rule("rr:triggering-condition.2")]
    [Fact]
    public void EveryClauseMatchesTheSameDefeat()
    {
        // A shared window does not make another player's minion defeat count
        // as this player's defeat merely because this player defeated a scheme.
        var world = Board();
        var room = world.CreateCard("01063", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var minion = world.CreateCard("01101", world.AreaOf(DeckType.EncounterDiscardPile));
        var scheme = world.CreateCard("01109", world.AreaOf(DeckType.EncounterDiscardPile));
        var occurrence = new Occurrence(1, Steps.CardDefeated);
        occurrence.Also(new Defeated(minion.ObjectId, 1, "Attack") { Kind = CardKind.Minion });
        occurrence.Also(new Defeated(scheme.ObjectId, 0, "Thwart") { Kind = CardKind.EncounterSideScheme });
        var runner = AuthoredCards.Runner();

        Assert.DoesNotContain(runner.Waiting(world, occurrence, WindowKind.Response),
            candidate => candidate.Card == room.ObjectId);

        var ownMinion = new Defeated(minion.ObjectId, 0, "Attack") { Incarnation = 2, Kind = CardKind.Minion };
        occurrence.Also(ownMinion);
        var offered = Assert.Single(runner.Waiting(world, occurrence, WindowKind.Response),
            candidate => candidate.Card == room.ObjectId);
        Assert.Same(ownMinion, offered.TriggeringDefeat);
    }

    [Rule("rr:triggering-condition.1")]
    [Rule("rr:triggering-condition.2")]
    [Fact]
    public void EachDefeatHasItsOwnResponseAndBindingSurvivesAChoice()
    {
        // Synthetic no-cost response isolates once-per-trigger bookkeeping.
        // Two defeats share one response window; choosing one retains its exact
        // defeater through suspension and does not spend the other instance.
        var world = Board();
        var source = world.CreateCard("01007", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var first = world.CreateCard("01101", world.AreaOf(DeckType.EncounterDiscardPile));
        var second = world.CreateCard("01102", world.AreaOf(DeckType.EncounterDiscardPile));
        world.Agenda.Add(new PhaseStep(new CompletedCardEffectOperation(), 1, 1));
        var occurrence = world.Agenda.Begin(world, Cards);
        world.Agenda.Advance();
        world.Agenda.Advance();
        occurrence.Also(new Defeated(first.ObjectId, 0, "Attack") { Kind = CardKind.Minion });
        occurrence.Also(new Defeated(second.ObjectId, 1, "Attack") { Kind = CardKind.Minion });
        var runner = new AbilityRunner(AbilityCatalog.Parse("""
            {"cards":[{"card":"01007","abilities":[{
              "trigger":{"event":"WhenCardDefeated","timing":"Response","subject":"game"},
              "when":{"isKind":{"card":"defeated","kind":"minion"}},
              "effect":{"choose":{"options":[
                {"heal":{"card":"defeater","amount":1}},
                {"heal":{"card":"defeater","amount":2}}]}}}]}]}
            """));
        var events = new List<GameEvent>();
        var offered = Sequence.Work(world, Cards, runner, events)!;
        Assert.Equal(2, offered.Affordances.Count);
        Assert.Equal(2, offered.Affordances.Select(option => option.Id).Distinct().Count());
        Assert.All(offered.Affordances, option => Assert.Equal(source.ObjectId, option.AnchorId));

        Resolve(offered.Affordances[1].Id);
        Assert.Equal(2, world.Seats[0].IdentityCard.Damage);
        Assert.Equal(1, world.Seats[1].IdentityCard.Damage);
        Assert.Same(occurrence.Defeats[1], Assert.Single(occurrence.Resolutions).TriggeringDefeat);
        Assert.Single(offered!.Affordances);
        Resolve(offered.Affordances[0].Id);
        Assert.Equal(1, world.Seats[0].IdentityCard.Damage);
        Assert.Null(offered);
        Assert.Equal(2, occurrence.Resolutions.Count);
        Assert.All(occurrence.Resolutions, entry => Assert.Equal(ResolutionStatus.Resolved, entry.Status));

        void Resolve(int handle)
        {
            Sequence.Answer(world, Cards, runner, offered!, Decision.Take(handle), events);
            var choice = Sequence.Work(world, Cards, runner, events)!;
            Assert.Equal(Marvel.Rules.Prompts.Question.Option, choice.Asking);
            Sequence.Answer(world, Cards, runner, choice, Decision.Take(choice.Affordances[0].Id), events);
            offered = Sequence.Work(world, Cards, runner, events);
        }
    }

    private static World Board()
    {
        var world = new World(Cards, players: 2);
        foreach (string face in new[] { "01001a", "01010a" })
        {
            var seat = world.CreateSeat(face);
            seat.IdentityCard = world.CreateCard(face, seat.Hero);
            seat.IdentityCard.TakeDamage(2);
        }
        var scheme = world.CreateCard("01097b", world.AreaOf(DeckType.MainSchemesArea));
        scheme.PlaceTokens("k_threat", 3);
        return world;
    }
}
