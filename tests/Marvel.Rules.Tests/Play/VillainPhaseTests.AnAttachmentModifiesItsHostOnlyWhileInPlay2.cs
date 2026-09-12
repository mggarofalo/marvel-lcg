using Marvel.Tests;
using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Rules.Timing;
using Xunit;

namespace Marvel.Rules.Tests.Play;
public sealed class VillainPhaseAnAttachmentModifiesItsHostOnlyWhileInPlayTests : VillainPhaseTestBase
{
    [Rule("rr:modifiers.6.1")]
    [Rule("rr:ability.1")]
    [Fact]
    public void AnAttachmentModifiesItsHostOnlyWhileInPlay()
    {
        // The recorded game cannot tell: its one modifier is an attachment in
        // `UpgradesArea`, which is in play, and its one other hosted card is a
        // Tough with no modifier printed on it. So a resolve that counted
        // modifiers from anywhere produces every recorded digest.
        //
        // A discarded attachment does not modify the card it used to be on.
        var printed = new Printed().With("villain", ("SCH", "1"), ("ATK", "2")).With("scheme", ("EscalationThreat", "0")).With("charge", ("ATK+", "3"));
        var world = Board(printed, players: 1);
        var villain = world.TheCardIn(DeckType.VillainArea)!;
        long Attack() => StateFields.For(villain, printed, 1, inPlay: true, hasHeldPools: true, hasFirstPlayerToken: false, world: world)["attack"];
        Assert.Equal(2, Attack());
        var upgrades = world.AreaOf(DeckType.UpgradesArea, villain.Area.PlayArea, villain.ObjectId);
        var charge = world.CreateCard("charge", upgrades);
        Assert.Equal(5, Attack());
        // Bound to the same host, and out of play. `StatusArea` is the case
        // that exists on a real board: the recorded Tough hangs off Rhino from
        // a zone that is not in play.
        var aside = world.AreaOf(DeckType.StatusArea, villain.Area.PlayArea, villain.ObjectId);
        World.MoveToTop(charge, aside);
        Assert.Equal(2, Attack());
    }

    [Rule("rr:forced.5")]
    [Rule("rr:quickstrike.2")]
    [Rule("rr:teamwork.2")]
    [Fact]
    public void TheFirstPlayerOrdersQuickstrikeAndTeamworkAfterAReveal()
    {
        // Both keywords provide forced responses to the minion entering play
        // and engaging. Neither printed order nor object-id order may decide
        // which initiates first; the first player does.
        var printed = new Printed().With("identity", ("HP", "10")).With("encounter", ("Quickstrike", "1"), ("Teamwork", "ACOLYTE"), ("ATK", "1"), ("HP", "3")).With("friend", ("HP", "3")).WithTrait("encounter", "ACOLYTE").WithTrait("friend", "ACOLYTE");
        printed.Kinds["encounter"] = CardKind.Minion;
        printed.Kinds["friend"] = CardKind.Minion;
        printed.Kinds["identity"] = CardKind.Hero;
        var world = Board(printed, players: 1);
        world.CreateCard("friend", world.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(0)));
        var encounter = world.AreaOf(DeckType.EncounterDeck).Cards.Single(card => card.FaceId == "encounter");
        World.MoveToTop(encounter, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)));
        world.Agenda.Add(new PhaseStep(Steps.RevealEncounterCard, 1, 4, Subject: encounter.ObjectId, Seat: 0));
        var events = new List<GameEvent>();
        var abilities = new NoCardAbilities();
        var asked = Assert.IsType<Prompt>(Sequence.Work(world, printed, abilities, events));
        Assert.Equal(Question.Order, asked.Asking);
        Assert.Equal(["Quickstrike", "Teamwork"], asked.Affordances.Select(option => option.Label));
        Sequence.Answer(world, printed, abilities, asked, Decision.Take(asked.Affordances[1].Id), events);
        Assert.Equal(2, world.Agenda.Outstanding.Where(step => step.What == Steps.Attack).Select(step => step.ActivationId).Distinct().Count());
    }

    [Rule("rr:forced.5")]
    [Rule("rr:incite-x.1")]
    [Rule("rr:surge.1")]
    [Fact]
    public void TheFirstPlayerOrdersTwoKeywordWhenRevealedAbilities()
    {
        var printed = new Printed().With("encounter", ("Incite", "2"), ("Surge", "1"));
        printed.Kinds["encounter"] = CardKind.Minion;
        var world = Board(printed, players: 1);
        var encounter = world.AreaOf(DeckType.EncounterDeck).Cards.Single(card => card.FaceId == "encounter");
        World.MoveToTop(encounter, world.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(0)));
        world.Agenda.Add(new PhaseStep(Steps.RevealEncounterCard, 1, 4, Subject: encounter.ObjectId, Seat: 0));
        var asked = Sequence.Work(world, printed, new NoCardAbilities(), new List<GameEvent>());
        Assert.NotNull(asked);
        Assert.Equal(Question.Order, asked.Asking);
        Assert.Equal(["Incite", "Surge"], asked.Affordances.Select(option => option.Label));
    }
}
