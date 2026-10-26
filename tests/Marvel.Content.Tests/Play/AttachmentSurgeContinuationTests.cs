using Marvel.Rules.Events;
using Marvel.Rules.Play;
using Marvel.Rules.Prompts;
using Marvel.Rules.State;
using Marvel.Tests;
using Xunit;

namespace Marvel.Content.Tests.Play;

public sealed class AttachmentSurgeContinuationTests
{
    [Fact]
    [Rule("rr:first-player.1")]
    [Rule("rr:surge.1")]
    public void TargetChoiceCompletesBeforeSurgeInterruptAndCannotBeAnsweredAgain()
    {
        // First player resolves target ties; surge says "Deal yourself 1 facedown encounter card."
        var f = new EspionageSurgeFixture();
        f.World.Agenda.Abandon();
        World.MoveToTop(f.Encounter, f.World.AreaOf(DeckType.EncounterDiscardPile));
        var first = f.World.CreateCard("01167", f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(1)));
        var second = f.World.CreateCard("01167", first.Area);
        var attachment = f.World.CreateCard("01185", f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)));
        f.World.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 4, Subject: attachment.ObjectId, Seat: 1));
        var target = f.Work();
        Assert.Equal(Steps.ChooseAttachmentTarget, target.Trigger);
        Assert.Equal(f.World.FirstPlayer, target.Player);
        Assert.Equal(new[] { first.ObjectId, second.ObjectId }, target.Affordances.Select(offer => offer.Id));
        string unchosen = f.World.Digest().Canonical();
        var waiting = f.World.Agenda.Outstanding.ToArray();
        Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(f.World, f.Cards, f.Runner,
            target, Decision.Take(int.MaxValue), f.Events));
        Assert.Equal(unchosen, f.World.Digest().Canonical());
        Assert.Equal(waiting, f.World.Agenda.Outstanding.ToArray());
        Sequence.Answer(f.World, f.Cards, f.Runner, target, Decision.Take(second.ObjectId), f.Events);

        var surge = f.Work();
        Assert.Equal("WhenSurgeWouldResolve", surge.Trigger);
        Assert.Equal(DeckType.UpgradesArea, attachment.Area.Type);
        Assert.Equal(second.ObjectId, attachment.Area.Host);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        string before = f.World.Digest().Canonical();
        var pending = f.World.Agenda.Outstanding.ToArray();
        Assert.Throws<ArgumentException>(() => Sequence.Answer(f.World, f.Cards, f.Runner,
            target, Decision.Take(first.ObjectId), f.Events));
        Assert.Equal(before, f.World.Digest().Canonical());
        Assert.Equal(pending, f.World.Agenda.Outstanding.ToArray());

        f.Answer(surge, f.Espionage[0].ObjectId);
        f.Finish();
        Assert.Equal(second.ObjectId, attachment.Area.Host);
        Assert.Single(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(1)).Cards);
        Assert.Empty(f.World.Agenda.Outstanding);
    }

    [Theory]
    [InlineData(0, false)]
    [InlineData(0, true)]
    [InlineData(1, false)]
    [InlineData(1, true)]
    [Rule("rr:first-player.1")]
    [Rule("rr:surge.1")]
    public void AttachmentChoiceAndSurgeRetainTheirDistinctPlayersAndResolveOnce(int resolver, bool accept)
    {
        // First player "selects among the eligible options"; surge says "Deal yourself 1 facedown encounter card."
        int controller = 1 - resolver;
        var (f, attachment, host) = AttachmentScenario(resolver, controller);
        var (control, _, controlHost) = AttachmentScenario(resolver, controller);
        var target = f.Work();
        var controlTarget = control.Work();
        Assert.Equal(0, target.Player);
        Assert.Equal(Steps.ChooseAttachmentTarget, target.Trigger);
        Assert.False(target.Cancellable);
        string unchosen = Snapshot(f);
        Assert.Throws<RulesNotImplementedException>(() => Sequence.Answer(f.World, f.Cards, f.Runner,
            target, Decision.Decline, f.Events));
        Assert.Equal(unchosen, Snapshot(f));
        Sequence.Answer(f.World, f.Cards, f.Runner, target, Decision.Take(host.ObjectId), f.Events);
        Sequence.Answer(control.World, control.Cards, control.Runner, controlTarget,
            Decision.Take(controlHost.ObjectId), control.Events);

        var surge = f.Work();
        var controlSurge = control.Work();
        Assert.Equal("WhenSurgeWouldResolve", surge.Trigger);
        Assert.Equal(controller, surge.Player);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(resolver)).Cards);
        string pending = Snapshot(f);
        Assert.Throws<ArgumentException>(() => Sequence.Answer(f.World, f.Cards, f.Runner,
            target, Decision.Take(host.ObjectId), f.Events));
        Assert.Equal(pending, Snapshot(f));
        if (accept)
        {
            f.Answer(surge, f.Espionage[0].ObjectId);
            control.Answer(controlSurge, control.Espionage[0].ObjectId);
        }
        else
        {
            f.Decline(surge);
            control.Decline(controlSurge);
        }
        f.Finish();
        control.Finish();

        Assert.Equal(Snapshot(control), Snapshot(f));
        Assert.Equal(DeckType.UpgradesArea, attachment.Area.Type);
        Assert.Equal(host.ObjectId, attachment.Area.Host);
        var attached = Assert.Single(f.Events.OfType<CardAttached>());
        Assert.Equal((attachment.ObjectId, host.ObjectId), (attached.Card, attached.Host));
        var dealt = Assert.Single(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(resolver)).Cards);
        Assert.False(dealt.FaceUp);
        Assert.Empty(f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(controller)).Cards);
        Assert.Single(f.Events.OfType<CardsMoved>(), moved => moved.Verb == "Deal"
            && moved.Cards.Any(card => card.Card == dealt.ObjectId));
        Assert.Equal(accept ? 2 : 0, f.World.Seats[controller].Hand.Cards.Count);
        Assert.Empty(f.World.Seats[resolver].Hand.Cards);
        Assert.Equal(accept ? DeckType.DiscardPile : DeckType.UpgradesArea, f.Espionage[0].Area.Type);
        Assert.Empty(f.World.Agenda.Outstanding);
        string completed = Snapshot(f);
        Assert.Null(Sequence.Work(f.World, f.Cards, f.Runner, f.Events));
        Assert.Equal(completed, Snapshot(f));
    }

    private static (EspionageSurgeFixture Fixture, Card Attachment, Card Host) AttachmentScenario(
        int resolver, int controller)
    {
        var f = new EspionageSurgeFixture(resolver, controller);
        f.World.Agenda.Abandon();
        World.MoveToTop(f.Encounter, f.World.AreaOf(DeckType.EncounterDiscardPile));
        var enemies = f.World.AreaOf(DeckType.EngagedEnemiesArea, PlayArea.Of(resolver));
        f.World.CreateCard("01167", enemies);
        var host = f.World.CreateCard("01167", enemies);
        var attachment = f.World.CreateCard("01185", f.World.AreaOf(DeckType.DealtEncounterCardsDeck, PlayArea.Of(resolver)));
        f.World.Agenda.Add(new PhaseStep(new RevealStageOperation(RevealStageKind.RevealEncounterCard),
            1, 4, Subject: attachment.ObjectId, Seat: resolver));
        return (f, attachment, host);
    }

    private static string Snapshot(EspionageSurgeFixture f) => System.Text.Json.JsonSerializer.Serialize(new
    {
        Digest = f.World.Digest().Canonical(), Random = f.World.Random.Generator.GetState(),
        f.World.Random.Generator.WordsConsumed, f.World.Agenda.Stage, Effects = f.World.Effects.Registered,
        Pending = f.World.Agenda.Outstanding.Select(s => new { s.What, s.Subject, s.Seat, s.Index }),
        f.Events,
    });
}
