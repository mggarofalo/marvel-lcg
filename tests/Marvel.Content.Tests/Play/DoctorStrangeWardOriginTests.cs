using System.Text.Json.Nodes;
using Marvel.Cards.Dsl;
using Marvel.Content.Tests.Cards;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

// Synthetic observer/action pair isolates the origin requirement before admission.
public sealed class DoctorStrangeWardOriginTests
{
    [Theory]
    [InlineData("encounterDeck", true)]
    [InlineData("encounterDiscardPile", false)]
    [InlineData("encounterDeck", true, true)]
    [InlineData("encounterDiscardPile", true, true)]
    [Rule("rr:cancel.4")]
    public void FullRevealOffersWardOnlyForEncounterDeckOrigin(string origin, bool eligible, bool dealt = false)
    {
        // A canceled treachery is "still regarded as revealed" and discarded.
        // Protective Ward's generated text additionally requires encounter-deck origin.
        var world = ThorActualFixture.Board("06001a", 2);
        var runner = DoctorStrangeAbilityFixture.WithReplacements(JsonNode.Parse("""
            [
             {"card":"01092","abilities":[{
              "trigger":{"event":"WhenActionTriggered","timing":"Action","subject":"this"},
              "effect":{"reveal":{"cardsIn":{"area":"ORIGIN","title":"Shadow of the Past"}}}}]},
             {"card":"09007","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"Interrupt","subject":"game","form":"hero"},
              "when":{"and":[{"isKind":{"card":"trigger.subject","kind":"treachery"}},{"revealedFromEncounterDeck":"trigger"}]},
              "effect":{"seq":[{"cancelOccurrence":"trigger"},{"discard":"trigger.subject"}]}}]},
             {"card":"01005","abilities":[{
              "trigger":{"event":"WhenCardRevealed","timing":"Interrupt","subject":"game","form":"hero"},
              "when":{"isKind":{"card":"trigger.subject","kind":"treachery"}},
              "effect":{"dealDamage":{"cards":"you","amount":1}}}]}
            ]
            """.Replace("ORIGIN", origin, StringComparison.Ordinal))!.AsArray());
        world.Abilities = runner;
        var area = world.AreaOf(origin switch
        {
            "encounterDeck" => DeckType.EncounterDeck,
            "encounterDiscardPile" => DeckType.EncounterDiscardPile,
            _ => throw new ArgumentException(origin),
        });
        var treachery = world.CreateCard("01190", area);
        Assert.Equal("Shadow of the Past", world.Facts.Title(treachery.FaceId));
        var source = world.CreateCard("01092", world.AreaOf(DeckType.SupportsArea, PlayArea.Of(0), cardOwner: 0));
        var ward = world.CreateCard("09007", world.Seats[1].Hand);
        world.CreateCard("01005", world.Seats[1].Hand);
        var payment = world.CreateCard("01088", world.Seats[1].Hand);
        var action = Assert.Single(runner.Actions(world, 0), a => a.Card == source.ObjectId);
        var events = new List<GameEvent>();
        if (dealt)
        {
            Deal.EncounterCard(world, treachery, 0, "synthetic identified deal", events);
            world.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCards), 1, 4, Plan: true));
        }
        else events.AddRange(runner.ExecuteAction(world, new PlayerAction(action, [], [])).Events);
        var reveal = Sequence.Work(world, world.Facts, runner, events)!;
        Assert.Equal(treachery.ObjectId, world.Agenda.Occurrence!.Subject);
        Assert.Equal(DeckType.RevealingArea, treachery.Area.Type);
        Assert.Equal(1, reveal.Player);
        Assert.Equal(eligible, reveal.Affordances.Any(a => a.AnchorId == ward.ObjectId));
        var receipt = Assert.IsType<Marvel.Rules.Timing.EncounterRevealOrigin>(world.Agenda.Occurrence.RevealOrigin);
        Assert.Equal(treachery.ObjectId, receipt.Card);
        Assert.Equal(Places.Reference(dealt
            ? world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)) : area), receipt.From);
        Assert.Equal(dealt, receipt.Dealt);
        if (!eligible)
        {
            Assert.Equal(DeckType.HandsArea, ward.Area.Type);
            Assert.Equal(DeckType.HandsArea, payment.Area.Type);
            return;
        }
        var offer = Assert.Single(reveal.Affordances, a => a.AnchorId == ward.ObjectId);
        Sequence.Answer(world, world.Facts, runner, reveal,
            Decision.Take(offer.Id, [], [payment.ObjectId]), events);
        Sequence.Finish(world, world.Facts, runner, events);
        Assert.Equal(DeckType.EncounterDiscardPile, treachery.Area.Type);
        Assert.Equal(DeckType.DiscardPile, ward.Area.Type);
        Assert.Equal(DeckType.DiscardPile, payment.Area.Type);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)).Cards);
        Assert.Empty(world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.False(world.Agenda.IsBusy);
        Assert.False(world.IsOver);
    }
    [Theory]
    [InlineData("you")]
    [InlineData("encounterDeck")]
    [InlineData("true")]
    public void OriginConditionRejectsOtherArguments(string argument)
    {
        var json = """
            {"cards":[{"card":"test","abilities":[{
            "trigger":{"event":"WhenCardRevealed","timing":"Interrupt","subject":"game"},
            "when":{"revealedFromEncounterDeck":"ARGUMENT"},
            "effect":{"cancelOccurrence":"trigger"}}]}]}
            """.Replace("ARGUMENT", argument, StringComparison.Ordinal);
        Assert.Contains("revealedFromEncounterDeck", Assert.Throws<AbilityException>(
            () => AbilityLowering.Book(AbilityCatalog.Parse(json))).Message);
    }
    [Fact]
    public void ScheduledOriginRoundTripsAndReconstructsTheSameOccurrenceAfterMovement()
    {
        var world = ThorActualFixture.Board("06001a", 2);
        var card = world.CreateCard("01190", world.AreaOf(DeckType.EncounterDiscardPile));
        var origin = new Marvel.Rules.Timing.EncounterRevealOrigin(card.ObjectId, Places.Reference(card.Area));
        var operation = new RevealStageOperation(RevealStageKind.RevealEncounterCard, origin);
        var restored = System.Text.Json.JsonSerializer.Deserialize<RevealStageOperation>(
            System.Text.Json.JsonSerializer.Serialize(operation))!;
        World.MoveToTop(card, world.AreaOf(DeckType.RevealingArea));
        var occurrence = new PhaseStep(restored, 1, 4, Subject: card.ObjectId, Seat: 0)
            .OccurrenceOf(world, world.Facts);
        Assert.Equal(origin, occurrence.RevealOrigin);
        Assert.Equal(card.ObjectId, occurrence.Subject);
        Assert.Equal(0, occurrence.Player);
        Assert.False(occurrence.RevealOrigin!.FromEncounterDeck);
        Assert.DoesNotContain("RevealOrigin", System.Text.Json.JsonSerializer.Serialize(occurrence));
    }

}
